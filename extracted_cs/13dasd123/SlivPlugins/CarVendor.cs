// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using Rust.Modular;

namespace Oxide.Plugins
{
    [Info("CarVendor", "senyaa", "1.1.2")]
    [Description("Adds a car vendor NPC")]
    public class CarVendor : RustPlugin 
    {
        #region Constants
        public const string SHOPKEEPER_PREFAB = "assets/prefabs/npc/bandit/shopkeepers/boat_shopkeeper.prefab";

        public const string SPAWNER_TRIGGER_PREFAB = "assets/prefabs/deployable/playerioents/gates/randswitch/electrical.random.switch.deployed.prefab";
        public const string SPAWNER_TRIGGER_CAT = "Car Vendor spawner";
        public const string MONUMENT_CAT = "Car Shop";
        public const string CAR_SPAWNPOINT_TRIGGER_PREFAB = "assets/prefabs/deployable/playerioents/gates/dflipflop/electrical.memorycell.deployed.prefab";
        public const string CAR_SPAWNPOINT_TRIGGER_CAT = "Car Spawnpoint";

        public const string BOLDTEXTCOLOR = "0.9686 0.9216 0.8824 0.5";
        public const string REGULARTEXTCOLOR = "0.9686 0.9215 0.8823 0.5235";
        public const string TRANSPARENT = "0 0 0 0";
        public const string RED = "0.8 0.28 0.2 1";
        public const string GREEN = "0.3647 0.4471 0.2235";
        public const string BG_COLOR = "0.1137 0.1059 0.0902 0.9";
        public const string DARK_GRAY = "0.3137 0.302 0.2824 1";
        public const string BLACK = "0.1686 0.1608 0.1412";

        public const BaseEntity.Flags CUSTOM_NPC_FLAG = BaseEntity.Flags.Reserved10;
        #endregion

        #region Fields
        public static CarVendor Instance;
        PluginConfig config;
        Dictionary<ulong, NPCTalking> LastTalkedTo;
        Dictionary<string, Dialog> Dialogs;
        Dictionary<NPCTalking, PositionData> NPCVehicleSpawnpoints;
        Dictionary<ulong, DialogChoices> PlayerDialogChoices;
        Dictionary<ulong, PositionData> PlayerSpawnpoints;
        Dictionary<string, KeyValuePair<PositionData, PositionData>> ManuallyPlacedNPCs;

        List<BaseNetworkable> TriggerList;
        List<PositionData> SpawnerList;
        List<PositionData> SpawnpointList;

        ItemDefinition priceDef;
        #endregion

        #region Config
        class PluginConfig
        {
            [JsonProperty("(0) Price item short name")]
            public string Price_Item = "scrap";

            [JsonProperty("(1) Price of chassis/components")]
            public Dictionary<string, int> Price = new Dictionary<string, int>()
            {
                {"ComponentsTier0", 0 },
                {"ComponentsTier1", 50 },
                {"ComponentsTier2", 150 },
                {"ComponentsTier3", 200 },
                {"Chassis2", 75 },
                {"Chassis3", 150 },
                {"Chassis4", 175 }
            };

            [JsonProperty("(2) Car modules")]
            public Dictionary<int, string[]> Modules = new Dictionary<int, string[]>()
            {
                {2, new string[]
                {
                "vehicle.1mod.cockpit.with.engine",
                "vehicle.1mod.rear.seats"
                }},
                {3, new string[]
                {
                "vehicle.1mod.engine",
                "vehicle.1mod.cockpit.with.engine",
                "vehicle.1mod.rear.seats"
                }},
                {4, new string[]
                {
                "vehicle.1mod.engine",
                "vehicle.1mod.cockpit.armored",
                "vehicle.1mod.passengers.armored",
                "vehicle.1mod.engine"
                }},
            };

            [JsonProperty("(3) NPC Name")]
            public string NPC_Name = "Car Vendor";

            [JsonProperty("(4) Spawn permission name")]
            public string Spawn_Permission_Name = "carvendor.spawn";

