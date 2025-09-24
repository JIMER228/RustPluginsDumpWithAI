using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("After Wipe", "S1m0n", "1.0.0")]
    class AfterWipe : RustPlugin
    {
        private PluginConfig _settings;

        #region Classes

        class PluginConfig
        {
            public bool Enabled { get; set; } = true;

            public List<BlockedItems> Belt { get; set; } = new List<BlockedItems>
            {
                new BlockedItems
                {
                    Date = new DateTime(2016, 12, 31, 23, 59, 59),
                    Items = new List<string>()
                }
            };

            public List<BlockedItems> Wear { get; set; } = new List<BlockedItems>
            {
                new BlockedItems
                {
                    Date = new DateTime(2016, 12, 31, 23, 59, 59),
                    Items = new List<string>()
                }
            };

            public Dictionary<string, string> Translate { get; set; } = new Dictionary<string, string>
            {
                { "rifle.ak", "калаш" }
            };

            public string PermissionAllowed { get; set; } = "AfterWipe.allowed";
        }

        class BlockedItems
        {
            public DateTime Date { get; set; }

            public List<string> Items { get; set; }
        }

        #endregion

        #region Oxide hooks 

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(new PluginConfig(), true);
            PrintWarning("Default configuration file created.");
        }

        private void Loaded()
        {
            _settings = Config.ReadObject<PluginConfig>();

            permission.RegisterPermission("AfterWipe.allowed", this);
        }

        // Called right after an item was added to a container
        // An entire stack has to be created, not just adding more wood to a wood stack for example
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!_settings.Enabled)
                return;

            if (container.HasFlag(ItemContainer.Flag.Belt))
                ReturnBlockItemFromAddedContainer(_settings.Belt, container, item);
            else if (container.HasFlag(ItemContainer.Flag.Clothing))
                ReturnBlockItemFromAddedContainer(_settings.Wear, container, item);
        }

        #endregion

        #region Helpers 

        private void ReturnBlockItemFromAddedContainer(List<BlockedItems> blockList, ItemContainer container, Item item)
        {
            var player = container.playerOwner;

            if (!permission.UserHasPermission(player.UserIDString, "AfterWipe.allowed"))
            {
                var currentDate = DateTime.UtcNow;

                var blockedItem = blockList.Where(x => x.Items.Contains(item.info.shortname) && x.Date >= currentDate).FirstOrDefault();
                if (blockedItem == null)
                    return;

                NextTick(() =>
                {
                    if (player.inventory.containerMain.IsFull())
                        item.Drop(player.GetDropPosition(), player.GetDropVelocity());
                    else
                        item.MoveToContainer(player.inventory.containerMain);

                    if (!player.IsSleeping())
                    {
                        var translate = _settings.Translate.ContainsKey(item.info.shortname) ? _settings.Translate[item.info.shortname] : item.info.displayName.english;
                        SendReply(player, $"<color=#a2d953>[Сервер]:</color> Вы не можете использовать <color=#ffd479>{translate}</color> еще {FormatTime(blockedItem.Date - currentDate)}");
                    }
                });
            }
        }

        private string FormatTime(TimeSpan time)
            => (time.Days == 0 ? string.Empty : FormatDays(time.Days)) + (time.Hours == 0 ? string.Empty : FormatHours(time.Hours)) + (time.Minutes == 0 ? string.Empty : FormatMinutes(time.Minutes)) + (time.Seconds == 0 ? string.Empty : FormatSeconds(time.Seconds));

        private string FormatDays(int days) => FormatUnits(days, "дней", "дня", "день");

        private string FormatHours(int hours) => FormatUnits(hours, "часов", "часа", "час");

        private string FormatMinutes(int minutes) => FormatUnits(minutes, "минут", "минуты", "минута");

        private string FormatSeconds(int seconds) => FormatUnits(seconds, "секунд", "секунды", "секунда");

        private string FormatUnits(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1} ";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2} ";

            return $"{units} {form3} ";
        }

        #endregion
    }
}