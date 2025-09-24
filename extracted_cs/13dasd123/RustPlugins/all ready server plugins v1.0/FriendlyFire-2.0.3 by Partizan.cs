using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FriendlyFire", "Partizan", "2.0.3")]
    public class FriendlyFire : RustPlugin
    {
        private readonly HashSet<ulong> bypass = new HashSet<ulong>();
        private readonly Dictionary<string, int> times = new Dictionary<string, int>();
        private Dictionary<ulong, bool> ffData;
        private ConfigData configData;

        [PluginReference]
        private Plugin Friends;
        [PluginReference]
        private Plugin Clans;

        class ConfigData
        {
            public bool FriendlyFire { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                FriendlyFire = false
            };
            Config.WriteObject(config, true);
        }

        private string _(string msgId, BasePlayer player, params object[] args)
        {
            return string.Format(lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        private bool FriendlyFireEnabled(ulong userId)
        {
            if (bypass.Contains(userId)) return true;
            bool enabled;
            return ffData.TryGetValue(userId, out enabled) && enabled;
        }

        private void EnableFriendlyFire(ulong userId)
        {
            ffData[userId] = true;
            Interface.Oxide.DataFileSystem.WriteObject("FriendlyFire", ffData);
        }

        private void DisableFriendlyFire(ulong userId)
        {
            ffData[userId] = false;
            Interface.Oxide.DataFileSystem.WriteObject("FriendlyFire", ffData);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CannotHurt", "{0} ваш друг, огонь по друзьям выключен. Чтобы вкл, используйте: <color=\"#33cccc\">/ff on</color>"},
                {"Usage", "Используйте: <color=\"#33cccc\">/ff [on|off]</color>"},
                {"FFFriends", "Дружественный огонь {0} для ваших друзей:"},
                {"FFNotAvailable", "Дружественный огонь не доступен."},
                {"FFEnabled", "<color=red>вкл</color>"},
                {"FFDisabled", "<color=green>выкл</color>"},
                {"NoFriends", "У вас нет друзей."},
                {"FFToggle", "Чтобы переключить дружественный огонь включен, либо выключен, используйте: <color=\"#33cccc\">/ff on|off</color>"},
                {"FFAlready", "Дружественный огонь для ваших друзей уже {0}."},
                {"FFChanged", "У вас {0} огонь по вашим друзьям."},
                {"FFHelp1", "<color=\"#33cccc\">/ff</color> - Отображает ваше дружеское состояние."},
                {"FFHelp2", "<color=\"#33cccc\">/ff on|off</color> - Включение дружественного огня <color=red>on</color> или <color=green>off</color> выключение."}
            }, this);
            configData = Config.ReadObject<ConfigData>();
            try
            {
                ffData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("FriendlyFire");
            }
            catch
            {
                ffData = new Dictionary<ulong, bool>();
            }
        }

        private object OnAttackInternal(BasePlayer attacker, BasePlayer victim, HitInfo hit)
        {
            if (configData.FriendlyFire || attacker == victim || FriendlyFireEnabled(attacker.userID)) return null;
            var victimId = victim.userID;
            var attackerId = attacker.userID;
            var hasFriend = (bool)(Friends?.CallHook("HasFriend", attackerId, victimId) ?? false);
            if (!hasFriend)
            {
                hasFriend = (bool)(Clans?.CallHook("HasFriend", attacker.UserIDString, victim.UserIDString) ?? false);
                if (!hasFriend) return null;
            }
            var now = Facepunch.Math.Epoch.Current;
            int time;
            var key = $"{attackerId}-{victimId}";
            if (!times.TryGetValue(key, out time) || time < now)
            {
                PrintToChat(attacker, _("CannotHurt", attacker, victim.displayName));
                times[key] = now + 10;
            }
            hit.damageTypes = new DamageTypeList();
            hit.DidHit = false;
            hit.HitEntity = null;
            hit.Initiator = null;
            hit.DoHitEffects = false;
            hit.HitMaterial = 0;
            return false;
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (hitInfo?.HitEntity is BasePlayer)
                OnAttackInternal(attacker, (BasePlayer)hitInfo.HitEntity, hitInfo);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer && hitInfo?.Initiator is BasePlayer)
                OnAttackInternal((BasePlayer)hitInfo.Initiator, (BasePlayer)entity, hitInfo);
        }

        [ConsoleCommand("ff.toggle")]
        private void ccmdFFToggle(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null && (arg.Player() == null || !arg.Player().IsAdmin())) return;
            configData.FriendlyFire = !configData.FriendlyFire;
            Config.WriteObject(configData, true);
        }

        [ChatCommand("ff")]
        private void cmdChatFF(BasePlayer player, string command, string[] args)
        {
            if (configData.FriendlyFire)
            {
                SendReply(player, _("FFNotAvailable", player));
                return;
            }
            if (args.Length > 1)
            {
                SendReply(player, _("Usage", player));
                return;
            }
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(_("FFFriends", player, _(FriendlyFireEnabled(player.userID) ? "FFEnabled" : "FFDisabled", player)));
                var friendList = (string[])Friends?.CallHook("GetFriendList", player.userID);
                if (friendList != null && friendList.Length > 0)
                    sb.AppendLine(string.Join(", ", friendList));
                else
                    sb.AppendLine(_("NoFriends", player));
                sb.Append(_("FFToggle", player));
                SendReply(player, sb.ToString());
                return;
            }
            switch (args[0])
            {
                case "1":
                case "on":
                case "true":
                case "yes":
                    if (FriendlyFireEnabled(player.userID))
                        SendReply(player, _("FFAlready", player, _("FFEnabled", player)));
                    else
                    {
                        EnableFriendlyFire(player.userID);
                        SendReply(player, _("FFChanged", player, _("FFEnabled", player)));
                    }
                    break;
                case "0":
                case "off":
                case "false":
                case "no":
                    if (!FriendlyFireEnabled(player.userID))
                        SendReply(player, _("FFAlready", player, _("FFDisabled", player)));
                    else
                    {
                        DisableFriendlyFire(player.userID);
                        SendReply(player, _("FFChanged", player, _("FFDisabled", player)));
                    }
                    break;
                default:
                    SendReply(player, _("Usage", player));
                    break;
            }
        }

        #region API Methods

        private bool EnableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string)userId);
            return bypass.Add((ulong)userId);
        }

        private bool DisableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string)userId);
            return bypass.Remove((ulong)userId);
        }

        #endregion
    }
}