            [JsonProperty("(5) Car starting fuel")]
            public float Starting_Fuel = 75f;

            [JsonProperty("(6) Max distance between NPC and spawnpoint trigger (meters)")]
            public float Spawnpoint_Distance = 100f;

            [JsonProperty("(7) Check for other cars within radius (meters)")]
            public float Safe_Radius = 15f;

            [JsonProperty("(8) Nudge players (true/false)")]
            public bool Nudge_Players = true;

            [JsonProperty("(9) Player nudge radius (meters)")]
            public float Nudge_Radius = 5f;

            [JsonProperty("CUI Name")]
            public string UI_Name = "carvendor_ui";
        }
        protected override void LoadDefaultConfig() => Config.WriteObject(new PluginConfig(), true);
        #endregion

        #region Types
        public class PositionData
        {
            public Vector3 Position;
            public Vector3 Rotation;

            public PositionData(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation.eulerAngles;
            }
            [JsonConstructor]
            public PositionData(Vector3 position, Vector3 rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
        public struct Dialog
        {
            [JsonProperty("(1) NPC Name")]
            public string NPC_Name;
            [JsonProperty("(2) Dialog text")]
            public string Text;
            [JsonProperty("(3) Dialog responses")]
            public DialogOption[] Options;
            [JsonProperty("(4) Response counter offset")]
            public int Counter_Offset;

            public Dialog(string NPC_Name, string Text, DialogOption[] Options, int Counter_Offset = 1)
            {
                this.NPC_Name = NPC_Name;   
                this.Text = Text;
                this.Options = Options;
                this.Counter_Offset = Counter_Offset;
            }
        }

        public struct DialogOption
        {
            [JsonProperty("(1) Response text")]
            public string Text;
            [JsonProperty("(2) Console command")]
            public string Command;

            public DialogOption(string Text, string Command = "carvendor.end")
            {
                this.Text = Text;
                this.Command = Command;
            }
        }

        public class DialogChoices
        {
            public int Chassis
            {
                get
                {
                    return _chassis;
                }
                set
                {
                    if (value >= 2 && value <= 4)
                        _chassis = value;
                    else
                        _chassis = 2;
                }
            }
            public int Components_Tier
            {
                get
                {
                    return _tier;
                }
                set
                {
                    if (value >= 0 && value <= 3)
                        _tier = value;
                    else

                        _tier = 1;
                }
            }

            private int _tier;
            private int _chassis;

            public DialogChoices()
            {
                _tier = 1;
                _chassis = 2;
            }

            public int CalculatePrice()

            {
                var result = 0;
                result += Instance.config.Price["ComponentsTier" + Components_Tier.ToString()];
                result += Instance.config.Price["Chassis" + Chassis.ToString()];
                return result;
            }

            public string GetPrefab() => $"assets/content/vehicles/modularcar/car_chassis_{Chassis}module.entity.prefab";
        }
        #endregion

        #region Hooks
        void Init()
        {
            Instance = this;
            LastTalkedTo = new Dictionary<ulong, NPCTalking>();
            Dialogs = new Dictionary<string, Dialog>();
            NPCVehicleSpawnpoints = new Dictionary<NPCTalking, PositionData>();
            PlayerDialogChoices = new Dictionary<ulong, DialogChoices>();
            PlayerSpawnpoints = new Dictionary<ulong, PositionData>();
            SpawnerList = Facepunch.Pool.GetList<PositionData>();
            SpawnpointList = Facepunch.Pool.GetList<PositionData>();
            TriggerList = Facepunch.Pool.GetList<BaseNetworkable>();

            config = Config.ReadObject<PluginConfig>();
            permission.RegisterPermission(config.Spawn_Permission_Name, this);

            try
            {
                priceDef = ItemManager.FindItemDefinition(config.Price_Item);
                if (priceDef == null)
                    throw new NullReferenceException();
            }
            catch (Exception)
            {
                PrintError("Item name is invalid! Players won't be charged for cars");
            }

            // TODO: Read dialogs from CFG
            Dialogs.Add(
                "intro",
                new Dialog(config.NPC_Name, "Hello there, is there anything I can help you with?",
                new DialogOption[]
                {
                    new DialogOption("I'm looking to buy a car", "carvendor.dialog choose_chassis"),
                    new DialogOption("Nah, just looking around"),
                }));
            Dialogs.Add(
                "padbusy",
                new Dialog(config.NPC_Name, "Hold up.. There is a car, give me a minute to get them outta here.",
                new DialogOption[]
                {
                    new DialogOption("Okay.")
                }));
            Dialogs.Add(
                "choose_chassis",
                new Dialog(config.NPC_Name, "What did you have in mind?",
                new DialogOption[]
                {
                    new DialogOption($"2 module chassis [{config.Price["Chassis2"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog choose_components 2"),
                    new DialogOption($"3 module chassis [{config.Price["Chassis3"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog choose_components 3"),
                    new DialogOption($"4 module chassis [{config.Price["Chassis4"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog choose_components 4"),
                }, 2));
            Dialogs.Add(
                "choose_components",
                new Dialog(config.NPC_Name, "What tier of components do you want in your car?",
                new DialogOption[]
                {
                    new DialogOption($"Tier 1 [{config.Price["ComponentsTier1"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog confirmation 1"),
                    new DialogOption($"Tier 2 [{config.Price["ComponentsTier2"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog confirmation 2"),
                    new DialogOption($"Tier 3 [{config.Price["ComponentsTier3"]} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog confirmation 3"),
                    new DialogOption($"No comps [FREE]", "carvendor.dialog confirmation 0"),
                }));
            Dialogs.Add(
                "purchase_complete",
                new Dialog(config.NPC_Name,
                    "Thanks for the purchase!\n" +
                    "Your car is behind you. Be sure to leave in the next 5 minutes or I'll have to repossess your purchase.\n" +
                    "No refunds",
                new DialogOption[]
                {
                    new DialogOption("Thanks.") 
                }));
        }

        void Unload()
        {
            var to_save = new Dictionary<int, PositionData>();

            foreach(var entry in NPCVehicleSpawnpoints)
                to_save.Add(entry.Key.GetInstanceID(), entry.Value);

            Interface.Oxide.DataFileSystem.WriteObject("CarVendor_CarSpawnpoints", to_save);

            to_save = null;
            LastTalkedTo = null;
            Dialogs = null;
            NPCVehicleSpawnpoints = null;
            PlayerDialogChoices = null;
            PlayerSpawnpoints = null;
            ManuallyPlacedNPCs = null;
            Instance = null;
        }

        object OnNpcConversationStart(NPCTalking npcTalking, BasePlayer player)
        {
            if (!npcTalking.HasFlag(CUSTOM_NPC_FLAG)) return null;

            if (LastTalkedTo.ContainsKey(player.userID))
                LastTalkedTo[player.userID] = npcTalking;
            else
                LastTalkedTo.Add(player.userID, npcTalking);

            if (!NPCVehicleSpawnpoints.ContainsKey(npcTalking))
                return null;

            var pos_data = NPCVehicleSpawnpoints[LastTalkedTo[player.userID]];

            npcTalking.ClientRPCPlayer(null, player, "Client_StartConversation", 0, "padbusy");

            ShowUI(player, Dialogs[IsPadOccupied(pos_data.Position) ? "padbusy" : "intro"]);
            return false;
        }

        void OnNpcConversationEnded(NPCTalking npcTalking, BasePlayer player)
        {
            if (!npcTalking.HasFlag(CUSTOM_NPC_FLAG)) return;

            EndDialog(player);
        }

        void OnWorldPrefabSpawned(GameObject gameObject, string category)
        {
            if ((category.Contains(SPAWNER_TRIGGER_CAT, System.Globalization.CompareOptions.IgnoreCase) && gameObject.name == SPAWNER_TRIGGER_PREFAB)
                || (category.Contains(MONUMENT_CAT, System.Globalization.CompareOptions.IgnoreCase) && gameObject.name == SPAWNER_TRIGGER_PREFAB))
            {
                var ent = gameObject.GetComponent<BaseNetworkable>();
                SpawnerList.Add(new PositionData(ent.transform.position, ent.transform.rotation));
                TriggerList.Add(ent);
                return;
            }

            if (category.Contains(CAR_SPAWNPOINT_TRIGGER_CAT, System.Globalization.CompareOptions.IgnoreCase) && gameObject.name == CAR_SPAWNPOINT_TRIGGER_PREFAB || 
                (category.Contains(MONUMENT_CAT, System.Globalization.CompareOptions.IgnoreCase) && gameObject.name == CAR_SPAWNPOINT_TRIGGER_PREFAB))
            {
                var ent = gameObject.GetComponent<BaseNetworkable>();
                SpawnpointList.Add(new PositionData(ent.transform.position, ent.transform.rotation));
                TriggerList.Add(ent);
            }
        }

        void OnServerInitialized(bool initial)
        {
            if (!initial)
            {
                Puts("Plugin hotloaded - Relinking");
                Facepunch.Pool.FreeList(ref SpawnerList);
                Facepunch.Pool.FreeList(ref SpawnpointList);
                
                var data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<int, PositionData>>("CarVendor_CarSpawnpoints");
                var c = 0;

                foreach (var ent in BaseNetworkable.serverEntities)
                {
                    if (!(ent is NPCTalking)) continue; 
                    var ent_id = ent.GetInstanceID();

                    if (!data.ContainsKey(ent_id)) continue;
                    NPCVehicleSpawnpoints.Add(ent as NPCTalking, data[ent_id]);
                    c++;
                }

                LoadManuallyPlacedNPCs();
                
                Puts($"Relinked {c} spawnpoint(s) to NPC(s)");
                return;
            }

            LoadManuallyPlacedNPCs();

            var count = 0;

            foreach(var spawner in SpawnerList)
            {
                PositionData spawnpoint_ent = null;
                var minDist = config.Spawnpoint_Distance;
                foreach(var spawnpoint in SpawnpointList)
                {
                    var distance = Vector3.Distance(spawnpoint.Position, spawner.Position);
                    if(distance < minDist)
                    {
                        spawnpoint_ent = spawnpoint;
                        minDist = distance;
                    }
                }

                if(spawnpoint_ent == null)
                {
                    PrintError($"No car spawnpoint was found! Not spawning NPC...");
                    continue;
                }

                var npcs = Facepunch.Pool.GetList<NPCTalking>();

                Vis.Entities(spawner.Position, 0.3f, npcs);
                if (npcs.Count == 0)
                {
                    var npc = GameManager.server.CreateEntity(SHOPKEEPER_PREFAB, spawner.Position, Quaternion.Euler(spawner.Rotation)) as NPCTalking;
                    npc.Spawn();
                    NPCVehicleSpawnpoints.Add(npc, spawnpoint_ent);
                    npc.SetFlag(CUSTOM_NPC_FLAG, true, true);
                    npc.enableSaving = false;
                    count++;
                } 
                else
                {
                    foreach(var npc in npcs)
                    {
                        NPCVehicleSpawnpoints.Add(npc, spawnpoint_ent);
                        npc.SetFlag(CUSTOM_NPC_FLAG, true, true);
                        npc.enableSaving = false;
                        count++;
                    }
                }
                Facepunch.Pool.FreeList(ref npcs);
            } 

            Facepunch.Pool.FreeList(ref SpawnerList);
            Facepunch.Pool.FreeList(ref SpawnpointList);

            foreach(var trigger in TriggerList)
                if (!trigger.IsDestroyed)
                    trigger.Kill();

            Facepunch.Pool.FreeList(ref TriggerList);
        }
        #endregion

        #region Methods
        void LoadManuallyPlacedNPCs()
        {
            ManuallyPlacedNPCs = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, KeyValuePair<PositionData, PositionData>>>("CarVendor_ManuallyPlacedNPCs");
            var i = 0;

            foreach (var npc_pos in ManuallyPlacedNPCs)
            {
                if (npc_pos.Key.StartsWith("MONUMENT"))
                {
                    var monumentName = npc_pos.Key.Split(':')[1];

                    foreach (var monument in TerrainMeta.Path.Monuments)
                    {
                        if (monument.name != monumentName) continue;
                        var spawnpoint = new PositionData(monument.transform.TransformPoint(npc_pos.Value.Value.Position), npc_pos.Value.Value.Rotation + monument.transform.rotation.eulerAngles);
                        SpawnNPC(monument.transform.TransformPoint(npc_pos.Value.Key.Position), Quaternion.Euler(npc_pos.Value.Key.Rotation + monument.transform.rotation.eulerAngles), spawnpoint);
                        i++;
                    }
                }
                else
                {
                    SpawnNPC(npc_pos.Value.Key.Position, Quaternion.Euler(npc_pos.Value.Key.Rotation), npc_pos.Value.Value);
                    i++;
                }
            }

            Puts($"Spawned {i} manually placed car vendor(s)");
        }

        void SpawnNPC(Vector3 position, Quaternion rotation, PositionData spawnpoint)
        {
            var npcs = Facepunch.Pool.GetList<NPCTalking>();
                
            Vis.Entities(position, 0.3f, npcs);
            if (npcs.Count == 0)
            {
                var vendor = GameManager.server.CreateEntity(SHOPKEEPER_PREFAB, position, rotation) as NPCTalking;
                vendor.Spawn();
                vendor.enableSaving = false;
                vendor.SetFlag(CUSTOM_NPC_FLAG, true, true);
                NPCVehicleSpawnpoints.Add(vendor, spawnpoint);
            }
            else 
            {
                foreach (var npc in npcs)
                {
                    npc.SetFlag(CUSTOM_NPC_FLAG, true, true);
                    if (!NPCVehicleSpawnpoints.ContainsKey(npc))
                        NPCVehicleSpawnpoints.Add(npc, spawnpoint);
                }
            }
            Facepunch.Pool.FreeList(ref npcs);
        }

        void EndDialog(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, config.UI_Name);

            if (LastTalkedTo.ContainsKey(player.userID)) 
            {
                LastTalkedTo[player.userID].ClientRPCPlayer(null, player, "Client_EndConversation");
                LastTalkedTo.Remove(player.userID);
            }
            if (PlayerDialogChoices.ContainsKey(player.userID))
                PlayerDialogChoices.Remove(player.userID);
        }

