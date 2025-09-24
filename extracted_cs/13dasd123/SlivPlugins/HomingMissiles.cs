// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using System;
using Oxide.Core.Libraries.Covalence;


namespace Oxide.Plugins
{
    [Info("HomingMissiles", "Fruster", "1.1.0")]
    [Description("HomingMissiles")]
    class HomingMissiles : CovalencePlugin
    {
        class Rocket
        {
            public BasePlayer playerOwn;
            public BaseEntity target;
            public BaseEntity rocket;
            public float rocketTimer;
            public float expRadius;
            public Vector3 offset;
            public int greenChTimer;

            public Rocket(BasePlayer playerOwn, BaseEntity target, BaseEntity rocket, float rocketTimer, float expRadius, Vector3 offset, int greenChTimer)
            {
                this.playerOwn = playerOwn;
                this.target = target;
                this.rocket = rocket;
                this.rocketTimer = rocketTimer;
                this.expRadius = expRadius;
                this.offset = offset;
                this.greenChTimer = greenChTimer;
            }
        }

        class TargetingKD
        {
            public BasePlayer playerOwn;
            public int timer;
            public bool ready;

            public TargetingKD(BasePlayer playerOwn, int timer, bool ready)
            {
                this.playerOwn = playerOwn;
                this.timer = timer;
                this.ready = ready;
            }
        }

        public class CraftItems
        {
            public string shortname { get; set; }
            public int amount { get; set; }
        }

        List<Rocket> rockets = new List<Rocket>();
        List<TargetingKD> targetsKD = new List<TargetingKD>();
        List<CraftItems> craftItems = new List<CraftItems>();
        private List<string> entityNames = new List<string>();
        List<string> zones = new List<string>();

        Color firstCrossHairColor = Color.red;
        Color secondCrossHairColor = Color.green;
        string firstCrossHairLeftPart;
        string firstCrossHairMiddlePart;
        string firstCrossHairRightPart;
        string secondCrossHair;
        float firstCrossHairSize;
        float secondCrossHairSize;
        float trainDamage;
        float submarineDamage;
        float playerDamage;
        float animalDamage;
        float npcDamage;
        float snowmobileDamage;
        float boatDamage;
        float modulecarDamage;
        float hotAirDamage;
        float scrapheliDamage;
        float minicopterDamage;
        float baseDamage;
        float heliDamage;
        float bradleyDamage;
        float chinookDamage;
        float rocketTimer = 1000;
        int maxRocketSpeed;
        float acceleration;
        float delayRocket;
        int targetTimer = 0;
        int maxTargetTimer = 30;
        int maxAimingTimer;
        float sizeAim;
        int rocketType = 0;
        int explosionType;
        string ammoName;
        const int layerS = ~(1 << 2 | 1 << 3 | 1 << 4 | 1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);
        string expPrefab;
        string rocketPrefab;
        bool enableSFX;
        string restrictionMessage;
        float selfDetonation;
        bool canCraft;
        int wbLevel;
        bool useBuildings;
        bool useConstructions;
        bool useItems;
        bool useTrapsTurrets;
        bool whiteList;
        [PluginReference] Plugin ZoneManager;

