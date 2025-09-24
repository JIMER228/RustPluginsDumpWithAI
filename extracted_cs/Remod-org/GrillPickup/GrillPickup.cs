#region License (GPL v2)
/*
    Floor grill pickup
    Copyright (c) RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; version 2
    of the License only.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GrillPickup", "RFC1920", "1.0.6")]
    [Description("Allows players to pickup floor grills.")]
    internal class GrillPickup : RustPlugin
    {
        private ConfigData configData;
        private const string permUse = "grillpickup.use";

        private static List<ulong> pickingUp = new List<ulong>();
        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            LoadConfigValues();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["pustart"] = "Pickup start.  Wait 2 seconds.",
                ["pufailed"] = "Pickup failed...",
                ["pickup"] = "Picked up grill!",
                ["tpickup"] = "Picked up triangle grill!"
            }, this);
        }

        private object RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            RaycastHit[] hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            const float distance = 6f;
            object target = false;
            foreach (RaycastHit hit in hits)
            {
                BaseEntity ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }
            return target;
        }

        private async void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            if (!player.userID.IsSteamId()) return;

            if (!configData.RequirePermission || (configData.RequirePermission && permission.UserHasPermission(player?.UserIDString, permUse)))
            {
                BaseEntity target = RaycastAll<BaseEntity>(player.eyes.HeadRay()) as BaseEntity;
                if (target == null) return;
                if (!target.ShortPrefabName.Contains("grill")) return;

                if (target.OwnerID == player.userID && player.GetHeldEntity() is Hammer && input.WasJustPressed(BUTTON.USE))
                {
                    if (!pickingUp.Contains(player.userID)) Message(player.IPlayer, "pustart");
                    pickingUp.Add(player.userID);
                    await WaitButton(player, input);
                    if (!input.IsDown(BUTTON.USE) || target == null)
                    {
                        //Puts("Player didn't hold button long enough");
                        Message(player.IPlayer, "pufailed");
                        pickingUp.Remove(player.userID);
                        return;
                    }

                    Item newgrill = null;
                    if (target.ShortPrefabName.Equals("floor.grill"))
                    {
                        newgrill = ItemManager.CreateByItemID(936496778, 1, 0);
                        if (configData.ApplyDamage)
                        {
                            newgrill.condition *= configData.DamageMultiplier;
                        }
                        newgrill.MoveToContainer(player.inventory.containerMain);
                        Message(player.IPlayer, "pickup");
                    }
                    else if (target.ShortPrefabName.Equals("floor.triangle.grill"))
                    {
                        newgrill = ItemManager.CreateByItemID(1983621560, 1, 0);
                        if (configData.ApplyDamage)
                        {
                            newgrill.condition *= configData.DamageMultiplier;
                        }
                        newgrill.MoveToContainer(player.inventory.containerMain);
                        Message(player.IPlayer, "tpickup");
                    }
                    pickingUp.Remove(player.userID);
                    if (newgrill == null || target == null) return;
                    target.DestroyShared();
                }
            }
        }

        public static async Task WaitButton(BasePlayer player, InputState input)
        {
            if (input == null || player == null)
            {
                pickingUp.Remove(player.userID);
                return;
            }
            await Task.Delay(1000);
        }

        #region config
        private class ConfigData
        {
            public bool RequirePermission;
            public bool ApplyDamage;
            public float DamageMultiplier;
            public VersionNumber Version;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                RequirePermission = false,
                ApplyDamage = false,
                DamageMultiplier = 0.8f,
                Version = Version
            };
            SaveConfig(config);
        }

        private void LoadConfigValues()
        {
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < new VersionNumber(1, 0, 6))
            {
                configData.DamageMultiplier = 0.8f;
            }
            if (configData.DamageMultiplier == 0 || configData.DamageMultiplier > 1)
            {
                configData.DamageMultiplier = 0.8f;
            }
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
