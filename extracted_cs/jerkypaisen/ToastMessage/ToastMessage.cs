using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ToastMessage", "jerkypaisen", "1.0.0")]
    [Description("You can send a toast message.")]
    class ToastMessage : RustPlugin
    {
        #region [Fields]
        private const string permUse = "toastmessage.use";
        #endregion

        #region [Oxide Hooks]
        private void Init() => permission.RegisterPermission(permUse, this);
        #endregion

        #region [Console Commands]
        [ConsoleCommand("toast")]
        private void cmdShowToastMessage(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length <= 0) return;
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                int style = 0;
                if (arg.Args.Length == 2)
                {
                    int i;
                    if (int.TryParse(arg.Args[1], out i)) style = i;
                }
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    activePlayer.ShowToast((GameTip.Styles)style, new Translate.Phrase("hoge", arg.Args[0]));
                }
            }
        }
        #endregion

        #region [Chat Commands]
        [ChatCommand("toast")]
        private void chtShowToastMessage(BasePlayer player, string command, string[] args)
        {
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            }
            if (args.Length <= 0) return;
            {
                int style = 0;
                if (args.Length == 2)
                {
                    int i;
                    if (int.TryParse(args[1], out i)) style = i;
                }
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    activePlayer.ShowToast((GameTip.Styles)style, new Translate.Phrase("hoge", args[0]));
                }
            }
        }
        #endregion
    }
}
