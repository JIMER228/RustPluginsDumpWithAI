using System.Collections;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Backpack Block List", "st-little", "0.1.0")]
    [Description("Blocks items registered in the block list from moving to the backpack.")]
    public class BackpackBlockList : RustPlugin
    {
        #region Fields

        private const string PermissionUse = "backpackblocklist.use";
        private const string PermissionBypass = "backpackblocklist.bypass";

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public List<string> BlockItemShortNames;
            public List<ulong> IgnoreSkins;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                BlockItemShortNames = new List<string>(),
                IgnoreSkins = new List<ulong>(),
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _configuration = Config.ReadObject<Configuration>();

                if (_configuration == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _configuration = GetDefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Oxide hooks

        void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionBypass, this);
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, ItemContainerId targetContainer, int targetSlot, int amount, ItemMoveModifier itemMoveModifier)
        {
            BasePlayer? player = playerLoot.GetBaseEntity() as BasePlayer;
            if (player == null) return null;

            if (!permission.UserHasPermission(player.UserIDString, PermissionUse) ||
                permission.UserHasPermission(player.UserIDString, PermissionBypass))
                return null;

            ItemContainer backpackContainer = playerLoot.GetContainer(PlayerInventory.Type.BackpackContents);
            if (backpackContainer == null) return null;

            if (!targetContainer.Equals(backpackContainer.uid)) return null;

            bool isBlockItem = _configuration.BlockItemShortNames.Contains(item.info.shortname);
            bool isIgnoreSkin = _configuration.IgnoreSkins.Contains(item.skin);
            if (!isBlockItem || isIgnoreSkin) return null;

            (playerLoot.GetBaseEntity() as BasePlayer)?.ChatMessage("You can't move this item to the backpack.");

            return false;
        }

        #endregion
    }
}