        private ConfigData Configuration;
        class ConfigData
        {
            [JsonProperty("Allow homing missiles crafting")]
            public bool canCraft = true;
            [JsonProperty("Workbench level required to craft(0-3)")]
            public int wbLevel = 3;
            [JsonProperty("Crafting costs")]
            public List<CraftItems> craftItems = new List<CraftItems> { new CraftItems { shortname = "ammo.rocket.basic", amount = 1 }, new CraftItems { shortname = "techparts", amount = 2 } };
            [JsonProperty("List of zones where homing missiles cannot be used (requires ZoneManager plugin)")]
            public List<string> zones = new List<string>() { "111111111", "222222222", "333333333" };
            [JsonProperty("Make it so that only in these zones you can use homing missiles")]
            public bool whiteList = false;
            [JsonProperty("A message when you are in an area where homing missiles cannot be used")]
            public string restrictionMessage = "You can't use homing missiles here";
            [JsonProperty("Rocket speed")]
            public int maxRocketSpeed = 50;
            [JsonProperty("Rocket acceleration(1 - 10)")]
            public float acceleration = 1f;
            [JsonProperty("Amount of time before the rocket self detonates")]
            public float selfDetonation = 20f;
            [JsonProperty("Amount of time to acquire target lock(in seconds)")]
            public float maxAimingTimer = 1f;
            [JsonProperty("First crosshair size")]
            public float firstCrossHairSize = 24f;
            [JsonProperty("First crosshair color Red (0-1)")]
            public float firstCrossHairColorR = 1;
            [JsonProperty("First crosshair color Green (0-1)")]
            public float firstCrossHairColorG = 0;
            [JsonProperty("First crosshair color Blue (0-1)")]
            public float firstCrossHairColorB = 0;
            [JsonProperty("Left side of the first crosshair")]
            public string firstCrossHairLeftPart = "<";
            [JsonProperty("Right side of the first crosshair")]
            public string firstCrossHairRightPart = ">";
            [JsonProperty("Middle of the first crosshair")]
            public string firstCrossHairMiddlePart = "+";
            [JsonProperty("Second crosshair size")]
            public float secondCrossHairSize = 24f;
            [JsonProperty("Second crosshair color Red (0-1)")]
            public float secondCrossHairColorR = 0;
            [JsonProperty("Second crosshair color Green (0-1)")]
            public float secondCrossHairColorG = 1;
            [JsonProperty("Second crosshair color Blue (0-1)")]
            public float secondCrossHairColorB = 0;
            [JsonProperty("Second crosshair")]
            public string secondCrossHair = "[ + ]";
            [JsonProperty("Enable sound effects when aiming")]
            public bool enableSFX = true;
            [JsonProperty("Base damage of the rocket(affect everything, including buildings)")]
            public float baseDamage = 100f;
            [JsonProperty("Damage to players")]
            public float playerDamage = 100f;
            [JsonProperty("Damage to animals")]
            public float animalDamage = 500f;
            [JsonProperty("Damage to patrol helicopter")]
            public float heliDamage = 3500f;
            [JsonProperty("Damage to chinook")]
            public float chinookDamage = 2000f;
            [JsonProperty("Damage to bradleyAPC")]
            public float bradleyDamage = 500f;
            [JsonProperty("Damage to submarine")]
            public float submarineDamage = 400f;
            [JsonProperty("Damage to NPCs")]
            public float npcDamage = 300f;
            [JsonProperty("Damage to snowmobile")]
            public float snowmobileDamage = 300f;
            [JsonProperty("Damage to boat")]
            public float boatDamage = 400f;
            [JsonProperty("Damage to modular cars")]
            public float modulecarDamage = 400f;
            [JsonProperty("Damage to hot air baloon")]
            public float hotAirDamage = 1000f;
            [JsonProperty("Damage to scrap transport helicopter")]
            public float scrapheliDamage = 500f;
            [JsonProperty("Damage to minicopter")]
            public float minicopterDamage = 750f;
            [JsonProperty("Damage to train")]
            public float trainDamage = 500f;
            [JsonProperty("Explosion type: 1 - basic; 2 - fire; 3 - smoke; 4 - heli; 5 - heli napalm; 6 - heli airburst; 7 - sam; 8 - 40mm_grenade_he; 9 - c4; 10 - f1; 11 - beancan grenade; 12 - satchelcharge; 13 - mlrs")]
            public int explosionType = 13;
            [JsonProperty("Use homing missiles for building blocks")]
            public bool useBuildings = false;
            [JsonProperty("Use homing missiles for constructions")]
            public bool useConstructions = false;
            [JsonProperty("Use homing missiles for items")]
            public bool useItems = false;
            [JsonProperty("Use homing missiles for traps and turrets")]
            public bool useTrapsTurrets = false;

        }


        #region Permissions
        const string PERM_USE = "homingmissiles.use";
        const string PERM_CRAFT = "homingmissiles.craft";

        bool CanUseHomingMissiles(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PERM_USE);
        }

