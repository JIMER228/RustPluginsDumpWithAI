// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("HomeCasino", "https://discord.gg/dNGbxafuJn", "1.0.2")]
    internal class HomeCasino : RustPlugin
    {
        #region Static

        [PluginReference] private Plugin Notifications;
        private static PluginConfig _config;

 
        public class License
        {
            [JsonProperty("Количество ставок на этой лицензии | Number of bids on this license")]
            public int GameAmount;

            [JsonProperty(
                "Настройка предметов доступных по этой лицензии(ShortName / Amount) | Configuring items available under this license")]
            public Dictionary<string, int> AllowedItems;
        }

        public class ItemSettings
        {
            [JsonProperty("ShortName")] public string ShortName;
            [JsonProperty("Количество | Amount")] public int Amount;
            [JsonProperty("Шанс спавна | Chance")] public int Chance;
            [JsonProperty("SkinID предмета")] public ulong SkinID;

            [JsonProperty("Имя предмета(оставить пустым если стандартное) | item name(leave empty if standard)")]
            public string DisplayName;
        }

        #endregion

        #region Config

        public class PluginConfig
        {
            [JsonProperty("Настройка предметов на которые можно играть(SkinID License) | Customize items to play with")]
            public Dictionary<ulong, License> Items;

            [JsonProperty("Настройка спавна | Spawn Settings")]
            public SpawnSettings Spawn;

            internal class SpawnSettings
            {
                [JsonProperty("Настройка спавна лицензий | Configuring license spawn")]
                public List<ItemSettings> LicenseList;
                [JsonProperty("Настройка спавна рулетки | Setting up the roulette spawn")]
                public ItemSettings RouletteSpawn;
                [JsonProperty("Настройка спавна предметов(ящик) | Setting up item spawn (box)")]
                public List<string> BoxList;
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig
            {
                Items = new Dictionary<ulong, License>()
                {
                    [2222103634] = new License()
                    {
                        GameAmount = 4,
                        AllowedItems = new Dictionary<string, int>()
                        {
                            ["sulfur"] = 1000
                        }
                    },
                    [2222149895] = new License()
                    {
                        GameAmount = 4,
                        AllowedItems = new Dictionary<string, int>()
                        {
                            ["stones"] = 1000
                        }
                    }
                },
                Spawn = new PluginConfig.SpawnSettings()
                {
                    BoxList = new List<string>()
                    {
                        "crate_elite",
                        "crate_normal"
                    },
                    RouletteSpawn = new ItemSettings()
                    {
                        ShortName = "table",
                        SkinID = 2223073610,
                        Amount = 1,
                        Chance = 15,
                        DisplayName = "Roulette"
                    },
                    LicenseList = new List<ItemSettings>()
                    {
                        new ItemSettings()
                        {
                            ShortName = "fuse",
                            SkinID = 2222103634,
                            Amount = 1,
                            Chance = 15,
                            DisplayName = "Red License"
                        },
                        new ItemSettings()
                        {
                            ShortName = "fuse",
                            SkinID = 2222149895,
                            Amount = 1,
                            Chance = 25,
                            DisplayName = "Blue License"
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
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
            PrintWarning(
                "Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            LoadConfig();
        }

        private void OnLootSpawn(LootContainer container)
        {
            NextTick(() =>
            {
                if (!_config.Spawn.BoxList.Contains(container.ShortPrefabName)) return;
                for (var i = 0; i < _config.Spawn.LicenseList.Count; i++)
                {
                    var license = _config.Spawn.LicenseList[i];
                    if (Random.Range(0, 100) > license.Chance) continue;
                    if (container.inventory.capacity == container.inventory.itemList.Count)
                        container.inventory.capacity++;
                    var item = ItemManager.CreateByName(license.ShortName, license.Amount, license.SkinID);
                    if (!string.IsNullOrEmpty(license.DisplayName))
                        item.name = license.DisplayName;
                    item.MoveToContainer(container.inventory);
                }

                if (Random.Range(0, 100) <= _config.Spawn.RouletteSpawn.Chance)
                {
                    if (container.inventory.capacity == container.inventory.itemList.Count)
                        container.inventory.capacity++;
                    var item = ItemManager.CreateByName(_config.Spawn.RouletteSpawn.ShortName, 1,
                        _config.Spawn.RouletteSpawn.SkinID);
                    if (!string.IsNullOrEmpty(_config.Spawn.RouletteSpawn.DisplayName))
                        item.name = _config.Spawn.RouletteSpawn.DisplayName;
                    item.MoveToContainer(container.inventory);
                }
            });
        }

        private void OnLootEntity(BasePlayer player, BigWheelBettingTerminal terminal)
        {
            if (player == null || terminal == null) return;
            if (terminal.OwnerID == 0) return;

            var license = player.GetActiveItem();
            if (license != null && IsLicense(license)) return;
            SendMsg(player, GetMsg(player.UserIDString, "LICENSE.NOTACTIVE"));
            NextTick(player.EndLooting);
        }
        private object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot,
            int amount)
        {
            if (playerLoot == null) return null;
            var terminal = playerLoot.loot?.entitySource?.GetComponent<BigWheelBettingTerminal>();
            if (terminal == null) return null;
            if (terminal.OwnerID == 0) return null;
            var player = item.GetOwnerPlayer();
            if (player == null) return null;
            if (playerLoot.FindContainer(targetContainer) == null)
                return null;
            var inv = playerLoot.GetComponent<BasePlayer>()?.inventory;
            var target = playerLoot.FindContainer(targetContainer);
            if (target == inv.containerMain || target == inv.containerBelt || target == inv.containerWear)
                return null;
            var license = player.GetActiveItem();
            if (license == null || !IsLicense(license))
            {
                SendMsg(player, GetMsg(player.UserIDString, "LICENSE.NOTACTIVE"));
                return false;
            }

            if (terminal.inventory.IsLocked()) return null;
           
            if (!_config.Items[license.skin].AllowedItems.ContainsKey(item.info.shortname))
            {
                SendMsg(player, GetMsg(player.UserIDString, "LICENSE.NOTITEM"));
                return false;
            }
            if (item.amount > _config.Items[license.skin].AllowedItems[item.info.shortname])
            {
                SendMsg(player, GetMsg(player.UserIDString, "LICENSE.MAXAMOUNT", new []{ _config.Items[license.skin].AllowedItems[item.info.shortname].ToString()} ));
                return false;
            }
            if(targetSlot == -1)
            {
                for (var index = 0; index < terminal.inventory.itemList.Count; index++)
                {
                    var check = terminal.inventory.itemList[index];
                    if (check.info.shortname != item.info.shortname) continue;
                    if (check.amount + item.amount > _config.Items[license.skin].AllowedItems[item.info.shortname])
                    {
                        SendMsg(player, GetMsg(player.UserIDString, "LICENSE.MAXAMOUNT", new []{ _config.Items[license.skin].AllowedItems[item.info.shortname].ToString()} ));
                        return false;
                    }
                    CanMoveItem(item, playerLoot, targetContainer, 1, amount);
                    NextTick(()=>{
                    item.MoveToContainer(check.GetRootContainer());});
                    return null;
                }
            }
            var gameamount = 1 * license.maxCondition / _config.Items[license.skin].GameAmount;
            license.condition -= gameamount;
            if (license.condition <= 0)
                license.DoRemove();
            terminal.inventory.canAcceptItem = null;
            if(item == null)return null;
            terminal.inventory.onlyAllowedItem = item.info;
            terminal.allowedItem = item.info;
            terminal.SendNetworkUpdate();
            return null;
        }

       

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var item = plan?.GetItem();
            if (item == null) return;
            if (item.skin != _config.Spawn.RouletteSpawn.SkinID) return;
            if (go == null || go.ToBaseEntity() == null) return;
            SpawnRoulette(go.ToBaseEntity());

        }

        #endregion

        #region Function

        [ConsoleCommand("casinogive")]
        private void cmdChatcasinogive(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (args.Connection != null)
                if (!player.IsAdmin)
                    return;
            if (args.Args.Length != 2)
            {
                Puts("Вы не верно ввели команду, используйте: casinogive Name/SteamID roulette/LicenseSkinID");
                return;
            }

            var findPlayer = BasePlayer.Find(args.Args[0]) ?? BasePlayer.FindSleeping(args.Args[0]);
            if (findPlayer == null)
            {
                Puts($"Игрок {args.Args[0]} не найден в списке игроков");
                return;
            }

            switch (args.Args[1])
            {
                case "roulette":
                {
                    var roulette = ItemManager.CreateByName(_config.Spawn.RouletteSpawn.ShortName,
                        _config.Spawn.RouletteSpawn.Amount, _config.Spawn.RouletteSpawn.SkinID);
                    findPlayer.GiveItem(roulette);
                    break;
                }
                default:
                {
                    var licence = ulong.Parse(args.Args[1]);
                    if (!_config.Items.ContainsKey(licence)) return;
                    var licenseItem = ItemManager.CreateByName(_config.Spawn.LicenseList[0].ShortName, 1, licence);
                    findPlayer.GiveItem(licenseItem);
                    break;
                    
                }
            }
        }
        private static bool IsLicense(Item item)
        {
            return _config.Items.ContainsKey(item.skin);
        }

        private void SendMsg(BasePlayer player, string msg)
        {
            if (Notifications)
            {
                Notifications?.Call("ShowNotify", player.userID, 5f, "РУЛЕТКА", $"<color=#FFFF>{msg}</color>",
                    "warning");
                return;
            }

            player.ChatMessage(msg);
        }

        private void SpawnRoulette(BaseEntity entity)
        {
            timer.In(0.4f, () =>
            {
                var ownerID = entity.OwnerID;
                var wheel =
                    GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/big_wheel.prefab") as
                        BigWheelGame;
                if (wheel == null) return;
                wheel.SetParent(entity);
                var transform = wheel.transform;
                var position = transform.localPosition;
                position.y += 0.65f;
                transform.localPosition = position;
                wheel.Spawn();
                wheel.OwnerID = ownerID;
                var terminal = GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab") as BigWheelBettingTerminal;
                if (terminal == null) return;
                terminal.SetParent(entity,false,true);
                var transform1 = terminal.transform;
                position = transform1.localPosition;
                position.x -= 1.95f;
                transform1.localPosition = position;
                terminal.OwnerID = ownerID;
                terminal.Spawn();
                timer.In(5, () =>
                {
                    wheel.terminals.Clear();
                    wheel.SendNetworkUpdateImmediate();

                    wheel.terminals.Add(terminal);
                    wheel.SendNetworkUpdateImmediate();
                });

            });
        }
        #endregion

        #region Lang

        private string GetMsg(string userID, string key, object[] args = null)
        {
            return args == null ? string.Format(lang.GetMessage(key, this, userID)) : string.Format(lang.GetMessage(key, this, userID), args);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LICENSE.NOTITEM"] = "Your license does not allow you to play on this resource.",
                ["LICENSE.NOTACTIVE"] = "Pick up the game license.",
                ["LICENSE.MAXAMOUNT"] = "You can only put {0} of this item"
            }, this); 

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LICENSE.NOTITEM"] = "Ваша лицензия не позволяет играть на этот ресурс.",
                ["LICENSE.NOTACTIVE"] = "Возьмите в руки лицензию на игру.",
                ["LICENSE.MAXAMOUNT"] = "Вы можете поставить только {0} данного предмета"

            }, this, "ru");
        }

        #endregion
    }
}