        void AddModules(ModularCar car, string[] moduleNames)
        {
            for (int socketIndex = 0; socketIndex < moduleNames.Length; socketIndex++)
            {
                var desiredItem = moduleNames[socketIndex];

                var moduleItem = ItemManager.CreateByPartialName(desiredItem);
                if (moduleItem != null)
                    car.TryAddModule(moduleItem, socketIndex);
            }
        }

        void AddComponents(ModularCar car, int tier)
        {
            foreach (var module in car.AttachedModuleEntities)
            {
                var engineModule = module as VehicleModuleEngine;
                if (engineModule == null)
                    continue;

                var engineStorage = engineModule.GetContainer() as EngineStorage;

                if (engineStorage != null)
                {
                    var inventory = engineStorage.inventory;

                    for (var slot = 0; slot < inventory.capacity; slot++)
                    {
                        ItemModEngineItem output;
                        if (!engineStorage.allEngineItems.TryGetItem(tier, engineStorage.slotTypes[slot], out output))
                            break;

                        var component = output.GetComponent<ItemDefinition>();
                        var item = ItemManager.Create(component);
                        if (item == null)
                            break;
                        
                        if (!item.MoveToContainer(engineStorage.inventory, slot, allowStack: false))
                        {
                            item.Remove();
                            break;
                        }
                    }
                }
            }
        }
        
