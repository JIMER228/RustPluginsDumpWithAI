using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AutoCodeLock", "Sparkless", "0.0.1")]
    public class AutoCodeLock : RustPlugin
    {
        public Dictionary<ulong, int> _codeData = new Dictionary<ulong, int>();


        [PluginReference] private Plugin Friends;

        void OnServerInitialized()
        {
            try
            {
                _codeData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, int>>(Name);
            }
            catch
            {
                _codeData = new Dictionary<ulong, int>();
            }
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _codeData);
        }

        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _codeData);
        }
        
        private static bool HasCodeLock(BasePlayer player)
        {
            return player.inventory.FindItemID(1159991980) != null;
        }


        void OnNewSave()
        {
            _codeData = new Dictionary<ulong, int>();
            Interface.Oxide.DataFileSystem.WriteObject(Name, _codeData);
            Interface.Oxide.ReloadPlugin(Name);
        }
        void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null) return;
            BaseEntity entity = go.GetComponent<BaseEntity>();

            if (entity as Door)
            {
                if (!HasCodeLock(player)) return;
                int code = UnityEngine.Random.Range(1000, 9999);
                if (!_codeData.ContainsKey(player.userID))
                {
                    _codeData.Add(player.userID, code);
                    player.ChatMessage($"Замок успешно установлен! Ваш код - {code}");
                }
                var Code = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab") as CodeLock;
                if (Code != null)
                {
                    Code.Spawn();
                    Code.code = _codeData[player.userID].ToString();
                    Code.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                    entity.SetSlot(BaseEntity.Slot.Lock, Code);
                    Code.whitelistPlayers.Add(player.userID);
                    Code.SetFlag(BaseEntity.Flags.Locked, true);
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab", Code.transform.position);
                }
                TakeCodeLock(player);
            }
        }
        
        object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (!(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0) return null;
            if (@lock.GetParentEntity().OwnerID == player.userID) return null;
            if (Friends)
            {
                if (HasFriend(@lock.GetParentEntity().OwnerID, player.userID))
                {
                    return true;
                }
            }
            return null;
        }
        
        private static void TakeCodeLock(BasePlayer player)
        {
            player.inventory.Take(null, 1159991980, 1);
        }
        
        private bool HasFriend(ulong playerId, ulong friendId)
        {
            return Friends != null && (bool) Friends?.Call<bool>("HasFriend", playerId, friendId);
        }
        
    }
}