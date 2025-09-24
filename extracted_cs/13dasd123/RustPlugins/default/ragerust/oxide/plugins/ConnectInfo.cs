using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("ConnectInfo", "1.0.0 - Fartus | 1.0.1+ - Drop Dead", "1.0.1")]
	[Description("Сообщения в чат об подключении/отключении игрока")]
    class ConnectInfo : CovalencePlugin
    {   
		#region Config
		
		private bool ShowConnect = true;
		private bool ShowDisconnect = true;
		
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
			LoadConfigValues();
        }
		
        private void LoadConfigValues()
        {
			GetConfig("Показывать сообщение об подключение", ref ShowConnect);
			GetConfig("Показывать сообщение об отключение", ref ShowDisconnect);
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
		
		#region Lang

        private string GetLangValue(string key, string userId) => lang.GetMessage(key, this, userId);		
		
        private new void LoadDefaultMessages()
        {	   
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["JoinMessage"] = "<color=#ff7043>{0}</color> присоединился к игре.",
                ["LeaveMessage"] = "<color=#ff7043>{0}</color> вышел с сервера. Причина: <color=#ff7043>({1})</color>"
            }, this);
        }
		
		#endregion		
		
        #region Hooks
		
        private void Loaded()		
        {
            LoadConfigValues();
            LoadDefaultMessages();
        }

        private void Broadcast(string key, params object[] args)
        {
            foreach (var player in players.Connected)
                player.Message(string.Format(GetLangValue(key, player.Id), args));
        }		
	   
	    void OnPlayerConnected(BasePlayer player)
        {
		    if (ShowConnect)
            {
                Broadcast("JoinMessage", player.displayName);				
            }
			return;
        }
	   
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (ShowDisconnect)
			{   
		       Broadcast("LeaveMessage", player.displayName, reason);
			}
			return;
	    }	   

		#endregion
    }
}