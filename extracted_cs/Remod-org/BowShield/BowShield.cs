#region License (GPL v2)
/*
    BowShield
    Copyright (c) 2025 RFC1920 <desolationoutpostpve@gmail.com>

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
#endregion
using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BowShield", "RFC1920", "1.0.1")]
    [Description("Enable shield for bow or jackhammer")]
    internal class BowShield : RustPlugin
    {
        private ConfigData configData;
        private Dictionary<ulong, bool> bowShieldStatus = new();

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["bowstatus"] = "BowShield status: {0}",
                ["disabled"] = "BowShield disabled",
                ["enabled"] = "BowShield enabled"
            }, this, "en");
        }

        private void OnServerInitialized()
        {
            LoadConfigVariables();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            bowShieldStatus.Add(player.userID, true);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            bowShieldStatus.Remove(player.userID);
        }

        private void DoLog(string message)
        {
            if (configData.Options.debug) Interface.GetMod().LogInfo(message);
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item activeItem)
        {
            if (activeItem == null) return;
            if (player == null) return;
            if (!player.userID.IsSteamId()) return;

            if (bowShieldStatus.ContainsKey(player.userID))
            {
                if (!bowShieldStatus[player.userID]) return;
            }
            else
            {
                bowShieldStatus.Add((ulong)player.userID, true);
            }

            FindAndSetupShield(player, activeItem);
        }

        private void FindAndSetupShield(BasePlayer player, Item activeItem)
        {
            string lcname = activeItem.info.displayName.english.ToLower();
            foreach (string search in configData.Options.searchItem)
            {
                DoLog($"Comparing {lcname} to {search}");
                if (lcname.Contains(search.ToLower()))
                {
                    DoLog($"Player {player?.displayName} holding {activeItem?.info.displayName.english} ({lcname}).");
                    if (PlayerHasShield(player, out ItemModWearable modWear))
                    {
                        HeldEntity bojack = player.GetHeldEntity();
                        if (bojack != null && modWear != null)
                        {
                            DoLog($"Player {player.displayName} holding {bojack.ShortPrefabName} and has shield.");
                            if (bojack.ShortPrefabName.ToLower().Contains(lcname))
                            {
                                DoLog($"Setting shield capability for {activeItem.info.displayName.english}");
                                ToggleShield(bojack, modWear);
                                SendReply(player, Lang("bowstatus", null, bowShieldStatus[player.userID]));
                                //bojack.canBeUsedWithShield = true;
                                //modWear.blocksAiming = false;
                                //modWear.blocksEquipping = false;
                                //modWear.occlusionType = 0;

                                Shield shield = player.GetHeldEntity() as Shield;
                                if (shield != null)
                                {
                                    shield.MaxBlockTime = configData.Options.blockTime;
                                    shield.DamageMitigationFactor = configData.Options.damageMitigation;
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool PlayerHasShield(BasePlayer player, out ItemModWearable shield)
        {
            shield = null;
            if (!player.userID.IsSteamId()) return false;
            foreach (Item item in player?.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    DoLog($"Checking wear item: {item.info.displayName.english}");
                    if (item.info.displayName.english.Contains("Shield"))
                    {
                        DoLog($"Player {player?.displayName} has {item.info.displayName.english}");
                        shield = item.info.GetComponent<ItemModWearable>();
                        return true;
                    }
                }
            }
            return false;
        }

        private void ToggleShield(HeldEntity bojack, ItemModWearable modWear, bool forceOn = false)
        {
            if (bojack != null && modWear != null)
            {
                bojack.canBeUsedWithShield = !bojack.canBeUsedWithShield | forceOn;
                modWear.blocksAiming = !modWear.blocksAiming | !forceOn;
                //modWear.blocksEquipping =! modWear.blocksEquipping | !forceOn;
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;

            if (input.WasJustPressed(BUTTON.FIRE_THIRD) && PlayerHasShield(player, out ItemModWearable shield))
            {
                if (shield != null)
                {
                    HeldEntity bojack = player.GetHeldEntity();
                    if (bojack != null)
                    {
                        ToggleShield(bojack, shield);
                        if (bowShieldStatus.ContainsKey(player.userID))
                        {
                            bowShieldStatus[player.userID] = bojack.canBeUsedWithShield;
                        }
                        else
                        {
                            bowShieldStatus.Add(player.userID, bojack.canBeUsedWithShield);
                        }
                        SendReply(player, Lang("bowstatus", null, bowShieldStatus[player.userID]));
                    }
                }
            }
        }

        #region config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new()
            {
                Options = new Options()
                {
                    searchItem = new string[2] { "bow_hunting", "jackhammer" },
                    blockTime = 30f,
                    damageMitigation = 1f,
                    debug = false
                },
                Version = Version
            };
            SaveConfig(config);
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

        public class ConfigData
        {
            public Options Options = new();
            public VersionNumber Version;
        }

        public class Options
        {
            [JsonProperty(PropertyName = "Held items that can use shield")]
            public string[] searchItem = new string[2] { "bow", "jackhammer" };

            [JsonProperty(PropertyName = "Block time")]
            public float blockTime;

            [JsonProperty(PropertyName = "Damage mitigation factor")]
            public float damageMitigation;

            public bool debug;

            //public int[] buttons = new int[6] { 2048, 2050, 2176, 2178, 2180, 3202 };
        }
        #endregion
    }
}
