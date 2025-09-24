using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using CompanionServer;

namespace Oxide.Plugins
{
    [Info("RaidAlertsRUST+", "https://topplugin.ru/ / https://discord.com/invite/5DPTsRmd3G"", "0.3.8")]
    class RaidAlertsRUST : RustPlugin
    {
        readonly string perm = "RaidAlertsRUST.use";
        [PluginReference] Plugin Friends, IQChat;
      
        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RaidAlertsRUST_Title"] = "Тебя рейдят!",
                ["RaidAlertsRUST_Text"] = "Вас рейдят {0}\nКвадрат {1}\nРазрушено: {2}\nСервер: {3}",
                ["RaidAlertsRUST_RaiderNames"] = "(Игрок: {0})",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RaidAlertsRUST_Title"] = "Тебя рейдят!",
                ["RaidAlertsRUST_Text"] = "Вас рейдят {0}\nКвадрат {1}\nРазрушено: {2}\nСервер: {3}",
                ["RaidAlertsRUST_RaiderNames"] = "(Игрок: {0})",

            }, this, "ru");
        }

        #endregion

        #region Config

        private static Configuration config = new Configuration();
        private class Configuration
        {

            [JsonProperty("Основные настройки плагина")]
            public Settings settings;

            internal class Settings
            {
                [JsonProperty("Отправлять всем друзьям оповещению - true || Только владельцу постройки - false")]
                public bool NotForFriend;
                [JsonProperty("Показывать имя рейдера ?")]
                public bool RaiderName;
                [JsonProperty("Дублировать сообщения в чат приложения ?")]
                public bool MsgSendChat;
                [JsonProperty("Интервал отправки оповещения")]
                public float IntervalN;
                [JsonProperty("Названия сервера")]
                public string ServerName;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {                
                    settings = new Settings
                    {
                       IntervalN = 5f,
                       NotForFriend = true,
                       MsgSendChat = true,
                       RaiderName = true,
                       ServerName = ConVar.Server.hostname
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #153" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            permission.RegisterPermission(perm, this);
        }
                private bool CheckObject(BaseEntity entity) => entity is AutoTurret || entity is BuildingBlock || entity is BuildingPrivlidge || entity is Door || entity is SimpleBuildingBlock;

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!CheckObject(entity))
            {
                return;
            }

            if (!permission.UserHasPermission(entity.OwnerID.ToString(), perm)) return;
            if (TimerMsg.ContainsKey(entity.OwnerID) &&
            Time.realtimeSinceStartup < TimerMsg[entity.OwnerID]) return;
            if (entity.lastAttacker is BasePlayer &&
               (entity as BaseEntity).OwnerID == ((BasePlayer)entity.lastAttacker).userID) return;

