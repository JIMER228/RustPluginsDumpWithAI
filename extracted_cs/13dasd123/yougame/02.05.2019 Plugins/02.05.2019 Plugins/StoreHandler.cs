using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("StoreHandler", "Hougan", "0.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class StoreHandler : RustPlugin
    {
        #region Variables

        private string APIKey;
        private string ServerID;

        #endregion

        #region Initialization
        
        protected override void LoadDefaultConfig()
        {
            Config["GS. API Ключ"] = APIKey = GetConfig("GS. API Ключ", "");
            Config["GS. ID Сервера"] = ServerID = GetConfig("GS. ID Сервера", "");
            
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            LoadDefaultConfig();
        }

        #endregion
        
        #region Hooks
        
        

        #endregion

        #region Methods

        private void HasRegistered(ulong userId, Action<bool> callback)
        {
            AddMoney(userId, 0.001f, "Проверка авторизации в магазине. Бесплатная копейка :)", callback);
        }

        #endregion

        #region Request

        private void AddMoney(ulong userId, float amount, string mess, Action<bool> callback)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                {"action", "moneys"},
                {"type", "plus"},
                {"steam_id", userId.ToString()},
                {"amount", amount.ToString()},
                {"mess", mess}
            }, callback);
        }
        
        private void ExecuteApiRequest(Dictionary<string, string> args, Action<bool> callback)
        {
            string url = $"http://panel.gamestores.ru/api?shop_id={ServerID}&secret={APIKey}" +
                         $"{string.Join("",args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    PrintError($"Ошибка зачисления, подробнисти в ЛОГ-Файле");
                    LogToFile("DailyBonus", $"Код ошибки: {i}, подробности:\n{s}", this);
                    callback(false);
                }
                else
                {
					if (s.Contains("fail"))
					{
						callback(false);
						return;
					}
                    callback(true);
                }
            }, this);
        }

        #endregion

        #region Utils

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));  
      
        #endregion
    }
}
