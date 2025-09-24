using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealClans", "Kira", "1.0.0")]
    public class ZealClans : RustPlugin
    {
        #region [Vars] / [Переменные]

        private StoredData DataBase = new StoredData();
        private static ZealClans _;

        #endregion

        #region [MonoBehaviours] 

        public class Cupboard : MonoBehaviour
        {
            public BuildingPrivlidge cupboard;
            public SphereCollider zone;
            public ulong owner;
            public List<int> flags = new List<int>();

            private void Awake()
            {
                cupboard = GetComponent<BuildingPrivlidge>();
                var o = gameObject;
                o.layer = (int) Layer.Reserved1;
                InitializedZone();
                _.PrintToChat($"Создан компонент");
            }

            private void InitializedZone()
            {
                zone = cupboard.gameObject.AddComponent<SphereCollider>();
                zone.isTrigger = true;
                zone.radius = 10f;
                zone.name = gameObject.name;
            }

            private void OnTriggerEnter(Collider other)
            {
                _.PrintToChat(other.name);
            }

            private void OnCollisionEnter(Collision other)
            {
                _.PrintToChat(other.collider.name);
            }
        }

        public class FlagTribute : MonoBehaviour
        {
            public BaseEntity flag;
            public SphereCollider zone;

            private void Awake()
            {
                flag = GetComponent<BaseEntity>();
                InitializedZone();
                _.PrintToChat($"Создан флаг");
            }

            private void InitializedZone()
            {
                zone = flag.gameObject.AddComponent<SphereCollider>();
                zone.isTrigger = true;
                zone.radius = 10f;
                zone.name = gameObject.name;
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            _ = this;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if ((entity.GetComponent<BaseEntity>() is BuildingPrivlidge))
            {
                var cupboard = (BuildingPrivlidge) entity;
                if (cupboard.skinID == 0) return;
                var component = cupboard.gameObject.AddComponent<Cupboard>();
                component.owner = cupboard.OwnerID;
            }

            if (entity.prefabID == 3188315846)
            {
                var component = entity.gameObject.AddComponent<FlagTribute>();
            }
        }

        private void OnTeamCreated(BasePlayer player)
        {
            NextTick(() => CreateClan($"{Core.Random.Range(0, 1000)}", "0", player.userID, player.Team.members));
        }

        private object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            NextTick(() => UpdateClan(team.teamLeader));
            return null;
        }

        private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            NextTick(() => ChangeName(player));
            return null;
        }

        private object OnTeamDisband(RelationshipManager.PlayerTeam team)
        {
            NextTick(() => RemoveClan(team.teamLeader));
            return null;
        }

        #endregion

        #region [ChatCommands] / [Чат команды]

        [ChatCommand("clan")]
        private void ClanCommand(BasePlayer player, string command, string[] args)
        {
            if (player.Team == null)
            {
                player.ChatMessage("Вы не состоите в клане");
                return;
            }

            if (args.Length < 2) return;
            if (args[0] != "tag") return;
            if (args[1].Length > 6)
            {
                player.ChatMessage($"Слишком длинное название, попробуйте сделать проще)");
                return;
            }

            player.Team.teamName = args[1];
            ServerMgr.Instance.StartCoroutine(UpdateNames(player.Team));
            player.ChatMessage($"Вы изменили тэг клана на {args[1]}");
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private void CreateClan(string name, string tag, ulong owner,
            List<ulong> members)
        {
            if (DataBase.Clans.ContainsKey(owner)) return;
            var team = RelationshipManager.ServerInstance.FindPlayersTeam(owner);
            team.teamName = name;
            DataBase.Clans.Add(owner, new StoredData.Clan
            {
                Name = name,
                CupboardID = owner,
                Tag = tag,
                Owner = owner,
                Members = members
            });
            foreach (var plobj in members) ChangeName(BasePlayer.FindByID(plobj));
            var item = ItemManager.CreateByName("cupboard.tool", 1, owner);
            item.SetFlag(global::Item.Flag.Cooking, true);
            team.GetLeader().GiveItem(item);
            PrintWarning($"{name} {tag} {owner}");
            SaveData();
        }

        private void UpdateClan(ulong owner)
        {
            if (RelationshipManager.ServerInstance.FindPlayersTeam(owner) == null) return;
            var team = RelationshipManager.ServerInstance.FindPlayersTeam(owner);
            DataBase.Clans[owner].Name = team.teamName;
            DataBase.Clans[owner].Members = team.members;
            ServerMgr.Instance.StartCoroutine(UpdateNames(team));
            PrintWarning($"Информация обновлена : {owner}");
        }

        private IEnumerator UpdateNames(RelationshipManager.PlayerTeam team)
        {
            DataBase.Clans[team.teamLeader].Tag = team.teamName;
            foreach (var player in team.members.Select(member => BasePlayer.FindByID(member)))
            {
                ChangeName(player);
                player.SendNetworkUpdate();
            }

            yield return 0;
        }

        private static void ChangeName(BasePlayer player)
        {
            if (player.IsValid() == false)
            {
                return;
            }

            var team = player.Team;
            if (team != null)
            {
                var index = player.displayName.LastIndexOf("]", StringComparison.Ordinal);
                if (index > -1)
                {
                    player.displayName = player.displayName.Substring(index + 1);
                }

                player.displayName = $"[{team.teamName}] " + player.displayName;
            }
            else
            {
                var index = player.displayName.LastIndexOf("]", StringComparison.Ordinal);
                if (index > -1)
                {
                    player.displayName = player.displayName.Substring(index + 1);
                }
            }

            player.SendNetworkUpdate();
        }

        private void RemoveClan(ulong owner)
        {
            if (!DataBase.Clans.ContainsKey(owner)) return;
            foreach (var plobj in DataBase.Clans[owner].Members) ChangeName(BasePlayer.FindByID(plobj));
            DataBase.Clans.Remove(owner);
            PrintWarning($"{owner}");
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public Dictionary<ulong, Clan> Clans = new Dictionary<ulong, Clan>();

            public class Clan
            {
                public string Name;
                public ulong CupboardID;
                public string Tag;
                public ulong Owner;
                public List<ulong> Members;
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [API]

        #endregion
    }
}