            try
            {
                string attacker = "";
                ulong raideruserID = ((BasePlayer)entity.lastAttacker).userID;
                if (config.settings.RaiderName)
                {
                    var raider = BasePlayer.activePlayerList.ToList().Find(x => x.userID == raideruserID);
                    if (raider != null)
                    {
                        attacker = raider.displayName;
                    }
                }
                if (!config.settings.NotForFriend)
                {
                    SendNotificationTo(attacker, entity.OwnerID, entity.transform.position, entity.ShortPrefabName);
                }
                else
                {
                    List<ulong> timate;
                    if (Friends)
                        timate =  GetFriends(entity.OwnerID);
                    else
                        timate = GetTimate(entity.OwnerID);
                    if (raideruserID == entity.OwnerID || timate != null && timate.Contains(raideruserID)) return;
                    if (!TimerMsg.ContainsKey(entity.OwnerID) || Time.realtimeSinceStartup >= TimerMsg[entity.OwnerID])
                    {
                        SendNotificationTo(attacker, entity.OwnerID, entity.transform.position, entity.ShortPrefabName);
                    }

                    foreach (var member in timate)
                    {
                        if (TimerMsg.ContainsKey(member) && Time.realtimeSinceStartup < TimerMsg[member])
                            continue;
                        SendNotificationTo(attacker, member, entity.transform.position, entity.ShortPrefabName);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
        #endregion

        #region Metods
        void SendNotificationTo(string raiderName, ulong userID, Vector3 pos, string ent)
        {
            BasePlayer player = BasePlayer.activePlayerList.ToList().Find(x => x.userID == userID);
            if (!TimerMsg.ContainsKey(userID))
            {
                TimerMsg.Add(userID, 0f);
            }
            if (config.settings.RaiderName)
                raiderName = String.Format(lang.GetMessage("RaidAlertsRUST_RaiderNames", this, userID.ToString()), raiderName);
            else raiderName = "";

            string msg = String.Format(lang.GetMessage("RaidAlertsRUST_Text", this, userID.ToString()), raiderName, PosToMapCoords(pos), ent, config.settings.ServerName);
            NotificationList.SendNotificationTo(player.userID, NotificationChannel.SmartAlarm, lang.GetMessage("RaidAlertsRUST_Title", this, userID.ToString()), msg, Util.GetServerPairingData());
            if (config.settings.MsgSendChat && player.Team != null && !player.IsConnected)
            {
                Util.BroadcastTeamChat(player.Team, player.userID, player.displayName, msg, "1 1 1 1524");
            }
            TimerMsg[userID] = Time.realtimeSinceStartup + config.settings.IntervalN;
        }
        #endregion

        #region Command

        [ChatCommand("RaidTest")]
        void CmdTestRaid (BasePlayer player)
        {
            if(!permission.UserHasPermission(player.userID.ToString(), perm))
            {
                SendChat(player, "У вас нет прав для использования этой команды");
                return;
            }
            SendNotificationTo(player.displayName, player.userID, player.transform.position, "Ниччивооооо");
            SendChat(player, "Тестовое оповещения отправлено");
        }

        #endregion

        #region Help
        string PosToMapCoords(Vector3 pos)
        {
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            pos.z = -pos.z;
            pos += new Vector3(TerrainMeta.Size.x, 0, TerrainMeta.Size.z) * .5f;

            var cubeSize = 146.14f;

            int xCube = (int)(pos.x / cubeSize);
            int zCube = (int)(pos.z / cubeSize);

            int firstLetterIndex = (int)(xCube / alpha.Length) - 1;
            string firstLetter = "";
            if (firstLetterIndex >= 0)
                firstLetter = $"{alpha[firstLetterIndex]}";

            var xStr = $"{firstLetter}{alpha[xCube % 26]}";
            var zStr = $"{zCube}";


            return $"{xStr}{zStr}";
        }

        public void SendChat(BasePlayer player, string Message, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        List<string> Prefab = new List<string>
        {
             "door", "window.bars", "window.glass","floor.frame", "floor.grill", "floor.ladder.hatch", "wall.frame", "shutter", "external"
        };

        private List<ulong> GetFriends(ulong ownerId)
        {
            var friends = Friends?.Call("GetFriends", ownerId);
            if (friends is ulong[]) return (friends as ulong[]).ToList();
            return new List<ulong>();
        }

        private List<ulong> GetTimate(ulong ownerId)
        {
            if (RelationshipManager.Instance.playerToTeam.ContainsKey(ownerId)) return RelationshipManager.Instance.playerToTeam[ownerId].members.ToList();
            else return null;
        }
        private Dictionary<ulong, float> TimerMsg = new Dictionary<ulong, float>();

        public bool IsBlock(BaseCombatEntity entity)
        {
            if (entity is BuildingBlock)
            {
                if (((BuildingBlock)entity).grade == BuildingGrade.Enum.Twigs) return false;
                return true;
            }
            if (entity is Door)
            {
                return true;
            }

            var prefabName = entity.ShortPrefabName;

            foreach (string p in Prefab)
            {
                if (prefabName.Contains(p))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