        void NudgePlayersInRadius(Vector3 spawnpoint, float radius)
        {
            var players = Facepunch.Pool.GetList<BasePlayer>();

            Vis.Entities(spawnpoint, radius, players, 131072, QueryTriggerInteraction.Collide);

            foreach (var player in players)
            {
                if (!player.IsNpc && !player.isMounted && player.IsConnected)
                {
                    var vector = spawnpoint;
                    vector += Vector3Ex.Direction2D(player.transform.position, spawnpoint) * radius;
                    vector += Vector3.up * 0.1f;
                    player.MovePosition(vector);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", vector);
                }
            }
            Facepunch.Pool.FreeList(ref players);
        }
        
	    BaseVehicle GetVehicleOccupying(Vector3 spawnpoint)
	    {
		    BaseVehicle result = null;
		    var vehicles = Facepunch.Pool.GetList<BaseVehicle>();
		    Vis.Entities(spawnpoint, 6f, vehicles, triggerInteraction: QueryTriggerInteraction.Ignore);
		    if (vehicles.Count > 0)
		    	result = vehicles[0];

		    Facepunch.Pool.FreeList(ref vehicles);
		    return result;
        }

        bool IsPadOccupied(Vector3 spawnpoint) => GetVehicleOccupying(spawnpoint) != null;
        #endregion

