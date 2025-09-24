// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
namespace Oxide.Plugins
{
    [Info("Random Gather", "Kaysharp", "1.0.8")]
    [Description("Get random items on gathering resources")]
    class RandomGather : CovalencePlugin
    {
        private readonly Random _random = new Random();
        public static ConfigData config { get; set; }
        private void Init()
        {
            if (!config.OnCollectiblePickup)
                Unsubscribe(nameof(OnCollectiblePickup));

            if (!config.isOnDispenserBonus)
                Unsubscribe(nameof(OnDispenserBonus));

            if (!config.isOnDispenserGather)
                Unsubscribe(nameof(OnDispenserGather));
        }
        private void Unload()
        {
            config = null;
        }
        private ItemDefinition FindItem(string itemNameOrId)
        {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemNameOrId.ToLower());
            if (itemDef == null)
            {
                int itemId;
                if (int.TryParse(itemNameOrId, out itemId))
                {
                    itemDef = ItemManager.FindItemDefinition(itemId);
                }
            }
            return itemDef;
        }
        #region Oxide Hooks
        object OnCollectiblePickup(CollectibleEntity entity, BasePlayer player)
        {
            foreach (var itemPicked in entity.itemList)
            {
                var item = ItemManager.Create(itemPicked.itemDef, (int)itemPicked.amount, 0UL);
                if (item.info.displayName.english.ToLower().Contains("seed"))
                    return null;
                var InitItem = item;
                var index = _random.Next(0, config.Items.Count());
                var itemName = config.Items[index];
                item = ItemManager.Create(FindItem(itemName));
                if (item != null)
                {
                    int amount;
                    if (config.Amount.TryGetValue(itemName, out amount))
                    {
                        item.amount = amount;
                    }
                    try
                    {
                        player.GiveItem(item);
                    }
                    catch (Exception E)
                    {
                        item.Drop(player.GetDropPosition(), player.GetDropVelocity());
                    }
                    try
                    {
                        player.GiveItem(InitItem, global::BaseEntity.GiveItemReason.ResourceHarvested);
                    }
                    catch
                    {
                        InitItem.Drop(player.GetDropPosition(), player.GetDropVelocity());
                    }
                    if (entity.pickupEffect.isValid)
                    {
                        Effect.server.Run(entity.pickupEffect.resourcePath, entity.transform.position, entity.transform.up, null, false);
                    }
                    RandomItemDispenser randomItemDispenser = PrefabAttribute.server.Find<RandomItemDispenser>(entity.prefabID);
                    if (randomItemDispenser != null)
                    {
                        randomItemDispenser.DistributeItems(player, entity.transform.position);
                    }
                    entity.Kill(global::BaseNetworkable.DestroyMode.None);
                    return true;
                }
            }
            
