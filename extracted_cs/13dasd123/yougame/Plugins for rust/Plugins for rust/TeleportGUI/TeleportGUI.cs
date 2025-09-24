// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*
        This plugin was written solely by PsychoTea.
        Please do not, under any circumstances, redistribute this code.
        If you find any bugs, please report them either directly to me at bensparkes8@gmail.com,
        or on the chaoscode.io page.
        The same applies for feature requests.
        For any enquiries please also email me at bensparkes8@gmail.com
        I do write private plugins.
    */

    [Info("TeleportGUI", "PsychoTea", "1.4.0")]

    class TeleportGUI : RustPlugin
    {
        const string permUse = "teleportgui.use";
        const string permCancel = "teleportgui.tpcancel";
        const string permBack = "teleportgui.tpback";
        const string permHere = "teleportgui.tphere";
        const string permSleepers = "teleportgui.sleepers";
        public static TeleportGUI Instance;
        Dictionary<BasePlayer, bool> guiOpen = new Dictionary<BasePlayer, bool>();
        Dictionary<BasePlayer, Vector3> lastTeleport = new Dictionary<BasePlayer, Vector3>();
        List<GameObject> gameObjects = new List<GameObject>();
        bool debuggingMode = false;

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin ServerRewards;

        #region Classes 

        class GUIManager
        {
            public static Dictionary<BasePlayer, GUIManager> Players = new Dictionary<BasePlayer, GUIManager>();

            public int Page = 1;
            public bool TPHere = false;
            public bool Sleepers = false;

            public static GUIManager Get(BasePlayer player)
            {
                if (Players.ContainsKey(player)) return Players[player];
                Players.Add(player, new GUIManager());
                return Players[player];
            }
        }

        class TeleportRequest : MonoBehaviour
        {
            GameObject GameObject;
            int Time;
            public BasePlayer From;
            public BasePlayer To;

            public static void Create(BasePlayer From, BasePlayer To, int TimeoutTime)
            {
                TeleportRequest tr = new TeleportRequest();
                tr.GameObject = new GameObject();
                tr = tr.GameObject.AddComponent<TeleportRequest>();
                tr.Time = TimeoutTime;
                tr.From = From;
                tr.To = To;
                Instance.gameObjects.Add(tr.GameObject);
            }

            void Start()
            {
                Instance.SendReply(From, Instance.GetMessage("RequestSent").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("RequestRecieved").Replace("{0}", From.displayName));
                PendingRequest pr = To.gameObject.AddComponent<PendingRequest>();
                pr.From = From;
                pr.TeleportRequest = this;
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                if (Time == 0) RequestTimeOut();
                Time--;
            }

            public void RequestAccepted()
            {
                int timeUntilTeleport = Instance.GetLowest(Instance.GetConfig<Dictionary<string, object>>("ДлительностьЗадержкиПередТелепортациейВСекундах"), From, Instance.GetConfig<int>("Стандартная длительность задержки перед телепортацией (в секундах)"));
                Instance.SendReply(From, Instance.GetMessage("RequestToAccepted").Replace("{0}", To.displayName).Replace("{1}", timeUntilTeleport.ToString()));
                Instance.SendReply(To, Instance.GetMessage("RequestFromAccepted").Replace("{0}", From.displayName).Replace("{1}", timeUntilTeleport.ToString()));

                int cooldown = Instance.GetLowest(Instance.GetConfig<Dictionary<string, object>>("ДлительностьПерезарядкиТелепортаВСекундах"), From, Instance.GetConfig<int>("Стандартная длительность перезарядки телепорта (в секундах)"));
                Instance.storedData.Cooldowns.Add(From.userID, cooldown + timeUntilTeleport);

                if (Instance.GetConfig<int>("Стандартные ограничения телепортаций в день") != -1)
                {
                    int usesRemaining = Instance.IncrementUses(From);
                    Instance.SendReply(From, Instance.GetMessage("TeleportsRemaining").Replace("{0}", usesRemaining.ToString()));
                }

                Teleporter teleporter = From.gameObject.AddComponent<Teleporter>();
                teleporter.Create(From, To, timeUntilTeleport);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);
                CancelInvoke();
                GameObject.Destroy(this.gameObject);
            }

            public void RequestDeclined()
            {
                Instance.SendReply(From, Instance.GetMessage("RequestToDenied").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("RequestFromDenied").Replace("{0}", From.displayName));
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(From);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
                    Instance.RefundServerRewards(From);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null)
                    GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            public void RequestCancelled()
            {
                Instance.SendReply(From, Instance.GetMessage("TeleportRequestToCancelled").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("TeleportRequestFromCancelled").Replace("{0}", From.displayName));
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(From);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
                    Instance.RefundServerRewards(From);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null)
                    GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            void RequestTimeOut()
            {
                Instance.SendReply(From, Instance.GetMessage("RequestToTimedOut").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("RequestFromTimedOut").Replace("{0}", From.displayName));
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(From);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
                    Instance.RefundServerRewards(From);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null)
                    GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            void CancelRequest()
            {
                Instance.SendReply(From, Instance.GetMessage("BlockTPTakeDamage"));
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(From);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
                    Instance.RefundServerRewards(From);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null)
                    GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            void OnDestroy()
            {
                CancelInvoke();
                Instance.gameObjects.Remove(GameObject);
            }
        }

        class PendingRequest : MonoBehaviour
        {
            public BasePlayer From;
            public TeleportRequest TeleportRequest;
        }

        class Teleporter : MonoBehaviour
        {
            GameObject GameObject;
            int TimeUntilTeleport;
            BasePlayer From;
            BasePlayer To;

            public void Create(BasePlayer from, BasePlayer to, int timeUntilTeleport)
            {
                this.GameObject = new GameObject();
                this.TimeUntilTeleport = timeUntilTeleport;
                this.From = from;
                this.To = to;
                Instance.gameObjects.Add(GameObject);
            }

            void Start() => InvokeRepeating("TimerTick", 0, 1.0f);

            void TimerTick()
            {
                if (TimeUntilTeleport == 0) Teleport();
                TimeUntilTeleport--;
            }

            void Teleport()
            {
                Vector3 currentPos = From.transform.position;
                Instance.RecordLastTP(From, currentPos);

                Instance.Teleport(From, To);

                Instance.SendReply(From, Instance.GetMessage("YouTeleportedTo").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("TeleportedToYou").Replace("{0}", From.displayName));

                GameObject.Destroy(this.gameObject.GetComponent<Teleporter>());
            }

            public void CancelTeleport()
            {
                Instance.SendReply(From, Instance.GetMessage("TeleportToCancelled").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("TeleportFromCancelled").Replace("{0}", From.displayName));
                if (Instance.storedData.Cooldowns.ContainsKey(From.userID))
                    Instance.storedData.Cooldowns.Remove(From.userID);
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(From);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
                    Instance.RefundServerRewards(From);
                GameObject.Destroy(this.gameObject.GetComponent<Teleporter>());
            }

            void StartSleeping(BasePlayer player)
            {
                if (player.IsSleeping())
                    return;
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                    BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
            }

            void OnDestory()
            {
                CancelInvoke();
                Instance.gameObjects.Remove(GameObject);
            }
        }

        class CooldownManager : MonoBehaviour
        {
            GameObject GameObject;

            public static void Create()
            {
                CooldownManager cm = new CooldownManager();
                cm.GameObject = new GameObject();
                cm.GameObject.AddComponent<CooldownManager>();
                Instance.gameObjects.Add(cm.GameObject);
            }

            void Start()
            {
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                if (Instance?.storedData?.Cooldowns == null) return;
                Dictionary<ulong, int> dict = new Dictionary<ulong, int>(Instance.storedData.Cooldowns);
                foreach (KeyValuePair<ulong, int> kvp in dict)
                {
                    Instance.storedData.Cooldowns[kvp.Key]--;
                    if (kvp.Value == 0)
                        Instance.storedData.Cooldowns.Remove(kvp.Key);
                }
            }

            void OnDestroy()
            {
                Instance?.SaveData();
                CancelInvoke();
                Instance.gameObjects.Remove(GameObject);
            }
        }

        class StoredData
        {
            public Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> UsesToday = new Dictionary<ulong, int>();
        }
        StoredData storedData;

        #endregion

        #region Oxide Hooks

        void Init()
        {
            //Debugging mode should be enabled?
            if (ConVar.Server.hostname == "PsychoTea's Testing Server")
            {
                debuggingMode = true;
                Puts("Debugging mode enabled.");
            }

            //Register permissions
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permCancel, this);
            permission.RegisterPermission(permBack, this);
            permission.RegisterPermission(permHere, this);
            permission.RegisterPermission(permSleepers, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("ДлительностьПерезарядкиТелепортаВСекундах").Keys)
                permission.RegisterPermission(perm, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "У вас нет доступа к этой команде" },
                { "TeleportTitle", "Телепорт" },
				{ "RequestSent", "Вы отправили запрос на телепортацию игроку <color=#81BEF7>{0}</color>\nИспользуйте <color=#BEF781>/tp</color> чтобы открыть меню телепорта и отменить запрос" },
                { "RequestRecieved", "<color=#81BEF7>{0}</color> отправил вам запрос на телепортацию\nИспользуйте <color=#BEF781>/tp</color> чтобы открыть меню телепорта и принять запрос" },
                { "RequestToTimedOut", "Запрос на телепортацию к <color=#81BEF7>{0}</color> отклонен\n<color=#F5DA81>ПРИЧИНА:</color> игрок не ответил на запрос вовремя" },
                { "RequestFromTimedOut", "Запрос на телепортацию от <color=#81BEF7>{0}</color> отклонен\n<color=#F5DA81>ПРИЧИНА:</color> вы не ответили на запрос вовремя" },
                { "HasPendingRequest", "У <color=#81BEF7>{0}</color> уже есть неотвеченный запрос на телепортацию." },
                { "RequestFrom", "Запрос от <color=#81BEF7>{0}</color>" },
				{ "RequestToAccepted", "<color=#81BEF7>{0}</color> принял запрос на телепорт\nВы телепортируетесь через <color=#BEF781>{1} сек.</color>" },
                { "RequestFromAccepted", "Вы приняли запрос от игрока <color=#81BEF7>{0}</color>\nТелепортация через <color=#BEF781>{1} сек.</color>" },
				{ "RequestToDenied", "<color=#81BEF7>{0}</color> отклонил запрос на телепортацию" },
                { "RequestFromDenied", "Вы отклонили запрос на телепортацию" },
				{ "YouTeleportedTo", "Вы телепортировались к игроку <color=#81BEF7>{0}</color>" },
                { "TeleportedToYou", "<color=#81BEF7>{0}</color> телепортировался к вам" },
                { "OnCooldown", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> телепорт перезаряжается, подождите <color=#BEF781>{0} сек.</color>" },
                { "NoPendingRequests", "Вы не можете принять запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> нет активных запросов на телепортацию" },
                { "SyntaxTPR", "Используйте <color=#BEF781>/tpr <ник игрока или steamid></color> чтобы отправить запрос на телепорт" },
                { "PlayerNotFound", "Игрок <color=#81BEF7>{0}</color> не найден" },
                { "MultiplePlayersFound", "<color=#F5DA81>НАЙДЕНО НЕСКОЛЬКО ИГРОКОВ:</color>\n{0}" },
                { "CantTeleportToSelf", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> невозможно отправить запрос самому себе" },
                { "PlayerIsBuildBlocked", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> строительство запрещено" },
                { "TargetIsBuildBlocked", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> у игрока запрещено строительство" },
                { "LocationIsBuildBlocked", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> строительство запрещено" },
                { "PlayerIsBleeding", "Вы не можете принять запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> вы истекаете кровью" },				
                { "CantAffordEconomics", "Вы не можете себе это позволить! Цена:  <color=#81BEF7>{0}₽</color>" },
                { "EconomicsYouSpent", "Вы потратили  <color=#81BEF7>{0}₽</color> на этот телепорт." },
                { "EconomicsRefunded", "Вам было возвращено  <color=#81BEF7>{0}₽</color>" },
                { "CantAffordServerRewards", "Вы не можете себе это позволить! Цена:  <color=#81BEF7>{0}RP</color>" },
                { "ServerRewardsYouSpent", "Вы потратилиt  <color=#81BEF7>{0}RP</color> на этот телепорт." },
                { "ServerRewardsRefunded", "Вам было возвращено  <color=#81BEF7>{0}RP</color>" },
                { "ЗапретитьТелепортВоВремяКрафта", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> вы не закончили крафт предметов." },
                { "MaxTeleportsReached", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> исчерпан дневной лимит телепортаций к игрокам." },
                { "TeleportsRemaining", "<color=#81BEF7>{0}</color> осталось телепортов на сегодня." },
                { "TeleportRequestFromCancelled", "Вы отклонили запрос на телепортацию от игрока <color=#81BEF7>{0}</color>." },
                { "TeleportRequestToCancelled", "Вы отклонили запрос на телепортацию к игроку <color=#81BEF7>{0}</color>" },
				{ "TeleportRequestCancelled", "Запрос телепорта отменен." },
                { "TeleportToCancelled", "Телепортация к <color=#81BEF7>{0}</color> отменена." },
                { "TeleportFromCancelled", "<color=#81BEF7>{0}</color> отменил телепортацию" },
                { "NoBackLocation", "У вас нет предыдущего места, к которому можно вернуться." },
                { "TeleportedBack", "Вы телепортированы на предыдущее местоположени." },
                { "SummonedToYou", "<color=#81BEF7>{0}</color> телепортирован к вам" },
                { "SummonedTo", "Вы телепортировались к <color=#81BEF7>{0}</color>" },
				{ "SyntaxTPHere", "Используйте <color=#BEF781>/tphere <ник игрока или steamid></color> чтобы телепортировать игрока к вам." },
                { "TPPos-InvalidSyntax", "Используйте <color=#BEF781>/tp {x} {y} {z}</color>" },
                { "TPToPos", "Вы телепортировались на <color=#BEF781>X:{x}</color>, <color=#BEF781>Y:{y}</color>, <color=#BEF781>Z:{z}</color>" },
				{ "NothingToCancel", "У вас нет запросов на телепортацию." }
            }, this, "en");

            ReadData();

            foreach (string cmdAlias in GetConfig<List<object>>("Команда альянса"))
                cmd.AddChatCommand(cmdAlias, this, "tpCommand");

            timer.Once(TimeUntilMidnight(), () => ResetDailyUses());

            if (debuggingMode) BasePlayer.activePlayerList.ForEach(x => ShowTeleportUI(x));
        }

        void OnServerInitialized()
        {
            Instance = this;

            CooldownManager.Create();

            if (Economics == null && GetConfig<bool>("Использовать плагин Economics"))
            {
                Debug.LogError("[TeleportGUI] Error! Economics is enabled in the config but is not installed! Please install Economics or disable 'Использовать плагин Economics' in the config!");
            }

            if (ServerRewards == null && GetConfig<bool>("Использовать плагин ServerRewards"))
            {
                Debug.LogError("[TeleportGUI] Error! ServerRewards is enabled in the config but is not installed! Please install ServerRewards or disable 'Использовать плагин ServerRewards' in the config!");
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;
            if (player == null) return;

            if (!HasComponent<Teleporter>(player)) return;
            if (!GetConfig<bool>("Отменять телепортацию если игрок получил урон")) return;

            var teleporter = player.gameObject.GetComponent<Teleporter>();
            teleporter.CancelTeleport();
        }

        void Unload()
        {
            foreach (GameObject go in gameObjects)
            {
                if (go == null) continue;
                if (HasComponent<Teleporter>(go) ||
                    HasComponent<PendingRequest>(go) ||
                    HasComponent<CooldownManager>(go))
                    GameObject.Destroy(go);
            }
            gameObjects.Clear();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CloseUI(player);

            SaveData();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config["Включить префикс"] = true;
            Config["Префикс"] = "<color=orange>TP: </color>";
            Config["Стандартная длительность задержки перед телепортацией (в секундах)"] = 25;
            Config["ДлительностьЗадержкиПередТелепортациейВСекундах"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 20 },
                { "teleportgui.elite", 15 },
                { "teleportgui.god", 5 },
                { "teleportgui.none", 0 }
            };
            Config["Команда альянса"] = new List<string>() { "teleport" };
            Config["Время ожидания запроса"] = 30;
            Config["Стандартная длительность перезарядки телепорта (в секундах)"] = 180;
            Config["ДлительностьПерезарядкиТелепортаВСекундах"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 60 },
                { "teleportgui.elite", 30 },
                { "teleportgui.god", 15 },
                { "teleportgui.none", 0 }
            };
            Config["Стандартные ограничения телепортаций в день"] = 3;
            Config["ОграниченияТелепортацийВДень"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 5 },
                { "teleportgui.elite", 8 },
                { "teleportgui.god", 15 },
                { "teleportgui.none", 9999 }
            };
            Config["Незаметный телепорт админа"] = false;
            Config["Включить телепорт админа"] = false;
            Config["Разрешить телепортацию во время кровотечения"] = false;
            Config["Разрешить телепортацию к игроку, котороый в зоне действия чужого шкафа"] = false;
            Config["Разрешить телепортацию в зону действия чужого шкафа"] = false;
            Config["Разрешить телепортацию из зоны действия чужого шкафа"] = false;
            Config["Использовать плагин Economics"] = false;
            Config["Цена для Economics"] = 100;
            Config["Использовать плагин ServerRewards"] = false;
            Config["Цена для ServerRewards"] = 10;
            Config["ЗапретитьТелепортВоВремяКрафта"] = true;
            Config["Разрешить специальные символы"] = false;
            Config["Отменять телепортацию если игрок получил урон"] = true;
        }

        #endregion

        #region Commands

        #region Chat

        [ChatCommand("tp")]
        void TPCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (player.IsAdmin && args.Length > 0)
            {
                if (args.Length < 3)
                {
                    SendReply(player, GetMessage("TPPos-InvalidSyntax"));
                    return;
                }

                float x, y, z;
                if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y) || !float.TryParse(args[2], out z))
                {
                    SendReply(player, GetMessage("TPPos-InvalidSyntax"));
                    return;
                }

                Teleport(player, new Vector3(x, y, z));
                SendReply(player, GetMessage("TPToPos")
                                    .Replace("{x}", x.ToString("N1"))
                                    .Replace("{y}", y.ToString("N1"))
                                    .Replace("{z}", z.ToString("N1")));
                return;
            }

            ShowTeleportUI(player);
        }

        [ChatCommand("tpr")]
        void TPRCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SyntaxTPR"));
                return;
            }

            string name = args[0];
            for (int i = 1; i < args.Length; i++)
                name += " " + args[i];

            List<BasePlayer> matches = FindByNameMulti(name);
            if (matches.Count() == 0)
            {
                SendReply(player, GetMessage("PlayerNotFound").Replace("{0}", name));
                return;
            }
            else if (matches.Count() > 1)
            {
                SendReply(player, GetMessage("MultiplePlayersFound").Replace("{0}", name));
                return;
            }
            BasePlayer targetPlayer = matches.First();

            if (targetPlayer == player && !debuggingMode)
            {
                SendReply(player, GetMessage("CantTeleportToSelf"));
                return;
            }

            TPR(player, targetPlayer);
            return;
        }

        [ChatCommand("tpa")]
        void TPACommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;

            if (tr == null)
            {
                SendReply(player, GetMessage("NoPendingRequests"));
                return;
            }

            tr.RequestAccepted();
            CloseUI(player);
        }

        [ChatCommand("tpd")]
        void TPDCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;

            if (tr == null)
            {
                SendReply(player, GetMessage("NoPendingRequests"));
                return;
            }

            tr.RequestDeclined();
            CloseUI(player);
        }

        [ChatCommand("tpc")]
        void TPCCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permCancel))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPC(player);
        }

        [ChatCommand("tpb")]
        void TPBCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permBack))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPB(player);
        }

        [ChatCommand("tphere")]
        void TPHereCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permHere))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SyntaxTPHere"));
                return;
            }

            TPHere(player, args[0]);
        }

        #endregion

        #region Console

        [ConsoleCommand("tpgui")]
        void TPGuiCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            string[] args = arg.Args ?? new string[] { };

            #region Check Perm
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }
            #endregion

            #region Open GUI
            if (args.Length == 0)
            {
                ShowTeleportUI(player);
                return;
            }

            if (args[0] == "True") //Because apparently bind b tpgui runs "tpgui True" -.-
            {
                ShowTeleportUI(player);
                return;
            }
            #endregion

            #region Close
            if (args[0] == "close")
            {
                CloseUI(player);
                return;
            }
            #endregion

            #region To
            if (args[0] == "to")
            {
                if (args.Length < 2) return;

                CloseUI(player);

                string name = args[1];
                for (int i = 2; i < args.Length; i++)
                    name += " " + args[i];

                BasePlayer targetPlayer = FindByName(name, GUIManager.Get(player).Sleepers);

                if (targetPlayer == null)
                {
                    SendReply(player, GetMessage("PlayerNotFound").Replace("{0}", name));
                    return;
                }

                if (GUIManager.Get(player).Sleepers)
                {
                    SendReply(player, GetMessage("YouTeleportedTo").Replace("{0}", targetPlayer.displayName));
                    Vector3 currentPos = player.transform.position;
                    RecordLastTP(player, currentPos);
                    Teleport(player, targetPlayer);
                    return;
                }

                TPR(player, targetPlayer);
                return;
            }
            #endregion

            #region Accept
            if (args[0] == "accept")
            {
                TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>().TeleportRequest;
                tr.RequestAccepted();
                CloseUI(player);
                return;
            }
            #endregion

            #region Decline
            if (args[0] == "decline")
            {
                TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>().TeleportRequest;
                tr.RequestDeclined();
                CloseUI(player);
                return;
            }
            #endregion

            #region Back
            if (args[0] == "back")
            {
                if (!HasPerm(player, permBack))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                TPB(player);
                CloseUI(player);
            }
            #endregion

            #region Cancel
            if (args[0] == "cancel")
            {
                if (!HasPerm(player, permCancel))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                TPC(player);
                CloseUI(player);
            }
            #endregion

            #region Here TP
            if (args[0] == "heretp")
            {
                if (!HasPerm(player, permHere))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                if (args.Length < 2) return;
                TPHere(player, args[1]);
            }
            #endregion

            #region Set
            if (args[0] == "set")
            {
                if (args.Length < 3) return;

                switch (args[1])
                {
                    case "page":
                        int page;
                        if (!Int32.TryParse(args[2], out page)) return;
                        GUIManager.Get(player).Page = page;
                        UIChooser(player);
                        break;
                    case "tphere":
                        if (!HasPerm(player, permHere))
                        {
                            SendReply(player, GetMessage("NoPermission"));
                            return;
                        }
                        bool here = args[2] == bool.TrueString;
                        GUIManager.Get(player).TPHere = here;
                        UIChooser(player);
                        break;
                    case "sleepers":
                        if (!HasPerm(player, permSleepers))
                        {
                            SendReply(player, GetMessage("NoPermission"));
                            return;
                        }
                        bool sleepers = args[2] == bool.TrueString;
                        GUIManager.Get(player).Sleepers = sleepers;
                        UIChooser(player);
                        break;
                }
                return;
            }
            #endregion
        }

        [ConsoleCommand("resetdatafile")]
        void ResetAllCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;

            if (!debuggingMode)
            {
                Debug.LogError("[TeleportGUI] You may not use this command. Warning: It is highly untested and unsafe. Please to not bypass this warning.");
                return;
            }

            storedData.Cooldowns.Clear();
            storedData.UsesToday.Clear();
            SaveData();
            Puts("Cleared data file and saved.");
        }

        #endregion

        #endregion

        #region GUIs

        void ShowTeleportUI(BasePlayer player)
        {
            if (!guiOpen.ContainsKey(player))
                guiOpen.Add(player, false);
            if (guiOpen[player])
            {
                CloseUI(player);
                guiOpen[player] = false;
                return;
            }
            guiOpen[player] = true;

            UIChooser(player);
        }

        void UIChooser(BasePlayer player)
        {
            if (BasePlayer.activePlayerList.Count() < 25)
                SmallUI(player);
            else
                BigUI(player);
        }

        void CloseUI(BasePlayer player)
        {
            if (!guiOpen.ContainsKey(player))
                guiOpen.Add(player, false);
            guiOpen[player] = false;
            CuiHelper.DestroyUi(player, "smallTeleportGUI");
            CuiHelper.DestroyUi(player, "bigTeleportGUI");
        }

        void SmallUI(BasePlayer player)
        {
            var GUIElement = new CuiElementContainer();

            List<BasePlayer> players = new List<BasePlayer>();
            if (GUIManager.Get(player).Sleepers)
                players.AddRange(BasePlayer.sleepingPlayerList);
            else
                players.AddRange(BasePlayer.activePlayerList);

            if (!debuggingMode && players.Contains(player))
                players.Remove(player);
            
            //if (debuggingMode) players.AddRange(SpareNames());
            players = players.OrderBy(x => x.displayName).ToList();

            #region Whole Panel

            var smallTeleportGUI = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0.3 0.3", //Left Bottom
                    AnchorMax = "0.7 0.75" // Right Top
                },
                CursorEnabled = true
            }, "Hud", "smallTeleportGUI");

            #endregion

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9", //Left Bottom
                    AnchorMax = "0.998 0.999" // Right Top
                }
            }, smallTeleportGUI);

            #region Pending Request Buttons

            bool pendingRequest = (player.gameObject.GetComponent<PendingRequest>() != null);
            string requestFrom = player.gameObject.GetComponent<PendingRequest>()?.From.displayName;

            if (pendingRequest)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.12 0.96"
                    },
                    Button =
                    {
                        Command = "tpgui accept",
                        Color = "0 1 0 1",
                    },
                    Text =
                    {
                        Text = "Принять",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.122 0",
                        AnchorMax = "0.24 0.96"
                    },
                    Button =
                    {
                        Command = "tpgui decline",
                        Color = "1 0 0 1",
                    },
                    Text =
                    {
                        Text = "Отклонить",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("RequestFrom", this).Replace("{0}", requestFrom),
                        FontSize = 16,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.26 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Title

            if (!pendingRequest)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("TeleportTitle", this),
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Sleepers
            if (HasPerm(player, permSleepers))
            {
                var sleepers = GUIManager.Get(player).Sleepers;
                string colour = (sleepers) ? "1 0.2 0.2 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.61 0",
                        AnchorMax = "0.71 0.97"
                    },
                    Text =
                    {
                        Text = "Спящие",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set sleepers {!sleepers}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPHere
            if (HasPerm(player, permHere))
            {
                var tpHere = GUIManager.Get(player).TPHere;
                string colour = (tpHere) ? "0 1 0 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.71 0",
                        AnchorMax = "0.78 0.97"
                    },
                    Text =
                    {
                        Text = "Сюда",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set tphere {!tpHere}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPBack
            if (HasPerm(player, permBack))
            {
                var colour = (lastTeleport.ContainsKey(player)) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";
                var command = (lastTeleport.ContainsKey(player)) ? "tpgui back" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.78 0",
                        AnchorMax = "0.85 0.97"
                    },
                    Text =
                    {
                        Text = "Назад",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPCancel
            if (HasPerm(player, permCancel))
            {
                var colour = (HasPendingTeleport(player)) ? "1 0.5 0 1" : "0.5 0.5 0.5 1";
                var command = (HasPendingTeleport(player)) ? "tpgui cancel" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.85 0",
                        AnchorMax = "0.934 0.97"
                    },
                    Text =
                    {
                        Text = "Отмена",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.935 0",
                    AnchorMax = "0.998 0.97"
                },
                Button =
                {
                    Command = "tpgui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Player List

            var playerList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, smallTeleportGUI);

            const float columnWidth = 0.2f;
            const float rowWidth = 0.2f;

            int playerCount = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (players.ToArray().Length <= playerCount) continue;

                    var panel = GUIElement.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = (columnWidth * j).ToString() + " " + (1f - (rowWidth * i) - rowWidth).ToString(),
                            AnchorMax = ((columnWidth * j) + columnWidth).ToString() + " " + (1f - (rowWidth * i)).ToString()
                        },
                        Image =
                        {
                            Color = "0 0 0 0"
                        }
                    }, playerList);

                    string playerName = players.ToArray()[playerCount].displayName;
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = CleanText(playerName),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Color = "1 1 1 1",
                            Font = "robotocondensed-regular.ttf"
                        },
                        Button =
                        {
                            Command = $"tpgui to {playerName}",
                            Color = "0 0 0 0"
                        }
                    }, panel);

                    if (GUIManager.Get(player).TPHere)
                    {
                        GUIElement.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.7 0.005",
                                AnchorMax = "0.97 0.2"
                            },
                            Text =
                            {
                                Text = "Сюда",
                                Color = "1 1 1 1",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 12
                            },
                            Button =
                            {
                                Command = $"tpgui heretp {playerName}",
                                Color = "0 1 0 0.5"
                            }
                        }, panel);
                    }

                    playerCount++;
                }
            }

            #endregion

            #region Empty List

            if (players.Count() == 0)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "Похоже, ты один выживший!",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, playerList);
            }

            #endregion

            CuiHelper.DestroyUi(player, "smallTeleportGUI");
            CuiHelper.AddUi(player, GUIElement);
        }

        void BigUI(BasePlayer player)
        {
            var GUIElement = new CuiElementContainer();

            List<BasePlayer> players = new List<BasePlayer>();
            if (GUIManager.Get(player).Sleepers)
                players.AddRange(BasePlayer.sleepingPlayerList);
            else
                players.AddRange(BasePlayer.activePlayerList);

            if (!debuggingMode && players.Contains(player))
                players.Remove(player);

            //if (debuggingMode) players.AddRange(SpareNames());
            players = players.OrderBy(x => x.displayName).ToList();

            int maxPages = CalculatePages(players.Count);

            #region Whole Panel

            var bigTeleportGUI = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0.2 0.125", //Left Bottom
                    AnchorMax = "0.8 0.9" // Right Top
                },
                CursorEnabled = true
            }, "Hud", "bigTeleportGUI");

            #endregion

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.91", //Left Bottom
                    AnchorMax = "0.997 1.0" // Right Top
                }
            }, bigTeleportGUI);

            #region Pending Request Buttons

            bool pendingRequest = (player.gameObject.GetComponent<PendingRequest>() != null);
            string requestFrom = player.gameObject.GetComponent<PendingRequest>()?.From.displayName;

            if (pendingRequest)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.01",
                        AnchorMax = "0.16 0.98"
                    },
                    Button =
                    {
                        Command = "tpgui accept",
                        Color = "0 1 0 1",
                    },
                    Text =
                    {
                        Text = "Принять",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.1615 0.01",
                        AnchorMax = "0.32 0.98"
                    },
                    Button =
                    {
                        Command = "tpgui decline",
                        Color = "1 0 0 1",
                    },
                    Text =
                    {
                        Text = "Отклонить",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("RequestFrom", this).Replace("{0}", requestFrom),
                        FontSize = 18,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.34 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Title

            if (!pendingRequest)
            {
                string pageNum = (maxPages > 1) ? $" - {GUIManager.Get(player).Page}" : "";
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("TeleportTitle", this) + pageNum,
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Sleepers
            if (HasPerm(player, permSleepers))
            {
                var sleepers = GUIManager.Get(player).Sleepers;
                string colour = (sleepers) ? "1 0.2 0.2 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.62 0",
                        AnchorMax = "0.70 0.97"
                    },
                    Text =
                    {
                        Text = "Спящие",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set sleepers {!sleepers}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPHere
            if (HasPerm(player, permHere))
            {
                var tpHere = GUIManager.Get(player).TPHere;
                string colour = (tpHere) ? "0 1 0 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.70 0",
                        AnchorMax = "0.78 0.97"
                    },
                    Text =
                    {
                        Text = "Сюда",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set tphere {!tpHere}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPBack
            if (HasPerm(player, permBack))
            {
                var colour = (lastTeleport.ContainsKey(player)) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";
                var command = (lastTeleport.ContainsKey(player)) ? "tpgui back" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.78 0",
                        AnchorMax = "0.86 0.97"
                    },
                    Text =
                    {
                        Text = "Назад",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPCancel
            if (HasPerm(player, permCancel))
            {
                var colour = (HasPendingTeleport(player)) ? "1 0.5 0 1" : "0.5 0.5 0.5 1";
                var command = (HasPendingTeleport(player)) ? "tpgui cancel" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.86 0",
                        AnchorMax = "0.9385 0.98"
                    },
                    Text =
                    {
                        Text = "Отмена",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.94 0.01",
                    AnchorMax = "1.0 0.98"
                },
                Button =
                {
                    Command = "tpgui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Player List

            var playerList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, bigTeleportGUI);

            var page = GUIManager.Get(player).Page;
            int playerCount = (page * 100) - 100;
            for (int j = 0; j < 20; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (players.ToArray().Length <= playerCount) continue;

                    var panel = GUIElement.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = (0.2f * i).ToString() + " " + (1f - (0.05f * j) - 0.05f).ToString(),
                            AnchorMax = ((0.2f * i) + 0.2f).ToString() + " " + (1f - (0.05f * j)).ToString()
                        },
                        Image =
                        {
                            Color = "0 1 0 0"
                        }
                    }, playerList);

                    string playerName = players.ToArray()[playerCount].displayName;

                    if (GUIManager.Get(player).TPHere)
                    {
                        GUIElement.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.85 0",
                                AnchorMax = "0.98 0.5"
                            },
                            Text =
                            {
                                Text = "Сюда",
                                FontSize = 8,
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Command = $"tpgui heretp {playerName}",
                                Color = "0 1 0 1"
                            }
                        }, panel);
                    }

                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = CleanText(playerName),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Command = $"tpgui to {playerName}",
                            Color = "0 0 0 0"
                        }
                    }, panel);

                    playerCount++;
                }
            }

            #endregion

            #region Page Buttons

            if (page < maxPages)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.575",
                        AnchorMax = "1.1 0.675"
                    },
                    Text =
                    {
                        Text = "Вверх",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"tpgui set page {(page + 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, bigTeleportGUI);
            }

            if (page > 1)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.45",
                        AnchorMax = "1.1 0.55"
                    },
                    Text =
                    {
                        Text = "Вниз",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"tpgui set page {(page - 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, bigTeleportGUI);
            }

            #endregion

            CuiHelper.DestroyUi(player, "bigTeleportGUI");
            CuiHelper.AddUi(player, GUIElement);
        }

        #endregion

        #region TP Functions

        void TPR(BasePlayer player, BasePlayer targetPlayer)
        {
            if (player.IsAdmin && GetConfig<bool>("Включить телепорт админа"))
            {
                if (!GetConfig<bool>("Незаметный телепорт админа"))
                    SendReply(player, GetMessage("TeleportedToYou").Replace("{0}", player.displayName));
                SendReply(player, GetMessage("YouTeleportedTo").Replace("{0}", targetPlayer.displayName));

                Vector3 currentPos = player.transform.position;
                RecordLastTP(player, currentPos);

                Teleport(player, targetPlayer);
                return;
            }

            if (storedData.Cooldowns.ContainsKey(player.userID))
            {
                SendReply(player, GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString()));
                return;
            }

            if (!GetConfig<bool>("Разрешить телепортацию во время кровотечения"))
            {
                if (player.metabolism.bleeding.value > 0f)
                {
                    SendReply(player, GetMessage("PlayerIsBleeding"));
                    return;
                }
            }

            if (!GetConfig<bool>("Разрешить телепортацию из зоны действия чужого шкафа"))
            {
                if (!player.CanBuild())
                {
                    SendReply(player, GetMessage("PlayerIsBuildBlocked"));
                    return;
                }
            }

            if (!GetConfig<bool>("Разрешить телепортацию к игроку, котороый в зоне действия чужого шкафа"))
            {
                if (!targetPlayer.CanBuild())
                {
                    SendReply(player, GetMessage("TargetIsBuildBlocked"));
                    return;
                }
            }

            if (!GetConfig<bool>("Разрешить телепортацию в зону действия чужого шкафа"))
            {
                if (IsBuildingBlocked(player, targetPlayer.transform.position))
                {
                    SendReply(player, GetMessage("LocationIsBuildBlocked"));
                    return;
                }
            }

            if (IsCrafting(player))
            {
                SendReply(player, GetMessage("ЗапретитьТелепортВоВремяКрафта"));
                return;
            }

            if (HasComponent<PendingRequest>(targetPlayer))
            {
                SendReply(player, GetMessage("HasPendingRequest").Replace("{0}", targetPlayer.displayName));
                return;
            }

            if (GetConfig<bool>("Использовать плагин Economics"))
            {
                if (EconomicsInstalled())
                {
                    if (!CanAffordEconomics(player))
                    {
                        SendReply(player, GetMessage("CantAffordEconomics").Replace("{0}", GetConfig<double>("Цена для Economics").ToString()));
                        return;
                    }
                    SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", GetConfig<double>("Цена для Economics").ToString()));
                }
            }

            if (GetConfig<bool>("Использовать плагин ServerRewards"))
            {
                if (ServerRewardsInstalled())
                {
                    if (!CanAffordServerRewards(player))
                    {
                        SendReply(player, GetMessage("CantAffordServerRewards").Replace("{0}", GetConfig<double>("Цена для ServerRewards").ToString()));
                        return;
                    }
                    SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", GetConfig<double>("Цена для ServerRewards").ToString()));
                }
            }

            if (GetConfig<int>("Стандартные ограничения телепортаций в день") != -1)
            {
                int maxTeleports = GetHighest(GetConfig<Dictionary<string, object>>("ОграниченияТелепортацийВДень"), player, GetConfig<int>("Стандартные ограничения телепортаций в день"));
                if (!storedData.UsesToday.ContainsKey(player.userID))
                    storedData.UsesToday.Add(player.userID, 0);
                if (storedData.UsesToday[player.userID] >= maxTeleports)
                {
                    SendReply(player, GetMessage("MaxTeleportsReached"));
                    return;
                }
            }

            string canTeleport = Interface.Oxide.CallHook("CanTeleport", player) as string;
            if (canTeleport != null)
            {
                SendReply(player, canTeleport);
                return;
            }

            TeleportRequest.Create(player, targetPlayer, GetConfig<int>("Время ожидания запроса"));
        }

        void TPC(BasePlayer player)
        {
            if (HasComponent<Teleporter>(player))
            {
                var teleporter = player.GetComponent<Teleporter>();
                teleporter.CancelTeleport();
                return;
            }

            var call = Interface.Oxide.CallHook("CancelAllTeleports", player);
            if (call is string)
            {
                SendReply(player, call as string);
                return;
            }

            SendReply(player, GetMessage("NothingToCancel"));
            return;
        }

        void TPB(BasePlayer player)
        {
            if (!lastTeleport.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoBackLocation"));
                return;
            }

            Teleport(player, lastTeleport[player]);
            SendReply(player, GetMessage("TeleportedBack"));
        }

        void TPHere(BasePlayer player, string targetName)
        {
            if (!HasPerm(player, permHere))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            List<BasePlayer> matches = FindByNameMulti(targetName, GUIManager.Get(player).Sleepers);
            if (matches.Count() == 0)
            {
                SendReply(player, GetMessage("PlayerNotFound").Replace("{0}", targetName));
                return;
            }
            else if (matches.Count() > 1)
            {
                SendReply(player, GetMessage("MultiplePlayersFound").Replace("{0}", targetName));
                return;
            }
            BasePlayer targetPlayer = matches.First();

            if (!debuggingMode && player == targetPlayer)
            {
                SendReply(player, GetMessage("CantTeleportToSelf"));
                return;
            }
            
            Teleport(targetPlayer, player);
            SendReply(player, GetMessage("SummonedToYou").Replace("{0}", targetPlayer.displayName));
            SendReply(targetPlayer, GetMessage("SummonedTo").Replace("{0}", player.displayName));
        }

        void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        #endregion

        #region Functions

        void RecordLastTP(BasePlayer player, Vector3 oldPos)
        {
            if (!lastTeleport.ContainsKey(player))
                lastTeleport.Add(player, Vector3.zero);
            lastTeleport[player] = oldPos;
        }

        bool HasPendingTeleport(BasePlayer player) => HasComponent<PendingRequest>(player) || HasComponent<Teleporter>(player);

        int IncrementUses(BasePlayer player)
        {
            if (!storedData.UsesToday.ContainsKey(player.userID))
                storedData.UsesToday.Add(player.userID, 0);
            storedData.UsesToday[player.userID]++;
            SaveData();
            int maxTeleports = Instance.GetHighest(Instance.GetConfig<Dictionary<string, object>>("ОграниченияТелепортацийВДень"), player, Instance.GetConfig<int>("Стандартные ограничения телепортаций в день"));
            int usesRemaining = maxTeleports - storedData.UsesToday[player.userID];
            return usesRemaining;
        }

        int GetLowest(Dictionary<string, object> objDict, BasePlayer player, int lowest = Int32.MaxValue)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (var kvp in objDict)
                dict.Add(kvp.Key, Int32.Parse(kvp.Value.ToString()));
            foreach (var kvp in dict)
                if (kvp.Value < lowest)
                    if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                        lowest = kvp.Value;
            return lowest;
        }

        int GetHighest(Dictionary<string, object> objDict, BasePlayer player, int highest = Int32.MinValue)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (var kvp in objDict)
                dict.Add(kvp.Key, Int32.Parse(kvp.Value.ToString()));
            foreach (var kvp in dict)
                if (kvp.Value > highest)
                    if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                        highest = kvp.Value;
            return highest;
        }

        bool IsBuildingBlocked(BasePlayer player, Vector3 pos)
        {
            var colliders = Pool.GetList<Collider>();
            Vis.Colliders(pos, 0.1f, colliders, LayerMask.GetMask("Trigger"));
            var cupboard = colliders.Select(x => x.GetComponentInParent<BuildingPrivlidge>()).Where(x => x != null).FirstOrDefault();
            Pool.FreeList(ref colliders);
            if (cupboard == null) return false;
            return player.userID != cupboard.OwnerID && !cupboard.IsAuthed(player);
        }

        bool IsCrafting(BasePlayer player) => (player.inventory.crafting.queue.Count() > 0);

        int CalculatePages(int value) => (int)Math.Ceiling(value / 100d);

        BasePlayer FindByName(string name, bool sleepers = false)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            if (sleepers) players = new List<BasePlayer>(BasePlayer.sleepingPlayerList);
            else players = new List<BasePlayer>(BasePlayer.activePlayerList);
            return players.Where(x => x.displayName.ToLower().Replace(" ", "")
                                       .Contains(name.ToLower().Replace(" ", "")))
                                       .FirstOrDefault();
        }

        List<BasePlayer> FindByNameMulti(string name, bool sleepers = false)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            if (sleepers) players = new List<BasePlayer>(BasePlayer.sleepingPlayerList);
            else players = new List<BasePlayer>(BasePlayer.activePlayerList);
            return players.Where(x => x.displayName.ToLower().Replace(" ", "")
                                       .Contains(name.ToLower().Replace(" ", "")))
                                       .ToList();
        }

        #region Economics/ServerRewards

        bool EconomicsInstalled() => Economics != null;

        bool ServerRewardsInstalled() => ServerRewards != null;

        bool CanAffordEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("Цена для Economics");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);

            if (playerMoney - price >= 0)
            {
                Economics?.Call("Set", player.userID, playerMoney - price);
                return true;
            }
            return false;
        }

        bool CanAffordServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("Цена для ServerRewards");
            int currentPoints;
            var call = ServerRewards?.Call("CheckPoints", player.userID);
            if (call == null) currentPoints = 0;
            else currentPoints = (int)call;

            if (currentPoints - price >= 0)
            {
                ServerRewards.Call("TakePoints", player.userID, price);
                return true;
            }
            return false;
        }

        void RefundPlayerEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("Цена для Economics");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);
            Economics?.Call("Set", player.userID, playerMoney + price);
            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("Цена для ServerRewards");
            ServerRewards.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

        void ResetDailyUses()
        {
            storedData.UsesToday.Clear();
            SaveData();
            timer.Once(TimeUntilMidnight(), () => ResetDailyUses());
        }

        #endregion

        #region Helpers

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasPerm(BasePlayer player, string perm) => (permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin);

        string CleanText(string text) => GetConfig<bool>("Разрешить специальные символы") ? text : new Regex(@"[^A-Za-z0-9\/:*?<>|!@#$%^&()\[\] ]+").Replace(text, " ");        

        int TimeUntilMidnight() => ((59 - DateTime.Now.Second) + ((59 - DateTime.Now.Minute) * 60) + ((23 - DateTime.Now.Hour) * 3600));

        T GetConfig<T>(string key)
        {
            if (Config[key] == null)
            {
                Debug.LogError($"[TeleportGUI] Tried to grab the key \"{key}\" from the config. Either add it manually or delete the config and allow it to regenerate.");
                return default(T);
            }
            return (T)Convert.ChangeType(Config[key], typeof(T));
        }

        string GetMessage(string key) => (GetConfig<bool>("Включить префикс") ? GetConfig<string>("Префикс") : "") + lang.GetMessage(key, this);

        bool HasComponent<T>(GameObject go) => (go.GetComponent<T>() != null);
        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(this.Title, storedData);
        void ReadData() => storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title);
        #endregion

        #region Spare Names (For Testing)

        List<BasePlayer> SpareNames()
        {
            List<BasePlayer> list = new List<BasePlayer>();
            foreach (var name in names.Split(' '))
                list.Add(new BasePlayer() { displayName = name });
            return list;
        }

        static string names = @"AaronLongjohnsonson John Thomas George James Henry Charles Joseph Frederick Robert Alfred Edward Arthur Richard Samuel Walter David Harry Albert Edwin Francis Frank Benjamin Herbert Daniel Tom Isaac Fred Peter Ernest Michael Stephen Patrick Matthew Edmund Frederic Alexander Philip Mark Evan Andrew Abraham Hugh Christopher Sidney Lewis Jonathan Jesse Ralph Joshua Sam Martin Owen Josiah Jacob Reuben Joe Leonard Edgar Eli Enoch Job Oliver Anthony Amos Horace Elijah Timothy Cornelius Moses Jeremiah Sydney Louis Nicholas Aaron Percy Ebenezer Willie Luke Dennis Jabez Levi Augustus Adam Nathaniel Harold Allen Griffith Bernard Rowland Ben Ellis Rees Archibald Ambrose Lawrence Morgan Noah Simon Ephraim Caleb Elias Reginald Roger Isaiah Phillip Jonas Nathan Clement Solomon Morris Charley Emanuel Gilbert Paul Hubert Maurice Simeon Abel Wilfred Dan Emmanuel Jim Ezra Squire Theodore Seth Horatio Wright Theophilus Vincent Wilson Alan Stanley Sampson Miles Wallace Israel Smith Humphrey Hiram Howard Cecil Felix Allan Oswald Silas Austin Nelson Douglas Hedley Enos Eugene Percival Spencer Edmond Septimus Robinson Luther Joel Adolphus Cuthbert Donald Bartholomew Elisha Uriah Laurence Johnson Lionel Clarence Llewellyn Oscar Norman Dick Charlie Godfrey Herman Colin Harvey Walker Denis Claude Zachariah Hezekiah Roland Llewelyn Harrison Julius Duncan Victor Jasper Jackson Lancelot Giles Jenkin Hartley Gerald Valentine Clifford Thompson Charlie Godfrey Herman Colin Harvey Walker Denis Claude Zachariah Hezekiah Roland Llewelyn Harrison Julius Duncan Victor Jasper Jackson Lancelot Giles Jenkin Hartley Gerald Valentine Clifford Thompson";
        string[] nameList = names.Split(' ');

        #endregion
    }
}