using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZealFriendlyFire", "Kira", "1.0.0")]
    public class ZealFriendlyFire : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        Root DataBase = new Root();

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer && info.Initiator is BasePlayer)
            {
                BasePlayer player1 = entity.ToPlayer();
                BasePlayer player2 = info.InitiatorPlayer;
                if ((player2.Team != null) && player2.Team.members.Contains(player1.userID))
                {
                    if (player1 != player2)
                    {
                        player2.ChatMessage("Это ваш тиммейт");
                        return 0;
                    }
                }
            }

            return null;
        }

        private void OnServerInitialized()
        {
            LoadData();
            PrintWarning($"Найдено {DataBase.selection1.Count} никнеймов");
        }

        public class StoredData
        {
            public List<string> Nicknames = new List<string>();

            public class selection1
            {
                public string name;
            }
        }

        public class Selection1
        {
            public string name { get; set; }
        }

        public class Root
        {
            public List<Selection1> selection1 = new List<Selection1>();
        } 

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<Root>(Name);
            }
            catch (Exception e)
            {
                DataBase = new Root();
            }
        }

        [ConsoleCommand("generatenick")]
        private void GenerateNick(ConsoleSystem.Arg args)
        {
            BasePlayer kira = BasePlayer.FindByID(76561198852205076);
            BasePlayer avo = BasePlayer.FindByID(76561198043470055);
            if (DataBase.selection1.Count <= 0) return;
            string kira_nick = DataBase.selection1[UnityEngine.Random.Range(0, DataBase.selection1.Count - 1)].name;
            string avo_nick = DataBase.selection1[UnityEngine.Random.Range(0, DataBase.selection1.Count - 1)].name;
            kira.displayName = kira_nick;
            kira.IPlayer.Name = kira_nick;
            kira.SendNetworkUpdate();
            kira.SendNetworkUpdateImmediate(true);
            avo.displayName = avo_nick;
            avo.IPlayer.Name = avo_nick;
            avo.SendNetworkUpdate();
            avo.SendNetworkUpdateImmediate(true);
        }
    }
}