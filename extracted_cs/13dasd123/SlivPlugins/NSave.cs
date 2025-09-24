// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Network;
using UnityEngine;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("NSave", "North", "2.0.0")]
    public class NSave : RustPlugin
    {

        #region Permission

        private const String Permission = "nsave.use";

        private void OnServerInitialized()
        {
            if (!permission.PermissionExists(Permission))
                permission.RegisterPermission(Permission, this);
        }

        #endregion

        #region Hook

        void OnServerSave()
        {
            Puts("Сервер был успешно сохранен! ");
        }

        #endregion

        #region Command

        [ChatCommand("save")]
        void CommandSave(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, Permission))
            {
                player.ChatMessage("Сервер сохраняется, пожалуйста, подождите! ");
                Server.Command($"save");
                return;
            }
            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                player.ChatMessage("У вас нет прав для использования данной команды! ");
                Puts("Игрок попытался использовать команду save! ");
                return;
            }
        }     

        #endregion

    }

}

