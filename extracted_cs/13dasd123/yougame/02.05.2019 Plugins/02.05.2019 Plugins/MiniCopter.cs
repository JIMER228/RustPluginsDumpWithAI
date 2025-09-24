// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MiniCopter", "Fartus", "1.0.1")] 
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Плагин для спавна минивертолетов на карте Barren")] 
    public class MiniCopter : RustPlugin
    {
        #region Classes
        class DataStorage
        {
			public Dictionary<ulong, MCDATA> MiniCopterSpawnData = new Dictionary<ulong, MCDATA>();
            public DataStorage() { }
        }
        class MCDATA
        {
            public int Count;
        }
        DataStorage data;
        private DynamicConfigFile MCData;
		
		Dictionary<ulong, int> cooldowns = new Dictionary<ulong, int>();
		#endregion

		#region Config
        private const string
		    permUse = "minicopter.use",
			permAdmin = "minicopter.admin",
			mcPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
			prefix = "<color=#E43A19>MiniCopter</color>";

		private int MiniCopterCount = 5;
		private int СooldownSeconds = 180;

		protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
            Config.Clear();
        }

        private void LoadConfigValues()
        {
            GetConfig("Количество минивертолетов на игрока", ref MiniCopterCount);
            GetConfig("Откат на использование команды (в секундах)", ref СooldownSeconds);

			SaveConfig();
        }
		#endregion

		#region Oxide Hooks
		void Loaded()
        {
            LoadConfigValues();
			permission.RegisterPermission(permUse, this);
			permission.RegisterPermission(permAdmin, this);
		}

		void OnServerInitialized()
        {
			MCData = Interface.Oxide.DataFileSystem.GetFile("MiniCopter");
            LoadData();
			timer.Every(1f, MiniCopterTimerHandler);
		}
		#endregion

        #region Data Storage
        void SaveData()
        {
            MCData.WriteObject(data);
        }

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("MiniCopter");
            }

            catch
            {
                data = new DataStorage();
            }
        }
		#endregion

		#region Commands
        [ChatCommand("get")]
        private void ChatCmd(BasePlayer player, string cmd, string[] Args)
        {
		    if (cooldowns.ContainsKey(player.userID))
            {
                player.ChatMessage($"<size=16>Вы недавно использовали эту команду!\nОжидайте <color=#BEF781>{cooldowns[player.userID]} сек.</color></size>");
                return;
            }

            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permUse))
            {
                player.ChatMessage($"<size=15>{prefix} У вас нет доступа к этой команде!\nВы можете приобрести доступ на нашем сайте!</size>");
                return;
            }

            if (Args == null || Args.Length == 0)
            {
                player.ChatMessage($"<size=15>{prefix} Используйте:\n<color=#FFA500>/get mc</color> - получить минивертолёт</size>");
                return;
            }

			var entityArg = Args[0];
			
            switch (entityArg)
            {
				    case "mc":
                    if (!data.MiniCopterSpawnData.ContainsKey(player.userID))
					{
						data.MiniCopterSpawnData.Add(player.userID, new MCDATA()
						{
							Count = 1
						});
						entityArg = mcPrefab;
						player.ChatMessage($"<size=15>{prefix} Вы получили минивертолёт.\nВам еще доступно {MiniCopterCount - 1} шт.</size>");
				    }
					else
					{
						if (data.MiniCopterSpawnData[player.userID].Count == MiniCopterCount)
						{
							player.ChatMessage($"<size=15>{prefix} Количество минивертолётов на игрока ограничено.\nВы исчерпали свой лимит!</size>");
						}
						else
						{
							data.MiniCopterSpawnData[player.userID].Count++;
							entityArg = mcPrefab;
							if (data.MiniCopterSpawnData[player.userID].Count == MiniCopterCount)
							{
								player.ChatMessage($"<size=15>{prefix} Вы получили последний минивертолёт.</size>");
							}
							else
							{
								player.ChatMessage($"<size=15>{prefix} Вы получили минивертолёт.\nВам еще доступно {MiniCopterCount - data.MiniCopterSpawnData[player.userID].Count} шт.</size>");
							}
						}
					}
					SaveData();
                    break;

                default:
                    player.ChatMessage($"<size=15>{prefix} Неизвестный тип транспорта!</size>");
                    return;
			}

            var pos = new Vector3(player.transform.position.x + 8, player.transform.position.y + 1,player.transform.position.z + 1); // TODO: Better spawn
            var entity = GameManager.server.CreateEntity(entityArg, pos);
            entity.OwnerID = player.OwnerID;
            entity.Spawn();
            
            cooldowns[player.userID] = СooldownSeconds;
        }
		#endregion

		#region Admin Commands
		[ChatCommand("resetmc")]
		private void cmdResetMC(BasePlayer player, string command, string[] args)
		{
			if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permAdmin))
            {
                player.ChatMessage($"<size=15>{prefix} У вас нет доступа к этой команде!</size>");
                return;
            }
			//Очистка дата-файла и сброс кулдауна
		    if (data.MiniCopterSpawnData.ContainsKey(player.userID))
			{
				data.MiniCopterSpawnData.Clear();  
				SaveData();
			}
			cooldowns.Remove(player.userID);
			//Очистка карты от заспавненых минивертолётов
			var allMiniCopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopterVehicle>();
			int destroyed = 0;

            foreach (var minicopters in allMiniCopters)
            {
				int baseMiniCopter = 0;
				if (baseMiniCopter == 0)
                {
                    minicopters.KillMessage();
                    destroyed++;
                }
			}
			player.ChatMessage($"<size=15>{prefix} Дата файл успешно очищен!\nКарта очищена от заспавненых минивертолётов!\nОткат на использования команды сброшен!</size>");
		}
		#endregion

		#region Console Commands
        [ConsoleCommand("ext.get")]
        private void Console(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendError(arg, "У вас нет доступа к этой команде!");
                return;
            }

            var playerArg = arg.Args[0];

            BasePlayer player = BasePlayer.Find(playerArg);
            if (player == null)
            {
                SendError(arg, $"Не удаётся найти игрока <{playerArg}>");
                return;
            }

            var entityArg = arg.Args[1];

            switch (entityArg)
            {
                case "mc":
                    entityArg = mcPrefab;
                    break;

                default:
                    SendError(arg, "Неизвестный тип транспорта!");
                    return;
            }

		    var pos = new Vector3(player.transform.position.x + 8, player.transform.position.y + 1,player.transform.position.z + 1); // TODO: Better spawn
            var entity = GameManager.server.CreateEntity(entityArg, pos);
            entity.OwnerID = player.OwnerID;
            entity.Spawn();		
		}
       	#endregion

		#region Helpers
        void MiniCopterTimerHandler()
        {
            List<ulong> uids = cooldowns.Keys.ToList();
            for (int i = uids.Count - 1; i >= 0; i--)
            {
                var userid = uids[i];
                var time = cooldowns[userid];
                if (--time < 0)
                    cooldowns.Remove(userid);
                else cooldowns[userid] = time;
            }
        }

        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
		#endregion
    }
}