        #region UI
        public void ShowUI(BasePlayer player, Dialog dialog) 
        {
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = config.UI_Name,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = TRANSPARENT
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "136 -122",
                        OffsetMax = "436 103"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_vendor_name",
                Parent = config.UI_Name,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = DARK_GRAY,
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -31",
                        OffsetMax = "111 -10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_vendor_name_text",
                Parent = $"{config.UI_Name}_vendor_name",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = dialog.NPC_Name,
                        Color = BOLDTEXTCOLOR,
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_textbox",
                Parent = config.UI_Name,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = DARK_GRAY,
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -116",
                        OffsetMax = "290 -35"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_textbox_text",
                Parent = config.UI_Name,
                Components =
                {
                    new CuiTextComponent
                    {
                        Font = "robotocondensed-regular.ttf",
                        Text = dialog.Text,
                        FontSize = 11,
                        Color = REGULARTEXTCOLOR,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "17 -110",
                        OffsetMax = "288 -42"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_close",
                Parent = config.UI_Name,
                Components = {
                    new CuiButtonComponent
                    {
                        Command = "carvendor.end",
                        Color = RED,
                    },
                    new CuiOutlineComponent
                    {
                        Color = BLACK,
                        Distance = "2.5 -2.5"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "-18 -18",
                        OffsetMax = "-2.5 -2.5",
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_close_text",
                Parent = $"{config.UI_Name}_close",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "X",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                        OffsetMin = "-1 0",
                        OffsetMax = "0 0"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Name = $"{config.UI_Name}_dialogcontainer",
                Parent = config.UI_Name,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = TRANSPARENT
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -215",
                        OffsetMax = "291 -120"
                    }
                }
            });

            for (int i = 0; i < dialog.Options.Length; i++)
            {
                container.Add(new CuiElement
                {
                    Name = $"{config.UI_Name}_dialogcontainer_option_{i}",
                    Parent = $"{config.UI_Name}_dialogcontainer",
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = BLACK,
                            Material = "assets/content/ui/uibackgroundblur.mat",
                            Command = dialog.Options[i].Command
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 {-25 * (i + 1) + 5}",
                            OffsetMax = $"281 {-25 * i}"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = $"{config.UI_Name}_dialogcontainer_option_{i}_label",
                    Parent = $"{config.UI_Name}_dialogcontainer_option_{i}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = dialog.Options[i].Text,
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            Color = REGULARTEXTCOLOR,
                            FontSize = 11
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "28 -21",
                            OffsetMax = "282 0"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = $"{config.UI_Name}_dialogcontainer_option_{i}_index",
                    Parent = $"{config.UI_Name}_dialogcontainer_option_{i}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = GREEN,
                            Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "5 -17",
                            OffsetMax = "20 -2"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = $"{config.UI_Name}_dialogcontainer_option_{i}_index_label",
                    Parent = $"{config.UI_Name}_dialogcontainer_option_{i}_index",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = (i + dialog.Counter_Offset).ToString(),
                            Color = BOLDTEXTCOLOR,
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });
            }
            CuiHelper.DestroyUi(player, config.UI_Name);
            CuiHelper.AddUi(player, container);;
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("carvendor.end")]
        void EndDialogCCmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            EndDialog(player);
        }

