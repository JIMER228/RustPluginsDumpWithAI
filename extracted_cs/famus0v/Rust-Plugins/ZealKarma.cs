using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZealKarma", "Kira", "1.0.0")]
    [Description("Karma")]
    public class ZealKarma : RustPlugin
    {
        #region [References] / [Ссылки]

        [PluginReference] private Plugin ZealMStatistics;
        private StoredData DataBase = new StoredData();

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            LoadData();
            foreach (var player in BasePlayer.activePlayerList) CheckInDataBase(player.userID);
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public readonly Dictionary<ulong, PlayerKarma> PlayersKarma = new Dictionary<ulong, PlayerKarma>();

            public class PlayerKarma
            {
                public float Karma;
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private void CheckInDataBase(ulong userID)
        {
            if (!DataBase.PlayersKarma.ContainsKey(userID))
                DataBase.PlayersKarma.Add(userID, new StoredData.PlayerKarma());
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [API]

        [HookMethod("GetKarma")]
        public float GetKarma(ulong userID)
        {
            if (!DataBase.PlayersKarma.ContainsKey(userID)) CheckInDataBase(userID);
            return DataBase.PlayersKarma[userID].Karma;
        }

        [HookMethod("GiveKarma")]
        public void GiveKarma(ulong userID, float count)
        {
            if (!DataBase.PlayersKarma.ContainsKey(userID)) CheckInDataBase(userID);
            DataBase.PlayersKarma[userID].Karma += count;
            SaveData();
        }

        [HookMethod("TakeKarma")]
        public void TakeKarma(ulong userID, float count)
        {
            if (!DataBase.PlayersKarma.ContainsKey(userID)) CheckInDataBase(userID);
            DataBase.PlayersKarma[userID].Karma -= count;
            SaveData();
        }

        #endregion 
    }
}