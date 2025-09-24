#region License (GPL v2)
/*
    DESCRIPTION
    Copyright (c) 2024 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.0

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)

using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("InstaWear", "RFC1920", "0.0.1")]
    [Description("Clothing items are worn on pickup or collection")]
    internal class InstaWear : RustPlugin
    {
        private ConfigData configData;
        private const string permUse = "instawear.use";

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            LoadConfigValues();
        }

        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item.CanMoveTo(player.inventory.containerWear))
            {
                if (!configData.RequirePermission || (configData.RequirePermission && permission.UserHasPermission(player?.UserIDString, permUse)))
                {
                    NextTick(() => item.MoveToContainer(player.inventory.containerWear));
                }
            }
            return null;
        }

        #region config
        private class ConfigData
        {
            public bool RequirePermission;
            public VersionNumber Version;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                RequirePermission = false,
                Version = Version
            };
            SaveConfig(config);
        }

        private void LoadConfigValues()
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
    }
}