        [ConsoleCommand("carvendor.dialog")]
        void CarDialogCCmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var args = arg.Args;

            if(player == null) return;
            if (!LastTalkedTo.ContainsKey(player.userID)) return;
            if (args.Length < 1) return;
            if (args[0] == "intro") return;

            if(args.Length == 2)
            {
                if (!PlayerDialogChoices.ContainsKey(player.userID))
                    PlayerDialogChoices.Add(player.userID, new DialogChoices());

                if (args[0] == "choose_components" && (args.Length == 2 || args[1].IsNumeric()))
                    PlayerDialogChoices[player.userID].Chassis = Convert.ToInt32(args[1]);
                else if (args[0] == "confirmation" && (args.Length == 2 || args[1].IsNumeric()))
                    PlayerDialogChoices[player.userID].Components_Tier = Convert.ToInt32(args[1]);
            }

            if(args[0] == "confirmation")
            {
                if (!PlayerDialogChoices.ContainsKey(player.userID))
                {
                    EndDialog(player);
                    return;
                }

                var price = PlayerDialogChoices[player.userID].CalculatePrice();
                var able_to_pay = player.inventory.GetAmount(priceDef.itemid) >= price;

                var dialog = new Dialog(config.NPC_Name, $"Sure, that'll be {price} {priceDef.displayName.english.ToLower()} please", 
                able_to_pay ?
                new DialogOption[]
                {
                    new DialogOption($"[PAY {price} {priceDef.displayName.english.ToUpper()}]", "carvendor.dialog purchase"),
                    new DialogOption("Whoa, that's way too much man, forget it")
                } 
                :
                new DialogOption[]
                {
                    new DialogOption("Whoa, that's way too much man, forget it")
                });
                ShowUI(player, dialog);
                return;
            }
            if(args[0] == "purchase")
            {
                if (!PlayerDialogChoices.ContainsKey(player.userID))
                {
                    EndDialog(player);
                    return;
                }

                var dialogChoices = PlayerDialogChoices[player.userID];

                var price = dialogChoices.CalculatePrice();
                var able_to_pay = player.inventory.GetAmount(priceDef.itemid) >= price;
                
                if(!able_to_pay)
                {
                    EndDialog(player);
                    return;
                }

                player.inventory.Take(null, priceDef.itemid, price);

                var pos_data = NPCVehicleSpawnpoints[LastTalkedTo[player.userID]];
                var car = GameManager.server.CreateEntity(dialogChoices.GetPrefab(), pos_data.Position, Quaternion.Euler(pos_data.Rotation)) as ModularCar;
                
                if(config.Nudge_Players)
                    NudgePlayersInRadius(pos_data.Position, config.Nudge_Radius);

                car.Spawn();
    
                ShowUI(player, Dialogs["purchase_complete"]);

                AddModules(car, config.Modules[dialogChoices.Chassis]);

                if (config.Starting_Fuel > 0) 
                {
                    var fuelSystem = car.GetFuelSystem();
                    fuelSystem.AddStartingFuel(config.Starting_Fuel);
                }

                if (dialogChoices.Components_Tier > 0)
                {
                    NextFrame(() =>
                    {
                        AddComponents(car, dialogChoices.Components_Tier);
                        car.SetupOwner(player, pos_data.Position, config.Safe_Radius);
                        car.OwnerID = player.userID;
                    });
                }
                return;
            }