            return null;
        }
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (!config.isOnDispenserBonus) return null;
            var index = _random.Next(0,config.Items.Count());
            var itemName = config.Items[index];
            item = ItemManager.Create(FindItem(itemName));
            if (item != null)
            {
                int amount;
                if (config.Amount.TryGetValue(itemName, out amount))
                {
                    item.amount = amount;
                }
                return item;
            }
            return null;
        }
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer())
            {
                return null ;
            }
            var index = _random.Next(0,config.Items.Count());
            var itemName = config.Items[index];
            item = ItemManager.Create(FindItem(itemName));
            if (item != null)
            {
                int amount;
                if (config.Amount.TryGetValue(itemName, out amount))
                {
                    item.amount = amount;
                }
                BasePlayer player = entity.ToPlayer();
                try
                {
                    player.GiveItem(item);
                }
                catch (Exception E)
                {
                    item.Drop(player.GetDropPosition(), player.GetDropVelocity());
                }
                return true;
            }
            return null;
        }
        #endregion
        #region Config
        public class ConfigData
        {
            [JsonProperty(PropertyName = "Items")]
            public List<string> Items = new List<string>();

            [JsonProperty(PropertyName = "Amount")]
            public Dictionary<string, int> Amount = new Dictionary<string, int>();

            [JsonProperty(PropertyName = "Give random items on gathering resources")]
            public bool isOnDispenserGather {get; set;}

            [JsonProperty(PropertyName = "Give random items only on full gather")]
            public bool isOnDispenserBonus {get; set;}

            [JsonProperty(PropertyName = "Give random item collecting resources")]
            public bool OnCollectiblePickup { get; set; }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }
        protected override void LoadDefaultConfig() => config = GetDefaultSettings();
        private ConfigData GetDefaultSettings()
        {
            return new ConfigData
            {
                OnCollectiblePickup = true,
                isOnDispenserGather = true,
                isOnDispenserBonus = false,
                Items = new List<string>
                {
                    "ammo.grenadelauncher.buckshot",
                    "ammo.grenadelauncher.he",
                    "ammo.grenadelauncher.smoke",
                    "ammo.handmade.shell",
                    "ammo.nailgun.nails",
                    "ammo.pistol",
                    "ammo.pistol.fire",
                    "ammo.pistol.hv",
                    "ammo.rifle",
                    "ammo.rifle.explosive",
                    "ammo.rifle.hv",
                    "ammo.rifle.incendiary",
                    "ammo.rocket.basic",
                    "ammo.rocket.fire",
                    "ammo.rocket.hv",
                    "ammo.rocket.sam",
                    "ammo.rocket.smoke",
                    "ammo.shotgun",
                    "ammo.shotgun.fire",
                    "ammo.shotgun.slug",
                    "apple",
                    "arcade.machine.chippy",
                    "arrow.bone",
                    "arrow.fire",
                    "arrow.hv",
                    "arrow.wooden",
                    "attire.bunnyears",
                    "attire.hide.boots",
                    "attire.hide.helterneck",
                    "attire.hide.pants",
                    "attire.hide.poncho",
                    "attire.hide.skirt",
                    "attire.hide.vest",
                    "attire.ninja.suit",
                    "autoturret",
                    "axe.salvaged",
                    "barricade.concrete",
                    "barricade.metal",
                    "barricade.sandbags",
                    "barricade.stone",
                    "barricade.wood.cover",
                    "battery.small",
                    "bbq",
                    "bed",
                    "black.berry",
                    "black.raspberries",
                    "blue.berry",
                    "blueberries",
                    "bone.armor.suit",
                    "boots.frog",
                    "bow.compound",
                    "bow.hunting",
                    "box.repair.bench",
                    "box.wooden",
                    "box.wooden.large",
                    "bucket.helmet",
                    "building.planner",
                    "burlap.gloves",
                    "burlap.gloves.new",
                    "burlap.headwrap",
                    "burlap.shirt",
                    "burlap.shoes",
                    "burlap.trousers",
                    "can.beans",
                    "can.tuna",
                    "cctv.camera",
                    "ceilinglight",
                    "chainsaw",
                    "chocholate",
                    "clatter.helmet",
                    "coffeecan.helmet",
                    "coffin.storage",
                    "computerstation",
                    "corn",
                    "crossbow",
                    "crude.oil",
                    "cupboard.tool",
                    "cursedcauldron",
                    "diving.fins",
                    "diving.mask",
                    "diving.tank",
                    "diving.wetsuit",
                    "door.double.hinged.metal",
                    "door.double.hinged.toptier",
                    "door.double.hinged.wood",
                    "door.hinged.industrial.a",
                    "door.hinged.metal",
                    "door.hinged.toptier",
                    "door.hinged.wood",
                    "dropbox",
                    "explosive.satchel",
                    "explosive.timed",
                    "explosives",
                    "flamethrower",
                    "flameturret",
                    "flashlight.held",
                    "floor.grill",
                    "floor.ladder.hatch",
                    "floor.triangle.grill",
                    "floor.triangle.ladder.hatch",
                    "fridge",
                    "furnace",
                    "furnace.large",
                    "fuse",
                    "gates.external.high.stone",
                    "gates.external.high.wood",
                    "gears",
                    "generator.wind.scrap",
                    "grenade.beancan",
                    "grenade.f1",
                    "grenade.smoke",
                    "guntrap",
                    "habrepair",
                    "hammer.salvaged",
                    "hat.beenie",
                    "hat.boonie",
                    "hat.cap",
                    "hat.ratmask",
                    "hat.wolf",
                    "hatchet",
                    "hazmatsuit",
                    "hazmatsuit.spacesuit",
                    "healingtea",
                    "healingtea.advanced",
                    "healingtea.pure",
                    "heavy.plate.helmet",
                    "heavy.plate.jacket",
                    "heavy.plate.pants",
                    "hitchtroughcombo",
                    "hobobarrel",
                    "hoodie",
                    "horse.armor.roadsign",
                    "horse.armor.wood",
                    "horse.saddle",
                    "horse.saddlebag",
                    "horse.shoes.advanced",
                    "horse.shoes.basic",
                    "icepick.salvaged",
                    "innertube",
                    "innertube.horse",
                    "innertube.unicorn",
                    "jacket",
                    "jacket.snow",
                    "jackhammer",
                    "jackolantern.angry",
                    "jackolantern.happy",
                    "jar.pickle",
                    "ammo.grenadelauncher.buckshot",
                    "ammo.grenadelauncher.he",
                    "ammo.grenadelauncher.smoke",
                    "ammo.handmade.shell",
                    "ammo.nailgun.nails",
                    "ammo.pistol",
                    "ammo.pistol.fire",
                    "ammo.pistol.hv",
                    "ammo.rifle",
                    "ammo.rifle.explosive",
                    "ammo.rifle.hv",
                    "ammo.rifle.incendiary",
                    "ammo.rocket.basic",
                    "ammo.rocket.fire",
                    "ammo.rocket.hv",
                    "ammo.rocket.sam",
                    "ammo.rocket.smoke",
                    "ammo.shotgun",
                    "ammo.shotgun.fire",
                    "ammo.shotgun.slug",
                    "jumpsuit.suit",
                    "jumpsuit.suit.blue",
                    "kayak",
                    "keycard_blue",
                    "keycard_green",
                    "keycard_red",
                    "knife.bone",
                    "knife.butcher",
                    "knife.combat",
                    "ladder.wooden.wall",
                    "lantern",
                    "largecandles",
                    "largemedkit",
                    "laserlight",
                    "lmg.m249",
                    "locker",
                    "longsword",
                    "lowgradefuel",
                    "mace",
                    "machete",
                    "mask.balaclava",
                    "mask.bandana",
                    "maxhealthtea",
                    "maxhealthtea.advanced",
                    "maxhealthtea.pure",
                    "metal.facemask",
                    "metal.fragments",
                    "metal.plate.torso",
                    "metal.refined",
                    "metalblade",
                    "metalpipe",
                    "metalspring",
                    "microphonestand",
                    "minihelicopter.repair",
                    "mining.quarry",
                    "mixingtable",
                    "multiplegrenadelauncher",
                    "mushroom",
                    "nightvisiongoggles",
                    "oretea",
                    "oretea.advanced",
                    "oretea.pure",
                    "paddle",
                    "paddlingpool",
                    "pants",
                    "pants.shorts",
                    "paper",
                    "partyhat",
                    "pickaxe",
                    "pistol.eoka",
                    "pistol.m92",
                    "pistol.nailgun",
                    "pistol.python",
                    "pistol.revolver",
                    "pistol.semiauto",
                    "pitchfork",
                    "planter.large",
                    "planter.small",
                    "plantfiber",
                    "pookie.bear",
                    "potato",
                    "powered.water.purifier",
                    "propanetank",
                    "pumpkin",
                    "pumpkinbasket",
                    "radiationremovetea",
                    "radiationremovetea.advanced",
                    "radiationremovetea.pure",
                    "radiationresisttea",
                    "radiationresisttea.advanced",
                    "radiationresisttea.pure",
                    "red.berry",
                    "research.table",
                    "rf.detonator",
                    "rf_pager",
                    "rifle.ak",
                    "rifle.bolt",
                    "rifle.l96",
                    "rifle.lr300",
                    "rifle.m39",
                    "rifle.semiauto",
                    "riflebody",
                    "riot.helmet",
                    "roadsign.gloves",
                    "roadsign.jacket",
                    "roadsign.kilt",
                    "roadsigns",
                    "rocket.launcher",
                    "rope",
                    "rug",
                    "rug.bear",
                    "salvaged.cleaver",
                    "salvaged.sword",
                    "samsite",
                    "scraptea",
                    "scraptea.advanced",
                    "scraptea.pure",
                    "scraptransportheli.repair",
                    "searchlight",
                    "secretlabchair",
                    "semibody",
                    "sewingkit",
                    "sheetmetal",
                    "shelves",
                    "ammo.grenadelauncher.buckshot",
                    "ammo.grenadelauncher.he",
                    "ammo.grenadelauncher.smoke",
                    "ammo.handmade.shell",
                    "ammo.nailgun.nails",
                    "ammo.pistol",
                    "ammo.pistol.fire",
                    "ammo.pistol.hv",
                    "ammo.rifle",
                    "ammo.rifle.explosive",
                    "ammo.rifle.hv",
                    "ammo.rifle.incendiary",
                    "ammo.rocket.basic",
                    "ammo.rocket.fire",
                    "ammo.rocket.hv",
                    "ammo.rocket.sam",
                    "ammo.rocket.smoke",
                    "ammo.shotgun",
                    "ammo.shotgun.fire",
                    "ammo.shotgun.slug",
                    "shirt.collared",
                    "shirt.tanktop",
                    "shoes.boots",
                    "shotgun.double",
                    "shotgun.pump",
                    "shotgun.spas12",
                    "shotgun.waterpipe",
                    "shutter.metal.embrasure.a",
                    "shutter.metal.embrasure.b",
                    "shutter.wood.a",
                    "sickle",
                    "sleepingbag",
                    "small.oil.refinery",
                    "smg.2",
                    "smg.mp5",
                    "smg.thompson",
                    "smgbody",
                    "spear.stone",
                    "spear.wooden",
                    "speargun",
                    "speargun.spear",
                    "stone.pickaxe",
                    "stonehatchet",
                    "storage.monitor",
                    "strobelight",
                    "submarine.torpedo.rising",
                    "submarine.torpedo.straight",
                    "supply.signal",
                    "surveycharge",
                    "syringe.medical",
                    "table",
                    "tactical.gloves",
                    "targeting.computer",
                    "tarp",
                    "techparts",
                    "tool.instant_camera",
                    "vending.machine",
                    "wall.external.high",
                    "wall.external.high.ice",
                    "wall.external.high.stone",
                    "waterpump",
                    "weapon.mod.8x.scope",
                    "weapon.mod.flashlight",
                    "weapon.mod.holosight",
                    "weapon.mod.lasersight",
                    "weapon.mod.muzzleboost",
                    "weapon.mod.muzzlebrake",
                    "weapon.mod.silencer",
                    "weapon.mod.simplesight",
                    "weapon.mod.small.scope",
                    "white.berry",
                    "wood.armor.helmet",
                    "wood.armor.jacket",
                    "wood.armor.pants",
                    "woodtea",
                    "woodtea.advanced",
                    "workbench1",
                    "workbench2",
                    "workbench3"
                },
                Amount = new Dictionary<string,int>
                {
                    {"ammo.grenadelauncher.buckshot",10},
                    {"ammo.grenadelauncher.he",1},
                    {"ammo.grenadelauncher.smoke",1},
                    {"ammo.handmade.shell",15},
                    {"ammo.nailgun.nails",30},
                    {"ammo.pistol",30},
                    {"ammo.pistol.fire",10},
                    {"ammo.pistol.hv",10},
                    {"ammo.rifle",30},
                    {"ammo.rifle.explosive",10},
                    {"ammo.rifle.hv",15},
                    {"ammo.rifle.incendiary",15},
                    {"ammo.rocket.basic",1},
                    {"ammo.rocket.fire",1},
                    {"ammo.rocket.hv",1},
                    {"ammo.rocket.sam",5},
                    {"ammo.rocket.smoke",1},
                    {"ammo.shotgun",10},
                    {"ammo.shotgun.fire",10},
                    {"ammo.shotgun.slug",10}
                }
            };
        }

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion
    }
} 