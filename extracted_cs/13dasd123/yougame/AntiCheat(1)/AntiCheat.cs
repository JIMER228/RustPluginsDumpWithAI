using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using ProtoBuf;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("AntiCheat", "OxideBro/RustPlugin.ru", "2.1.41")]
    class AntiCheat : RustPlugin
    {
        public static Oxide.Core.Libraries.Permission perm => Oxide.Core.Interface.Oxide.GetLibrary<Oxide.Core.Libraries.Permission>("Permission");
        private static HashSet<PlayerData> LoadedPlayerData = new HashSet<PlayerData>();

        class PlayerData
        {
            public string Date, SteamID, Reason;
        }

        #region AdminsData
        class DataStorage
        {
            public Dictionary<ulong, ADMINDATA> AdminData = new Dictionary<ulong, ADMINDATA>();
            public DataStorage() { }
        }

        class ADMINDATA
        {
            public string Name;
            public bool Check;
        }

        DataStorage adata;
        private DynamicConfigFile AdminData;

        static StoredData storedData;

        static Hash<string, List<AntiCheatLog>> anticheatlogs = new Hash<string, List<AntiCheatLog>>();

        class StoredData
        {
            public HashSet<AntiCheatLog> AntiCheatLog = new HashSet<AntiCheatLog>();
            public StoredData() { }
        }

        public class AntiCheatLog
        {
            public string UserId;
            public string UserName;
            public string Time;
            public string Type;
            public string Messages;

            public AntiCheatLog(string UserId, string UserName, string Time, string Type, string Messages)
            {
                this.UserId = UserId;
                this.UserName = UserName;
                this.Time = Time;
                this.Type = Type;
                this.Messages = Messages;
            }
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        void LoadData()
        {
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("AntiCheat/DetectLogs");
                adata = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("AntiCheat/AdminData");
                foreach (var thelog in storedData.AntiCheatLog)
                {
                    if (anticheatlogs[thelog.UserId] == null)
                        anticheatlogs[thelog.UserId] = new List<AntiCheatLog>();
                    (anticheatlogs[thelog.UserId]).Add(thelog);
                }
            }
            catch
            {
                storedData = new StoredData();
                adata = new DataStorage();
            }

        }
        #endregion

        static int b = 0;
        [PluginReference]
        Plugin Duel;
        [PluginReference]
        Plugin IPBlacklist;

        int DetectCountMacros = 10;
        int DetectCountFSH = 10;
        bool SHEnable = true;
        bool FHEnable = true;
        bool SHEnabled = true;
        bool FHEnabled = true;
        bool EnabledSilentAim = true;
        bool SHKickEnabled = false;
        bool FHKickEnabled = false;
        bool AntiRecoilEnabled = true;
        static bool SendsLogs = true;
        float AimPercent = 50;
        float AimPercentOverCount = 40f;
        static bool textureenable = true;
        bool init = false;
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            GetConfig("[Общее]", "Количество детектов для автоматического бана за Макрос:", ref DetectCountMacros);
            GetConfig("[Основное]", "Включить проверку на СпидХак?", ref SHEnable);
            GetConfig("[Основное]", "Включить проверку на ФлайХак?", ref FHEnable);
            GetConfig("[Основное]", "Включить проверку на Макрос", ref AntiRecoilEnabled);
            GetConfig("[Общее]", "Включить бан игроков за SpeedHack (Превышающее количество детектов)", ref SHEnabled);
            GetConfig("[Основное]", "Включить kick игроков за SpeedHack (При каждом детекте)", ref SHKickEnabled);
            GetConfig("[Общее]", "Включить бан игроков за FlyHack (Превышающее количество детектов)", ref FHEnabled);
            GetConfig("[Основное]", "Включить kick игроков за FlyHack (При каждом детекте)", ref FHKickEnabled);
            GetConfig("[Общее]", "Количество детектов для автоматического бана (FlyHack and SpeedHack):", ref DetectCountFSH);
            GetConfig("[Основное]", "Включить отправку детектов в чат (По привилегии)?", ref SendsLogs);
            GetConfig("[Аим]", "Процент попадания в голову для автоматического бана:", ref AimPercent);
            GetConfig("[Аим]", "Количество попаданий для автоматического бана, если процент попадания больше зазначеного в конфиге:", ref AimPercentOverCount);
            GetConfig("[Аим]", "Включить проверку на SilentAim?", ref EnabledSilentAim);
            GetConfig("[Основное]", "Включить проверку на проникновение в текстуры (Пока тестируеться)?", ref textureenable);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        static AntiCheat instance;
        static int PlayerLayer = LayerMask.NameToLayer("Player (Server)");


        static int constructionColl;
        void Loaded()
        {
            instance = this;
            LoadConfigValues();
            constructionColl = UnityEngine.LayerMask.GetMask(new string[]
            {
              "Construction"
            });
        }



        int raycastCount = 0;

        void OnServerInitialized()
        {
            LoadData();
            LoadDefaultConfig();
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }
            init = true;
        }

        public void Arrow(BasePlayer player, Vector3 from, Vector3 to)
        {
            player.SendConsoleCommand("ddraw.arrow", 5, Color.magenta, from, to, 0.1f);
        }

        void OnServerSave()
        {
            if (!init) return;
            SaveData();
            SavePlayerData();
        }

        #region BanCommands

        [ConsoleCommand("ban.user")]
        private void cmdBan(ConsoleSystem.Arg arg)
        {
            var date = DateTime.Now.ToShortDateString();
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            if (arg.Args == null || arg.Args.Length < 2)
            {
                arg.ReplyWith("Неверный синтаксис! Используйте ban.user <SteamID> <Причина>");
                return;
            }
            BasePlayer target = null;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.UserIDString == arg.Args[0])
                {
                    target = player;
                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { 0, $"Игрок <color=#FF6347>{player.displayName}</color>({player.UserIDString}) забанен! \nПричина: <color=#FF6347>{arg.Args[1]}!</color> " });
                }
            }

            LoadedPlayerData.Add(new PlayerData
            {
                Date = date,
                SteamID = arg.Args[0],
                Reason = arg.Args[1]
            });
            SaveData();
            if (target != null && target.IsConnected)
            {
                Kick(target, $"Вы были забанены. Причина: {arg.Args[1]}");
            }
            arg.ReplyWith($"{arg.Args[0]} забанен. Причина: {arg.Args[1]}!");
        }

        [ConsoleCommand("unban.user")]
        private void UnbanCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            if (arg.Args == null || arg.Args.Length != 1)
            {
                arg.ReplyWith("Неверный синтаксис! Используйте unban.user <SteamID>");
                return;
            }
            LoadedPlayerData.RemoveWhere(p => p.SteamID == arg.Args[0]);
            arg.ReplyWith($"{arg.Args[0]} разбанен");
            SaveData();
        }

        object OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            if (EnabledSilentAim)
            {
                if (player != null && info.HitEntity != null && info.HitEntity is BasePlayer)
                {
                    float y = Mathf.Abs(info.HitPositionWorld.y - info.HitEntity.CenterPoint().y);
                    if (y > 2f)
                    {
                        var messages = $"Обнаружен SilentAim! Стрельба с {y} м.";
                        AddLog(player.userID.ToString(), player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "SilentAim", messages);
                        return true;
                    }
                }
            }

            return null;
        }

        object CanUserLogin(string name, string id, string ip)
        {
            if (LoadedPlayerData.Any(p => p.SteamID == id))
            {
                return $"Вы забанены на данном сервере!";
            }
            return null;
            if (LoadedPlayerData.Any(p => p.SteamID == id || IPBlacklist && (bool)IPBlacklist?.CallHook("checkIp", ip))) return false;
        }


        public List<BasePlayer> Players => BasePlayer.activePlayerList;

        public BasePlayer FindById(ulong id)
        {
            foreach (var player in Players)
            {
                if (!id.Equals(player.userID)) continue;
                return player;
            }
            return null;
        }

        public bool IsConnected(BasePlayer player) => BasePlayer.activePlayerList.Contains(player);
        public void Kick(BasePlayer player, string reason = "") => player.Kick(reason);
        public bool IsBanned(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Banned);

        public void Ban(ulong id, string reason = "")
        {
            if (IsBanned(id)) return;

            var player = FindById(id);
            ServerUsers.Set(id, ServerUsers.UserGroup.Banned, player?.displayName ?? "Unknown", reason);
            ServerUsers.Save();
            if (player != null && IsConnected(player)) Kick(player, reason);
        }
        #endregion

        private readonly Dictionary<ulong, AimLockData> aimlock = new Dictionary<ulong, AimLockData>();

        public class AimLockData
        {
            public int Ticks = 1;
            public string Body = "";
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;

            return false;
        }

        private void OnEntityDeath(BaseCombatEntity victim, HitInfo info, BasePlayer player)
        {
            if (player is NPCPlayer) return;
            if (info == null) return;
            BasePlayer victimBP = victim.ToPlayer();
            BasePlayer initiator = info.InitiatorPlayer;
            if (IsNPC(victimBP)) return;
            if (IsNPC(initiator)) return;
            if (victim == null) return;
            if (victim?.net?.ID == null) return;
            if (victimBP == null) return;
            if (victim is BasePlayer)
            {
                if (victimBP != null && !IsNPC(victimBP))
                {
                    if (victim.ToPlayer().IsSleeping()) return;
                    string death = Convert.ToString(victim.ToPlayer().userID);
                    PlayerAntiCheat con = (from x in ACD where x.UID == death select x).FirstOrDefault();
                    con.Смертей += 1;
                }
            }
            if (IsNPC(initiator)) return;
            if (initiator?.net?.ID == null) return;
            if (initiator == null) return;
            if (victimBP == initiator) return;
            if (info.Initiator is BasePlayer)
            {
                if (info?.Initiator is BasePlayer)
                {
                    PlayerAntiCheat con2 = (from x in ACD where x.UID == Convert.ToString(initiator.userID) select x).FirstOrDefault();
                    if (initiator != null)
                    {
                        var ent = victim as BasePlayer;
                        if (ent && initiator && ent != initiator)
                        {
                            con2.Убийств += 1;
                        }
                    }

                }
            }

        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info, BasePlayer player)
        {
            if (entity is BasePlayer && info.Initiator is BasePlayer)
            {
                if (!(entity is BasePlayer)) return;
                if (!(info.Initiator is BasePlayer)) return;
                if (entity == null) return;
                if (entity?.net?.ID == null) return;
                BasePlayer init = info.Initiator.ToPlayer();
                if (init is NPCPlayer) return;
                if (entity is NPCPlayer) return;
                if (init == null) return;
                var distance = info.Initiator.Distance(entity.transform.position);

                if (distance > 10 && !IsNPC(init))
                {
                    AimLockData bodylock;
                    if (!aimlock.TryGetValue(init.userID, out bodylock))
                    {
                        aimlock.Add(init.userID, bodylock = new AimLockData());
                    }
                    var _bodyPart = entity?.skeletonProperties?.FindBone(info.HitBone)?.name?.english ?? "";
                    if (_bodyPart == "") return;
                    if (bodylock.Body == _bodyPart && _bodyPart != "lower spine")
                    {
                        bodylock.Ticks++;
                    }
                    else
                    {
                        bodylock.Ticks = 1;
                    }
                    if (bodylock.Ticks > 5)
                    {
                        var messages = $"Обнаружен АимЛок! Обнаружений {bodylock.Ticks} |  {bodylock?.Body ?? ""} | {distance} м.";
                        Debug.LogWarning($"[Анти-чит] {init.displayName}({init.UserIDString}) Обнаружен АимЛок! Обнаружений {bodylock.Ticks} |  {bodylock?.Body ?? ""} | {distance} м.");
                        AddLog(init.userID.ToString(), init.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "AimLock", messages);
                        bodylock.Ticks = 1;
                    }
                    bodylock.Body = _bodyPart;
                    PlayerAntiCheat con = (from x in ACD where x.UID == init.UserIDString select x).FirstOrDefault();
                    con.Попаданий += 1;
                    if (info.isHeadshot)
                    {
                        con.Голова += 1;
                    }
                    double aim = Math.Floor((con.Голова * 1f / con.Попаданий * 1f) * 100f);
                    PlayerAntiCheat play = (from x in ACD where x.UID == init.UserIDString select x).FirstOrDefault();
                    if (play.Смертей == 0) play.Смертей = 1;
                    double kdr = Math.Round(play.Убийств * 1f / play.Смертей * 1f, 2);
                    if (con.Попаданий > AimPercentOverCount && aim > AimPercent && kdr > 2)
                    {
                        var messages = $"<color=#ffa500>[Античит детект]</color> (AimLock) {init.displayName} забанен! Соотношение попаданий в голову {aim}% и КДР - ({kdr}) аномальные!";
                        foreach (var admin in BasePlayer.activePlayerList)
                            SendDetection(admin, messages);
                        Debug.LogWarning($"[Анти-чит] {init.displayName}({init.UserIDString}) забанен! Причина: AimLock!");
                        Ban(init.userID, "[Анти-чит] AimLock");
                        AddLog(init.userID.ToString(), init.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "AimLock", $"Игрок забанен!  Соотношение попаданий в голову {aim}% и КДР - ({kdr}) аномальные!");
                        return;
                    }
                    if (con.Попаданий > AimPercentOverCount && aim > AimPercent)
                    {
                        var messages = $"<color=#ffa500>[Античит детект]</color> {init.displayName}({init.UserIDString}) забанен! Процент попаданий в голову слишком большой {aim}%";
                        foreach (var admin in BasePlayer.activePlayerList)
                            SendDetection(admin, messages);
                        Debug.LogWarning($"[Анти-чит] {init.displayName}({init.UserIDString}) забанен! Причина: AimHack!");
                        Ban(init.userID, "[Анти-чит] AimHack");
                        AddLog(init.userID.ToString(), init.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "AimHack", $"Игрок забанен! Процент попаданий в голову слишком большой {aim}%");

                    }

                }
            }
        }

        #region Chat Command
        [ChatCommand("ac")]
        void cmdChatDetect(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !perm.UserHasPermission(player.UserIDString, "anticheat.toggleadmin"))
            {
                SendReply(player, "У вас нету привилегии использовать эту команду");
                return;
            }
            if (adata.AdminData.ContainsKey(player.userID))
            {
                if (adata.AdminData[player.userID].Check == true)
                {
                    adata.AdminData[player.userID].Check = adata.AdminData[player.userID].Check = false;
                    SendReply(player, "Админ дебаг выключен. Вас не детектит.");
                    SaveDataAdmin();
                    return;
                }
                if (adata.AdminData[player.userID].Check == false)
                {
                    adata.AdminData[player.userID].Check = adata.AdminData[player.userID].Check = true;
                    SendReply(player, "Админ дебаг включен. Вас детектит.");
                    SaveDataAdmin();
                }
            }
            else
            {
                SendReply(player, "Вас нету в базе администраторов, пожалуйста перезейдите!");
            }
        }
        #endregion

        #region Console Commands
        [HookMethod("AimCheck")]
        private void AimCheck(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin && !arg.Player())
            {
                return;
            }
            if (arg.Args == null || arg.Args.Length < 1)
            {
                arg.ReplyWith("Неверный синтаксис! Используйте aim.check <SteamID>");
                return;
            }
            if (arg.Args.Length == 1)
            {
                var check = (from x in ACD where x.UID == arg.Args[0] select x).Count();
                if (check > 0)
                {
                    PlayerAntiCheat con = (from x in ACD where x.UID == arg.Args[0] select x).FirstOrDefault();
                    double aim = Math.Floor((con.Голова * 1f / con.Попаданий * 1f) * 100);
                    arg.ReplyWith($"[Анти-чит] {con.Ник}: Aim: {aim}% при {con.Попаданий} попаданиях (с растояния 10 метров и выше)");
                }
                else
                {
                    arg.ReplyWith("Игрока не найдено!");
                }
            }
            return;
        }

        [HookMethod("AimCheckServer")]
        private void AimCheckServer(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            double popa = 0;
            double head = 0;
            var Top = (from x in ACD select x);
            foreach (var top in Top)
            {
                popa = popa + top.Попаданий;
                head = head + top.Голова;
            }

            arg.ReplyWith($"[Анти-чит]: В голову попадают в {Math.Floor((head * 1f / popa * 1f) * 100f)}% случаев (с растояния 10 метров и выше)");

            return;
        }

        [HookMethod("CheckServer")]
        private void CheckServer(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            int i = 0;
            string players = "";
            var reply = 16;
            if (reply == 0) { }
            double popa = 0;
            double head = 0;
            string aimdesc = "";
            var Top = (from x in ACD select x);
            foreach (var top in Top)
            {
                popa = popa + top.Попаданий;
                head = head + top.Голова;
            }
            double aimserver = Math.Floor((head * 1f / popa * 1f) * 100f);
            players = "----------------------------------Игроки---------------------------------- \n";
            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerAntiCheat play = (from x in ACD where x.UID == player.UserIDString select x).FirstOrDefault();
                if (play.Смертей == 0) play.Смертей = 1;
                PlayerAntiCheat aim = (from x in ACD where x.UID == player.UserIDString select x).FirstOrDefault();
                if (aim.Попаданий == 0) aim.Попаданий = 1;
                double aimprocent = Math.Floor((aim.Голова * 1f / aim.Попаданий * 1f) * 100f);
                double kdr = Math.Round(play.Убийств * 1f / play.Смертей * 1f, 2);
                double razn = aimserver - aimprocent;
                if (aim.Попаданий < 30 || play.Убийств < 10)
                {
                    aimdesc = "Новый игрок";
                }
                else if (razn > -5 && razn > 5 && kdr < 2)
                {
                    aimdesc = "Простой игрок";
                }
                else if (razn > -5 && razn > 5 && kdr < 3)
                {
                    aimdesc = "Подозрительный игрок";
                }
                else if (razn > -5 && razn > 5 && kdr >= 3)
                {
                    aimdesc = "Очень подозрительный игрок";
                }
                else if (razn > -5 && razn < -8 && kdr < 2)
                {
                    aimdesc = "Игрок с хорошей точностью в голову";
                }
                else if (razn > -5 && razn < -8 && kdr < 3)
                {
                    aimdesc = "Скилловый игрок";
                }
                else if (razn > -5 && razn < -8 && kdr < 4)
                {
                    aimdesc = "Подозрительный игрок";
                }
                if (razn > -5 && razn < -8 && kdr >= 4)
                {
                    aimdesc = "Читер";
                }
                else if (razn > 5 && razn < 8 && kdr < 1)
                {
                    aimdesc = "Игрок со слабым скиллом";
                }
                else if (razn > 5 && razn < 8 && kdr < 2)
                {
                    aimdesc = "Подозрительный игрок";
                }
                else if (razn > 5 && razn < 8 && kdr < 3)
                {
                    aimdesc = "Очень подозрительный игрок";
                }
                if (razn > 5 && razn < 8 && kdr >= 4)
                {
                    aimdesc = "Читер";
                }


                i++;
                players = players + $"{i}. {play.Ник} ({play.UID}) | aim: {aimprocent}% | kdr {kdr} | {aimdesc} \n";


            }
            arg.ReplyWith(players + "-------------------------------------------------------------------------------");
        }

        #endregion

        void Unload()
        {
            DestroyAll<PlayerHack>();
            SaveData();
            SavePlayerData();
            init = false;
        }

        void DestroyAll<T>()
        {
            UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (UnityEngine.Object gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            if (player.IsAdmin && perm.UserHasPermission(player.UserIDString, "anticheat.toggleadmin"))
            {
                if (!adata.AdminData.ContainsKey(player.userID))
                {
                    adata.AdminData.Add(player.userID, new ADMINDATA()
                    {
                        Name = player.displayName,
                        Check = false,
                    });
                    SaveDataAdmin();
                }
                else
                {
                    adata.AdminData[player.userID].Name = player.displayName.ToString();

                }

                if (adata.AdminData[player.userID].Check == true)
                {
                    SendReply(player, "<color=RED>Внимание!</color> У вас включен админ дебаг. Советуем его отключить (/ac)");
                }
            }
            else if (!player.IsAdmin && !perm.UserHasPermission(player.UserIDString, "anticheat.toggleadmin") && adata.AdminData.ContainsKey(player.userID))
            {
                adata.AdminData[player.userID].Check = adata.AdminData[player.userID].Check = true;
                SaveDataAdmin();
            }
            var check = (from x in ACD where x.UID == player.UserIDString select x).Count();
            if (check == 0) CreateInfo(player);
            PlayerAntiCheat con = (from x in ACD where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            if (player.displayName != con.Ник) con.Ник = player.displayName;
            SavePlayerData();
            new PluginTimers(this).Once(2f, () => CheckSpeed(player));
            timer.Once(1f, () => RefreshPlayer(player));
            new PluginTimers(this).Once(2f, () => AimPlayer(player));
        }

        void RefreshPlayer(BasePlayer player)
        {
            if (player.GetComponent<PlayerHack>() == null)
                player.gameObject.AddComponent<PlayerHack>();
        }

        private void AimPlayer(BasePlayer player)
        {
            if (player == null) return;

            var check = (from x in ACD where x.UID == player.UserIDString select x).Count();
            if (check == 0)
            {
                ACD.Add(new PlayerAntiCheat(player.displayName, player.UserIDString, 0, 0, 0, 0));
                SavePlayerData();
            }
        }
        private void AutoBan(BasePlayer player, string reason)
        {
            if (b == DetectCountFSH)
            {
                Ban(player.userID, "[Анти-чит] Banned: вы были забанены на сервере.");
                LogToFile("ban", $"[{DateTime.Now.ToShortTimeString()}] -  Ban: Reason - {reason} {player.displayName}({player.UserIDString}) забанен! Количество детектов привысило заданный предел.  Предупреждений: {b + 1}", this, true);
            }
        }
        [HookMethod("CheckSpeed")]
        private void CheckSpeed(BasePlayer player)
        {
            if (player == null) return;
            if (!player.IsConnected) return;
            var position = player.transform.position;
            int f = 0;
            new PluginTimers(this).Repeat(2f, 0, () =>
            {
                if (!player.IsConnected) return;
                if (adata.AdminData.ContainsKey(player.userID))
                {
                    if (adata.AdminData[player.userID].Check == false) return;
                }
                if (player.IsFlying && FHEnable && !player.IsSwimming() && !player.IsDead() && !player.IsSleeping() && !player.IsWounded())
                {
                    f++;
                    if (f >= 1)
                    {
                        if (b == DetectCountFSH && FHEnabled)
                        {
                            AutoBan(player, "FlyHack");
                        }
                        else
                        {
                            if (FHKickEnabled)
                            {
                                Kick(player, "[Анти-чит] Обнаружен FlyHack");
                                UnityEngine.Debug.LogError($"[Анти-чит], {player.displayName}, ({player.UserIDString}) кикнут! Причина: FlyHack!");
                            }
                            SendDetection(player, string.Format("<color=#ffa500>[Античит детект]</color> " + "(FLYHack) Игрок" + player.displayName + $" Слишком долго находиться в воздухе! Предупреждений: {b + 1}"));
                            var messages = $"Обнаружен FlyHack Предупреждений: {b + 1}";
                            AddLog(player.userID.ToString(), player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "FlyHack", messages);
                            b++;
                            return;
                        }
                    }
                }
                else
                {
                    f = 0;
                }

            });

        }

        #region SpeedHack
        public class PlayerHack : MonoBehaviour
        {
            public BasePlayer player;

            public Vector3 lastPosition;
            public Vector3 currentDirection;

            public bool isonGround;
            public bool wasGround = true;


            public float Distance3D;
            public float VerticalDistance;
            public float deltaTick;
            public float flyHackDetections = 0f;
            public float speedHackDetections = 0f;

            public double currentTick;
            public double lastTick;
            public double lastTickFly;
            public double lastTickSpeed;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("CheckPlayer", 1f, 1f);
                lastPosition = player.transform.position;
            }
            static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            static double CurrentTime()
            {
                return DateTime.UtcNow.Subtract(epoch).TotalMilliseconds;
            }
            static List<PlayerHack> fpsCalled = new List<PlayerHack>();
            static double fpsTime;
            static bool fpsCheckCalled = false;
            static void CheckForHacks(PlayerHack hack)
            {
                CheckForSpeedHack(hack);
            }
            static int fpsIgnore = 30;

            void CheckPlayer()
            {
                if (!player.IsConnected) GameObject.Destroy(this);
                currentTick = CurrentTime();
                deltaTick = (float)((currentTick - lastTick) / 1000.0);
                Distance3D = Vector3.Distance(player.transform.position, lastPosition) / deltaTick;
                VerticalDistance = (player.transform.position.y - lastPosition.y) / deltaTick;
                currentDirection = (player.transform.position - lastPosition).normalized;
                isonGround = player.IsOnGround();

                if (!player.IsWounded() && !player.IsDead() && !player.IsSleeping() && deltaTick < 1.1f && Performance.current.frameRate > fpsIgnore)
                    CheckForHacks(this);

                lastPosition = player.transform.position;

                if (fpsCheckCalled)
                    if (!fpsCalled.Contains(this))
                    {
                        fpsCalled.Add(this);
                        fpsTime += (CurrentTime() - currentTick);
                    }

                lastTick = currentTick;
            }
        }
        static float minSpeedPerSecond = 10f;

        static void AddLog(string userid, string UserName, string time, string logType, string messages)
        {
            if (anticheatlogs[userid] == null)
                anticheatlogs[userid] = new List<AntiCheatLog>();
            AntiCheatLog newlog = new AntiCheatLog(userid, UserName, time, logType, messages);
            (anticheatlogs[userid]).Add(newlog);
            storedData.AntiCheatLog.Add(newlog);
            instance.SaveData();
        }

        static double LogTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        static void CheckForSpeedHack(PlayerHack hack)
        {
            if (instance.adata.AdminData.ContainsKey(hack.player.userID))
            {
                if (instance.adata.AdminData[hack.player.userID].Check == false) return;
            }
            if (hack.Distance3D < minSpeedPerSecond) return;
            if (hack.VerticalDistance < -8f) return;
            if (hack.lastTickSpeed == hack.lastTick)
            {
                if (hack.player.IsSwimming() && hack.player.IsDead() && hack.player.IsSleeping() && hack.player.IsWounded()) return;
                if (hack.player.GetMounted()) return;
                hack.speedHackDetections++;
                if (instance.SHEnable)
                {
                    if (hack.player.IsOnGround())
                    {
                        if (b == instance.DetectCountFSH && instance.SHEnabled)
                        {
                            instance.AutoBan(hack.player, "SpeedHack");

                        }
                        else
                        {
                            if (instance.SHKickEnabled)
                            {
                                instance.Kick(hack.player, "[Анти-чит] Обнаружен SpeedHack");
                                UnityEngine.Debug.LogError($"[Анти-чит] {hack.player.displayName}({hack.player.UserIDString}) кикнут! Причина: SpeedHack!");
                            }
                            var messages = $"Обнаружен Speedhack ({hack.Distance3D.ToString()} м/с) Предупреждений: {b + 1}";
                            AddLog(hack.player.userID.ToString(), hack.player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "SpeedHack", messages);
                            SendDetection(hack.player, string.Format("<color=#ffa500>[Античит детект]</color> " + "(SPEEDHack) " + hack.player.displayName + $" кикнут! Двигался со скоростью выше нормы! Предупреждений: {b + 1}"));

                            b++;
                        }
                    }

                }

            }
            else
            {
                hack.speedHackDetections = 0f;
            }
            hack.lastTickSpeed = hack.currentTick;
        }
        #endregion

        static int bulletmask;
        static DamageTypeList emptyDamage = new DamageTypeList();
        static Vector3 VectorDown = new Vector3(0f, -1f, 0f);
        static Hash<BasePlayer, float> lastWallhack = new Hash<BasePlayer, float>();
        Hash<ulong, ColliderCheckTest> playerWallcheck = new Hash<ulong, ColliderCheckTest>();
        static RaycastHit cachedRaycasthit;

        [HookMethod("WallhackKillCheck")]
        private void WallhackKillCheck(BasePlayer player, BasePlayer attacker, HitInfo hitInfo)
        {
            if (adata.AdminData.ContainsKey(player.userID))
            {
                if (adata.AdminData[player.userID].Check == false) return;
            }
            if (Physics.Linecast(attacker.eyes.position, hitInfo.HitPositionWorld, out cachedRaycasthit, bulletmask))
            {
                BuildingBlock block = cachedRaycasthit.collider.GetComponentInParent<BuildingBlock>();
                if (block != null)
                {
                    if (block.blockDefinition.hierachyName == "wall.window") return;

                    CancelDamage(hitInfo);
                    if (Time.realtimeSinceStartup - lastWallhack[attacker] > 0.5f)
                    {
                        lastWallhack[attacker] = Time.realtimeSinceStartup;
                        UnityEngine.Debug.LogError($"WalhackAttack обнаружен у {player.displayName}");

                        var messages = $"Обнаружен WalhackAttack! Нанес урон через препятствие!";
                        AddLog(player.userID.ToString(), player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "WalhackAttack", messages);

                        SendDetection(player, string.Format("<color=#ffa500>[Античит детект]</color> " + "(WalhackAttack) " + player.displayName + " нанес урон через препятствие!"));

                    }
                }
            }
        }

        private void CancelDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = emptyDamage;
            hitinfo.HitEntity = null;
        }
        private readonly Dictionary<ulong, NoRecoilData> data = new Dictionary<ulong, NoRecoilData>();
        private readonly Dictionary<ulong, Timer> detections = new Dictionary<ulong, Timer>();
        private readonly int detectionDiscardSeconds = 300;
        private readonly int violationProbability = 30;
        private readonly int maximumViolations = 30;
        private readonly Dictionary<string, int> probabilityModifiers = new Dictionary<string, int>() {
            {"weapon.mod.muzzleboost", -5},
            {"weapon.mod.silencer", -5},
            {"weapon.mod.holosight", -5},
            {"crouching", -8},
            {"aiming", -5}

        };

        private readonly List<string> blacklistedAttachments = new List<string>()
        {
            "weapon.mod.muzzlebreak",
            "weapon.mod.silencer",
            "weapon.mod.small.scope"
        };


        public class NoRecoilData
        {
            public int Ticks = 0;
            public int Count;
            public int Violations;
        }

        [HookMethod("OnWeaponFired")]
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot)
        {
            if (player == null) return;
            if (!AntiRecoilEnabled) return;
            if (adata.AdminData.ContainsKey(player.userID))
            {
                if (adata.AdminData[player.userID].Check == false) return;
            }
            var item = player.GetActiveItem();
            if (!(item.info.shortname == "rifle.ak" || item.info.shortname == "lmg.m249"))
                return;
            var counts = 0;
            foreach (Item attachment in item.contents.itemList)
            {
                if (attachment.info.shortname == "weapon.mod.muzzlebrake" || attachment.info.shortname == "weapon.mod.holosight")
                    counts++;
            }
            if (counts == 2)
                return;
            if (item.contents.itemList.Any(x => blacklistedAttachments.Contains(x.info.shortname)))
                return;
            NoRecoilData info;
            if (!data.TryGetValue(player.userID, out info))
                data.Add(player.userID, info = new NoRecoilData());
            UnityEngine.Vector3 eyesDirection = player.eyes.HeadForward();

            if (eyesDirection.y < -0.80)
                return;

            info.Ticks++;
            int probModifier = 0;
            foreach (Item attachment in item.contents.itemList)
                if (probabilityModifiers.ContainsKey(attachment.info.shortname))
                    probModifier += probabilityModifiers[attachment.info.shortname];

            if (player.modelState.aiming && probabilityModifiers.ContainsKey("aiming"))
                probModifier += probabilityModifiers["aiming"];
            if (player.IsDucked() && probabilityModifiers.ContainsKey("crouching"))
                probModifier += probabilityModifiers["crouching"];
            Timer detectionTimer;
            if (detections.TryGetValue(player.userID, out detectionTimer))
                detectionTimer.Reset(detectionDiscardSeconds);
            else
                detections.Add(player.userID, timer.Once(detectionDiscardSeconds, delegate ()
                {
                    if (info.Violations > 0)
                        info.Violations--;
                }));
            timer.Once(0.5f, () =>
            {
                ProcessRecoil(projectile, player, mod, projectileShoot, info, probModifier, eyesDirection);
            });
        }

        [HookMethod("ProcessRecoil")]
        private void ProcessRecoil(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot, NoRecoilData info, int probModifier, UnityEngine.Vector3 eyesDirection)
        {

            var nextEyesDirection = player.eyes.HeadForward();
            if (Math.Abs(nextEyesDirection.y - eyesDirection.y) < .009 &&
                nextEyesDirection.y < .8) info.Count++;
            if (info.Ticks <= 10) return;
            var prob = 100 * info.Count / info.Ticks;
            var item = player.GetActiveItem();

            if (prob > ((100 - violationProbability) + probModifier))
            {
                if (prob > 100) prob = 100;
                if (prob < 80) return;
                info.Violations++;
                Debug.LogError("(Макрос) " + player.displayName + ": вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString() + ".");
                SendDetection(player, string.Format("<color=#ffa500>[Античит детект]</color> " + "(NoRecoil) " + "У игрока " + player.displayName + " обнаружен NoRecoil " + ",вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString()));
                var messages = $"Обнаружен Макрос! Вероятность {string.Format("{0}", prob)}% обнаружений { info.Violations.ToString()}, оружие: {item.info.shortname}";
                AddLog(player.userID.ToString(), player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "Macros", messages);

                if (info.Violations > DetectCountMacros)
                {
                    Ban(player.userID, "[Анти-чит] Обнаружен скрипт для макроса");
                    LogToFile("ban", $"[{DateTime.Now.ToShortTimeString()}] - (Макрос) Игрок" + player.displayName + "забанен. Вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString() + " | " + item.info.shortname, this, true);
                }
            }

            info.Ticks = 0;
            info.Count = 0;
        }


        static Hash<BasePlayer, int> wallhackDetec = new Hash<BasePlayer, int>();

        public class ColliderCheckTest : MonoBehaviour
        {
            public BasePlayer player;
            Hash<Collider, Vector3> entryPosition = new Hash<Collider, Vector3>();
            SphereCollider col;
            public float teleportedBack;
            public Collider lastCollider;

            void Awake()
            {
                player = transform.parent.GetComponent<BasePlayer>();

                col = gameObject.AddComponent<SphereCollider>();
                col.radius = 0.1f;
                col.isTrigger = true;
                col.center = new Vector3(0f, 0.5f, 0f);
            }

            void OnTriggerEnter(Collider collision)
            {
                // new PluginTimers(this).Repeat(1f, 0, () =>
                // {
                if (Time.realtimeSinceStartup < teleportedBack + 0.2f && collision != lastCollider) return;
                // if( hasBuildingPrivileges(player ) ) return;
                if (collision.GetComponent<MeshCollider>() == null) return;
                if (collision.gameObject.name != "Mesh Collider Batch") return;
                MeshColliderBatch meshcoll = collision.GetComponent<MeshColliderBatch>();
                if (meshcoll == null) return;
                entryPosition[collision] = player.transform.position;
                // });
            }


            public static BaseEntity GetCollEntity(Vector3 entry, Vector3 exist)
            {
                var rayArray = Physics.RaycastAll(exist, entry, Vector3.Distance(entry, exist), constructionColl);
                for (int i = 0; i < rayArray.Length; i++)
                {
                    return rayArray[i].GetEntity();
                }
                return null;
            }

            void OnTriggerExit(Collider collision)
            {
                if (textureenable)
                    if (entryPosition.ContainsKey(collision))
                    {
                        MeshColliderBatch meshcoll = collision.GetComponent<MeshColliderBatch>();
                        BaseEntity targetent = GetCollEntity(entryPosition[collision], player.transform.position);
                        if (targetent != null)
                        {
                            BuildingBlock block = targetent.GetComponent<BuildingBlock>();
                            if (block != null)
                            {
                                if (!block.gameObject.name.Contains("foundation.steps") && !block.gameObject.name.Contains("block.halfheight.slanted"))
                                {
                                    SendDetection(player, string.Format($"{player.displayName},({player.userID}) Обнаружен TextureHack!"));
                                    ForcePlayerBack(this, collision, entryPosition[collision], player.transform.position);
                                    if (Time.realtimeSinceStartup - lastWallhack[player] < 10f)
                                    {
                                        SendDetection(player, string.Format($"{player.displayName},({player.userID}) Обнаружен WallHack! Детект № {wallhackDetec[player]}"));
                                        wallhackDetec[player]++;
                                        var messages = $"Обнаружен WallHack! Обнаружений {wallhackDetec[player]}";
                                        AddLog(player.userID.ToString(), player.displayName.ToString(), $"{DateTime.Now.ToShortDateString()} [{DateTime.Now.ToShortTimeString()}]", "WallHack", messages);
                                    }

                                    lastWallhack[player] = Time.realtimeSinceStartup;
                                }
                            }
                        }
                        entryPosition.Remove(collision);
                    }
            }
            void OnDestroy()
            {
                GameObject.Destroy(gameObject);
                GameObject.Destroy(col);
            }
        }

        static void ForcePlayerBack(ColliderCheckTest colcheck, Collider collision, Vector3 entryposition, Vector3 exitposition)
        {
            Vector3 rollBackPosition = GetRollBackPosition(entryposition, exitposition, 4f);
            Vector3 rollDirection = (entryposition - exitposition).normalized;
            foreach (RaycastHit rayhit in UnityEngine.Physics.RaycastAll(rollBackPosition, (exitposition - entryposition).normalized, 5f))
            {
                if (rayhit.collider == collision)
                {
                    rollBackPosition = rayhit.point + rollDirection * 1f;
                }
            }
            colcheck.teleportedBack = Time.realtimeSinceStartup;
            colcheck.lastCollider = collision;
            ForcePlayerPosition(colcheck.player, rollBackPosition);
        }
        static Vector3 GetRollBackPosition(Vector3 entryposition, Vector3 exitposition, float distance)
        {
            distance = Vector3.Distance(exitposition, entryposition) + distance;
            var direction = (entryposition - exitposition).normalized;
            return (exitposition + (direction * distance));
        }

        static void ForcePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
        }


        [HookMethod("OnBasePlayerAttacked")]
        private void OnBasePlayerAttacked(BasePlayer player, HitInfo hitInfo)
        {
            if (player.IsDead()) return;
            if (hitInfo.Initiator == null) return;
            if (player.health - hitInfo.damageTypes.Total() > 0f) return;
            BasePlayer attacker = hitInfo.Initiator.ToPlayer();
            if (attacker == null) return;
            if (attacker == player) return;
            WallhackKillCheck(player, attacker, hitInfo);
        }

        #region Other Methods

        public static void msgPlayer(BasePlayer player, string msg)
        {
            player.ChatMessage($"[Анти-Чит] {msg}");
        }

        public static void msgAll(string msg)
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, $"[Анти-Чит] {msg}");
        }

        #endregion

        [HookMethod("Init")]
        private void Init()
        {
            perm.RegisterPermission("anticheat.toggleadmin", this);
            perm.RegisterPermission("anticheat.sendlogs", this);
            Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command>(null).AddConsoleCommand("aim.check", this, "AimCheck");
            Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command>(null).AddConsoleCommand("aim.server", this, "AimCheckServer");
            Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command>(null).AddConsoleCommand("check.server", this, "CheckServer");
            ACD = Interface.Oxide.DataFileSystem.ReadObject<List<PlayerAntiCheat>>("AntiCheat/PlayerAntiCheat");
            LoadedPlayerData = Interface.Oxide.DataFileSystem.ReadObject<HashSet<PlayerData>>("AntiCheat/Blacklist");
            foreach (var player in BasePlayer.activePlayerList)
            {
                var check = (from x in ACD where x.UID == player.UserIDString select x).Count();
                if (check == 0) CreateInfo(player);
            }

        }
        [HookMethod("CreateInfo")]
        private void CreateInfo(BasePlayer player)
        {

            if (player == null) return;
            ACD.Add(new PlayerAntiCheat((string)player.displayName, player.UserIDString, 0, 0, 0, 0));
            SavePlayerData();
        }

        private void SaveDataAdmin()
        {
            Interface.GetMod().DataFileSystem.WriteObject("AntiCheat/AdminData", adata);
        }

        [HookMethod("SaveData")]
        private void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("AntiCheat/DetectLogs", storedData);
            Interface.Oxide.DataFileSystem.WriteObject("AntiCheat/Blacklist", LoadedPlayerData);

        }

        private void SavePlayerData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AntiCheat/PlayerAntiCheat", ACD);
        }

        static void SendDetection(BasePlayer player, string msg)
        {
            if (SendsLogs)
            {
                if (perm.UserHasPermission(player.UserIDString, "anticheat.sendlogs"))
                {
                    player.SendConsoleCommand("chat.add", new object[] { 0, msg });

                }
            }
        }

        public List<PlayerAntiCheat> ACD = new List<PlayerAntiCheat>();
        public class PlayerAntiCheat
        {
            public PlayerAntiCheat(string Ник, string UID, int Убийств, int Смертей, int Попаданий, int Голова)
            {
                this.Ник = Ник;
                this.UID = UID;
                this.Убийств = Убийств;
                this.Смертей = Смертей;
                this.Попаданий = Попаданий;
                this.Голова = Голова;
            }

            public string Ник { get; set; }
            public string UID { get; set; }
            public int Убийств { get; set; }
            public int Смертей { get; set; }
            public int Попаданий { get; set; }
            public int Голова { get; set; }
        }
    }
}
                   