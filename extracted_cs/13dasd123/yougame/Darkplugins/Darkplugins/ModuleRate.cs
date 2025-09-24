using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("ModuleRate", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    public class ModuleRate : RustPlugin
    {
        #region Конфиг
        private List<LootContainer> ignoredContainer = new List<LootContainer>();

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            _config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config, true);
        }


        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        private class PluginConfig
        {
            [JsonProperty("Множитель рейтов(карьер) ")]
            public float defQuarry;
            [JsonProperty("Множитель рейтов(бочки) для обычных игроков")]
            public float defBarrel;

            [JsonProperty("Множитель рейтов(ящик) для обычных игроков")]
            public float defBox;

            [JsonProperty("Множитель рейтов(поднимаемых) для обычных игроков")]
            public float defPuckup;

            [JsonProperty("Множитель рейтов(добываемых) для обычных игроков")]
            public float defRate;

            [JsonProperty("Список игнорируемых предметов")]
            public List<string> ignoreList;

            [JsonProperty("Множитель рейтов(бочки) для привилегий")]
            public Dictionary<string, float> permBarrel;

            [JsonProperty("Множитель рейтов(ящик) для привилегий")]
            public Dictionary<string, float> permBox;

            [JsonProperty("Множитель рейтов(поднимаемых) для привилегий")]
            public Dictionary<string, float> permPickup;

            [JsonProperty("Множитель рейтов(добываемых) для привилегий")]
            public Dictionary<string, float> permRate;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    defQuarry = 1f,
                    defRate = 1f,
                    permRate = new Dictionary<string, float>
                    {
                        {"modulerate.vip", 2f},
                        {"modulerate.elite", 3f}
                    },
                    defPuckup = 1f,
                    permPickup = new Dictionary<string, float>
                    {
                        {"modulerate.vip", 2f},
                        {"modulerate.elite", 3f}
                    },
                    defBarrel = 1f,
                    permBarrel = new Dictionary<string, float>
                    {
                        {"modulerate.vip", 2f},
                        {"modulerate.elite", 3f}
                    },
                    defBox = 1f,
                    permBox = new Dictionary<string, float>
                    {
                        {"modulerate.vip", 2f},
                        {"modulerate.elite", 3f}
                    },
                    ignoreList = new List<string>
                    {
                        "scrap",
                        "apple"
                    }
                };
            }
        }

        #endregion

        #region OxideHooks

        private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            "     Author - Sempai#3239\n" +
            "     VK - https://vk.com/rustnastroika/n" +
            "     Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            LoadConfig();
            PermissionService.RegisterPermissions(this, _config.permBarrel.Keys.ToList());
            PermissionService.RegisterPermissions(this, _config.permBox.Keys.ToList());
            PermissionService.RegisterPermissions(this, _config.permRate.Keys.ToList());
            PermissionService.RegisterPermissions(this, _config.permPickup.Keys.ToList());
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item) =>
            OnDispenserBonus(dispenser, player, item);

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return;
            var rate = GetMaxRate(player.UserIDString) * item.amount;
            item.amount = rate;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player == null) return;
            var rate = GetMaxPickup(player.UserIDString) * item.amount;
            item.amount = rate;
        }

        private void OnEntityDeath(LootContainer entity, HitInfo info)
        {
            if (entity == null || info == null || info.InitiatorPlayer == null) return;
            if (entity.ShortPrefabName.Contains("food")) return;
            var inventory = entity.GetComponent<LootContainer>()?.inventory;
            if (inventory == null) return;
            for (var i = 0; i < inventory.itemList.Count; i++)
            {
                var item = inventory.itemList[i];
                if (CheckIgnore(item)) continue;
                if (item.MaxStackable() <= 1 || item.IsBlueprint()) continue;
                item.amount = (item.amount * GetMaxBarrel(info.InitiatorPlayer.UserIDString));
                item.amount = item.amount > 1 ? item.amount : 1;
            }
        }

        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (entity == null || ignoredContainer.Contains(entity)) return;
            var container = entity.inventory;
            if (container == null) return;
            for (var i = 0; i < container.itemList.Count; i++)
            {
                var item = container.itemList[i];
                if (CheckIgnore(item)) continue;
                if (item.MaxStackable() <= 1 || item.IsBlueprint()) continue;
                item.amount = (item.amount * GetMaxBox(player.UserIDString));
                item.amount = item.amount > 1 ? item.amount : 1;
                item.MarkDirty();
            }

            ignoredContainer.Add(entity);
        }

        
        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            var newAmount = item.amount * _config.defQuarry;
            item.amount = (int)newAmount;
        }
        #endregion


        #region Функции

        private bool CheckIgnore(Item item)
        {
            var i = 0;
            foreach (var check in _config.ignoreList)
                if (item.info.shortname == check)
                    i++;

            if (i != 0)
                return true;
            else
                return false;
        }
        
        private static class PermissionService
        {
            private static readonly Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException(nameof(owner));
                if (permissions == null) throw new ArgumentNullException(nameof(permissions));
                foreach (var permissionName in permissions.Where(permissionName =>
                    !permission.PermissionExists(permissionName)))
                    permission.RegisterPermission(permissionName, owner);
            }
        }

        private int GetMaxRate(string userid)
        {
            var rate = (int) _config.defRate;
            foreach (var privilege in _config.permRate)
            {
                if (permission.UserHasPermission(userid, privilege.Key))
                    rate = (int) Mathf.Max(rate, privilege.Value);
            }

            return rate;
        }

        private int GetMaxPickup(string userid)
        {
            var rate = (int) _config.defPuckup;
            foreach (var privilege in _config.permPickup)
                if (permission.UserHasPermission(userid, privilege.Key))
                    rate = (int) Mathf.Max(rate, privilege.Value);
            return rate;
        }

        private int GetMaxBarrel(string userid)
        {
            var rate = (int) _config.defBarrel;
            foreach (var privilege in _config.permBarrel)
                if (permission.UserHasPermission(userid, privilege.Key))
                    rate = (int) Mathf.Max(rate, privilege.Value);
            return rate;
        }

        private int GetMaxBox(string userid)
        {
            var rate = (int) _config.defBox;
            foreach (var privilege in _config.permBox)
                if (permission.UserHasPermission(userid, privilege.Key))
                    rate = (int) Mathf.Max(rate, privilege.Value);
            return rate;
        }

        #endregion
    }
}