        bool CanCraftHomingMissiles(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PERM_CRAFT);
        }

        void RegisterPermissions()
        {
            if (permission.PermissionExists(PERM_USE, this) == false)
                permission.RegisterPermission(PERM_USE, this);
            if (permission.PermissionExists(PERM_CRAFT, this) == false)
                permission.RegisterPermission(PERM_CRAFT, this);
        }


        #endregion

        private void OnServerInitialized()
        {
            Unsubscribe("OnPlayerInput");
            Unsubscribe("OnTick");
            TargetingKD newTargetingItem;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                newTargetingItem = new TargetingKD(player, 0, false);
                targetsKD.Add(newTargetingItem);
            }

            LoadConfig();
            RegisterPermissions();

            maxRocketSpeed = Configuration.maxRocketSpeed;
            acceleration = Configuration.acceleration;
            maxAimingTimer = (int)((Configuration.maxAimingTimer) * 15f);
            baseDamage = Configuration.baseDamage;
            submarineDamage = Configuration.submarineDamage;
            playerDamage = Configuration.playerDamage;
            animalDamage = Configuration.animalDamage;
            npcDamage = Configuration.npcDamage;
            snowmobileDamage = Configuration.snowmobileDamage;
            boatDamage = Configuration.boatDamage;
            modulecarDamage = Configuration.modulecarDamage;
            hotAirDamage = Configuration.hotAirDamage;
            scrapheliDamage = Configuration.scrapheliDamage;
            minicopterDamage = Configuration.minicopterDamage;
            heliDamage = Configuration.heliDamage;
            bradleyDamage = Configuration.bradleyDamage;
            chinookDamage = Configuration.chinookDamage;
            trainDamage = Configuration.trainDamage;
            explosionType = Configuration.explosionType;
            enableSFX = Configuration.enableSFX;
            firstCrossHairSize = Configuration.firstCrossHairSize;
            firstCrossHairColor.r = Configuration.firstCrossHairColorR;
            firstCrossHairColor.g = Configuration.firstCrossHairColorG;
            firstCrossHairColor.b = Configuration.firstCrossHairColorB;
            firstCrossHairLeftPart = Configuration.firstCrossHairLeftPart;
            firstCrossHairRightPart = Configuration.firstCrossHairRightPart;
            firstCrossHairMiddlePart = Configuration.firstCrossHairMiddlePart;
            secondCrossHairSize = Configuration.secondCrossHairSize;
            secondCrossHairColor.r = Configuration.secondCrossHairColorR;
            secondCrossHairColor.g = Configuration.secondCrossHairColorG;
            secondCrossHairColor.b = Configuration.secondCrossHairColorB;
            secondCrossHair = Configuration.secondCrossHair;
            zones = Configuration.zones;
            restrictionMessage = Configuration.restrictionMessage;
            selfDetonation = Configuration.selfDetonation;
            canCraft = Configuration.canCraft;
            craftItems = Configuration.craftItems;
            wbLevel = Configuration.wbLevel;
            useBuildings = Configuration.useBuildings;
            useConstructions = Configuration.useConstructions;
            useItems = Configuration.useItems;
            useTrapsTurrets = Configuration.useTrapsTurrets;
            whiteList = Configuration.whiteList;

            entityNames.Add("assets/prefabs/deployable/drone/drone.deployed.prefab");
            entityNames.Add("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab");
            entityNames.Add("assets/prefabs/npc/ch47/ch47scientists.entity.prefab");
            entityNames.Add("assets/prefabs/npc/m2bradley/bradleyapc.prefab");
            entityNames.Add("assets/content/vehicles/minicopter/minicopter.entity.prefab");
            entityNames.Add("assets/prefabs/deployable/hot air balloon/hotairballoon.prefab");
            entityNames.Add("assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab");
            entityNames.Add("assets/prefabs/player/player.prefab");
            entityNames.Add("assets/content/vehicles/boats/rhib/rhib.prefab");
            entityNames.Add("assets/content/vehicles/boats/rowboat/rowboat.prefab");
            entityNames.Add("assets/content/vehicles/workcart/workcart.entity.prefab");
            entityNames.Add("assets/content/vehicles/snowmobiles/snowmobile.prefab");
            entityNames.Add("assets/rust.ai/nextai/testridablehorse.prefab");
            entityNames.Add("assets/rust.ai/agents/bear/polarbear.prefab");
            entityNames.Add("assets/rust.ai/agents/chicken/chicken.prefab");
            entityNames.Add("assets/rust.ai/agents/boar/boar.prefab");
            entityNames.Add("assets/rust.ai/agents/wolf/wolf.prefab");
            entityNames.Add("assets/rust.ai/agents/stag/stag.prefab");
            entityNames.Add("assets/rust.ai/agents/bear/bear.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldweller.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/underwaterdweller/npc_underwaterdweller.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_lr300.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_ch47_gunner.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_excavator.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab");
            entityNames.Add("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roamtethered.prefab");
            entityNames.Add("assets/rust.ai/agents/zombie/zombie.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_cockpit.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_cockpit_armored.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_cockpit_with_engine.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_engine.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_flatbed.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_passengers_armored.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_rear_seats.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_storage.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/1module_taxi.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/2module_camper.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/2module_flatbed.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/2module_fuel_tank.prefab");
            entityNames.Add("assets/content/vehicles/modularcar/module_entities/2module_passengers.prefab");
            entityNames.Add("assets/content/vehicles/submarine/submarinesolo.entity.prefab");
            entityNames.Add("assets/content/vehicles/submarine/submarineduo.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagonunloadablefuel.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagonunloadable.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagona.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagonb.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagonc.entity.prefab");
            entityNames.Add("assets/content/vehicles/train/trainwagonunloadableloot.entity.prefab");
            entityNames.Add("assets/content/vehicles/locomotive/locomotive.entity.prefab");
            entityNames.Add("assets/content/vehicles/workcart/workcart_aboveground.entity.prefab");
            entityNames.Add("assets/content/vehicles/workcart/workcart_aboveground2.entity.prefab");
            entityNames.Add("assets/content/vehicles/workcart/workcart.entity.prefab");

            if (useBuildings)
            {
                entityNames.Add("assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab");
                entityNames.Add("assets/prefabs/building core/stairs.spiral.triangle/block.stair.spiral.triangle.prefab");
                entityNames.Add("assets/prefabs/building core/foundation/foundation.prefab");
                entityNames.Add("assets/prefabs/building core/roof.triangle/roof.triangle.prefab");
                entityNames.Add("assets/prefabs/building core/roof/roof.prefab");
                entityNames.Add("assets/prefabs/building core/wall.window/wall.window.prefab");
                entityNames.Add("assets/prefabs/building core/wall.doorway/wall.doorway.prefab");
                entityNames.Add("assets/prefabs/building core/wall/wall.prefab");
                entityNames.Add("assets/prefabs/building core/floor.triangle.frame/floor.triangle.frame.prefab");
                entityNames.Add("assets/prefabs/building core/floor.frame/floor.frame.prefab");
                entityNames.Add("assets/prefabs/building core/floor/floor.prefab");
                entityNames.Add("assets/prefabs/building core/floor.triangle/floor.triangle.prefab");
                entityNames.Add("assets/prefabs/building core/wall.frame/wall.frame.prefab");
                entityNames.Add("assets/prefabs/building core/stairs.spiral/block.stair.spiral.prefab");
                entityNames.Add("assets/prefabs/building core/stairs.l/block.stair.lshape.prefab");
                entityNames.Add("assets/prefabs/building core/stairs.u/block.stair.ushape.prefab");
                entityNames.Add("assets/prefabs/building core/wall.half/wall.half.prefab");
                entityNames.Add("assets/prefabs/building core/wall.low/wall.low.prefab");
                entityNames.Add("assets/prefabs/building core/ramp/ramp.prefab");
                entityNames.Add("assets/prefabs/building core/foundation.steps/foundation.steps.prefab");
            }

            if (useConstructions)
            {
                entityNames.Add("assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.concrete.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.woodwire.prefab");
                entityNames.Add("assets/prefabs/building/door.hinged/door.hinged.toptier.prefab");
                entityNames.Add("assets/prefabs/building/door.double.hinged/door.double.hinged.toptier.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.fence/wall.frame.fence.gate.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab");
                entityNames.Add("assets/prefabs/building/floor.grill/floor.grill.prefab");
                entityNames.Add("assets/prefabs/building/floor.triangle.grill/floor.triangle.grill.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/icewalls/wall.external.high.ice.prefab");
                entityNames.Add("assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab");
                entityNames.Add("assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab");
                entityNames.Add("assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.metal.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab");
                entityNames.Add("assets/prefabs/deployable/water catcher/water_catcher_large.prefab");
                entityNames.Add("assets/prefabs/building/floor.ladder.hatch/floor.ladder.hatch.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.b.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.bars/wall.window.bars.metal.prefab");
                entityNames.Add("assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab");
                entityNames.Add("assets/prefabs/building/door.hinged/door.hinged.metal.prefab");
                entityNames.Add("assets/prefabs/building/door.hinged/door.hinged.wood.prefab");
                entityNames.Add("assets/prefabs/building/floor.triangle.ladder.hatch/floor.triangle.ladder.hatch.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.shutter/shutter.wood.a.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.reinforcedglass/wall.window.glass.reinforced.prefab");
                entityNames.Add("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab");
                entityNames.Add("assets/prefabs/building/wall.window.bars/wall.window.bars.toptier.prefab");
                entityNames.Add("assets/prefabs/building/door.double.hinged/door.double.hinged.wood.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.cell/wall.frame.cell.gate.prefab");
                entityNames.Add("assets/prefabs/building/door.double.hinged/door.double.hinged.metal.prefab");
                entityNames.Add("assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.prefab");
                entityNames.Add("assets/prefabs/deployable/water catcher/water_catcher_small.prefab");
                entityNames.Add("assets/prefabs/building/watchtower.wood/watchtower.wood.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/icewalls/icewall.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.wood.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.stone.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.cover.wood.prefab");
                entityNames.Add("assets/prefabs/deployable/barricades/barricade.sandbags.prefab");
            }

            if (useItems)
            {
                entityNames.Add("assets/prefabs/deployable/signs/sign.post.town.prefab");
                entityNames.Add("assets/prefabs/deployable/mixingtable/mixingtable.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/table/table.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/tier 2 workbench/workbench2.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/tier 1 workbench/workbench1.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/advent_calendar/advendcalendar.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/mailbox/mailbox.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/research table/researchtable_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/repair bench/repairbench_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/scarecrow/scarecrow.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pole.banner.large.prefab");
                entityNames.Add("assets/prefabs/deployable/dropbox/dropbox.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/photoframe/photoframe.portrait.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.large.wood.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.hanging.banner.large.prefab");
                entityNames.Add("assets/prefabs/deployable/composter/composter.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/xmastree/xmas_tree.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.post.double.prefab");
                entityNames.Add("assets/prefabs/deployable/frankensteintable/frankensteintable.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab");
                entityNames.Add("assets/prefabs/deployable/small stash/small_stash_deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/skull_fire_pit/skull_fire_pit.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.post.single.prefab");
                entityNames.Add("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/furnace.large/furnace.large.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/snowman/snowman.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pictureframe.portrait.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.medium.wood.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/photoframe/photoframe.large.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pictureframe.landscape.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/photoframe/photoframe.landscape.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pictureframe.xl.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.huge.wood.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.small.wood.prefab");
                entityNames.Add("assets/prefabs/deployable/sofa/sofa.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/sofa/sofa.pattern.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.hanging.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.post.town.roof.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.hanging.ornate.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pictureframe.xxl.prefab");
                entityNames.Add("assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab");
                entityNames.Add("assets/prefabs/deployable/planters/planter.large.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/planters/planter.small.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/chair/chair.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/bed/bed_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/bbq/bbq.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/twitch/hobobarrel/hobobarrel.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/furnace/furnace.prefab");
                entityNames.Add("assets/prefabs/deployable/fridge/fridge.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/hitch & trough/hitchtrough.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/waterpurifier/waterpurifier.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/campfire/campfire.prefab");
                entityNames.Add("assets/prefabs/deployable/tuna can wall lamp/tunalight.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/chinesenewyear/chineselantern/chineselantern.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/chippy arcade/chippyarcademachine.prefab");
                entityNames.Add("assets/prefabs/deployable/secretlab chair/secretlabchair.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab");
                entityNames.Add("assets/prefabs/misc/trophy/trophy.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/locker/locker.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/shelves/shelves.prefab");
                entityNames.Add("assets/prefabs/deployable/signs/sign.pictureframe.tall.prefab");
                entityNames.Add("assets/prefabs/deployable/spinner_wheel/spinner.wheel.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/lantern/lantern.deployed.prefab");
                entityNames.Add("assets/content/vehicles/boats/kayak/kayak.prefab");
                entityNames.Add("assets/prefabs/deployable/fireplace/fireplace.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/boogie_board/boogieboard.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/waterpump/water.pump.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/inner_tube/innertube.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/windmill/windmillsmall/electric.windmill.small.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/neon_sign/sign.neon.xl.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/lights/simplelight.prefab");
                entityNames.Add("assets/prefabs/voiceaudio/hornspeaker/connectedspeaker.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/skull spikes/skullspikes.deployed.prefab");
                entityNames.Add("assets/prefabs/instruments/piano/piano.deployed.prefab");
                entityNames.Add("assets/prefabs/instruments/xylophone/xylophone.deployed.prefab");
                entityNames.Add("assets/prefabs/instruments/drumkit/drumkit.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/paddling_pool/paddlingpool.deployed.prefab");
                entityNames.Add("assets/prefabs/voiceaudio/boombox/boombox.deployed.prefab");
                entityNames.Add("assets/prefabs/voiceaudio/laserlight/laserlight.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/sled/sled.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/beach_chair/beachtable.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/beach_chair/beachparasol.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/beach_chair/beachchair.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/generators/fuel generator/small_fuel_generator.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/beach_towel/beachtowel.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/modular car lift/electrical.modularcarlift.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/snow_machine/models/snowmachine.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/spookyspeaker/spookyspeaker.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/deployablegravestone/gravestone.wood.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/alarms/audioalarm.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/batteries/large/large.rechargable.battery.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/deployablegravestone/gravestone.stone.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/graveyard_fence/graveyardfence.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/batteries/medium/medium.rechargable.battery.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/poweredwaterpurifier/poweredwaterpurifier.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/xmas/neon_sign/sign.neon.125x215.prefab");
                entityNames.Add("assets/prefabs/deployable/elevator/elevator.prefab");
                entityNames.Add("assets/prefabs/deployable/computerstation/computerstation.deployed.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/cursed_cauldron/cursedcauldron.deployed.prefab");
                entityNames.Add("assets/content/props/fog machine/fogmachine.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/coffin/coffinstorage.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/teslacoil/teslacoil.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/batteries/smallrechargablebattery.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/app/smartalarm/smartalarm.prefab");
                entityNames.Add("assets/prefabs/voiceaudio/discoball/discoball.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/search light/searchlight.deployed.prefab");
                entityNames.Add("assets/prefabs/voiceaudio/telephone/telephone.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/gates/rfreceiver/rfreceiver.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/candles/smallcandleset.prefab");
                entityNames.Add("assets/prefabs/misc/halloween/candles/largecandleset.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/gates/rfbroadcaster/rfbroadcaster.prefab");
                entityNames.Add("assets/prefabs/deployable/reactive target/reactivetarget_deployed.prefab");
                entityNames.Add("assets/prefabs/misc/summer_dlc/abovegroundpool/abovegroundpool.deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/playerioents/electricheater/electrical.heater.prefab");
                entityNames.Add("assets/bundled/prefabs/radtown/loot_barrel_1.prefab");
                entityNames.Add("assets/bundled/prefabs/radtown/loot_barrel_2.prefab");
                entityNames.Add("assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab");
                entityNames.Add("assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab");
                entityNames.Add("assets/bundled/prefabs/radtown/oil_barrel.prefab");
            }

            if (useTrapsTurrets)
            {
                entityNames.Add("assets/prefabs/deployable/floor spikes/spikes.floor.prefab");
                entityNames.Add("assets/prefabs/deployable/landmine/landmine.prefab");
                entityNames.Add("assets/prefabs/npc/flame turret/flameturret.deployed.prefab");
                entityNames.Add("assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/single shot trap/guntrap.deployed.prefab");
                entityNames.Add("assets/prefabs/npc/autoturret/autoturret_deployed.prefab");
                entityNames.Add("assets/prefabs/deployable/bear trap/beartrap.prefab");
            }

            if (maxAimingTimer < 2)
                maxAimingTimer = 2;
            if (acceleration < 1)
                acceleration = 1;
            if (acceleration > 10)
                acceleration = 10;

            switch (rocketType)
            {
                case 0:
                    rocketPrefab = "assets/prefabs/ammo/rocket/rocket_smoke.prefab";
                    ammoName = "ammo_rocket_smoke.item (ItemDefinition)";
                    break;
                case 1:
                    rocketPrefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
                    ammoName = "ammo_rocket_basic.item (ItemDefinition)";
                    break;
                case 2:
                    rocketPrefab = "assets/prefabs/ammo/rocket/rocket_hv.prefab";
                    ammoName = "ammo_rocket_hv.item (ItemDefinition)";
                    break;
                case 3:
                    rocketPrefab = "assets/prefabs/ammo/rocket/rocket_fire.prefab";
                    ammoName = "ammo_rocket_fire.item (ItemDefinition)";
                    break;
            }

            switch (Configuration.explosionType)
            {
                case 1:
                    expPrefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
                    break;
                case 2:
                    expPrefab = "assets/prefabs/ammo/rocket/rocket_fire.prefab";
                    break;
                case 3:
                    expPrefab = "assets/prefabs/ammo/rocket/rocket_smoke.prefab";
                    break;
                case 4:
                    expPrefab = "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab";
                    break;
                case 5:
                    expPrefab = "assets/prefabs/npc/patrol helicopter/rocket_heli_napalm.prefab";
                    break;
                case 6:
                    expPrefab = "assets/prefabs/npc/patrol helicopter/rocket_heli_airburst.prefab";
                    break;
                case 7:
                    expPrefab = "assets/prefabs/npc/sam_site_turret/rocket_sam.prefab";
                    break;
                case 8:
                    expPrefab = "assets/prefabs/ammo/40mmgrenade/40mm_grenade_he.prefab";
                    break;
                case 9:
                    expPrefab = "assets/prefabs/tools/c4/explosive.timed.deployed.prefab";
                    break;
                case 10:
                    expPrefab = "assets/prefabs/weapons/f1 grenade/grenade.f1.deployed.prefab";
                    break;
                case 11:
                    expPrefab = "assets/prefabs/weapons/beancan grenade/grenade.beancan.deployed.prefab";
                    break;
                case 12:
                    expPrefab = "assets/prefabs/weapons/satchelcharge/explosive.satchel.deployed.prefab";
                    break;
                case 13:
                    expPrefab = "assets/content/vehicles/mlrs/rocket_mlrs.prefab";
                    break;
            }

            delayRocket = acceleration * -5f;

        }

        [Command("hmcraft")]
        private void hmcraft(IPlayer iplayer, string command, string[] args)
        {
            var player = (BasePlayer)iplayer.Object;

            if (CanCraftHomingMissiles(player))
                if (canCraft)
                {
                    int amount = 1;
                    if (args.Length == 0 || args.Length > 1) ;
                    amount = 1;
                    if (args.Length == 1)
                    {
                        int x = 0;
                        int.TryParse(args[0], out x);
                        if (x <= 0)
                            x = 1;
                        amount = x;
                    }

                    int level = 0;

                    if (wbLevel > 0)
                    {
                        if (player.triggers != null)
                            if (player.triggers.Count > 0)
                                for (int i = 0; i < player.triggers.Count; i++)
                                {
                                    if (player.triggers[i].name == "WorkbenchSource")
                                    {
                                        var trigger = player.triggers[i] as TriggerWorkbench;
                                        if (trigger.parentBench.Workbenchlevel != null)
                                            if (level < trigger.parentBench.Workbenchlevel)
                                                level = trigger.parentBench.Workbenchlevel;
                                    }
                                }
                        if (level < wbLevel)
                        {
                            player.ChatMessage("Requires a level " + wbLevel.ToString() + " workbench");
                            return;
                        }
                    }

                    int doneAmount = 0;
                    for (int index = 0; index < amount; index++)
                    {
                        int s = 0;
                        for (int i = 0; i < craftItems.Count; i++)
                        {
                            ItemDefinition item = ItemManager.itemDictionaryByName[craftItems[i].shortname];
                            if (item)
                                if (player.inventory.GetAmount(item.itemid) >= craftItems[i].amount)
                                    s++;
                        }
                        if (s == craftItems.Count)
                        {

                            for (int i = 0; i < craftItems.Count; i++)
                            {
                                ItemDefinition item = ItemManager.itemDictionaryByName[craftItems[i].shortname];
                                player.inventory.Take(null, item.itemid, craftItems[i].amount);
                            }
                            player.GiveItem(ItemManager.CreateByName("ammo.rocket.smoke", 1, 0), BaseEntity.GiveItemReason.PickedUp);
                            doneAmount++;
                        }
                        else
                        {
                            if (amount > 1)
                                if (doneAmount == 1)
                                    player.ChatMessage("Only 1 missile were crafted");
                                else
                                    player.ChatMessage("Only " + doneAmount.ToString() + " missiles were crafted");

                            player.ChatMessage("Not enough resources for crafting");

                            return;
                        }
                    }
                }
                else
                    player.ChatMessage("You can't craft homing missiles");
            else
                player.ChatMessage("You do not have permission to craft homing missiles");

        }

        void SaveConfig(object config) => Config.WriteObject(config, true);

        void LoadConfig()
        {
            base.Config.Settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            Configuration = Config.ReadObject<ConfigData>();
            SaveConfig(Configuration);
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData();
            SaveConfig(config);
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            timer.Once(3f, () =>
            {
                if (player.IsConnected)
                    SetReadyPlayers(player);
            });
        }

        object OnWeaponReload(BaseProjectile projectile, BasePlayer player)
        {
            timer.Once(7f, () =>
            {
                if (player)
                    SetReadyPlayers(player);
            });
            return null;
        }

        void OnTick()
        {
            int s = 0;
            for (int i = 0; i < rockets.Count; i++)
            {
                rockets[i].greenChTimer--;
                if (rockets[i].greenChTimer > 0)
                {
                    DrawGreenCrosshair(rockets[i]);
                    s++;
                }
                else
                    if (!rockets[i].rocket)
                    rockets.Remove(rockets[i]);
            }

            if (s == 0)
                Unsubscribe("OnTick");
        }

        void SetReadyPlayers(BasePlayer player)
        {
            if (player == null)
                return;
            TargetingKD playerItem = targetsKD.Find(item => item.playerOwn == player);
            if (playerItem == null)
                return;

            playerItem.ready = false;
            BaseProjectile heldEntity = player.GetHeldEntity() as BaseProjectile;
            if (heldEntity != null)
                if (CanUseHomingMissiles(playerItem.playerOwn))
                    if (heldEntity.name == "assets/prefabs/weapons/rocketlauncher/rocket_launcher.entity.prefab" && heldEntity.primaryMagazine.ammoType.ToString() == ammoName && heldEntity.primaryMagazine.contents > 0)
                    {
                        playerItem.ready = true;
                        Subscribe("OnPlayerInput");

                        int s = 0;
                        for (int i = 0; i < zones.Count; i++)
                        {
                            bool ZoneCheck = Convert.ToBoolean(ZoneManager?.Call("isPlayerInZone", zones[i], player));
                            if (ZoneCheck) s++;
                        }
                        if (s > 0)
                            if (!whiteList)
                            {
                                playerItem.ready = false;
                                playerItem.playerOwn.ChatMessage(restrictionMessage);
                            }
                        if (s == 0)
                            if (whiteList)
                            {
                                playerItem.ready = false;
                                playerItem.playerOwn.ChatMessage(restrictionMessage);
                            }
                    }

            bool ok = false;
            for (int i = 0; i < targetsKD.Count; i++)
                if (targetsKD[i].ready)
                    ok = true;

            if (!ok)
                Unsubscribe("OnPlayerInput");

        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            TargetingKD playerItem = targetsKD.Find(item => item.playerOwn == player);
            if (playerItem != null)
                playerItem.timer = 0;

            if (entity.name == rocketPrefab)
            {
                var explosive = entity as TimedExplosive;
                explosive.SetFuse(selfDetonation);
                foreach (var damage in explosive.damageTypes)
                    damage.amount = baseDamage;
                BaseEntity eny = GameManager.server.CreateEntity(expPrefab);
                var explosive1 = eny as TimedExplosive;
                explosive.explosionEffect = explosive1.explosionEffect;
            }

            SetReadyPlayers(player);
            for (int i = 0; i < rockets.Count; i++)
                if (rockets[i].target != null && rockets[i].playerOwn == player && rockets[i].rocket == null && entity.name == rocketPrefab)
                {

                    rockets[i].rocket = entity;
                    rockets[i].rocketTimer = 0;
                    rockets[i].offset = Vector3.up;
                    rockets[i].expRadius = 3f;

                    switch (rockets[i].target.PrefabName)
                    {
                        case "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab":
                            rockets[i].offset = Vector3.up * -0.5f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/prefabs/npc/ch47/ch47scientists.entity.prefab":
                            rockets[i].offset = Vector3.up * -0.5f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/prefabs/npc/m2bradley/bradleyapc.prefab":
                            rockets[i].offset = Vector3.up * 2f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/rust.ai/agents/bear/polarbear.prefab":
                            rockets[i].offset = Vector3.up * 1f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab":
                            rockets[i].offset = Vector3.up * 2f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab":
                            rockets[i].offset = Vector3.up * 2f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/content/vehicles/boats/rhib/rhib.prefab":
                            rockets[i].offset = Vector3.up * 2f;
                            rockets[i].expRadius = 5f;
                            break;
                        case "assets/content/vehicles/workcart/workcart.entity.prefab":
                            rockets[i].offset = Vector3.up * 3f;
                            rockets[i].expRadius = 6f;
                            break;
                    }

                    RocketActive(rockets[i]);
                }

        }

        void RocketActive(Rocket rocket)
        {
            timer.Repeat(0.2f, 50, () =>
            {
                if (rocket.target && rocket.rocket)
                {
                    rocket.rocketTimer += acceleration * 2f;
                    Vector3 lTargetDir = (rocket.target.transform.position + rocket.offset) - rocket.rocket.transform.position;
                    rocket.rocket.transform.rotation = Quaternion.RotateTowards(rocket.rocket.transform.rotation, Quaternion.LookRotation(lTargetDir), 33f);
                    Vector3 direction = rocket.rocket.transform.forward;
                    if (rocket.rocketTimer > maxRocketSpeed)
                        rocket.rocketTimer = maxRocketSpeed;

                    rocket.rocket.SendMessage("InitializeVelocity", direction * rocket.rocketTimer);

                    if (Vector3.Distance(rocket.rocket.transform.position, rocket.target.transform.position + rocket.offset) < rocket.expRadius)
                        rocket.rocket.GetComponent<TimedExplosive>().Explode();

                }
            });
        }

        void DrawRedCrosshair(BasePlayer player, BaseEntity entity, string s)
        {
            if (maxAimingTimer > 2)
            {

                if (!player.IsAdmin)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    player.SendNetworkUpdateImmediate();
                    player.SendConsoleCommand("ddraw.text", 0.06f, firstCrossHairColor, entity.transform.position + Vector3.up * 1f, "<size=" + firstCrossHairSize.ToString() + ">" + firstCrossHairLeftPart + s + firstCrossHairMiddlePart + s + firstCrossHairRightPart + "</size>");
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
                else
                    player.SendConsoleCommand("ddraw.text", 0.06f, firstCrossHairColor, entity.transform.position + Vector3.up * 1f, "<size=" + firstCrossHairSize.ToString() + ">" + firstCrossHairLeftPart + s + firstCrossHairMiddlePart + s + firstCrossHairRightPart + "</size>");

                if (s.Length == maxAimingTimer - 2 && enableSFX)
                {
                    string sfxPrefab = "assets/prefabs/npc/autoturret/effects/targetlost.prefab";
                    Effect.server.Run(sfxPrefab, player.transform.position);
                }
            }
        }

        void DrawGreenCrosshair(Rocket rocket)
        {

            if (rocket.target != null)
                if (!rocket.playerOwn.IsAdmin)
                {
                    rocket.playerOwn.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    rocket.playerOwn.SendNetworkUpdateImmediate();
                    rocket.playerOwn.SendConsoleCommand("ddraw.text", 0.1f, secondCrossHairColor, rocket.target.transform.position + Vector3.up * 1f, "<size=" + secondCrossHairSize.ToString() + ">" + secondCrossHair + "</size>");
                    rocket.playerOwn.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    rocket.playerOwn.SendNetworkUpdateImmediate();
                }
                else
                    rocket.playerOwn.SendConsoleCommand("ddraw.text", 0.1f, secondCrossHairColor, rocket.target.transform.position + Vector3.up * 1f, "<size=" + secondCrossHairSize.ToString() + ">" + secondCrossHair + "</size>");

        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {

            if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                SetReadyPlayers(player);

            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                TargetingKD playerItem = targetsKD.Find(item => item.playerOwn == player);
                if (playerItem.ready)
                {

                    if (playerItem.timer < maxAimingTimer)
                    {
                        if (playerItem.timer > 0)
                            playerItem.timer--;
                        RaycastHit hit = new RaycastHit();
                        if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue, layerS))
                        {
                            BaseEntity entity = hit.GetEntity();
                            if (entity != null)
                            {
                                bool ok = false;
                                for (int i = 0; i < entityNames.Count; i++)
                                    if (entity.PrefabName == entityNames[i])
                                        ok = true;
                                if (ok)
                                {
                                    playerItem.timer += 2;
                                    string s = "";
                                    for (int i = 0; i < maxAimingTimer - playerItem.timer; i++)
                                        s += " ";
                                    DrawRedCrosshair(player, entity, s);

                                    if (playerItem.timer == maxAimingTimer)
                                    {
                                        var currentItem = new Rocket(player, null, null, 1000, 0, Vector3.zero, maxTargetTimer);
                                        currentItem.target = entity;
                                        currentItem.playerOwn = player;

                                        if (enableSFX)
                                        {
                                            string sfxPrefab = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
                                            Effect.server.Run(sfxPrefab, player.transform.position);
                                        }

                                        Rocket alreadyItem = rockets.Find(item => item.playerOwn == player);

                                        if (alreadyItem == null)
                                            rockets.Add(currentItem);
                                        if (alreadyItem != null)
                                            if (alreadyItem.rocket != null)
                                                rockets.Add(currentItem);
                                            else
                                            {
                                                alreadyItem.playerOwn = player;
                                                alreadyItem.target = entity;
                                                alreadyItem.rocketTimer = 1000;
                                                alreadyItem.greenChTimer = maxTargetTimer;
                                                currentItem = alreadyItem;
                                            }
                                        Subscribe("OnTick");
                                        SetReadyPlayers(currentItem.playerOwn);
                                    }
                                }
                            }
                        }
                    }

                }

            }

            if (input.WasJustReleased(BUTTON.FIRE_SECONDARY))
            {
                TargetingKD playerItem = targetsKD.Find(item => item.playerOwn == player);
                if (playerItem != null)
                    playerItem.timer = 0;
            }

        }

        void OnExitZone(string ZoneID, BasePlayer player)
        {
            if (player)
                if (player.IsConnected)
                    SetReadyPlayers(player);
        }

        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            if (player)
                if (player.IsConnected)
                    SetReadyPlayers(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            TargetingKD alreadyItem = targetsKD.Find(item => item.playerOwn == player);
            if (alreadyItem != null) targetsKD.Remove(alreadyItem);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            var newTargetingItem = new TargetingKD(player, 0, false);
            targetsKD.Add(newTargetingItem);
        }

        void OnEntityKill(BaseNetworkable entityOwn)
        {
            if (entityOwn.name == rocketPrefab)
            {
                Rocket alreadyItem = rockets.Find(item => item.rocket == entityOwn);

                if (alreadyItem != null)
                    rockets.Remove(alreadyItem);
            }

        }

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (info.WeaponPrefab)
                if (info.WeaponPrefab.ToString() == "rocket_smoke[0]")
                {
                    var ent = entity as BaseCombatEntity;
                    info.damageTypes.ScaleAll(0);
                    info.damageTypes.Set(Rust.DamageType.Generic, baseDamage);

                    if (entity.PrefabName.Contains("assets/content/vehicles/modularcar/module_entities/"))
                        info.damageTypes.Set(Rust.DamageType.Generic, modulecarDamage);


                    if (entity.PrefabName.Contains("assets/rust.ai/agents/npcplayer/humannpc/"))
                        info.damageTypes.Set(Rust.DamageType.Generic, npcDamage);


                    switch (entity.PrefabName)
                    {
                        case "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, heliDamage);
                            break;
                        case "assets/prefabs/npc/ch47/ch47scientists.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, chinookDamage);
                            break;
                        case "assets/prefabs/npc/m2bradley/bradleyapc.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, bradleyDamage);
                            break;
                        case "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, scrapheliDamage);
                            break;
                        case "assets/content/vehicles/minicopter/minicopter.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, minicopterDamage);
                            break;
                        case "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, hotAirDamage);
                            break;
                        case "assets/content/vehicles/boats/rowboat/rowboat.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, boatDamage);
                            break;
                        case "assets/content/vehicles/boats/rhib/rhib.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, boatDamage);
                            break;
                        case "assets/content/vehicles/snowmobiles/snowmobile.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, snowmobileDamage);
                            break;
                        case "assets/rust.ai/nextai/testridablehorse.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/bear/polarbear.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/chicken/chicken.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/boar/boar.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/wolf/wolf.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/stag/stag.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/rust.ai/agents/bear/bear.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, animalDamage);
                            break;
                        case "assets/prefabs/player/player.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, playerDamage);
                            break;
                        case "assets/content/vehicles/submarine/submarinesolo.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, submarineDamage);
                            break;
                        case "assets/content/vehicles/submarine/submarineduo.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, submarineDamage);
                            break;
                        case "assets/content/vehicles/train/trainwagonunloadablefuel.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/train/trainwagonunloadable.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/train/trainwagona.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/train/trainwagonb.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/train/trainwagonc.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/locomotive/locomotive.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/workcart/workcart_aboveground.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/workcart/workcart_aboveground2.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;
                        case "assets/content/vehicles/workcart/workcart.entity.prefab":
                            info.damageTypes.Set(Rust.DamageType.Generic, trainDamage);
                            break;

                    }
                }
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player.IsConnected)
                SetReadyPlayers(player);
            return null;
        }


    }
}