            if (!Dialogs.ContainsKey(args[0])) return;
            
            ShowUI(player, Dialogs[args[0]]);
        }
        #endregion

        #region Chat Commands
        [ChatCommand("carvendor")]
        void CarVendorCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.Spawn_Permission_Name))
            {
                PrintToChat(player, "You don't have permission to run this command!");
                return;
            }
            
            if (args.Length == 0)
            {
                PrintToChat(player, "Usage: /carvendor npc_spawn/set_car_spawnpoint/reset");
                return;
            }

            switch (args[0])
            {
                case "npc_spawn":
                    if(!PlayerSpawnpoints.ContainsKey(player.userID))
                    {
                        PrintToChat(player, "You need to set a car spawnpoint.\nRun /carvendor set_car_spawnpoint");
                        return;
                    }

                    var vendor = GameManager.server.CreateEntity(SHOPKEEPER_PREFAB, player.transform.position, player.transform.rotation) as NPCTalking;
                    vendor.Spawn();
                    vendor.enableSaving = false;
                    vendor.SetFlag(CUSTOM_NPC_FLAG, true, true);

                    NPCVehicleSpawnpoints.Add(vendor, PlayerSpawnpoints[player.userID]);

                    var alreadySpawned = false;

                    foreach (var monument in TerrainMeta.Path.Monuments)
                    {
                        if (Vector3.Distance(player.transform.position, monument.transform.position) < monument.Bounds.size.x)
                        {
                            var playerPosition = new PositionData(monument.transform.InverseTransformPoint(player.transform.position), player.viewAngles - monument.transform.rotation.eulerAngles);
;
                            var spawnpoint = new PositionData(monument.transform.InverseTransformPoint(PlayerSpawnpoints[player.userID].Position), PlayerSpawnpoints[player.userID].Rotation - monument.transform.rotation.eulerAngles);
                            ManuallyPlacedNPCs.Add($"MONUMENT:{monument.name}:{Guid.NewGuid()}", new KeyValuePair<PositionData, PositionData>(playerPosition, spawnpoint));
                            PrintToChat(player, $"You are on a monument! Saving positions relative to that monument");
                            alreadySpawned = true;
                            break;
                        }
                    }

                    if (!alreadySpawned)
                        ManuallyPlacedNPCs.Add(Guid.NewGuid().ToString(), new KeyValuePair<PositionData, PositionData>(new PositionData(player.transform.position, player.transform.rotation), PlayerSpawnpoints[player.userID]));

                    PlayerSpawnpoints.Remove(player.userID);

                    Interface.Oxide.DataFileSystem.WriteObject("CarVendor_ManuallyPlacedNPCs", ManuallyPlacedNPCs);

                    PrintToChat(player, "Car vendor spawned");
                    break;
                case "set_car_spawnpoint":
                    var data = new PositionData(player.transform.position, player.eyes.GetLookRotation().eulerAngles);
                    if (PlayerSpawnpoints.ContainsKey(player.userID))
                        PlayerSpawnpoints[player.userID] = data;
                    else
                        PlayerSpawnpoints.Add(player.userID, data);
                    PrintToChat(player, "Car spawnpoint set.\nNow run /carvendor npc_spawn to spawn the NPC");
                    break;
                case "reset":
                    foreach(var npc_pos in ManuallyPlacedNPCs)
                    {
                        var npcs = Facepunch.Pool.GetList<NPCTalking>();
                        Vis.Entities(npc_pos.Value.Key.Position, 0.3f, npcs);

                        foreach(var npc in npcs)
                        {
                            if (NPCVehicleSpawnpoints.ContainsKey(npc))
                                NPCVehicleSpawnpoints.Remove(npc);
                            if (!npc.IsDestroyed)
                                npc.Kill();
                        }
                        Facepunch.Pool.FreeList(ref npcs);
                    }
                    ManuallyPlacedNPCs = new Dictionary<string, KeyValuePair<PositionData, PositionData>>();
                    Interface.Oxide.DataFileSystem.WriteObject("CarVendor_ManuallyPlacedNPCs", ManuallyPlacedNPCs);
                    PrintToChat(player, "All car vendors are removed!");
                    break;
                default:
                    PrintToChat(player, "Usage: /carvendor npc_spawn/set_car_spawnpoint/reset");
                    break;
            }
        }
        #endregion
    }
}