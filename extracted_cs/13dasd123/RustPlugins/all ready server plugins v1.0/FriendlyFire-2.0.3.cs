using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info(FF, "Nogrod", "2.0.3")]
    public class FriendlyFire : RustPlugin
    {
        private const string FF = "FriendlyFire";
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
            Interface.Oxide.DataFileSystem.WriteObject(FF, ffData);
        }

        private void DisableFriendlyFire(ulong userId)
        {
            ffData[userId] = false;
            Interface.Oxide.DataFileSystem.WriteObject(FF, ffData);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CannotHurt", "{0}, хэй, это ваш друг. Включить урон по друзьям : <color=\"#33cccc\">/ff on</color>"},
                {"Usage", "Что-бы включить или выключить урон по друзьям ввидите <color=\"#33cccc\">/ff [on|off]</color>"},
                {"FFFriends", "Урон по друзьям: {0}"},
                {"FFNotAvailable", "Урон по друзьям недоступен."},
                {"FFEnabled", "<color=red>Активен</color>"},
                {"FFDisabled", "<color=green>Неактивен</color>"},
                {"NoFriends", "У вас нет друзей."},
                {"FFToggle", "Что-бы включить или выключить урон по друзьям ввидите <color=\"#33cccc\">/ff [on|off]</color>"},
                {"FFAlready", "Дружеский урон {0}."},
                {"FFChanged", "У вас {0} урон по друзьям"},
                {"FFHelp1", "<color=\"#33cccc\">/ff</color> - Посмотреть статус урона по друзьям"},
                {"FFHelp2", "<color=\"#33cccc\">/ff on|off</color> - Что бы включить урон по друзьям ввидите <color=red>on</color> выключить <color=green>off</color>"}
            }, this);
            configData = Config.ReadObject<ConfigData>();
            try
            {
                ffData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(FF);
            }
            catch
            {
                ffData = new Dictionary<ulong, bool>();
            }
        }

        private object OnAttackInternal(BasePlayer attacker, BasePlayer victim, HitInfo hit)
        {
            if (configData.FriendlyFire || attacker == victim || FriendlyFireEnabled(attacker.userID))
                return null;
            var victimId = victim.userID;
            var attackerId = attacker.userID;
            var hasFriend = (bool)(Friends?.CallHook("HasFriend", attackerId, victimId) ?? false);
            if (!hasFriend)
            {
                hasFriend = (bool)(Clans?.CallHook("HasFriend", attacker.UserIDString, victim.UserIDString) ?? false);
                if (!hasFriend)
                    return null;
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
                OnAttackInternal(attacker, (BasePlayer) hitInfo.HitEntity, hitInfo);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer && hitInfo?.Initiator is BasePlayer)
                OnAttackInternal((BasePlayer) hitInfo.Initiator, (BasePlayer) entity, hitInfo);
        }

        [ConsoleCommand("ff.toggle")]
        private void ccmdFFToggle(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && (arg.Player() == null || !arg.Player().IsAdmin)) return;
            configData.FriendlyFire = !configData.FriendlyFire;
            Config.WriteObject(configData, true);
        }

        [ChatCommand("ff")]
        private void cmdChatFF(BasePlayer player, string command, string[] args)
        {
            if (configData.FriendlyFire)
            {
                ReplyWithHelper(player, _("FFNotAvailable", player));
                return;
            }
            if (args.Length > 1)
            {
                ReplyWithHelper(player, _("Usage", player));
                return;
            }
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(_("FFFriends", player, _(FriendlyFireEnabled(player.userID) ? "FFEnabled" : "FFDisabled", player)));
                var friendList = (string[]) Friends?.CallHook("GetFriendList", player.userID);
                if (friendList != null && friendList.Length > 0)
                    sb.AppendLine(string.Join(", ", friendList));
                else
                    sb.AppendLine(_("NoFriends", player));
                sb.Append(_("FFToggle", player));
                ReplyWithHelper(player, sb.ToString());
                return;
            }
            switch (args[0])
            {
                case "1":
                case "on":
                case "true":
                case "yes":
                    if (FriendlyFireEnabled(player.userID))
                        ReplyWithHelper(player, _("FFAlready", player, _("FFEnabled", player)));
                    else
                    {
                        EnableFriendlyFire(player.userID);
                        ReplyWithHelper(player, _("FFChanged", player, _("FFEnabled", player)));
                    }
                    break;
                case "0":
                case "off":
                case "false":
                case "no":
                    if (!FriendlyFireEnabled(player.userID))
                        ReplyWithHelper(player, _("FFAlready", player, _("FFDisabled", player)));
                    else
                    {
                        DisableFriendlyFire(player.userID);
                        ReplyWithHelper(player, _("FFChanged", player, _("FFDisabled", player)));
                    }
                    break;
                default:
                    ReplyWithHelper(player, _("Usage", player));
                    break;
            }
        }

        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append("  ").AppendLine(_("FFHelp1", player));
            sb.Append("  ").Append(_("FFHelp2", player));
            player.ChatMessage(sb.ToString());
        }

        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null, ulong senderID = 76561198854646370, string header = "PonyHelper")
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add", new object[2]
            {
                senderID,
                string.Format("<size=16><color=#3B85F5B1>{0}</color>:</size>\n{1}", header, message)
            });
        }

        #region API Methods

        private bool EnableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string) userId);
            return bypass.Add((ulong) userId);
        }

        private bool DisableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string) userId);
            return bypass.Remove((ulong) userId);
        }

        #endregion
    }
}
