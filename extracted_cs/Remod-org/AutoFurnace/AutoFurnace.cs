#region License (GPL v2)
/*
    AutoFurnace
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

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

namespace Oxide.Plugins
{
    [Info("AutoFurnace", "RFC1920", "1.0.2")]
    [Description("Automatically start and stop electric furnace based on contents")]

    // If the input slots are empty, turn the furnace off.
    // If the input slots contain ore (default item allow), turn the furnace on.
    internal class AutoFurnace : RustPlugin
    {
        private object CanMoveItem(Item item, PlayerInventory inventory, ItemContainerId targetContainerId, int targetSlot, int amount)
        {
            if (item == null) return null;
            if (inventory == null) return null;

            BaseOven oven = inventory?.loot.entitySource as BaseOven;
            global::ElectricOven furnace = inventory?.loot?.entitySource?.GetComponent<global::ElectricOven>();
            ItemContainer targetContainer = inventory?.FindContainer(targetContainerId);
            if (targetContainer != null && !(targetContainer?.entityOwner is BaseOven)) return null;

            if (furnace != null && furnace is global::ElectricOven && oven?.inventory.GetSlot(0) != null && oven?.inventory.GetSlot(1) != null)
            {
                oven.StartCooking();
            }
            return null;
        }

        private void OnOvenCooked(BaseOven oven, Item item, BaseEntity slot)
        {
            global::ElectricOven furnace = oven?.GetComponent<global::ElectricOven>();
            if (furnace != null)
            {
                if (oven.inventory == null || oven.inventory.itemList.Count == 0)
                {
                    oven.StopCooking();
                }
                else if (oven.inventory.GetSlot(0) == null && oven.inventory.GetSlot(1) == null)
                {
                    oven.StopCooking();
                }
            }
        }
    }
}