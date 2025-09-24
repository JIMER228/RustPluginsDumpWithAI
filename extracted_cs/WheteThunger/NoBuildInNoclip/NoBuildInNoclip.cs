using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Bulid In Noclip", "WhiteThunder", "0.1.0")]
    [Description("Disallows players from building and repairing while in noclip.")]
    internal class NoBuildInNoclip : CovalencePlugin
    {
        #region Fields

        private const string PermissionBypass = "nobuildinnoclip.bypass";

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionBypass, this);
        }

        private bool? CanBuild(Planner planner)
        {
            if (!VerifyNotInNoclip(planner.GetOwnerPlayer()))
                return false;

            return null;
        }

        private bool? OnHammerHit(BasePlayer player)
        {
            if (!VerifyNotInNoclip(player))
                return false;

            return null;
        }

        #endregion

        #region Helper Methods

        private bool VerifyNotInNoclip(BasePlayer player)
        {
            if (!player.IsFlying || permission.UserHasPermission(player.UserIDString, PermissionBypass))
                return true;

            ChatMessage(player, Lang.ErrorNoPermission);
            return false;
        }

        #endregion

        #region Localization

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.UserIDString, messageName), args));

        private class Lang
        {
            public const string ErrorNoPermission = "Error.NoPermission";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.ErrorNoPermission] = "You don't have permission to do that while in noclip.",
            }, this, "en");
        }

        #endregion
    }
}
