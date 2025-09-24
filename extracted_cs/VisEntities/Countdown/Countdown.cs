/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Countdown", "VisEntities", "1.0.0")]
    [Description("Allows starting timed countdowns with custom messages.")]
    public class Countdown : RustPlugin
    {
        #region Fields

        private static Countdown _plugin;
        private Timer _countdownTimer;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            if (_countdownTimer != null)
                _countdownTimer.Destroy();

            _plugin = null;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "countdown.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions

        #region Commands

        private static class Cmd
        {
            /// <summary>
            /// countdown <interval> <repeats> <message>
            /// </summary>
            public const string COUNTDOWN = "countdown";
        }

        [ChatCommand(Cmd.COUNTDOWN)]
        private void cmdCountdown(BasePlayer player, string cmd, string[] args)
        {
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
            {
                SendMessage(player, Lang.NoPermission);
                return;
            }

            if (args.Length < 3 || !float.TryParse(args[0], out float interval) || !int.TryParse(args[1], out int repeats))
            {
                SendMessage(player, Lang.InvalidUsage, cmd);
                return;
            }

            string message = string.Join(" ", args.Skip(2));

            if (_countdownTimer != null)
                _countdownTimer.Destroy();

            _countdownTimer = timer.Repeat(interval, repeats, () =>
            {
                PrintToChat(string.Format(lang.GetMessage(Lang.CountdownMessage, this), message));
            });
        }

        #endregion Commands

        #region Localization

        private class Lang
        {
            public const string NoPermission = "NoPermission";
            public const string InvalidUsage = "InvalidUsage";
            public const string CountdownMessage = "CountdownMessage";

        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NoPermission] = "You don't have permission to use this command.",
                [Lang.InvalidUsage] = "Invalid usage! Correct syntax: /countdown <interval> <repeats> <message>.",
                [Lang.CountdownMessage] = "{0}",
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}