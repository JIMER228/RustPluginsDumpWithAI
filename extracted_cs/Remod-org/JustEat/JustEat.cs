#region License (GPL v2)
/*
    DESCRIPTION
    Copyright (c) 2022 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

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
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("JustEat", "RFC1920", "1.0.1")]
    [Description("Just eat collectible food")]
    internal class JustEat : RustPlugin
    {
        private ConfigData configData;

        private void OnServerInitialized()
        {
            LoadConfigVariables();
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            bool reverse = false;
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (input.IsDown(BUTTON.SPRINT)) reverse = true;
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 3f))
                {
                    CollectibleEntity mr = hit.GetEntity() as CollectibleEntity;
                    if (mr != null)
                    {
                        if (mr.ShortPrefabName.Contains("mushroom") ||
                            mr.ShortPrefabName.Contains("berry") ||
                            mr.ShortPrefabName.Contains("potato"))
                        {
                            mr?.DoPickup(player, (configData.defaultEat & !reverse));
                        }
                    }
                }
            }
        }

        private class ConfigData
        {
            public bool defaultEat;
            public VersionNumber Version;
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData()
            {
                defaultEat = true
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
    }
}
