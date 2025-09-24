#region License (GPL v2)
/*
    ChuteManager
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

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
using Network;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ChuteManager", "RFC1920", "1.0.12")]
    [Description("Manage parachute speed, backpack pickup, and condition.")]
    internal class ChuteManager : RustPlugin
    {
        private ConfigData configData;
        private const string zipper = "assets/prefabs/deployable/locker/sound/equip_zipper.prefab";
        private const string permFastFlight = "chutemanager.fast";
        private const string permFastPickup = "chutemanager.pickup";
        private const string permPickupCondition = "chutemanager.condition";
        private bool enabled;

        private void OnServerInitialized()
        {
            LoadConfigValues();
            if (!configData.Settings.RequirePermissionForFastPickup)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server.FromServer(), "server.parachuteRepackTime 0");
            }
            enabled = true;
            permission.RegisterPermission(permFastPickup, this);
            permission.RegisterPermission(permFastFlight, this);
        }

        private void OnThreatLevelUpdate(BasePlayer player)
        {
            if (!enabled) return;
            if (player == null) return;
            if (!configData.Settings.ExcludeParachuteFromClothingAmount) return;
            if (player.inventory.containerWear.GetAmount(602628465, false) > 0 && player.inventory.containerWear.itemList.Count > 2 && player.cachedThreatLevel > 0)
            {
                player.cachedThreatLevel--;
            }
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!enabled) return;
            if (container == null) return;
            if (item == null) return;

            if (!configData.Settings.ExcludeParachuteFromClothingAmount) return;
            BasePlayer player = container?.entityOwner as BasePlayer;
            if (player != null && item?.info.name == "parachute.item" && container == player.inventory.containerWear && player.inventory.containerWear.itemList.Count > 2 && player.cachedThreatLevel > 0)
            {
                player.cachedThreatLevel--;
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null) return;
            if (input == null) return;

            // Allow fast pickup of an unpacked chute
            if (input.IsDown(BUTTON.USE))
            {
                if (configData.Settings.RequirePermissionForFastPickup && !permission.UserHasPermission(player.UserIDString, permFastPickup)) { return; }
                List<ParachuteUnpacked> chutes = new List<ParachuteUnpacked>();
                Vis.Entities(player.transform.position, 5f, chutes);
                foreach (ParachuteUnpacked chuteuteunpacked in chutes)
                {
                    if (chuteuteunpacked.OwnerID == 0)
                    {
                        Item ch = ItemManager.CreateByItemID(602628465);
                        if (configData.Settings.RestoreConditionOnPickup
                            && !configData.Settings.RequirePermissionForCondition
                            && (!configData.Settings.RequirePermissionForCondition || !permission.UserHasPermission(player.UserIDString, permPickupCondition)))
                        {
                            ch.condition = chuteuteunpacked.health;
                        }

                        ch.MoveToContainer(player.inventory.containerMain);
                        chuteuteunpacked.Kill();
                        if (configData.Settings.PlaySoundOnPickup)
                        {
                            SendEffectTo(zipper, player);
                        }
                        return;
                    }
                }
                return;
            }

            // Make a deployed chute move faster with SPRINT key
            if (configData.Settings.RequirePermissionForFastFlight && !permission.UserHasPermission(player.UserIDString, permFastFlight)) { return; }
            BaseMountable what = player?.GetMounted();
            if (what?.name.Equals("assets/prefabs/vehicle/seats/parachuteseat.prefab") == true)
            {
                Parachute chute = what.gameObject.GetComponentInParent<Parachute>();
                if (input.IsDown(BUTTON.BACKWARD))
                {
                    if (input.IsDown(BUTTON.SPRINT) && chute != null)
                    {
                        if (configData.Settings.DescentBoost) chute.rigidBody.AddForce(Vector3.down, ForceMode.Impulse);
                        chute.BackInputForceMultiplier = configData.Settings.revMultiplier > 0 ? configData.Settings.revMultiplier : 2f;
                    }
                    else if (chute != null)
                    {
                        chute.BackInputForceMultiplier = 0.2f; // Default from Facepunch
                    }
                    return;
                }
                if (input.IsDown(BUTTON.FORWARD))
                {
                    if (input.IsDown(BUTTON.SPRINT) && chute != null)
                    {
                        if (configData.Settings.DescentBoost) chute.rigidBody.AddForce(Vector3.down, ForceMode.Impulse);
                        chute.ForwardTiltAcceleration = configData.Settings.fwdMultiplier > 0 ? configData.Settings.fwdMultiplier : 40f;
                    }
                    else if (chute != null)
                    {
                        chute.ForwardTiltAcceleration = 2f; // Default from Facepunch
                    }
                }
            }
        }

        private void SendEffectTo(string effect, BasePlayer player)
        {
            if (player == null) return;
            if (effect == null) return;

            var EffectInstance = new Effect();
            EffectInstance.Init(Effect.Type.Generic, player, 0, Vector3.up, Vector3.zero);
            EffectInstance.pooledstringid = StringPool.Get(effect);
            NetWrite writer = Net.sv.StartWrite();
            writer.PacketID(Message.Type.Effect);
            EffectInstance.WriteToStream(writer);
            writer.Send(new SendInfo(player.net.connection));
            EffectInstance.Clear();
        }

        #region config
        private class ConfigData
        {
            public Settings Settings;
            public VersionNumber Version;
        }

        private class Settings
        {
            public float fwdMultiplier;
            public float revMultiplier;
            public bool DescentBoost;
            public bool AllowFastPickup;
            public bool RequirePermissionForFastPickup;
            public bool RequirePermissionForFastFlight;
            public bool RequirePermissionForCondition;
            public bool PlaySoundOnPickup;
            public bool ExcludeParachuteFromClothingAmount;
            public bool RestoreConditionOnPickup;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Settings = new Settings()
                {
                    fwdMultiplier = 40f,
                    revMultiplier = 2f,
                    AllowFastPickup = true,
                    RequirePermissionForFastFlight = false,
                    RequirePermissionForFastPickup = false,
                    PlaySoundOnPickup = true
                },
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
