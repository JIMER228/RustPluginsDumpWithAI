/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Skin It", "VisEntities", "1.1.0")]
    [Description("Change item skins directly through chat without needing a repair bench.")]
    public class SkinIt : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin PlayerDLCAPI;

        #endregion 3rd Party Dependencies

        #region Fields

        private static SkinIt _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void OnServerInitialized(bool isStartup)
        {
            CheckDependencies();
        }

        private void Unload()
        {
            _plugin = null;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "skinit.use";
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

        #region Helper Functions

        private bool CanUseSkin(BasePlayer player, string shortname, ulong skinId)
        {
            if (!PluginLoaded(PlayerDLCAPI))
                return true;

            bool initialized = false;
            try
            {
                initialized = PlayerDLCAPI.Call<bool>("Initialized");
            }
            catch
            {
                initialized = false;
            }

            if (!initialized)
                return true;

            return PlayerDLCAPI.Call<bool>("IsOwnedOrFreeItem", player, shortname, skinId);
        }

        private bool CheckDependencies()
        {
            if (!PluginLoaded(PlayerDLCAPI))
            {
                Puts("Player DLC API is not loaded. Ownership checks are DISABLED. Install it to enforce DLC/skin ownership.");
                return false;
            }

            bool initialized = false;
            try
            {
                initialized = PlayerDLCAPI.Call<bool>("Initialized");
            }
            catch
            {
                initialized = false;
            }

            if (!initialized)
                Puts("Player DLC API is present but not initialized yet. Until it's ready, ownership checks are DISABLED.");

            return true;
        }

        private static bool PluginLoaded(Plugin plugin)
        {
            if (plugin != null && plugin.IsLoaded)
                return true;
            else
                return false;
        }

        #endregion Helper Functions

        #region Commands

        [ChatCommand("skin")]
        private void cmdSkin(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
            {
                ReplyToPlayer(player, Lang.NoPermission);
                return;
            }

            if (args == null || args.Length == 0)
            {
                ReplyToPlayer(player, Lang.Usage);
                return;
            }

            if (!ulong.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong newSkinId))
            {
                ReplyToPlayer(player, Lang.InvalidSkinId, args[0]);
                return;
            }

            string partialItemName = null;
            if (args.Length >= 2)
            {
                partialItemName = string.Join(" ", args.Skip(1).ToArray());
            }

            if (string.IsNullOrEmpty(partialItemName))
            {
                var activeItem = player.GetActiveItem();
                if (activeItem == null)
                {
                    ReplyToPlayer(player, Lang.NoActiveItem);
                    return;
                }

                if (!CanUseSkin(player, activeItem.info.shortname, newSkinId))
                {
                    ReplyToPlayer(player, Lang.SkinRestricted);
                    return;
                }

                bool success = ReskinItem(player, activeItem, newSkinId);
                if (success)
                {
                    ReplyToPlayer(player, Lang.SkinSuccess, activeItem.info.displayName.translated, newSkinId.ToString());
                }
                else
                {
                    ReplyToPlayer(player, Lang.SkinFail);
                }

            }
            else
            {
                List<Item> foundItems = FindAllItemsByName(player, partialItemName);
                if (foundItems.Count == 0)
                {
                    ReplyToPlayer(player, Lang.NoItemFound, partialItemName);
                    return;
                }

                if (foundItems.Count == 1)
                {
                    var singleItem = foundItems[0];

                    if (!CanUseSkin(player, singleItem.info.shortname, newSkinId))
                    {
                        ReplyToPlayer(player, Lang.SkinRestricted);
                        return;
                    }

                    bool success = ReskinItem(player, singleItem, newSkinId);
                    if (success)
                    {
                        string itemName;
                        if (singleItem.info.displayName.translated != null)
                            itemName = singleItem.info.displayName.translated;
                        else
                            itemName = singleItem.info.shortname;

                        ReplyToPlayer(player, Lang.SkinSuccess, itemName, newSkinId.ToString());
                    }
                    else
                    {
                        ReplyToPlayer(player, Lang.SkinFail);
                    }
                }
                else
                {
                    int reskinnedCount = 0;
                    int failCount = 0;

                    foreach (Item item in foundItems)
                    {
                        if (!CanUseSkin(player, item.info.shortname, newSkinId))
                        {
                            failCount++;
                            continue;
                        }

                        bool success = ReskinItem(player, item, newSkinId);
                        if (success)
                        {
                            reskinnedCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }

                    ReplyToPlayer(player, Lang.SkinSuccessSummary, reskinnedCount, (failCount + reskinnedCount), partialItemName, newSkinId);
                }
            }
        }

        #endregion Commands

        #region Reskin Logic

        private List<Item> FindAllItemsByName(BasePlayer player, string partialName)
        {
            partialName = partialName.ToLowerInvariant();

            List<Item> matched = new List<Item>();
            matched.AddRange(SearchContainerAll(player.inventory.containerBelt, partialName));
            matched.AddRange(SearchContainerAll(player.inventory.containerMain, partialName));
            matched.AddRange(SearchContainerAll(player.inventory.containerWear, partialName));
            return matched;
        }

        private List<Item> SearchContainerAll(ItemContainer container, string partialName)
        {
            List<Item> results = new List<Item>();
            if (container == null)
                return results;

            foreach (Item item in container.itemList)
            {
                string display;
                if (item.info.displayName.translated != null)
                    display = item.info.displayName.translated.ToLowerInvariant();
                else
                    display = "";

                string shortName;
                if (item.info.shortname != null)
                    shortName = item.info.shortname.ToLowerInvariant();
                else
                    shortName = "";

                if (display.Contains(partialName) || shortName.Contains(partialName))
                {
                    results.Add(item);
                }
            }
            return results;
        }

        private bool ReskinItem(BasePlayer player, Item oldItem, ulong newSkinId, string customName = null)
        {
            if (player == null || oldItem == null || oldItem.info == null)
                return false;

            int oldPosition = oldItem.position;
            ItemContainer oldContainer = oldItem.parent as ItemContainer;
            var shortname = oldItem.info.shortname;
            float condition = oldItem.condition;
            float maxCondition = oldItem.maxCondition;
            int amount = oldItem.amount;

            int savedAmmo = 0;
            ItemDefinition ammoDef = null;
            BaseEntity oldHeld = oldItem.GetHeldEntity();
            BaseProjectile oldProj = oldHeld as BaseProjectile;
            if (oldProj != null)
            {
                if (oldProj.primaryMagazine != null)
                {
                    savedAmmo = oldProj.primaryMagazine.contents;
                    ammoDef = oldProj.primaryMagazine.ammoType;
                }
            }

            List<Item> oldSubItems = null;
            if (oldItem.contents != null && oldItem.contents.itemList.Count > 0)
            {
                oldSubItems = oldItem.contents.itemList.ToList();
            }

            Item newItem = ItemManager.CreateByName(shortname, amount, newSkinId);
            if (newItem == null)
                return false;

            newItem.condition = condition;
            newItem.maxCondition = maxCondition;
            if (!string.IsNullOrEmpty(customName))
                newItem.name = customName;

            if (oldSubItems != null && oldSubItems.Count > 0)
            {
                if (newItem.info.itemMods != null)
                {
                    int maxSlotCount = newItem.info.itemMods.Length;

                    if (newItem.contents == null)
                        newItem.contents = new ItemContainer();

                    newItem.contents.ServerInitialize(newItem, maxSlotCount);
                    newItem.contents.SetFlag(ItemContainer.Flag.IsLocked, false);
                }

                foreach (var sub in oldSubItems)
                {
                    var newSub = ItemManager.Create(sub.info, sub.amount, sub.skin);
                    if (newSub == null) continue;

                    newSub.condition = sub.condition;
                    newSub.maxCondition = sub.maxCondition;

                    newSub.MoveToContainer(newItem.contents);
                    newSub.MarkDirty();
                }

                if (newItem.contents != null)
                    newItem.contents.MarkDirty();
            }

            NextTick(() =>
            {
                if (oldItem != null)
                {
                    oldItem.RemoveFromContainer();
                    oldItem.Remove();
                }

                bool placedSuccessfully = false;

                if (oldContainer != null)
                {
                    if (newItem.MoveToContainer(oldContainer, oldPosition, false))
                    {
                        placedSuccessfully = true;
                    }
                    else
                    {
                        if (newItem.MoveToContainer(player.inventory.containerMain, -1, false))
                        {
                            placedSuccessfully = true;
                        }
                    }
                }
                else
                {
                    if (newItem.MoveToContainer(player.inventory.containerMain, -1, false))
                    {
                        placedSuccessfully = true;
                    }
                }

                if (!placedSuccessfully)
                {
                    newItem.Remove();
                    return;
                }

                BaseEntity newHeld = newItem.GetHeldEntity();
                if (newHeld is BaseProjectile newProj && newProj.primaryMagazine != null)
                {
                    newProj.primaryMagazine.contents = savedAmmo;
                    newProj.primaryMagazine.ammoType = ammoDef;
                    newHeld.skinID = newSkinId;
                    newHeld.SendNetworkUpdateImmediate();
                }

                newItem.MarkDirty();
            });

            return true;
        }

        #endregion Reskin Logic

        #region Localization

        private class Lang
        {
            public const string NoPermission = "NoPermission";
            public const string Usage = "Usage";
            public const string InvalidSkinId = "InvalidSkinId";
            public const string NoActiveItem = "NoActiveItem";
            public const string NoItemFound = "NoItemFound";
            public const string SkinSuccess = "SkinSuccess";
            public const string SkinFail = "SkinFail";
            public const string SkinSuccessSummary = "SkinSuccessSummary";
            public const string SkinRestricted = "SkinRestricted";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NoPermission] = "You do not have permission to use this command.",
                [Lang.Usage] = "Usage:\n/skin <skinId> [partialItemName]\nIf item name isn't provided, it skins your active item.",
                [Lang.InvalidSkinId] = "'{0}' is not a valid skin ID.",
                [Lang.NoActiveItem] = "You have no active item to skin! Please specify an item name or hold the item.",
                [Lang.NoItemFound] = "Couldn't find any item matching '{0}' in your inventory.",
                [Lang.SkinSuccess] = "Skinned your '{0}' to skin ID {1}.",
                [Lang.SkinFail] = "Failed to reskin the item.",
                [Lang.SkinSuccessSummary] = "Reskinned {0} out of {1} items matching '{2}' to skin ID {3}.",
                [Lang.SkinRestricted] = "That skin is restricted (paid DLC or not owned).",

            }, this, "en");
        }

        private static string GetMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string userId;
            if (player != null)
                userId = player.UserIDString;
            else
                userId = null;

            string message = _plugin.lang.GetMessage(messageKey, _plugin, userId);

            if (args.Length > 0)
                message = string.Format(message, args);

            return message;
        }

        public static void ReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                _plugin.SendReply(player, message);
        }

        #endregion Localization
    }
}