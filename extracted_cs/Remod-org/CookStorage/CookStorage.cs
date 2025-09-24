#region License (GPL v2)
/*
    CookStorage - Allow any item to be added to campfires, ovens, furnaces, etc.
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.0.

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
using System;
using System.Collections.Generic;
using HarmonyLib;
using Oxide.Core;
using Oxide.Core.Plugins;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("Cook Storage", "RFC1920", "0.0.4")]
    [Description("Allow non-food items in ovens and furnaces.  Also, optionally, allow cooking of those items.")]
    internal class CookStorage : RustPlugin
    {
        private ConfigData configData;

        private void OnServerInitialized()
        {
            LoadConfigVariables();
        }

        [AutoPatch]
        [HarmonyPatch(typeof(BaseOven), "GetAllowedSlots")]
        public static class GetAllowedSlotsPatch
        {
            [HarmonyPrefix]
            static bool Prefix(BaseOven __instance, ref Item item, ref BaseOven.MinMax? __result)
            {
                if (!__instance.IsBurnableItem(item) && !__instance.IsOutputItem(item) && !__instance.IsMaterialInput(item) && !__instance.IsMaterialOutput(item))
                {
                    int allowed = __instance.fuelSlots + __instance.inputSlots + __instance.outputSlots;
                    __result = new BaseOven.MinMax?(new BaseOven.MinMax(0, allowed));
                    return false;
                }
                return true;
            }
        }

        private Dictionary<ulong, int> cookingItems = new Dictionary<ulong, int>();
        private object OnOvenCook(BaseOven oven, Item item)
        {
            if (!configData.cookItems) return null;
            List<Item> cooking = new List<Item>();

            //Puts(oven?.ShortPrefabName);
            switch (oven?.ShortPrefabName)
            {
                case "campfire":
                case "cursedcauldron.deployed":
                    cooking.Add(oven.inventory.GetSlot(1));
                    break;
                case "bbq.deployed":
                    cooking.Add(oven?.inventory.GetSlot(1));
                    cooking.Add(oven?.inventory.GetSlot(2));
                    cooking.Add(oven?.inventory.GetSlot(3));
                    break;
            }

            ulong i = 0;
            foreach (Item cook in cooking)
            {
                i++;
                if (cook == null) continue;
                if (cook?.info?.category.ToString() == "Food")
                {
                    // Allow for normal food cooking
                    continue;
                }
                ulong itemid = (ulong)cook?.info?.GetInstanceID();

                if (configData.itemlist.ContainsKey(cook?.info?.name))
                {
                    ItemCookInfo cookingInfo = configData.itemlist[cook?.info?.name];
                    string output = cookingInfo.result;
                    int delay = cookingInfo.delay;
                    int mult = cookingInfo.mult;

                    if (!cookingItems.ContainsKey(oven.net.ID.Value * itemid * i))
                    {
                        cookingItems.Add(oven.net.ID.Value * itemid * i, delay);
                    }
                    cookingItems[oven.net.ID.Value * itemid * i]--;

                    if (cookingItems[oven.net.ID.Value * itemid * i] == 0)
                    {
                        cookingItems[oven.net.ID.Value * itemid * i] = delay;
                        cook.amount--;
                        cook.MarkDirty();

                        Item frags = ItemManager.CreateByName(output, mult);
                        frags.MoveToContainer(oven.inventory);
                    }
                }
            }

            return null;
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

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                itemlist = new Dictionary<string, ItemCookInfo>()
                {
                    { "1module_cockpit_armored.item", new ItemCookInfo("metal.fragments") },
                    { "1module_cockpit.item", new ItemCookInfo("metal.fragments") },
                    { "1module_cockpit_with_engine.item", new ItemCookInfo("metal.fragments") },
                    { "1module_engine.item", new ItemCookInfo("metal.fragments") },
                    { "1module_flatbed.item", new ItemCookInfo("metal.fragments") },
                    { "1module_passengers_armored.item", new ItemCookInfo("metal.fragments") },
                    { "1module_rear_seats.item", new ItemCookInfo("metal.fragments") },
                    { "1module_storage.item", new ItemCookInfo("metal.fragments") },
                    { "1module_taxi.item", new ItemCookInfo("metal.fragments") },
                    { "2module_camper.item", new ItemCookInfo("metal.fragments") },
                    { "2module_chassis.item", new ItemCookInfo("metal.fragments") },
                    { "2module_flatbed.item", new ItemCookInfo("metal.fragments") },
                    { "2module_fuel_tank.item", new ItemCookInfo("metal.fragments") },
                    { "2module_passengers.item", new ItemCookInfo("metal.fragments") },
                    { "3module_chassis.item", new ItemCookInfo("metal.fragments") },
                    { "4module_chassis.item", new ItemCookInfo("metal.fragments") },
                    { "8xscope.item", new ItemCookInfo("metal.fragments") },
                    { "abovegroundpool.item", new ItemCookInfo("metal.fragments") },
                    { "advent.calendar.item", new ItemCookInfo("metal.fragments") },
                    { "ak47u_diver.item", new ItemCookInfo("metal.fragments") },
                    { "ak47u._ice.item", new ItemCookInfo("metal.fragments") },
                    { "ak47u.item", new ItemCookInfo("metal.fragments") },
                    { "ammo_snowballgun.item", new ItemCookInfo("metal.fragments") },
                    { "andswitch.item", new ItemCookInfo("metal.fragments") },
                    { "antiradpills.item", new ItemCookInfo("metal.fragments") },
                    { "audioalarm.item", new ItemCookInfo("metal.fragments") },
                    { "autoturret.item", new ItemCookInfo("metal.fragments") },
                    { "axe_salvaged.item", new ItemCookInfo("metal.fragments") },
                    { "bandage.item", new ItemCookInfo("metal.fragments") },
                    { "barrelcostume.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.concrete.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.cover.wood.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.metal.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.sandbags.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.stone.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.wood.item", new ItemCookInfo("metal.fragments") },
                    { "barricade.woodwire.item", new ItemCookInfo("metal.fragments") },
                    { "_base_vehicle_chassis_item.item", new ItemCookInfo("metal.fragments") },
                    { "_base_vehicle_module_item.item", new ItemCookInfo("metal.fragments") },
                    { "bass.item", new ItemCookInfo("metal.fragments") },
                    { "battery_small.item", new ItemCookInfo("metal.fragments") },
                    { "bbq.item", new ItemCookInfo("metal.fragments") },
                    { "beachchair.item", new ItemCookInfo("metal.fragments") },
                    { "beachparasol.item", new ItemCookInfo("metal.fragments") },
                    { "beachtable.item", new ItemCookInfo("metal.fragments") },
                    { "beachtowel.item", new ItemCookInfo("metal.fragments") },
                    { "beartrap.item", new ItemCookInfo("metal.fragments") },
                    { "bed.item", new ItemCookInfo("metal.fragments") },
                    { "binoculars.item", new ItemCookInfo("metal.fragments") },
                    { "bolt_rifle.item", new ItemCookInfo("metal.fragments") },
                    { "bone_club.item", new ItemCookInfo("metal.fragments") },
                    { "boogieboard.item", new ItemCookInfo("metal.fragments") },
                    { "boombox.item", new ItemCookInfo("metal.fragments") },
                    { "boomboxportable.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.blue.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.champagne.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.green.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.orange.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.pattern.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.red.item", new ItemCookInfo("metal.fragments") },
                    { "boomer.violet.item", new ItemCookInfo("metal.fragments") },
                    { "bow_hunting.item", new ItemCookInfo("metal.fragments") },
                    { "box_wooden.item", new ItemCookInfo("metal.fragments") },
                    { "box.wooden.large.item", new ItemCookInfo("metal.fragments") },
                    { "bronzeegg.item", new ItemCookInfo("metal.fragments") },
                    { "bucket.item", new ItemCookInfo("metal.fragments") },
                    { "building_planner.item", new ItemCookInfo("metal.fragments") },
                    { "bunny.onsie.item", new ItemCookInfo("metal.fragments") },
                    { "burlap_shirt.item", new ItemCookInfo("metal.fragments") },
                    { "burlap_shoes.item", new ItemCookInfo("metal.fragments") },
                    { "burstmodule.item", new ItemCookInfo("metal.fragments") },
                    { "butcherknife.item", new ItemCookInfo("metal.fragments") },
                    { "button.item", new ItemCookInfo("metal.fragments") },
                    { "cabletunnel.item", new ItemCookInfo("metal.fragments") },
                    { "cake.item", new ItemCookInfo("metal.fragments") },
                    { "camera.item", new ItemCookInfo("metal.fragments") },
                    { "campfire.item", new ItemCookInfo("metal.fragments") },
                    { "candy_cane_club.item", new ItemCookInfo("metal.fragments") },
                    { "candycane.item", new ItemCookInfo("metal.fragments") },
                    { "captainslog.item", new ItemCookInfo("metal.fragments") },
                    { "carburetor1.item", new ItemCookInfo("metal.fragments") },
                    { "carburetor2.item", new ItemCookInfo("metal.fragments") },
                    { "carburetor3.item", new ItemCookInfo("metal.fragments") },
                    { "cardtable.item", new ItemCookInfo("metal.fragments") },
                    { "carvable.pumpkin.item", new ItemCookInfo("metal.fragments") },
                    { "cassette.item", new ItemCookInfo("metal.fragments") },
                    { "cassette.medium.item", new ItemCookInfo("metal.fragments") },
                    { "cassetterecorder.item", new ItemCookInfo("metal.fragments") },
                    { "cassette.short.item", new ItemCookInfo("metal.fragments") },
                    { "cauldron.item", new ItemCookInfo("metal.fragments") },
                    { "cctv_camera.item", new ItemCookInfo("metal.fragments") },
                    { "ceilinglight.item", new ItemCookInfo("metal.fragments") },
                    { "chainsaw.item", new ItemCookInfo("metal.fragments") },
                    { "chair.icethrone.item", new ItemCookInfo("metal.fragments") },
                    { "chair.item", new ItemCookInfo("metal.fragments") },
                    { "chineselantern.item", new ItemCookInfo("metal.fragments") },
                    { "christmas_door_wreath.item", new ItemCookInfo("metal.fragments") },
                    { "clantable.item", new ItemCookInfo("metal.fragments") },
                    { "coffinstorage.item", new ItemCookInfo("metal.fragments") },
                    { "composter.item", new ItemCookInfo("metal.fragments") },
                    { "compound_bow.item", new ItemCookInfo("metal.fragments") },
                    { "computerstation.item", new ItemCookInfo("metal.fragments") },
                    { "concrete_hatchet.item", new ItemCookInfo("metal.fragments") },
                    { "concrete_pickaxe.item", new ItemCookInfo("metal.fragments") },
                    { "connectedspeaker.item", new ItemCookInfo("metal.fragments") },
                    { "counter.item", new ItemCookInfo("metal.fragments") },
                    { "cowbell.item", new ItemCookInfo("metal.fragments") },
                    { "crankshaft1.item", new ItemCookInfo("metal.fragments") },
                    { "crankshaft2.item", new ItemCookInfo("metal.fragments") },
                    { "crankshaft3.item", new ItemCookInfo("metal.fragments") },
                    { "cratecostume.item", new ItemCookInfo("metal.fragments") },
                    { "crossbow.item", new ItemCookInfo("metal.fragments") },
                    { "cupboard.tool.item", new ItemCookInfo("metal.fragments") },
                    { "detonator.item", new ItemCookInfo("metal.fragments") },
                    { "discoball.item", new ItemCookInfo("metal.fragments") },
                    { "discofloor.item", new ItemCookInfo("metal.fragments") },
                    { "discofloor.largetiles.item", new ItemCookInfo("metal.fragments") },
                    { "diver_hatchet.item", new ItemCookInfo("metal.fragments") },
                    { "diver_pickaxe.item", new ItemCookInfo("metal.fragments") },
                    { "diver_torch.item", new ItemCookInfo("metal.fragments") },
                    { "diving.fins.item", new ItemCookInfo("metal.fragments") },
                    { "diving.mask.item", new ItemCookInfo("metal.fragments") },
                    { "diving.tank.item", new ItemCookInfo("metal.fragments") },
                    { "diving.wetsuit.item", new ItemCookInfo("metal.fragments") },
                    { "dogtag_blue.item", new ItemCookInfo("metal.fragments") },
                    { "dogtag_neutral.item", new ItemCookInfo("metal.fragments") },
                    { "dogtag_red.item", new ItemCookInfo("metal.fragments") },
                    { "doorcloser.item", new ItemCookInfo("metal.fragments") },
                    { "doorcontroller.item", new ItemCookInfo("metal.fragments") },
                    { "doorgarland.item", new ItemCookInfo("metal.fragments") },
                    { "door.hinged.metal.industrial.a.item", new ItemCookInfo("metal.fragments") },
                    { "door.hinged.metal.industrial.d.item", new ItemCookInfo("metal.fragments") },
                    { "door_key.item", new ItemCookInfo("metal.fragments") },
                    { "double_doorgarland.item", new ItemCookInfo("metal.fragments") },
                    { "dragondoorknocker.item", new ItemCookInfo("metal.fragments") },
                    { "drone.item", new ItemCookInfo("metal.fragments") },
                    { "dropbox.item", new ItemCookInfo("metal.fragments") },
                    { "drop_item", new ItemCookInfo("metal.fragments") },
                    { "drumkit.item", new ItemCookInfo("metal.fragments") },
                    { "ducttape.item", new ItemCookInfo("metal.fragments") },
                    { "easter_basket.item", new ItemCookInfo("metal.fragments") },
                    { "easter_door_wreath.item", new ItemCookInfo("metal.fragments") },
                    { "egg.suit.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.blocker.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.branch.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.combiner.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.heater.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.memorycell.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.modularcarlift.item", new ItemCookInfo("metal.fragments") },
                    { "electrical.random.switch.item", new ItemCookInfo("metal.fragments") },
                    { "electric.flasherlight.item", new ItemCookInfo("metal.fragments") },
                    { "electricfurnace.item", new ItemCookInfo("metal.fragments") },
                    { "electric.sirenlight.item", new ItemCookInfo("metal.fragments") },
                    { "electric.sprinkler.item", new ItemCookInfo("metal.fragments") },
                    { "elevator.item", new ItemCookInfo("metal.fragments") },
                    { "explosive.satchel.item", new ItemCookInfo("metal.fragments") },
                    { "explosives.item", new ItemCookInfo("metal.fragments") },
                    { "explosive.timed.item", new ItemCookInfo("metal.fragments") },
                    { "extendedmags.item", new ItemCookInfo("metal.fragments") },
                    { "firecrackers.item", new ItemCookInfo("metal.fragments") },
                    { "fireplace.item", new ItemCookInfo("metal.fragments") },
                    { "fishing_rod.item", new ItemCookInfo("metal.fragments") },
                    { "fishing.tackle.item", new ItemCookInfo("metal.fragments") },
                    { "flamethrower.item", new ItemCookInfo("metal.fragments") },
                    { "flameturret.item", new ItemCookInfo("metal.fragments") },
                    { "flare.item", new ItemCookInfo("metal.fragments") },
                    { "flashlight.item", new ItemCookInfo("metal.fragments") },
                    { "flashlightmod.item", new ItemCookInfo("metal.fragments") },
                    { "fluid.combiner.item", new ItemCookInfo("metal.fragments") },
                    { "fluidsplitter.item", new ItemCookInfo("metal.fragments") },
                    { "fluidswitch.item", new ItemCookInfo("metal.fragments") },
                    { "flute.item", new ItemCookInfo("metal.fragments") },
                    { "fogmachine.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.01.head.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.01.legs.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.01.torso.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.02.head.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.02.legs.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.02.torso.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.03.head.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.03.legs.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteins.monster.03.torso.item", new ItemCookInfo("metal.fragments") },
                    { "frankensteintable.item", new ItemCookInfo("metal.fragments") },
                    { "fridge.item", new ItemCookInfo("metal.fragments") },
                    { "frogboots.item", new ItemCookInfo("metal.fragments") },
                    { "furnace.item", new ItemCookInfo("metal.fragments") },
                    { "furnace.large.item", new ItemCookInfo("metal.fragments") },
                    { "fuse.item", new ItemCookInfo("metal.fragments") },
                    { "gears.item", new ItemCookInfo("metal.fragments") },
                    { "geiger_counter.item", new ItemCookInfo("metal.fragments") },
                    { "generator.small.item", new ItemCookInfo("metal.fragments") },
                    { "generator.wind.scrap.item", new ItemCookInfo("metal.fragments") },
                    { "ghostsheet.item", new ItemCookInfo("metal.fragments") },
                    { "giantcandycane.item", new ItemCookInfo("metal.fragments") },
                    { "giantlollipops.item", new ItemCookInfo("metal.fragments") },
                    { "glock.item", new ItemCookInfo("metal.fragments") },
                    { "gloves.burlap.item", new ItemCookInfo("metal.fragments") },
                    { "gloves.leather.item", new ItemCookInfo("metal.fragments") },
                    { "gloves.roadsign.item", new ItemCookInfo("metal.fragments") },
                    { "gloves.tactical.item", new ItemCookInfo("metal.fragments") },
                    { "gloweyes.item", new ItemCookInfo("metal.fragments") },
                    { "goldegg.item", new ItemCookInfo("metal.fragments") },
                    { "gravestone.stone.item", new ItemCookInfo("metal.fragments") },
                    { "gravestone.wood.item", new ItemCookInfo("metal.fragments") },
                    { "graveyardfence.item", new ItemCookInfo("metal.fragments") },
                    { "grenade.beancan.item", new ItemCookInfo("metal.fragments") },
                    { "grenade.f1.item", new ItemCookInfo("metal.fragments") },
                    { "grenade.flashbang.item", new ItemCookInfo("metal.fragments") },
                    { "grenade.molotov.item", new ItemCookInfo("metal.fragments") },
                    { "guitar.item", new ItemCookInfo("metal.fragments") },
                    { "hab.repair.item", new ItemCookInfo("metal.fragments") },
                    { "halloween_candy.item", new ItemCookInfo("metal.fragments") },
                    { "halloween.mummysuit.item", new ItemCookInfo("metal.fragments") },
                    { "halloween.scarecrowhead.item", new ItemCookInfo("metal.fragments") },
                    { "halloween.scarecrow.item", new ItemCookInfo("metal.fragments") },
                    { "halterneck.hide.item", new ItemCookInfo("metal.fragments") },
                    { "hammer.item", new ItemCookInfo("metal.fragments") },
                    { "hammer_salvaged.item", new ItemCookInfo("metal.fragments") },
                    { "hat.beenie.item", new ItemCookInfo("metal.fragments") },
                    { "hat.boonie.item", new ItemCookInfo("metal.fragments") },
                    { "hat.bucket.item", new ItemCookInfo("metal.fragments") },
                    { "hat.bullmask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.bunnyears.item", new ItemCookInfo("metal.fragments") },
                    { "hat.bunnyhat.item", new ItemCookInfo("metal.fragments") },
                    { "hat.burlap.wrap.item", new ItemCookInfo("metal.fragments") },
                    { "hat.candle.item", new ItemCookInfo("metal.fragments") },
                    { "hat.cap.base.item", new ItemCookInfo("metal.fragments") },
                    { "hat.cap.headset.item", new ItemCookInfo("metal.fragments") },
                    { "hatchet.item", new ItemCookInfo("metal.fragments") },
                    { "hat.clatter.item", new ItemCookInfo("metal.fragments") },
                    { "hat.coffeecan.item", new ItemCookInfo("metal.fragments") },
                    { "hat.deerskullmask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.dragonmask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.gas.mask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.heavyplate.item", new ItemCookInfo("metal.fragments") },
                    { "hat.miner.item", new ItemCookInfo("metal.fragments") },
                    { "hat.nvg.item", new ItemCookInfo("metal.fragments") },
                    { "hat.party.item", new ItemCookInfo("metal.fragments") },
                    { "hat.rabbitmask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.ratmask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.reindeerantlersheadband.item", new ItemCookInfo("metal.fragments") },
                    { "hat.riot.item", new ItemCookInfo("metal.fragments") },
                    { "hat.snowmanhelmet.item", new ItemCookInfo("metal.fragments") },
                    { "hat.tigermask.item", new ItemCookInfo("metal.fragments") },
                    { "hat.wolf.item", new ItemCookInfo("metal.fragments") },
                    { "hat.woodarmor.item", new ItemCookInfo("metal.fragments") },
                    { "hazmat_suit.arcticsuit.item", new ItemCookInfo("metal.fragments") },
                    { "hazmatsuit.diver.item", new ItemCookInfo("metal.fragments") },
                    { "hazmat_suit.item", new ItemCookInfo("metal.fragments") },
                    { "hazmat_suit.lumberjack.item", new ItemCookInfo("metal.fragments") },
                    { "hazmat_suit.nomadsuit.item", new ItemCookInfo("metal.fragments") },
                    { "hazmat_suit.spacesuit.item", new ItemCookInfo("metal.fragments") },
                    { "hbhfsensor.item", new ItemCookInfo("metal.fragments") },
                    { "hideboots.item", new ItemCookInfo("metal.fragments") },
                    { "hideskirt.item", new ItemCookInfo("metal.fragments") },
                    { "hidevest.item", new ItemCookInfo("metal.fragments") },
                    { "hitchtrough.item", new ItemCookInfo("metal.fragments") },
                    { "hmlmg.item", new ItemCookInfo("metal.fragments") },
                    { "hobobarrel.item", new ItemCookInfo("metal.fragments") },
                    { "holosight.item", new ItemCookInfo("metal.fragments") },
                    { "hoodie.red.item", new ItemCookInfo("metal.fragments") },
                    { "horse.armor.roadsign.item", new ItemCookInfo("metal.fragments") },
                    { "horse.armor.wood.item", new ItemCookInfo("metal.fragments") },
                    { "horse.saddlebag.item", new ItemCookInfo("metal.fragments") },
                    { "horse.saddle.double.item", new ItemCookInfo("metal.fragments") },
                    { "horse.saddle.item", new ItemCookInfo("metal.fragments") },
                    { "horse.saddle.single.item", new ItemCookInfo("metal.fragments") },
                    { "horse.shoes.advanced.item", new ItemCookInfo("metal.fragments") },
                    { "horse.shoes.basic.item", new ItemCookInfo("metal.fragments") },
                    { "hosetool.item", new ItemCookInfo("metal.fragments") },
                    { "icepick_salvaged.item", new ItemCookInfo("metal.fragments") },
                    { "icewall.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_blue.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_gray.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_green.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_orange.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_pink.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_purple.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_red.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_white.item", new ItemCookInfo("metal.fragments") },
                    { "idtag_yellow.item", new ItemCookInfo("metal.fragments") },
                    { "igniter.item", new ItemCookInfo("metal.fragments") },
                    { "industrialcombiner.item", new ItemCookInfo("metal.fragments") },
                    { "industrialconveyor.item", new ItemCookInfo("metal.fragments") },
                    { "industrialcrafter.item", new ItemCookInfo("metal.fragments") },
                    { "industrialsplitter.item", new ItemCookInfo("metal.fragments") },
                    { "industrial.wall.lamp.green.item", new ItemCookInfo("metal.fragments") },
                    { "industrial.wall.lamp.item", new ItemCookInfo("metal.fragments") },
                    { "industrial.wall.lamp.red.item", new ItemCookInfo("metal.fragments") },
                    { "innertube.horse.item", new ItemCookInfo("metal.fragments") },
                    { "innertube.item", new ItemCookInfo("metal.fragments") },
                    { "innertube.unicorn.item", new ItemCookInfo("metal.fragments") },
                    { "instant_camera.item", new ItemCookInfo("metal.fragments") },
                    { "item_drop", new ItemCookInfo("metal.fragments") },
                    { "item_drop_backpack", new ItemCookInfo("metal.fragments") },
                    { "item_drop_buoyant", new ItemCookInfo("metal.fragments") },
                    { "item.painted.storage", new ItemCookInfo("metal.fragments") },
                    { "jacket.bonearmor.item", new ItemCookInfo("metal.fragments") },
                    { "jacket.heavyplate.item", new ItemCookInfo("metal.fragments") },
                    { "jacket.snow.item", new ItemCookInfo("metal.fragments") },
                    { "jacket.vagabond.item", new ItemCookInfo("metal.fragments") },
                    { "jackhammer.item", new ItemCookInfo("metal.fragments") },
                    { "jackolantern.angry.item", new ItemCookInfo("metal.fragments") },
                    { "jackolantern.happy.item", new ItemCookInfo("metal.fragments") },
                    { "jerrycanguitar.item", new ItemCookInfo("metal.fragments") },
                    { "knife_bone.item", new ItemCookInfo("metal.fragments") },
                    { "knife.combat.item", new ItemCookInfo("metal.fragments") },
                    { "l96.item", new ItemCookInfo("metal.fragments") },
                    { "landmine.item", new ItemCookInfo("metal.fragments") },
                    { "lantern.item", new ItemCookInfo("metal.fragments") },
                    { "largecandles.item", new ItemCookInfo("metal.fragments") },
                    { "largemedkit.item", new ItemCookInfo("metal.fragments") },
                    { "large.rechargable.battery.item", new ItemCookInfo("metal.fragments") },
                    { "laserdetector.item", new ItemCookInfo("metal.fragments") },
                    { "laserlight.item", new ItemCookInfo("metal.fragments") },
                    { "lasersight.item", new ItemCookInfo("metal.fragments") },
                    { "lock.code.item", new ItemCookInfo("metal.fragments") },
                    { "locker.item", new ItemCookInfo("metal.fragments") },
                    { "lock.key.item", new ItemCookInfo("metal.fragments") },
                    { "longsword.item", new ItemCookInfo("metal.fragments") },
                    { "lootbag.large.item", new ItemCookInfo("metal.fragments") },
                    { "lootbag.medium.item", new ItemCookInfo("metal.fragments") },
                    { "lootbag.small.item", new ItemCookInfo("metal.fragments") },
                    { "lr300.item", new ItemCookInfo("metal.fragments") },
                    { "lumberjack_axe.item", new ItemCookInfo("metal.fragments") },
                    { "lumberjack_hoodie.item", new ItemCookInfo("metal.fragments") },
                    { "lumberjack_pick.item", new ItemCookInfo("metal.fragments") },
                    { "m249.item", new ItemCookInfo("metal.fragments") },
                    { "m39.item", new ItemCookInfo("metal.fragments") },
                    { "m92.item", new ItemCookInfo("metal.fragments") },
                    { "mace.baseballbat.item", new ItemCookInfo("metal.fragments") },
                    { "mace.item", new ItemCookInfo("metal.fragments") },
                    { "machete.item", new ItemCookInfo("metal.fragments") },
                    { "mailbox.item", new ItemCookInfo("metal.fragments") },
                    { "mask.balaclava.item", new ItemCookInfo("metal.fragments") },
                    { "mask.bandana.item", new ItemCookInfo("metal.fragments") },
                    { "mask.metal.item", new ItemCookInfo("metal.fragments") },
                    { "medium.rechargable.battery.item", new ItemCookInfo("metal.fragments") },
                    { "megaphone.item", new ItemCookInfo("metal.fragments") },
                    { "metalblade.item", new ItemCookInfo("metal.fragments") },
                    { "metal.facemask.hockey.item", new ItemCookInfo("metal.fragments") },
                    { "metal.facemask.icemask.item", new ItemCookInfo("metal.fragments") },
                    { "metalpipe.item", new ItemCookInfo("metal.fragments") },
                    { "metal.plate.torso.icevest.item", new ItemCookInfo("metal.fragments") },
                    { "metal_plate_torso.item", new ItemCookInfo("metal.fragments") },
                    { "mgl.item", new ItemCookInfo("metal.fragments") },
                    { "microphonestand.item", new ItemCookInfo("metal.fragments") },
                    { "mixingtable.item", new ItemCookInfo("metal.fragments") },
                    { "mobilephone.item", new ItemCookInfo("metal.fragments") },
                    { "movember_moustache_card.item", new ItemCookInfo("metal.fragments") },
                    { "movember_moustache_style01.item", new ItemCookInfo("metal.fragments") },
                    { "mp5.item", new ItemCookInfo("metal.fragments") },
                    { "muzzlebooster.item", new ItemCookInfo("metal.fragments") },
                    { "muzzlebrake.item", new ItemCookInfo("metal.fragments") },
                    { "nailgun.item", new ItemCookInfo("metal.fragments") },
                    { "nailgunnail.item", new ItemCookInfo("metal.fragments") },
                    { "nest.hat.item", new ItemCookInfo("metal.fragments") },
                    { "newyeargong.item", new ItemCookInfo("metal.fragments") },
                    { "ninja.suit.item", new ItemCookInfo("metal.fragments") },
                    { "note.item", new ItemCookInfo("metal.fragments") },
                    { "orswitch.item", new ItemCookInfo("metal.fragments") },
                    { "paddle.item", new ItemCookInfo("metal.fragments") },
                    { "paddlingpool.item", new ItemCookInfo("metal.fragments") },
                    { "paintedeggs.item", new ItemCookInfo("metal.fragments") },
                    { "pants.burlap.item", new ItemCookInfo("metal.fragments") },
                    { "pants.cargo.item", new ItemCookInfo("metal.fragments") },
                    { "pants.heavyplate.item", new ItemCookInfo("metal.fragments") },
                    { "pants.hide.item", new ItemCookInfo("metal.fragments") },
                    { "pants.roadsign.item", new ItemCookInfo("metal.fragments") },
                    { "pants.shorts.item", new ItemCookInfo("metal.fragments") },
                    { "paper.item", new ItemCookInfo("metal.fragments") },
                    { "photoframe.landscape.item", new ItemCookInfo("metal.fragments") },
                    { "photoframe.large.item", new ItemCookInfo("metal.fragments") },
                    { "photoframe.portrait.item", new ItemCookInfo("metal.fragments") },
                    { "photo.item", new ItemCookInfo("metal.fragments") },
                    { "piano.item", new ItemCookInfo("metal.fragments") },
                    { "pickaxe.item", new ItemCookInfo("metal.fragments") },
                    { "pickup_item", new ItemCookInfo("metal.fragments") },
                    { "pipetool.item", new ItemCookInfo("metal.fragments") },
                    { "pistol_eoka.item", new ItemCookInfo("metal.fragments") },
                    { "pistol_revolver.item", new ItemCookInfo("metal.fragments") },
                    { "pistol_semiauto.item", new ItemCookInfo("metal.fragments") },
                    { "pistons1.item", new ItemCookInfo("metal.fragments") },
                    { "pistons2.item", new ItemCookInfo("metal.fragments") },
                    { "pistons3.item", new ItemCookInfo("metal.fragments") },
                    { "pitchfork.item", new ItemCookInfo("metal.fragments") },
                    { "planter.large.item", new ItemCookInfo("metal.fragments") },
                    { "planter.small.item", new ItemCookInfo("metal.fragments") },
                    { "poncho.hide.item", new ItemCookInfo("metal.fragments") },
                    { "pookie.item", new ItemCookInfo("metal.fragments") },
                    { "poweredwaterpurifier.item", new ItemCookInfo("metal.fragments") },
                    { "present.large.item", new ItemCookInfo("metal.fragments") },
                    { "present.medium.item", new ItemCookInfo("metal.fragments") },
                    { "present.small.item", new ItemCookInfo("metal.fragments") },
                    { "pressurepad.item", new ItemCookInfo("metal.fragments") },
                    { "propanetank.item", new ItemCookInfo("metal.fragments") },
                    { "ptz_cctv_camera.item", new ItemCookInfo("metal.fragments") },
                    { "pumpkin_basket.item", new ItemCookInfo("metal.fragments") },
                    { "python.item", new ItemCookInfo("metal.fragments") },
                    { "reactivetarget.item", new ItemCookInfo("metal.fragments") },
                    { "repair_bench.item", new ItemCookInfo("metal.fragments") },
                    { "research_table.item", new ItemCookInfo("metal.fragments") },
                    { "rfbroadcaster.item", new ItemCookInfo("metal.fragments") },
                    { "rfpager.item", new ItemCookInfo("metal.fragments") },
                    { "rfreceiver.item", new ItemCookInfo("metal.fragments") },
                    { "riflebody.item", new ItemCookInfo("metal.fragments") },
                    { "roadsign_armor.item", new ItemCookInfo("metal.fragments") },
                    { "roadsigns.item", new ItemCookInfo("metal.fragments") },
                    { "rocket_launcher.item", new ItemCookInfo("metal.fragments") },
                    { "romancandle.blue.item", new ItemCookInfo("metal.fragments") },
                    { "romancandle.green.item", new ItemCookInfo("metal.fragments") },
                    { "romancandle.red.item", new ItemCookInfo("metal.fragments") },
                    { "romancandle.violet.item", new ItemCookInfo("metal.fragments") },
                    { "rope.item", new ItemCookInfo("metal.fragments") },
                    { "rug.bear.item", new ItemCookInfo("metal.fragments") },
                    { "rug.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_a.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_b.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_c.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_d.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_e.item", new ItemCookInfo("metal.fragments") },
                    { "rustige_egg_f.item", new ItemCookInfo("metal.fragments") },
                    { "salvaged_cleaver.item", new ItemCookInfo("metal.fragments") },
                    { "salvaged_sword.item", new ItemCookInfo("metal.fragments") },
                    { "sam.rocket.item", new ItemCookInfo("metal.fragments") },
                    { "sam.site.item", new ItemCookInfo("metal.fragments") },
                    { "santabeard.item", new ItemCookInfo("metal.fragments") },
                    { "santahat.item", new ItemCookInfo("metal.fragments") },
                    { "scarecrow.item", new ItemCookInfo("metal.fragments") },
                    { "scientistsuitarctic.item", new ItemCookInfo("metal.fragments") },
                    { "scientistsuitheavy.item", new ItemCookInfo("metal.fragments") },
                    { "scientistsuit.item", new ItemCookInfo("metal.fragments") },
                    { "scientistsuitnvgm.item", new ItemCookInfo("metal.fragments") },
                    { "scientistsuitpeacekeeper.item", new ItemCookInfo("metal.fragments") },
                    { "scope.small.item", new ItemCookInfo("metal.fragments") },
                    { "searchlight.item", new ItemCookInfo("metal.fragments") },
                    { "secretlabchair.item", new ItemCookInfo("metal.fragments") },
                    { "semi_auto_rifle.item", new ItemCookInfo("metal.fragments") },
                    { "semibody.item", new ItemCookInfo("metal.fragments") },
                    { "sewingkit.item", new ItemCookInfo("metal.fragments") },
                    { "sheetmetal.item", new ItemCookInfo("metal.fragments") },
                    { "shelves.item", new ItemCookInfo("metal.fragments") },
                    { "shirt.collared.item", new ItemCookInfo("metal.fragments") },
                    { "shirt.tanktop.item", new ItemCookInfo("metal.fragments") },
                    { "shoes.boots.brown.item", new ItemCookInfo("metal.fragments") },
                    { "shotgun_double.item", new ItemCookInfo("metal.fragments") },
                    { "shotgun_pump.item", new ItemCookInfo("metal.fragments") },
                    { "shotguntrap.item", new ItemCookInfo("metal.fragments") },
                    { "shotgun_waterpipe.item", new ItemCookInfo("metal.fragments") },
                    { "shutter.metal.embrasure.a.item", new ItemCookInfo("metal.fragments") },
                    { "shutter.metal.embrasure.b.item", new ItemCookInfo("metal.fragments") },
                    { "shutter.wood.a.item", new ItemCookInfo("metal.fragments") },
                    { "sickle.item", new ItemCookInfo("metal.fragments") },
                    { "sign.hanging.banner.large.item", new ItemCookInfo("metal.fragments") },
                    { "sign.hanging.item", new ItemCookInfo("metal.fragments") },
                    { "sign.hanging.ornate.item", new ItemCookInfo("metal.fragments") },
                    { "sign.neon.125x125.item", new ItemCookInfo("metal.fragments") },
                    { "sign.neon.125x215.animated.item", new ItemCookInfo("metal.fragments") },
                    { "sign.neon.125x215.item", new ItemCookInfo("metal.fragments") },
                    { "sign.neon.xl.animated.item", new ItemCookInfo("metal.fragments") },
                    { "sign.neon.xl.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pictureframe.landscape.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pictureframe.portrait.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pictureframe.tall.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pictureframe.xl.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pictureframe.xxl.item", new ItemCookInfo("metal.fragments") },
                    { "sign.pole.banner.large.item", new ItemCookInfo("metal.fragments") },
                    { "sign.post.double.item", new ItemCookInfo("metal.fragments") },
                    { "sign.post.single.item", new ItemCookInfo("metal.fragments") },
                    { "sign.post.town.item", new ItemCookInfo("metal.fragments") },
                    { "sign.post.town.roof.item", new ItemCookInfo("metal.fragments") },
                    { "sign.wooden.huge.item", new ItemCookInfo("metal.fragments") },
                    { "sign.wooden.large.item", new ItemCookInfo("metal.fragments") },
                    { "sign.wooden.medium.item", new ItemCookInfo("metal.fragments") },
                    { "sign.wooden.small.item", new ItemCookInfo("metal.fragments") },
                    { "silencer.item", new ItemCookInfo("metal.fragments") },
                    { "silveregg.item", new ItemCookInfo("metal.fragments") },
                    { "simpelight.item", new ItemCookInfo("metal.fragments") },
                    { "simplesight.item", new ItemCookInfo("metal.fragments") },
                    { "skull_door_knocker.item", new ItemCookInfo("metal.fragments") },
                    { "skull_fire_pit.item", new ItemCookInfo("metal.fragments") },
                    { "skull.item", new ItemCookInfo("metal.fragments") },
                    { "skullspikes.candles.item", new ItemCookInfo("metal.fragments") },
                    { "skullspikes.item", new ItemCookInfo("metal.fragments") },
                    { "skullspikes.pumpkin.item", new ItemCookInfo("metal.fragments") },
                    { "skulltrophy.item", new ItemCookInfo("metal.fragments") },
                    { "skulltrophy.jar2.item", new ItemCookInfo("metal.fragments") },
                    { "skulltrophy.jar.item", new ItemCookInfo("metal.fragments") },
                    { "skulltrophy.table.item", new ItemCookInfo("metal.fragments") },
                    { "skylantern.item", new ItemCookInfo("metal.fragments") },
                    { "skylantern.skylantern.green.item", new ItemCookInfo("metal.fragments") },
                    { "skylantern.skylantern.orange.item", new ItemCookInfo("metal.fragments") },
                    { "skylantern.skylantern.purple.item", new ItemCookInfo("metal.fragments") },
                    { "skylantern.skylantern.red.item", new ItemCookInfo("metal.fragments") },
                    { "sled.item", new ItemCookInfo("metal.fragments") },
                    { "sled.item.xmas", new ItemCookInfo("metal.fragments") },
                    { "sleepingbag.item", new ItemCookInfo("metal.fragments") },
                    { "smallcandles.item", new ItemCookInfo("metal.fragments") },
                    { "small_fuel_generator.item", new ItemCookInfo("metal.fragments") },
                    { "small_oil_refinery.item", new ItemCookInfo("metal.fragments") },
                    { "small.rechargable.battery.item", new ItemCookInfo("metal.fragments") },
                    { "small_stash.item", new ItemCookInfo("metal.fragments") },
                    { "smartalarm.item", new ItemCookInfo("metal.fragments") },
                    { "smartswitch.item", new ItemCookInfo("metal.fragments") },
                    { "smgbody.item", new ItemCookInfo("metal.fragments") },
                    { "smg.item", new ItemCookInfo("metal.fragments") },
                    { "smoke_grenade.item", new ItemCookInfo("metal.fragments") },
                    { "snowballgun.item", new ItemCookInfo("metal.fragments") },
                    { "snowball.item", new ItemCookInfo("metal.fragments") },
                    { "snowmachine.item", new ItemCookInfo("metal.fragments") },
                    { "snowman.item", new ItemCookInfo("metal.fragments") },
                    { "sofa.item", new ItemCookInfo("metal.fragments") },
                    { "sofa.pattern.item", new ItemCookInfo("metal.fragments") },
                    { "solarpanel.large.item", new ItemCookInfo("metal.fragments") },
                    { "soundlight.item", new ItemCookInfo("metal.fragments") },
                    { "sparkplugs1.item", new ItemCookInfo("metal.fragments") },
                    { "sparkplugs2.item", new ItemCookInfo("metal.fragments") },
                    { "sparkplugs3.item", new ItemCookInfo("metal.fragments") },
                    { "spas12.item", new ItemCookInfo("metal.fragments") },
                    { "speargun.item", new ItemCookInfo("metal.fragments") },
                    { "speargun_spear.item", new ItemCookInfo("metal.fragments") },
                    { "spear_stone.item", new ItemCookInfo("metal.fragments") },
                    { "spear_wooden.item", new ItemCookInfo("metal.fragments") },
                    { "spiderweb.item", new ItemCookInfo("metal.fragments") },
                    { "spikes.floor.item", new ItemCookInfo("metal.fragments") },
                    { "spinner.wheel.item", new ItemCookInfo("metal.fragments") },
                    { "splitter.item", new ItemCookInfo("metal.fragments") },
                    { "spookyspeaker.item", new ItemCookInfo("metal.fragments") },
                    { "spraycan.item", new ItemCookInfo("metal.fragments") },
                    { "spring.item", new ItemCookInfo("metal.fragments") },
                    { "sticks.item", new ItemCookInfo("metal.fragments") },
                    { "stocking.large.item", new ItemCookInfo("metal.fragments") },
                    { "stocking.small.item", new ItemCookInfo("metal.fragments") },
                    { "stonehatchet.item", new ItemCookInfo("metal.fragments") },
                    { "stone_pickaxe.item", new ItemCookInfo("metal.fragments") },
                    { "storageadaptor.item", new ItemCookInfo("metal.fragments") },
                    { "storagemonitor.item", new ItemCookInfo("metal.fragments") },
                    { "strobelight.item", new ItemCookInfo("metal.fragments") },
                    { "submit_items", new ItemCookInfo("metal.fragments") },
                    { "suit.banditguard.item", new ItemCookInfo("metal.fragments") },
                    { "suit.jumpsuit.blue.item", new ItemCookInfo("metal.fragments") },
                    { "suit.jumpsuit.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses02.black.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses02.camo.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses02.red.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses03.black.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses03.chrome.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses03.gold.item", new ItemCookInfo("metal.fragments") },
                    { "sunglasses.item", new ItemCookInfo("metal.fragments") },
                    { "supply_signal.item", new ItemCookInfo("metal.fragments") },
                    { "surgeon_suit.item", new ItemCookInfo("metal.fragments") },
                    { "surveycharge.item", new ItemCookInfo("metal.fragments") },
                    { "survivalfishtrap.item", new ItemCookInfo("metal.fragments") },
                    { "switch.item", new ItemCookInfo("metal.fragments") },
                    { "syringe_medical.item", new ItemCookInfo("metal.fragments") },
                    { "table.item", new ItemCookInfo("metal.fragments") },
                    { "tambourine.item", new ItemCookInfo("metal.fragments") },
                    { "targeting_computer.item", new ItemCookInfo("metal.fragments") },
                    { "tarp.item", new ItemCookInfo("metal.fragments") },
                    { "techparts.item", new ItemCookInfo("metal.fragments") },
                    { "telephone.item", new ItemCookInfo("metal.fragments") },
                    { "teslacoil.item", new ItemCookInfo("metal.fragments") },
                    { "thompson.item", new ItemCookInfo("metal.fragments") },
                    { "timer.item", new ItemCookInfo("metal.fragments") },
                    { "toolgun.item", new ItemCookInfo("metal.fragments") },
                    { "torch.item", new ItemCookInfo("metal.fragments") },
                    { "torch.skull.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.baubels.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.candycanes.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.gingerbreadmen.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.lights.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.pinecones.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.star.item", new ItemCookInfo("metal.fragments") },
                    { "treedecor.tinsel.item", new ItemCookInfo("metal.fragments") },
                    { "trophy.item", new ItemCookInfo("metal.fragments") },
                    { "trumpet.item", new ItemCookInfo("metal.fragments") },
                    { "tshirt.green.item", new ItemCookInfo("metal.fragments") },
                    { "tshirt.long.blue.item", new ItemCookInfo("metal.fragments") },
                    { "tuba.item", new ItemCookInfo("metal.fragments") },
                    { "tunalight.item", new ItemCookInfo("metal.fragments") },
                    { "valves1.item", new ItemCookInfo("metal.fragments") },
                    { "valves2.item", new ItemCookInfo("metal.fragments") },
                    { "valves3.item", new ItemCookInfo("metal.fragments") },
                    { "vendingmachine.item", new ItemCookInfo("metal.fragments") },
                    { "volcanofirework.item", new ItemCookInfo("metal.fragments") },
                    { "volcanofirework.red.item", new ItemCookInfo("metal.fragments") },
                    { "volcanofirework.violet.item", new ItemCookInfo("metal.fragments") },
                    { "wall.external.high.ice.item", new ItemCookInfo("metal.fragments") },
                    { "wall.external.high.stone.item", new ItemCookInfo("metal.fragments") },
                    { "wall.external.high.wood.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.cell.gate.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.cell.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.fence.gate.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.fence.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.garagedoor.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.netting.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.shopfront.item", new ItemCookInfo("metal.fragments") },
                    { "wall.frame.shopfront.metal.item", new ItemCookInfo("metal.fragments") },
                    { "wall.window.bars.metal.item", new ItemCookInfo("metal.fragments") },
                    { "wall.window.bars.toptier.item", new ItemCookInfo("metal.fragments") },
                    { "wall.window.bars.wood.item", new ItemCookInfo("metal.fragments") },
                    { "wall.window.glass.reinforced.item", new ItemCookInfo("metal.fragments") },
                    { "watchtower.wood.item", new ItemCookInfo("metal.fragments") },
                    { "waterbarrel.item", new ItemCookInfo("metal.fragments") },
                    { "water_catcher_large.item", new ItemCookInfo("metal.fragments") },
                    { "water_catcher_small.item", new ItemCookInfo("metal.fragments") },
                    { "watergun.item", new ItemCookInfo("metal.fragments") },
                    { "waterpistol.item", new ItemCookInfo("metal.fragments") },
                    { "water.pump.item", new ItemCookInfo("metal.fragments") },
                    { "waterpurifier.item", new ItemCookInfo("metal.fragments") },
                    { "windowgarland.item", new ItemCookInfo("metal.fragments") },
                    { "wiretool.item", new ItemCookInfo("metal.fragments") },
                    { "wood_armor_jacket.item", new ItemCookInfo("metal.fragments") },
                    { "wood_armor_pants.item", new ItemCookInfo("metal.fragments") },
                    { "workbench1.item", new ItemCookInfo("metal.fragments") },
                    { "workbench2.item", new ItemCookInfo("metal.fragments") },
                    { "workbench3.item", new ItemCookInfo("metal.fragments") },
                    { "wrappedgift.item", new ItemCookInfo("metal.fragments") },
                    { "wrappingpaper.item", new ItemCookInfo("metal.fragments") },
                    { "xmas.advanced.lights.item", new ItemCookInfo("metal.fragments") },
                    { "xmas.gingerbreadsuit.item", new ItemCookInfo("metal.fragments") },
                    { "xmas.lightstring.item", new ItemCookInfo("metal.fragments") },
                    { "xmas_tree.item", new ItemCookInfo("metal.fragments") },
                    { "xorswitch.item", new ItemCookInfo("metal.fragments") },
                    { "xylophone.item", new ItemCookInfo("metal.fragments") }
                }
            };
            SaveConfig(config);
        }

        public class ConfigData
        {
            public bool cookItems;
            public Dictionary<string, ItemCookInfo> itemlist = new Dictionary<string, ItemCookInfo>();
            public VersionNumber Version;
        }

        public class ItemCookInfo
        {
            public string result;
            public int delay;
            public int mult;

            public ItemCookInfo(string result, int delay = 20, int mult = 2)
            {
                this.result = result;
                this.delay = delay;
                this.mult = mult;
            }
        }
    }
}
