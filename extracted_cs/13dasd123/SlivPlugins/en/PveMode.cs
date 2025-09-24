// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Oxide.Plugins.PveModeExtensionMethods;

namespace Oxide.Plugins
{
    [Info("PveMode", "KpucTaJl", "1.1.5")]
    internal class PveMode : RustPlugin
    {
        #region Config
        private const bool En = true;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class PluginConfig
        {
            [JsonProperty(En ? "The time to clear the information of the players' damage to NPC after NPC has take the last damage [sec.]" : "Время очистки информации о нанесенном уроне от игроков к NPC после нанесения последнего урона по NPC [sec.]")] public int TimeLastDamage { get; set; }
            [JsonProperty(En ? "Block a player from entering the event area if he is the owner of another event? [true/false]" : "Запрещать игроку входить внутрь зоны ивента, если он является владельцем другого ивента? [true/false]")] public bool NoEnterAnotherOwner { get; set; }
            [JsonProperty(En ? "Ignore administrators? [true/false]" : "Игнорировать администраторов? [true/false]")] public bool IgnoreAdmin { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    TimeLastDamage = 300,
                    NoEnterAnotherOwner = false,
                    IgnoreAdmin = false,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoLootScientist"] = "You <color=#ce3f27>are unable</color> to loot this NPC due to another player doing more damage!",
                ["NoLootCrateEvent"] = "You <color=#ce3f27>cannot</color> loot the crate! You are not the Event Owner and you are not on their team!",
                ["NoHackCrateEvent"] = "You <color=#ce3f27>cannot</color> hack the locked crate! You are not the Event Owner and you are not on their team!",
                ["NoLootScientistEvent"] = "You <color=#ce3f27>cannot</color> loot an NPC's corpse! You are not the Event Owner and you are not on their team!",
                ["NoDamageTankEvent"] = "You <color=#ce3f27>cannot</color> damage Bradley! You are not the Event Owner and you are not on their team!",
                ["NoDamageHelicopterEvent"] = "You <color=#ce3f27>cannot</color> damage Patrol Helicopter! You are not the Event Owner and you are not on their team!",
                ["NoDamageTurretEvent"] = "You <color=#ce3f27>cannot</color> damage Turret! You are not the Event Owner and you are not on their team!",
                ["NoDamageNpcEvent"] = "You <color=#ce3f27>cannot</color> damage NPC! You are not the Event Owner and you are not on their team!",
                ["NoEnterEvent"] = "You <color=#ce3f27>cannot</color> enter the Event zone! You are not the Event Owner and you are not on their team!",
                ["YouOwnerEvent"] = "You are now the <color=#738d43>Event Owner</color>!",
                ["ChangeOwnerEventToFriend"] = "You have exited the <color=#ce3f27>Event Zone</color>. The <color=#738d43>Event owner</color> is now <color=#55aaff>{0}</color>",
                ["TimerStartEvent"] = "You <color=#ce3f27>have left</color> the Event zone. You have to return to the Event zone in <color=#55aaff>{0} sec.</color> or you will lose Event Owner status",
                ["AlertTimerEvent"] = "You have <color=#55aaff>{0} sec.</color> to return to the Event Zone and keep Event Owner status",
                ["YouNonOwnerEvent"] = "You <color=#ce3f27>lost</color> the Event Owner status!",
                ["NoCanActionEvent"] = "You <color=#ce3f27>cannot</color> perform this action! You are not the Event Owner and you are not on their team!",
                ["OwnerEndEvent"] = "Event <color=#55aaff>{0}</color> is over. You were the Event Owner. You can play this event no earlier than in <color=#55aaff>{1} hours</color>"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoLootScientist"] = "Вы <color=#ce3f27>не можете</color> ограбить этого NPC, потому что другой игрок нанес по нему большее количество урона!",
                ["NoLootCrateEvent"] = "Вы <color=#ce3f27>не можете</color> ограбить этот ящик, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoHackCrateEvent"] = "Вы <color=#ce3f27>не можете</color> начать взлом этого заблокированного ящика, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoLootScientistEvent"] = "Вы <color=#ce3f27>не можете</color> ограбить этого NPC, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoDamageTankEvent"] = "Вы <color=#ce3f27>не можете</color> нанести урон этому Bradley, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoDamageHelicopterEvent"] = "Вы <color=#ce3f27>не можете</color> нанести урон этому вертолету, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoDamageTurretEvent"] = "Вы <color=#ce3f27>не можете</color> нанести урон этой турели, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoDamageNpcEvent"] = "Вы <color=#ce3f27>не можете</color> нанести урон этому NPC, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["NoEnterEvent"] = "Вы <color=#ce3f27>не можете</color> войти внутрь зоны ивента, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["YouOwnerEvent"] = "Вы <color=#738d43>стали</color> владельцем ивента!",
                ["ChangeOwnerEventToFriend"] = "Вы <color=#ce3f27>вышли</color> из зоны ивента. Владелец ивента <color=#738d43>сменился</color> на игрока <color=#55aaff>{0}</color>",
                ["TimerStartEvent"] = "Вы <color=#ce3f27>вышли</color> из зоны ивента. Чтобы не потерять статус владельца ивента вам необходимо вернуться в зону ивента в течении <color=#55aaff>{0} сек.</color>",
                ["AlertTimerEvent"] = "У вас осталось <color=#55aaff>{0} сек.</color> чтобы вернуться в зону ивента и не потерять статус владельца ивента",
                ["YouNonOwnerEvent"] = "Вы <color=#ce3f27>утратили</color> статус владельца ивента!",
                ["NoCanActionEvent"] = "Вы <color=#ce3f27>не можете</color> выполнить это действие, потому что не являетесь владельцем ивента и не состоите в команде с владельцем ивента!",
                ["OwnerEndEvent"] = "Ивент <color=#55aaff>{0}</color> окончен. Вы были владельцем ивента. Участие в данном ивенте возможно не ранее чем через <color=#55aaff>{1} ч.</color>"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Oxide Hooks
        private static PveMode _ins;

        private void Init() => _ins = this;

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            LoadData();
        }

        private void Unload()
        {
            foreach (KeyValuePair<ulong, ControllerScientist> dic in _scientists) UnityEngine.Object.Destroy(dic.Value);
            foreach (ControllerEvent controllerEvent in _events) UnityEngine.Object.Destroy(controllerEvent.gameObject);
            _ins = null;
        }
        #endregion Oxide Hooks

        #region Team
        [PluginReference] private readonly Plugin Friends, Clans;

        private bool IsTeam(BasePlayer player, ulong targetId)
        {
            if (player == null || targetId == 0) return false;
            if (player.userID == targetId) return true;
            if (player.currentTeam != 0)
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (playerTeam == null) return false;
                if (playerTeam.members.Contains(targetId)) return true;
            }
            if (plugins.Exists("Friends") && (bool)Friends.Call("AreFriends", player.userID, targetId)) return true;
            if (plugins.Exists("Clans") && Clans.Author == "k1lly0u" && (bool)Clans.Call("IsMemberOrAlly", player.UserIDString, targetId.ToString())) return true;
            return false;
        }

        private bool IsTeam(ulong playerId, ulong targetId)
        {
            if (playerId == 0 || targetId == 0) return false;
            if (playerId == targetId) return true;
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(playerId);
            if (playerTeam != null && playerTeam.members.Contains(targetId)) return true;
            if (plugins.Exists("Friends") && (bool)Friends.Call("AreFriends", playerId, targetId)) return true;
            if (plugins.Exists("Clans") && Clans.Author == "k1lly0u" && (bool)Clans.Call("IsMemberOrAlly", playerId.ToString(), targetId.ToString())) return true;
            return false;
        }
        #endregion Team

        #region Scientists
        private void ScientistAddPveMode(ScientistNPC npc)
        {
            if (npc == null || npc.net == null) return;
            _scientists.Add(npc.net.ID.Value, npc.gameObject.AddComponent<ControllerScientist>());
        }

        private void ScientistRemovePveMode(ScientistNPC npc)
        {
            if (npc == null || npc.net == null) return;

            ulong id = npc.net.ID.Value;

            ControllerScientist controllerScientist = null;
            if (_scientists.TryGetValue(id, out controllerScientist))
            {
                _scientists.Remove(id);
                UnityEngine.Object.Destroy(controllerScientist);
            }
        }

        private void CrateAddScientistPveMode(ulong crateId, ulong scientistId)
        {
            if (crateId == 0 || scientistId == 0) return;
            ControllerScientist controllerScientist = null;
            if (_scientists.TryGetValue(scientistId, out controllerScientist)) controllerScientist.CrateId = crateId;
        }

        private readonly Dictionary<ulong, ControllerScientist> _scientists = new Dictionary<ulong, ControllerScientist>();

        internal class ControllerScientist : FacepunchBehaviour
        {
            private int _timeLastDamage = 0;

            internal ulong CrateId = 0;

            internal Dictionary<ulong, float> Players = new Dictionary<ulong, float>();

            private void OnDestroy() => CancelInvoke(IncrementTime);

            internal void AddDamage(BasePlayer attacker, float damage)
            {
                if (Players.ContainsKey(attacker.userID)) Players[attacker.userID] += damage;
                else Players.Add(attacker.userID, damage);
                if (_timeLastDamage == 0) InvokeRepeating(IncrementTime, 1f, 1f);
                _timeLastDamage = _ins._config.TimeLastDamage;
            }

            private void IncrementTime()
            {
                _timeLastDamage--;
                if (_timeLastDamage == 0)
                {
                    Players.Clear();
                    CancelInvoke(IncrementTime);
                }
            }

            internal ulong GetWinner => Players.Max(s => s.Value).Key;
        }
        #endregion Scientists

        #region Events
        internal class ScaleDamageConfig { public string Type; public float Scale; }

        internal class EventConfig
        {
            public float Damage { get; set; }
            public HashSet<ScaleDamageConfig> ScaleDamage { get; set; }
            public bool LootCrate { get; set; }
            public bool HackCrate { get; set; }
            public bool LootNpc { get; set; }
            public bool DamageNpc { get; set; }
            public bool DamageTank { get; set; }
            public bool DamageHelicopter { get; set; }
            public bool DamageTurret { get; set; }
            public bool TargetNpc { get; set; }
            public bool TargetTank { get; set; }
            public bool TargetHelicopter { get; set; }
            public bool TargetTurret { get; set; }
            public bool CanEnter { get; set; }
            public bool CanEnterCooldownPlayer { get; set; }
            public int TimeExitOwner { get; set; }
            public int AlertTime { get; set; }
            public bool RestoreUponDeath { get; set; }
            public double CooldownOwner { get; set; }
            public int Darkening { get; set; }
        }

        private void EventAddPveMode(string shortname, JObject configJson, Vector3 position, float radius, HashSet<ulong> crates, HashSet<ulong> npc, HashSet<ulong> tanks, HashSet<ulong> helicopters, HashSet<ulong> owners, BasePlayer owner = null)
        {
            ControllerEvent controllerEvent = new GameObject().AddComponent<ControllerEvent>();
            controllerEvent.transform.position = position;
            controllerEvent.ShortName = shortname;
            controllerEvent.Config = configJson.ToObject<EventConfig>();
            controllerEvent.Radius = radius;
            controllerEvent.Crates = crates;
            controllerEvent.Npc = npc;
            controllerEvent.Tanks = tanks;
            controllerEvent.Helicopters = helicopters;
            controllerEvent.Owners = owners;
            if (owner != null) controllerEvent.SetOwner(owner);
            controllerEvent.InitSphere();
            _events.Add(controllerEvent);
            LogToFile("CreateZone", $"[{DateTime.Now.ToShortTimeString()}] The zone {shortname} has been created at {position}", _ins);
        }

        private void EventRemovePveMode(string shortname, bool addCooldownOwners = true)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            if (addCooldownOwners)
            {
                foreach (ulong id in controllerEvent.Owners)
                {
                    PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == id);
                    if (playerData == null) _playersData.Add(new PlayerData { SteamId = id, LastTime = new Dictionary<string, double> { [shortname] = CurrentTime } });
                    else
                    {
                        if (playerData.LastTime.ContainsKey(shortname)) playerData.LastTime[shortname] = CurrentTime;
                        else playerData.LastTime.Add(shortname, CurrentTime);
                    }
                    BasePlayer player = BasePlayer.FindByID(id);
                    if (player != null) PrintToChat(player, GetMessage("OwnerEndEvent", player.UserIDString, shortname, (int)(controllerEvent.Config.CooldownOwner / 3600)));
                }
            }
            _events.Remove(controllerEvent);
            UnityEngine.Object.Destroy(controllerEvent.gameObject);
            LogToFile("DestroyZone", $"[{DateTime.Now.ToShortTimeString()}] The zone {shortname} has been destroyed", _ins);
            SaveData();
        }

        private void EventAddCooldown(string shortname, HashSet<ulong> owners, double cooldown)
        {
            foreach (ulong id in owners)
            {
                PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == id);
                if (playerData == null) _playersData.Add(new PlayerData { SteamId = id, LastTime = new Dictionary<string, double> { [shortname] = CurrentTime } });
                else
                {
                    if (playerData.LastTime.ContainsKey(shortname)) playerData.LastTime[shortname] = CurrentTime;
                    else playerData.LastTime.Add(shortname, CurrentTime);
                }
                BasePlayer player = BasePlayer.FindByID(id);
                if (player != null) PrintToChat(player, GetMessage("OwnerEndEvent", player.UserIDString, shortname, (int)(cooldown / 3600)));
            }
            SaveData();
        }

        private void EventAddCrates(string shortname, HashSet<ulong> crates)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            foreach (ulong id in crates) if (!controllerEvent.Crates.Contains(id)) controllerEvent.Crates.Add(id);
        }

        private void EventAddScientists(string shortname, HashSet<ulong> npc)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            foreach (ulong id in npc) if (!controllerEvent.Npc.Contains(id)) controllerEvent.Npc.Add(id);
        }

        private void EventAddTanks(string shortname, HashSet<ulong> tanks)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            foreach (ulong id in tanks) if (!controllerEvent.Tanks.Contains(id)) controllerEvent.Tanks.Add(id);
        }

        private void EventAddHelicopters(string shortname, HashSet<ulong> helicopters)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            foreach (ulong id in helicopters) if (!controllerEvent.Helicopters.Contains(id)) controllerEvent.Helicopters.Add(id);
        }

        private void EventAddTurrets(string shortname, HashSet<ulong> turrets)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            foreach (ulong id in turrets) if (!controllerEvent.Turrets.Contains(id)) controllerEvent.Turrets.Add(id);
        }

        private void SetEventOwner(string shortname, ulong owner)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return;
            controllerEvent.Owner = owner;
        }

        private const int TargetLayers = ~(1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);

        private readonly HashSet<ControllerEvent> _events = new HashSet<ControllerEvent>();

        internal class ControllerEvent : FacepunchBehaviour
        {
            internal string ShortName;

            internal EventConfig Config = null;

            internal float Radius = 0f;

            internal HashSet<ulong> Crates = null;
            internal HashSet<ulong> Backpacks = new HashSet<ulong>();

            internal HashSet<ulong> Npc = null;
            internal HashSet<ulong> Tanks = null;
            internal HashSet<ulong> Helicopters = null;
            internal HashSet<ulong> Turrets = new HashSet<ulong>();

            internal Dictionary<ulong, float> Players = new Dictionary<ulong, float>();
            internal ulong Owner { get; set; } = 0;
            private int _timerExitOwner = 0;
            internal HashSet<ulong> Owners = null;

            private SphereCollider _sphereCollider = null;
            internal HashSet<BasePlayer> InsidePlayers = new HashSet<BasePlayer>();

            private readonly HashSet<SphereEntity> _spheres = new HashSet<SphereEntity>();

            private void OnDestroy()
            {
                CancelInvoke(IncrementTime);
                if (_sphereCollider != null) Destroy(_sphereCollider);
                foreach (SphereEntity sphere in _spheres) if (sphere.IsExists()) sphere.Kill();
            }

            internal void InitSphere()
            {
                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = Radius;
                CreateDome();
            }

            private void CreateDome()
            {
                if (Config.Darkening == 0) return;
                for (int i = 0; i < Config.Darkening; i++)
                {
                    SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", transform.position) as SphereEntity;
                    sphere.currentRadius = Radius * 2;
                    sphere.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    _spheres.Add(sphere);
                }
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    if (player.IsAdmin && _ins._config.IgnoreAdmin) return;
                    if (!Config.CanEnterCooldownPlayer && !_ins.CanTimeOwner(ShortName, player.userID, Config.CooldownOwner)) KickOutPlayer(player);
                    if (_ins._config.NoEnterAnotherOwner && _ins._events.Any(x => x.ShortName != ShortName && x.Owners.Contains(player.userID))) KickOutPlayer(player);
                    if (Owner == 0) InsidePlayers.Add(player);
                    else
                    {
                        if (Config.CanEnter || _ins.IsTeam(player, Owner))
                        {
                            InsidePlayers.Add(player);
                            if (_timerExitOwner > 0 && _ins.IsTeam(player, Owner) && _ins.CanTimeOwner(ShortName, player.userID, Config.CooldownOwner))
                            {
                                CancelInvoke(IncrementTime);
                                _timerExitOwner = 0;
                                if (Owner != player.userID) SetOwner(player);
                            }
                        }
                        else KickOutPlayer(player);
                    }
                }
                else CheckJetpack(other);
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer()) ExitPlayer(player);
            }

            internal void SetOwner(BasePlayer player)
            {
                if (!player.IsPlayer()) return;
                Owner = player.userID;
                if (!Owners.Contains(player.userID)) Owners.Add(player.userID);
                Interface.Oxide.CallHook("SetOwnerPveMode", ShortName, player);
                _ins.PrintToChat(player, _ins.GetMessage("YouOwnerEvent", player.UserIDString));
                _ins.LogToFile("SetOwner", $"[{DateTime.Now.ToShortTimeString()}] {player.displayName} [{player.userID}] became owner of zone {ShortName}", _ins);
            }

            internal void ExitPlayer(BasePlayer player)
            {
                if (InsidePlayers.Contains(player)) InsidePlayers.Remove(player);
                if (player.userID == Owner)
                {
                    BasePlayer friend = InsidePlayers.FirstOrDefault(x => _ins.IsTeam(x, Owner) && _ins.CanTimeOwner(ShortName, x.userID, Config.CooldownOwner));
                    if (friend != null)
                    {
                        _ins.PrintToChat(player, _ins.GetMessage("ChangeOwnerEventToFriend", player.UserIDString, friend.displayName));
                        SetOwner(friend);
                    }
                    else
                    {
                        _timerExitOwner = Config.TimeExitOwner;
                        InvokeRepeating(IncrementTime, 1f, 1f);
                        _ins.PrintToChat(player, _ins.GetMessage("TimerStartEvent", player.UserIDString, _timerExitOwner));
                    }
                }
            }

            private void IncrementTime()
            {
                _timerExitOwner--;
                if (Config.AlertTime > 0 && _timerExitOwner == Config.AlertTime)
                {
                    BasePlayer player = BasePlayer.FindByID(Owner);
                    if (player != null) _ins.PrintToChat(player, _ins.GetMessage("AlertTimerEvent", player.UserIDString, Config.AlertTime));
                }
                if (_timerExitOwner == 0)
                {
                    CancelInvoke(IncrementTime);
                    BasePlayer player = BasePlayer.FindByID(Owner);
                    if (player != null) _ins.PrintToChat(player, _ins.GetMessage("YouNonOwnerEvent", player.UserIDString));
                    Owner = 0;
                    Interface.Oxide.CallHook("ClearOwnerPveMode", ShortName);
                }
            }

            internal void AddDamage(BasePlayer player, float damage)
            {
                if (_ins._config.NoEnterAnotherOwner && _ins._events.Any(x => x.ShortName != ShortName && x.Owners.Contains(player.userID))) return;

                if (Players.ContainsKey(player.userID)) Players[player.userID] += damage;
                else Players.Add(player.userID, damage);

                if (!_ins.CanTimeOwner(ShortName, player.userID, Config.CooldownOwner)) return;

                if (Players[player.userID] >= Config.Damage)
                {
                    SetOwner(player);
                    Players.Clear();
                    if (!Config.CanEnter)
                    {
                        foreach (BasePlayer insidePlayer in InsidePlayers.ToHashSet())
                        {
                            if (_ins.IsTeam(insidePlayer, Owner)) continue;
                            else KickOutPlayer(insidePlayer);
                        }
                    }
                }
            }

            internal void KickOutPlayer(BasePlayer player)
            {
                if (player.isMounted)
                {
                    BaseVehicle vehicle = player.GetMounted().VehicleParent();
                    if (vehicle != null)
                    {
                        vehicle.transform.rotation = Quaternion.Euler(vehicle.transform.eulerAngles.x, vehicle.transform.eulerAngles.y - 180f, vehicle.transform.eulerAngles.z);
                        vehicle.rigidBody.velocity *= -2f;
                        return;
                    }
                    else player.DismountObject();
                }
                Vector3 position = transform.position + ((player.transform.position.XZ3D() - transform.position.XZ3D()).normalized * (Radius + 10f));
                position.y = 500f;
                RaycastHit raycastHit;
                if (Physics.Raycast(position, Vector3.down, out raycastHit, 500f, TargetLayers, QueryTriggerInteraction.Ignore)) position.y = raycastHit.point.y;
                else position.y = TerrainMeta.HeightMap.GetHeight(position);
                player.MovePosition(position);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
                player.SendNetworkUpdateImmediate();
                _ins.PrintToChat(player, _ins.GetMessage("NoEnterEvent", player.UserIDString));
            }

            private void CheckJetpack(Collider collider)
            {
                if (collider == null || collider is CapsuleCollider == false) return;

                DroppedItemContainer droppedItemContainer = collider.GetComponentInParent<DroppedItemContainer>();
                if (!droppedItemContainer.IsExists()) return;

                BaseMountable baseMountable = droppedItemContainer.GetComponentInChildren<BaseMountable>();
                if (!baseMountable.IsExists()) return;

                BasePlayer player = baseMountable._mounted;
                if (player == null || !player.userID.IsSteamId()) return;

                if (player.IsAdmin && _ins._config.IgnoreAdmin) return;

                if (!Config.CanEnterCooldownPlayer && !_ins.CanTimeOwner(ShortName, player.userID, Config.CooldownOwner)) KickJetpack(droppedItemContainer, player);

                if (_ins._config.NoEnterAnotherOwner && _ins._events.Any(x => x.ShortName != ShortName && x.Owners.Contains(player.userID))) KickJetpack(droppedItemContainer, player);

                if (Owner != 0 && !Config.CanEnter && !_ins.IsTeam(player, Owner)) KickJetpack(droppedItemContainer, player);
            }

            private void KickJetpack(DroppedItemContainer droppedItemContainer, BasePlayer player)
            {
                droppedItemContainer.Kill();
                player.DismountObject();
                KickOutPlayer(player);
            }
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!player.IsPlayer()) return;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.InsidePlayers.Contains(player));
            if (controllerEvent != null) controllerEvent.ExitPlayer(player);
        }

        private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (!player.IsPlayer()) return;
            if (player.IsAdmin && _config.IgnoreAdmin) return;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && Vector3.Distance(x.transform.position, player.transform.position) < x.Radius);
            if (controllerEvent != null)
            {
                if (!controllerEvent.Config.CanEnterCooldownPlayer && !CanTimeOwner(controllerEvent.ShortName, player.userID, controllerEvent.Config.CooldownOwner)) controllerEvent.KickOutPlayer(player);
                if (_config.NoEnterAnotherOwner && _events.Any(x => x.ShortName != controllerEvent.ShortName && x.Owners.Contains(player.userID))) controllerEvent.KickOutPlayer(player);
                if (!controllerEvent.Config.CanEnter && !IsTeam(player, controllerEvent.Owner)) controllerEvent.KickOutPlayer(player);
            }
        }

        private object OnRestoreUponDeath(BasePlayer player)
        {
            if (!player.IsPlayer()) return null;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => Vector3.Distance(x.transform.position, player.transform.position) < x.Radius);
            if (controllerEvent == null) return null;
            else
            {
                if (controllerEvent.Config.RestoreUponDeath) return false;
                else return null;
            }
        }

        private object CanActionEvent(string shortname, BasePlayer player)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.ShortName == shortname);
            if (controllerEvent != null)
            {
                if (IsTeam(player, controllerEvent.Owner)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoCanActionEvent", player.UserIDString));
                    return false;
                }
            }
            return null;
        }

        private ControllerEvent GetControllerEventAtPosition(Vector3 pos) => _events.FirstOrDefault(x => Vector3.Distance(pos, x.transform.position) <= x.Radius);
        #endregion Events

        #region Time
        public class PlayerData
        {
            public ulong SteamId { get; set; }
            public Dictionary<string, double> LastTime { get; set; }
        }

        private HashSet<PlayerData> _playersData = new HashSet<PlayerData>();

        private void LoadData() => _playersData = Interface.Oxide.DataFileSystem.ReadObject<HashSet<PlayerData>>(Name);

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _playersData);

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private static double CurrentTime => DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        private bool CanTimeOwner(string nameEvent, ulong steamId, double cooldown)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == steamId);
            if (playerData == null) return true;
            if (playerData.LastTime.ContainsKey(nameEvent))
            {
                if (playerData.LastTime[nameEvent] + cooldown < CurrentTime) return true;
                else return false;
            }
            else return true;
        }

        [ConsoleCommand("ClearTimePveMode")]
        private void ConsoleClearTimePveMode(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null || arg.Args == null || arg.Args.Length != 1) return;
            ulong id = Convert.ToUInt64(arg.Args[0]);
            PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == id);
            if (playerData != null)
            {
                playerData.LastTime.Clear();
                Puts($"You have cleared the time data from player {id}");
            }
            else Puts($"Player {id} not found in the plugin database");
        }

        [ChatCommand("EventsTime")]
        private void ChaEventsTime(BasePlayer player)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == player.userID);
            if (playerData == null || playerData.LastTime.Count == 0) return;
            string message = "List of events in which you were a participant last time:";
            foreach (KeyValuePair<string, double> dic in playerData.LastTime) message += $"\n- {dic.Key} = {GetTimeFormat(CurrentTime - dic.Value)}";
            PrintToChat(player, message);
        }

        private static string GetTimeFormat(double seconds)
        {
            int integer = (int)seconds;
            if (seconds <= 60) return $"{integer} sec.";
            else if (seconds <= 3600)
            {
                int sec = integer % 60;
                int min = (integer - sec) / 60;
                return $"{min} min. {sec} sec.";
            }
            else
            {
                int hour = (int)(seconds / 3600);
                seconds -= hour * 3600;
                integer = (int)seconds;
                int sec = integer % 60;
                int min = (integer - sec) % 60;
                return $"{hour} h. {min} min. {sec} sec.";
            }
        }
        #endregion Time

        #region Loot
        private readonly Dictionary<ulong, ulong> _canLootScientist = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, ulong> _canLootCrateScientist = new Dictionary<ulong, ulong>();

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || container.net == null || !player.IsPlayer()) return null;

            ulong id = container.net.ID.Value;

            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Crates.Contains(id));
            if (controllerEvent != null)
            {
                if (controllerEvent.Config.LootCrate || IsTeam(player, controllerEvent.Owner)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoLootCrateEvent", player.UserIDString));
                    return true;
                }
            }

            ulong ownerId = 0;
            if (_canLootCrateScientist.TryGetValue(id, out ownerId))
            {
                if (IsTeam(player, ownerId)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoLootScientist", player.UserIDString));
                    return true;
                }
            }

            return null;
        }

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (crate == null || crate.net == null || !player.IsPlayer()) return null;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Crates.Contains(crate.net.ID.Value));
            if (controllerEvent != null)
            {
                if (controllerEvent.Config.HackCrate || IsTeam(player, controllerEvent.Owner)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoHackCrateEvent", player.UserIDString));
                    return true;
                }
            }
            else return null;
        }

        private object CanLootEntity(BasePlayer player, NPCPlayerCorpse corpse)
        {
            if (corpse == null || !player.IsPlayer()) return null;
            ulong id = corpse.playerSteamID;
            if (id == 0) return null;
            return CanLootScientist(player, id);
        }

        private object CanLootEntity(BasePlayer player, DroppedItemContainer container)
        {
            if (!player.IsPlayer() || container == null || container.ShortPrefabName != "item_drop_backpack") return null;
            ulong id = container.playerSteamID;
            if (id == 0) return null;
            return CanLootScientist(player, id);
        }

        private object CanLootScientist(BasePlayer player, ulong targetId)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Backpacks.Contains(targetId));
            if (controllerEvent != null)
            {
                if (controllerEvent.Config.LootNpc || IsTeam(player, controllerEvent.Owner)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoLootScientistEvent", player.UserIDString));
                    return true;
                }
            }
            ulong ownerId = 0;
            if (_canLootScientist.TryGetValue(targetId, out ownerId))
            {
                if (IsTeam(player, ownerId)) return null;
                else
                {
                    PrintToChat(player, GetMessage("NoLootScientist", player.UserIDString));
                    return true;
                }
            }
            return null;
        }

        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || entity.net == null || corpse == null) return;

            ulong id = entity.net.ID.Value;

            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Npc.Contains(id));
            if (controllerEvent != null)
            {
                controllerEvent.Npc.Remove(id);
                controllerEvent.Backpacks.Add(corpse.playerSteamID);
                return;
            }

            ControllerScientist controllerScientist = null;
            if (_scientists.TryGetValue(id, out controllerScientist))
            {
                ulong playerSteamId = corpse.playerSteamID;
                NextTick(() =>
                {
                    if (controllerScientist.Players.Count != 0)
                    {
                        ulong winner = controllerScientist.GetWinner;
                        _canLootScientist.Add(playerSteamId, winner);
                        if (controllerScientist.CrateId != 0) _canLootCrateScientist.Add(controllerScientist.CrateId, winner);
                    }
                    _scientists.Remove(id);
                    UnityEngine.Object.Destroy(controllerScientist);
                });
                return;
            }
        }

        private object CanCustomAnimalSpawnCorpse(BaseAnimalNPC entity)
        {
            if (entity == null || entity.net == null) return null;
            ulong id = entity.net.ID.Value;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Npc.Contains(id));
            if (controllerEvent != null) controllerEvent.Npc.Remove(id);
            return null;
        }

        private void OnEntityKill(StorageContainer crate)
        {
            if (crate == null || crate.net == null) return;

            ulong id = crate.net.ID.Value;

            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Crates.Contains(id));
            if (controllerEvent != null) controllerEvent.Crates.Remove(id);
            
            if (_canLootCrateScientist.ContainsKey(id)) _canLootCrateScientist.Remove(id);
        }

        private void OnEntityKill(DroppedItemContainer container)
        {
            if (container == null || container.ShortPrefabName != "item_drop_backpack") return;
            ulong id = container.playerSteamID;
            if (id == 0) return;
            if (_canLootScientist.ContainsKey(id)) _canLootScientist.Remove(id);
        }

        private object OnLootLockedEntity(BasePlayer player, NPCPlayerCorpse corpse)
        {
            if (corpse == null || !player.IsPlayer()) return null;
            ulong id = corpse.playerSteamID;
            if (id == 0) return null;
            return CanLootScientist(player, id) == null ? null : (object)false;
        }

        private object OnLootLockedEntity(BasePlayer player, DroppedItemContainer container)
        {
            if (!player.IsPlayer() || container == null || container.ShortPrefabName != "item_drop_backpack") return null;
            ulong id = container.playerSteamID;
            if (id == 0) return null;
            return CanLootScientist(player, id) == null ? null : (object)false;
        }
        #endregion Loot

        #region Damage
        private object OnEventEntityTakeDamage(BaseCombatEntity entity, HitInfo info, bool addDamage = true, bool sendMessage = true)
        {
            if (entity == null || entity.net == null || info == null) return null;

            ulong id = entity.net.ID.Value;

            string type = string.Empty;
            ControllerEvent controllerEvent = null;

            switch (entity)
            {
                case ScientistNPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Npc.Contains(id));
                    type = "NPC";
                    break;
                case BaseAnimalNPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Npc.Contains(id));
                    type = "Animal";
                    break;
                case BradleyAPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Tanks.Contains(id));
                    type = "Bradley";
                    break;
                case PatrolHelicopter _:
                    controllerEvent = _events.FirstOrDefault(x => x.Helicopters.Contains(id));
                    type = "Helicopter";
                    break;
                case AutoTurret _:
                case FlameTurret _:
                case GunTrap _:
                case SamSite _:
                    controllerEvent = _events.FirstOrDefault(x => x.Turrets.Contains(id));
                    type = "Turret";
                    break;
            }

            if (controllerEvent == null) return null;

            BaseEntity attacker = info.Initiator;
            BasePlayer attackerPlayer = info.InitiatorPlayer;
            bool isPlayer = attackerPlayer.IsPlayer();

            if (controllerEvent.Owner == 0)
            {
                if (isPlayer && addDamage)
                {
                    ScaleDamageConfig scaleDamageConfig = controllerEvent.Config.ScaleDamage.FirstOrDefault(x => x.Type == type);
                    controllerEvent.AddDamage(attackerPlayer, info.damageTypes.Total() * scaleDamageConfig?.Scale ?? 0f);
                }
                return null;
            }

            switch (type)
            {
                case "NPC":
                case "Animal":
                    {
                        if (controllerEvent.Config.DamageNpc) return null;
                        break;
                    }
                case "Bradley" when controllerEvent.Config.DamageTank:
                case "Helicopter" when controllerEvent.Config.DamageHelicopter:
                case "Turret" when controllerEvent.Config.DamageTurret:
                    return null;
            }

            if (isPlayer)
            {
                if (IsTeam(attackerPlayer, controllerEvent.Owner)) return null;
                else
                {
                    if (sendMessage)
                    {
                        switch (type)
                        {
                            case "NPC":
                            case "Animal":
                                PrintToChat(attackerPlayer, GetMessage("NoDamageNpcEvent", attackerPlayer.UserIDString));
                                break;
                            case "Bradley":
                                PrintToChat(attackerPlayer, GetMessage("NoDamageTankEvent", attackerPlayer.UserIDString));
                                break;
                            case "Helicopter":
                                PrintToChat(attackerPlayer, GetMessage("NoDamageHelicopterEvent", attackerPlayer.UserIDString));
                                break;
                            case "Turret":
                                PrintToChat(attackerPlayer, GetMessage("NoDamageTurretEvent", attackerPlayer.UserIDString));
                                break;
                        }
                    }
                    return true;
                }
            }
            else
            {
                if (attacker != null && IsTeam(attacker.OwnerID, controllerEvent.Owner)) return null;
                else
                {
                    if (type == "Helicopter" && attacker == null && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Generic) return null;
                    return true;
                }
            }
        }

        private object OnEventInitiatorNullTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info.Initiator != null) return null;
            BaseEntity weaponPrefab = info.WeaponPrefab;
            if (weaponPrefab == null) return null;
            string type = string.Empty;
            if (weaponPrefab.ShortPrefabName == "maincannonshell") type = "Bradley";
            if (weaponPrefab.ShortPrefabName == "rocket_heli" || weaponPrefab.ShortPrefabName == "rocket_heli_napalm") type = "Helicopter";
            if (string.IsNullOrEmpty(type)) return null;
            ControllerEvent controllerEvent = GetControllerEventAtPosition(entity.transform.position);
            if (controllerEvent == null) return null;
            if (type == "Bradley" && controllerEvent.Tanks.Count != 0) return null;
            if (type == "Helicopter" && controllerEvent.Helicopters.Count != 0) return null;
            return true;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || entity.net == null || info == null) return null;

            if (entity is ScientistNPC)
            {
                ScientistNPC npc = entity as ScientistNPC;
                ControllerScientist controllerScientist;
                if (_scientists.TryGetValue(npc.net.ID.Value, out controllerScientist))
                {
                    BasePlayer attacker = info.InitiatorPlayer;
                    if (attacker.IsPlayer()) controllerScientist.AddDamage(attacker, info.damageTypes.Total());
                    return null;
                }
            }

            if (OnEventEntityTakeDamage(entity, info) is bool) return true;

            if (OnEventEntityTarget(info.Initiator, entity) is bool) return true;

            if (OnEventInitiatorNullTakeDamage(entity, info) is bool) return true;

            return null;
        }

        private object OnPlayerAttack(BasePlayer attacker, HitInfo info) => info == null ? null : OnEventEntityTakeDamage(info.HitEntity as PatrolHelicopter, info);

        private object CanEntityTakeDamage(ScientistNPC npc, HitInfo info)
        {
            if (OnEventEntityTakeDamage(npc, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(BaseAnimalNPC animal, HitInfo info)
        {
            if (OnEventEntityTakeDamage(animal, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            if (OnEventEntityTakeDamage(bradley, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(AutoTurret turret, HitInfo info)
        {
            if (OnEventEntityTakeDamage(turret, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(FlameTurret turret, HitInfo info)
        {
            if (OnEventEntityTakeDamage(turret, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(GunTrap turret, HitInfo info)
        {
            if (OnEventEntityTakeDamage(turret, info, false, false) is bool) return false;
            else return null;
        }

        private object CanEntityTakeDamage(SamSite turret, HitInfo info)
        {
            if (OnEventEntityTakeDamage(turret, info, false, false) is bool) return false;
            else return null;
        }

        private void OnEntityKill(BradleyAPC bradley)
        {
            if (bradley == null || bradley.net == null) return;
            ulong id = bradley.net.ID.Value;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Tanks.Contains(id));
            if (controllerEvent != null) controllerEvent.Tanks.Remove(id);
        }

        private void OnEntityKill(PatrolHelicopter heli)
        {
            if (heli == null || heli.net == null) return;
            ulong id = heli.net.ID.Value;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Helicopters.Contains(id));
            if (controllerEvent != null) controllerEvent.Helicopters.Remove(id);
        }

        private void OnEntityKill(AutoTurret turret)
        {
            if (turret == null || turret.net == null) return;
            OnTurretKill(turret.net.ID.Value);
        }

        private void OnEntityKill(FlameTurret turret)
        {
            if (turret == null || turret.net == null) return;
            OnTurretKill(turret.net.ID.Value);
        }

        private void OnEntityKill(GunTrap turret)
        {
            if (turret == null || turret.net == null) return;
            OnTurretKill(turret.net.ID.Value);
        }

        private void OnEntityKill(SamSite turret)
        {
            if (turret == null || turret.net == null) return;
            OnTurretKill(turret.net.ID.Value);
        }

        private void OnTurretKill(ulong id)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Turrets.Contains(id));
            if (controllerEvent != null) controllerEvent.Turrets.Remove(id);
        }
        #endregion Damage

        #region Target
        private object OnEventEntityTarget(BaseNetworkable attacker, BaseEntity target)
        {
            if (attacker == null || attacker.net == null || target == null) return null;

            ulong id = attacker.net.ID.Value;

            string type = string.Empty;
            ControllerEvent controllerEvent = null;

            switch (attacker)
            {
                case ScientistNPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Npc.Contains(id));
                    type = "NPC";
                    break;
                case BaseAnimalNPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Npc.Contains(id));
                    type = "Animal";
                    break;
                case BradleyAPC _:
                    controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Tanks.Contains(id));
                    type = "Bradley";
                    break;
                case AutoTurret _:
                case FlameTurret _:
                case GunTrap _:
                case SamSite _:
                    controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Turrets.Contains(id));
                    type = "Turret";
                    break;
            }

            if (controllerEvent == null) return null;

            switch (type)
            {
                case "NPC":
                case "Animal":
                {
                    if (controllerEvent.Config.TargetNpc) return null;
                    break;
                }
                case "Bradley" when controllerEvent.Config.TargetTank:
                case "Turret" when controllerEvent.Config.TargetTurret:
                    return null;
            }

            if (type == "NPC" && target is ScientistNPC  && target.net != null && controllerEvent.Npc.Contains(target.net.ID.Value)) return null;

            BasePlayer targetPlayer = target as BasePlayer;

            if (targetPlayer.IsPlayer())
            {
                if (IsTeam(targetPlayer, controllerEvent.Owner)) return null;
                else return true;
            }
            else
            {
                if (IsTeam(target.OwnerID, controllerEvent.Owner)) return null;
                else return true;
            }
        }

        private object OnNpcTarget(ScientistNPC attacker, BaseEntity target) => OnEventEntityTarget(attacker, target);

        private object OnNpcTarget(BaseAnimalNPC attacker, BaseEntity target) => OnEventEntityTarget(attacker, target);

        private object OnCustomNpcTarget(ScientistNPC attacker, BasePlayer target)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object OnCustomAnimalTarget(BaseAnimalNPC attacker, BaseEntity target)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object CanBradleyApcTarget(BradleyAPC attacker, BaseEntity target)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object OnTurretTarget(AutoTurret attacker, BaseCombatEntity target) => OnEventEntityTarget(attacker, target);

        private object CanBeTargeted(BasePlayer target, FlameTurret attacker)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object CanBeTargeted(BasePlayer target, GunTrap attacker)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object OnSamSiteTarget(SamSite attacker, BaseEntity target) => OnEventEntityTarget(attacker, target);

        private object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            if (heli == null || heli.helicopterBase == null || heli.helicopterBase.net == null || !player.IsPlayer()) return null;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Helicopters.Contains(heli.helicopterBase.net.ID.Value));
            if (controllerEvent == null || controllerEvent.Config.TargetHelicopter) return null;
            if (IsTeam(player, controllerEvent.Owner)) return null;
            else return false;
        }

        private object CanHelicopterStrafeTarget(PatrolHelicopterAI heli, BasePlayer player) => CanHelicopterTarget(heli, player);

        private object OnHelicopterTarget(HelicopterTurret turret, BasePlayer player)
        {
            if (turret == null || turret._heliAI == null || turret._heliAI.helicopterBase == null || turret._heliAI.helicopterBase.net == null || !player.IsPlayer()) return null;
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.Owner != 0 && x.Helicopters.Contains(turret._heliAI.helicopterBase.net.ID.Value));
            if (controllerEvent == null || controllerEvent.Config.TargetHelicopter) return null;
            if (IsTeam(player, controllerEvent.Owner)) return null;
            else return true;
        }

        private object CanEntityBeTargeted(BaseEntity target, SamSite attacker)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }

        private object CanEntityBeTargeted(BasePlayer target, BaseEntity attacker)
        {
            if (OnEventEntityTarget(attacker, target) is bool) return false;
            else return null;
        }
        #endregion Target

        #region API
        private bool IsPlayerInEventZone(ulong id)
        {
            foreach (ControllerEvent controller in _events) if (controller.InsidePlayers.Any(x => x.userID == id)) return true;
            return false;
        }

        private HashSet<string> GetEventsPlayer(ulong id)
        {
            HashSet<string> result = new HashSet<string>();
            foreach (ControllerEvent controller in _events) if (controller.InsidePlayers.Any(x => x.userID == id)) result.Add(controller.ShortName);
            return result;
        }

        private Dictionary<string, double> GetTimesPlayer(ulong id)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.SteamId == id);
            if (playerData == null) return null;
            Dictionary<string, double> result = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> dic in playerData.LastTime) result.Add(dic.Key, CurrentTime - dic.Value);
            return result;
        }

        private ulong GetEventOwner(string shortname)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return 0;
            else return controllerEvent.Owner;
        }

        private HashSet<ulong> GetEventOwners(string shortname)
        {
            ControllerEvent controllerEvent = _events.FirstOrDefault(x => x.ShortName == shortname);
            if (controllerEvent == null) return null;
            else return controllerEvent.Owners;
        }
        #endregion API
    }
}

namespace Oxide.Plugins.PveModeExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }

        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;
    }
}