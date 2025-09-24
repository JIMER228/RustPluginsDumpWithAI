using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ClanTournament", "Scrooge", "0.0.1")]
    public class ClanTournament : RustPlugin
    {

        [PluginReference] private Plugin Clans;
        public class CupSettings
        {
            public ulong ownerclan;
            public bool isremove;
            public uint build;
        }
        public List<CupSettings> ClanData = new List<CupSettings>();

        [PluginReference] private Plugin RustMap;
        void AddMarker(CupSettings settings, string icon = "https://i.ibb.co/gj54RB5/1.png")
        {
            if (string.IsNullOrEmpty(icon))
            {
                PrintWarning("Иконка нуль! Маркер не добавился!");
                return;
            }
            RemoveMarker(settings);
            string name = Clans?.Call("ClanAlready", settings.ownerclan) as string;
            RustMap?.Call("ApiAddPointUrl", icon, $"priv{settings.build}", BaseNetworkable.serverEntities.Find(settings.build)?.transform.position, name, 0.06f);
        }
        void RemoveMarker(CupSettings settings)
        {
            RustMap?.Call("ApiRemovePointUrl", $"priv{settings.build}");
        }

        void OnServerInitialized()
        {
            try
            {
                ClanData = Interface.GetMod().DataFileSystem.ReadObject<List<CupSettings>>(Name);
            }
            catch
            {
                ClanData = new List<CupSettings>();
            }
            if (ClanData.Count > 0)
            {
                foreach (var cup in ClanData)
                {
                    var entity = BaseNetworkable.serverEntities.Find(cup.build);
                    if (entity == null && cup.isremove == false)
                    {
                        cup.isremove = true;
                    }
                    if (entity != null && cup.isremove == false)
                    {
                        AddMarker(cup);
                    }
                }
            }
        }

        void OnServerSave()
        {
            if (ClanData != null)
                Interface.Oxide.DataFileSystem.WriteObject(Name, ClanData);
        }

        void Unload()
        {
            if (ClanData.Count > 0)
            {
                foreach (var cup in ClanData)
                {
                    if (cup.isremove == false)
                    {
                        RemoveMarker(cup);
                    }
                }
            }
            if (ClanData != null)
                Interface.Oxide.DataFileSystem.WriteObject(Name, ClanData);
        }

        string CanRemove(BasePlayer player, BaseEntity entity)
        {
            if (!(entity as BuildingPrivlidge)) return null;
            var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
            if (find != null && find.isremove == false)
            {
                return "Вы не имеете право удалять данный шкаф!";
            }
            return null;
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (info.InitiatorPlayer == null) return null;
            if (entity as BuildingPrivlidge)
            {
                var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
                if (find != null)
                {
                    var clan = Clans?.CallHook("CheckClans", entity.net.ID, info.InitiatorPlayer.OwnerID);
                    if (clan is bool && (bool)clan)
                    {
                        info.InitiatorPlayer.ChatMessage("Вы не имеете право уничтожать турнирный шкаф своего же клана!");
                        info.damageTypes.ScaleAll(0f);
                        return false;
                    }
                    if (entity.OwnerID == info.InitiatorPlayer.userID)
                    {
                        info.InitiatorPlayer.ChatMessage("Вы не имеете право уничтожать турнирный шкаф своего же клана!");
                        info.damageTypes.ScaleAll(0f);
                        return false;
                    }
                }
            }
            return null;
        }



        bool CheckTournament(ulong owner)
        {
            var find = ClanData.FirstOrDefault(p => p.ownerclan == owner);
            if (find == null)
            {
                return false;
            }
            if (find.isremove)
            {
                return false;
            }
            return true;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity as BuildingPrivlidge && info != null && info.InitiatorPlayer != null)
            {
                var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
                if (find != null)
                {
                    if (find.isremove == true)
                    {
                        info.InitiatorPlayer.ChatMessage("Произошла критическая ошибка! Плагин не выдал очки");
                        return;
                    }
                    Clans?.CallHook("ScoreRemove", find.ownerclan, info.InitiatorPlayer.userID);
                    info.InitiatorPlayer.ChatMessage("<color=orange>Вы уничтожили турнирный шкаф чужого клана!</color>");
                    find.isremove = true;
                    string name = Clans?.Call("ClanAlready", find.ownerclan) as string;
                    string clanAttacker = Clans?.Call("ClanAlready", info.InitiatorPlayer.userID) as string;
                    foreach (var player13 in BasePlayer.activePlayerList)
                    {
                        player13.ChatMessage($"Клан <color=orange>{clanAttacker}</color> взорвал шкаф клана <color=orange>{name}</color>");
                    }
                    RemoveMarker(find);
                    LogToFile("ClanTournament", $"Клан {name} вылетел из турнира в {DateTime.Now.ToString(CultureInfo.InvariantCulture)}", this);
                }
            }
        }
    }

}