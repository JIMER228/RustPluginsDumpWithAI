#region License (GPL v2)
/*
    Recyle items into blueprints
    Copyright (c) 2021-2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v3)
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Recycle Blueprint", "RFC1920", "1.0.4")]
    [Description("Get blueprints from recycled items not already learned")]
    internal class RecycleBP : RustPlugin
    {
        #region vars
        private ConfigData configData;

        private const string permRecyleBP = "recyclebp.use";
        private Dictionary<uint, ulong> rcloot = new Dictionary<uint, ulong>();
        #endregion

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        #region init
        private void OnServerInitialized()
        {
            permission.RegisterPermission(permRecyleBP, this);
        }

        private void Loaded()
        {
            LoadConfigVariables();
        }
        #endregion

        #region config
        private class ConfigData
        {
            public bool usePermission;
            public bool debug;
            public VersionNumber Version;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                usePermission = false,
                Version = Version,
            };
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region Main
        private object OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (recycler.IsOn()) return null;

            ItemBPCheck(recycler, player.IPlayer);

            return null;
        }

        private bool ItemBPCheck(Recycler recycler, IPlayer player)
        {
            if (player != null && configData.usePermission && !player.HasPermission(permRecyleBP))
            {
                return true;
            }

            //for (int i = 0; i < 6; i++)
            for (int i = 0; i < recycler.inventory.availableSlots.Count; i++)
            {
                Item item = recycler.inventory.GetSlot(i);
                if (item == null) continue;
                if (configData.debug && configData.debug) Puts($"{i} Found {item.info.name}");
                if (item.info.Blueprint.isResearchable)
                {
                    BasePlayer bluepl = player.Object as BasePlayer;
                    if (!bluepl.blueprints.HasUnlocked(item.info))
                    {
                        if (configData.debug) Puts($"Player has not unlocked BP for {item.info.name}");
                        int targetslot = FindEmptyOutputSlot(recycler);
                        if (targetslot > 0)
                        {
                            if (configData.debug) Puts($"Creating BP for {item.info.name}");
                            Item newbp = ItemManager.CreateByName("blueprintbase");
                            newbp.blueprintTarget = item.info.itemid;
                            newbp.MoveToContainer(recycler.inventory, targetslot);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private int FindEmptyOutputSlot(Recycler recycler)
        {
            for (int i = 6; i < 12; i++)
            {
                Item item = recycler.inventory.GetSlot(i);
                if (item == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            Recycler rc = container.GetComponentInParent<Recycler>();
            if (rc == null) return null;

            if (configData.debug) Puts($"Adding recycler {rc.net.ID}");
            if (rcloot.ContainsKey((uint)rc.net.ID.Value))
            {
                rcloot.Remove((uint)rc.net.ID.Value);
            }
            rcloot.Add((uint)rc.net.ID.Value, player.userID);

            return null;
        }

        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity == null) return;
            if (player == null) return;
            if (!(entity is Recycler)) return;
            if (!rcloot.ContainsKey((uint)entity.net.ID.Value)) return;

            if (rcloot[(uint)entity.net.ID.Value] == player.userID && configData.debug)
            {
                Puts($"Removing recycler {entity.net.ID}");
            }
        }
        #endregion
    }
}
