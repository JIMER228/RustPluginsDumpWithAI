// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Linq; 
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using HarmonyLib;

namespace Oxide.Plugins
{
    [Info("NPCKits", "Steenamaroo", "1.2.6", ResourceId = 29)]  
    [Description("Give custom Kits to all default Rust npc types.")] 

    //  DisplayNames Fix
    public class NPCKits : RustPlugin 
    { 
        [PluginReference] Plugin Kits;

        public static NPCKits nk; 
        bool loaded;
        public int Increment = 10;
        public enum FStein { None, Light, Medium, Heavy }
        public System.Random random = new System.Random();
        Dictionary<ulong, string> Copy = new Dictionary<ulong, string>();
        public Dictionary<ulong, Info> BotInfo = new Dictionary<ulong, Info>();
        public Dictionary<ulong, int> DeadNPCPlayerIds = new Dictionary<ulong, int>();
        public Dictionary<ulong, Settings> LiveNpcs = new Dictionary<ulong, Settings>();

        public class Info 
        {
            public NPCPlayer npc; 
            public string profilename;
            public List<InvContents>[] inventory = { new List<InvContents>(), new List<InvContents>(), new List<InvContents>() };   
        }

        public class InvContents 
        {
            public int ID;
            public int amount;
            public ulong skinID;  
        }

        #region Hooks
        void Init() => permission.RegisterPermission(permAllowed, this);

        void Unload()
        {
            harmony.UnpatchAll("NPCKits");
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyMenu(player, true);
            if (configData != null && configData.SaveUIOnReload) 
                SaveConfig(configData); 
        }

        List<NPCPlayer> AllNPCs = new List<NPCPlayer>();
        void OnServerInitialized() 
        {
            nk = this;
            harmony.UnpatchAll("NPCKits");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            
            if (!LoadConfigVariables()) 
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            configData.CorpseTypes = configData.CorpseTypes.Where(x => new ConfigData().CorpseTypes.ContainsKey(x.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            if (Kits == null)
                PrintWarning("Kits plugin is not installed.");
            CheckKits();
            loaded = true;
            AllNPCs = BaseNetworkable.serverEntities.OfType<NPCPlayer>().ToList();
            foreach (var npc in AllNPCs)
                AddToBotList(npc, false);
        }

        void Reload(string profile)
        {
            foreach (var inv in BotInfo.Where(x => x.Value.npc != null && x.Value.profilename == profile).ToList())
                AddToBotList(inv.Value.npc, true);
        }
        #endregion

        #region BotHandling
        void OnEntitySpawned(BaseEntity entity)
        {
            if (!loaded || entity == null) return;

            var npc = entity as NPCPlayer;
            if (npc != null)
                AllNPCs.Add(npc);

            timer.Once(0.3f, () =>
            {
                if (npc != null && !npc.IsDestroyed) 
                    AddToBotList(npc, false); 
            });

            var corpse = entity as LootableCorpse;
            if (corpse != null && LiveNpcs.ContainsKey(corpse.playerSteamID))
            {
                var pos = corpse.transform.position;
                Info botInv = new Info();
                ulong id = corpse.playerSteamID;
                Settings record;
                record = LiveNpcs[id]; 
                timer.Once(0.3f, () =>
                {
                    if (corpse == null || corpse.containers == null || corpse.containers.Length == 0 || !BotInfo.ContainsKey(id))
                        return;
                    botInv = BotInfo[id]; 

                    var ARL = record.Default_Rust_Loot_Percent >= random.Next(1, 101);
                    var WM = record.Wipe_Main_Inventory_Percent >= random.Next(1, 101);
                    var WC = record.Wipe_Clothing_Percent >= random.Next(1, 101);
                    var WB = record.Wipe_Belt_Percent >= random.Next(1, 101);
                    if (!ARL)
                        corpse.containers[0].Clear();

                    for (int i = 0; i < botInv.inventory.Length; i++)
                    {
                        if (i == 0 && WM || i == 1 || i == 2 && WB)
                            continue;

                        foreach (var item in botInv.inventory[i])
                        {
                            var giveItem = ItemManager.CreateByItemID(item.ID, item.amount, item.skinID);
                            giveItem.MoveToContainer(corpse.containers[i], -1, true);
                        }
                    }
                    if (WC)
                        corpse.containers[1].Clear();

                    corpse._playerName = record.DisplayName;
                    corpse.lootPanelName = record.DisplayName;

                    corpse.containers = new ItemContainer[] { corpse.containers[0], new ItemContainer(), new ItemContainer(), corpse.containers[1], corpse.containers[2] };

                    foreach (var item in corpse.containers[3].itemList.ToList())
                    {
                        if (item.info.shortname.Contains("frankensteins."))
                        {
                            corpse.containers[0].capacity += 1;
                            item.MoveToContainer(corpse.containers[0], corpse.containers[0].capacity - 1);
                        }
                    }

                    DeadNPCPlayerIds.Add(id, record.Backpack_Minutes * 60); 

                    LiveNpcs.Remove(corpse.playerSteamID);
                    BotInfo.Remove(id);

                    timer.Once(record.Corpse_Minutes * 60, () => { if (corpse != null && !corpse.IsDestroyed) corpse?.Kill(); });
                    timer.Once(2, () => { corpse?.ResetRemovalTime(record.Corpse_Minutes * 60); });
                });
            }
        }

        void OnEntitySpawned(DroppedItemContainer container)
        {
            NextTick(() =>
            {
                if (!loaded || container?.playerSteamID == null || container.IsDestroyed || container.playerSteamID == 0)
                    return;

                if (DeadNPCPlayerIds.ContainsKey(container.playerSteamID))
                {
                    container.CancelInvoke(container.RemoveMe);
                    timer.Once(DeadNPCPlayerIds[container.playerSteamID], () =>
                    {
                        if (container != null && !container.IsDestroyed)
                            container?.Kill();
                    });
                    DeadNPCPlayerIds.Remove(container.playerSteamID);
                }
            });
        }

        float GetPercent(int min, int max, float weapMax)
        {
            min = Mathf.Max(1, min);
            max = Mathf.Max(1, max);
            if (min >= max)
                return weapMax / 100f * max;
            return weapMax / 100f * random.Next(min, max);
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info) => OnEntityKill(player, info); 
        void OnEntityKill(BaseNetworkable entity, HitInfo info)
        {
            NPCPlayer npc = entity as NPCPlayer;
            if (npc != null && AllNPCs.Contains(npc)) 
            {
                AllNPCs.Remove(npc);
                if (npc?.userID != null && LiveNpcs.ContainsKey(npc.userID))
                {
                    if (info?.InitiatorPlayer != null)
                    {
                        Item activeItem = npc.GetActiveItem();
                        var record = LiveNpcs[npc.userID];
                        int chance = random.Next(1, 101);

                        if (record.Weapon_Drop_Percent >= chance && activeItem != null)
                        {
                            activeItem.condition = GetPercent(record.Min_Weapon_Drop_Condition_Percent, record.Max_Weapon_Drop_Condition_Percent, activeItem.info.condition.max);

                            var held = activeItem.GetHeldEntity();
                            if (held != null)
                            {
                                BaseProjectile gun = held as BaseProjectile;
                                if (gun != null)
                                {
                                    bool wipeAmmo = record.Dropped_Weapon_Has_Ammo_Percent_Chance < random.Next(1, 101);
                                    gun.primaryMagazine.contents = wipeAmmo ? 0 : gun.primaryMagazine.capacity;
                                    gun.SendNetworkUpdateImmediate();
                                }
                                
                                if (gun != null || held is BaseMelee)
                                {
                                    activeItem.Drop(npc.eyes.position, new Vector3(), new Quaternion());
                                    npc.svActiveItemID = new ItemId();
                                    npc.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                }
                            }
                        }
                    }

                    ItemContainer[] source = { npc.inventory.containerMain, npc.inventory.containerWear, npc.inventory.containerBelt };

                    Info botInv;
                    if (!BotInfo.TryGetValue(npc.userID, out botInv))
                        return;
                    for (int i = 0; i < source.Length; i++)
                    {
                        foreach (var item in source[i].itemList)
                        {
                            if (item.info.shortname.Contains("frankensteins."))
                                continue;
                            botInv.inventory[i].Add(new InvContents
                            {
                                ID = item.info.itemid,
                                amount = item.amount,
                                skinID = item.skin,
                            });
                        }
                    }
                }
            }
        }

        void AddToBotList(NPCPlayer player, bool reload)  
        {
            if (player == null || player.Categorize() == "Zombie")
                return;
            if (Interface.CallHook("OnNpcKits", player.userID.Get()) != null || Interface.CallHook("OnNpcKits", player) != null)
                return;

            foreach (var entry in names)
                if (player.ShortPrefabName == entry.Key)
                {
                    ProcessNpc(player, entry.Value, reload);
                    return;
                }

            foreach (var entry in names) if (player.ShortPrefabName.Contains(entry.Key))
                if (player.ShortPrefabName.Contains(entry.Key))
                {
                    ProcessNpc(player, entry.Value, reload);
                    return;
                }

            var instance = player?.GetComponent<SpawnPointInstance>()?.parentSpawnPoint;
            if (instance != null)
            {
                var name = instance?.GetComponentInParent<PrefabParameters>()?.ToString(); 
                if (name != null) 
                {
                    foreach (var n in names)
                        if (name.Contains(n.Key))
                        {
                            ProcessNpc(player, n.Value, reload);
                            return;
                        }
                }
            }

            //Puts($"Prefab name {player.PrefabName}");
            //foreach (var p in BasePlayer.activePlayerList)
            //        p.SendConsoleCommand("ddraw.line", 30f, Color.blue, player.transform.position, player.transform.position + new Vector3(0, 100, 0));
        }

        Dictionary<string, string> names = new Dictionary<string, string>()
        {
            {"oilrig", "OilRig"},
            {"excavator", "Excavator"},
            {"peacekeeper", "CompoundScientist"},
            {"bandit_guard", "BanditTown"},
            {"_ch47_gunner", "MountedScientist"},
            {"junkpile", "JunkPileScientist"},
            {"scarecrow_dungeon", "DungeonScarecrow" },
            {"scarecrow", "ScareCrow"},
            {"military_tunnel", "MilitaryTunnelScientist" },
            {"scientist_full", "MilitaryTunnelScientist"},
            {"scientist_turret", "CargoShip"},
            {"scientistnpc_cargo", "CargoShip"},
            {"scientist_astar", "CargoShip"},
            {"scientistnpc_bradley", "APCScientist" },
            {"scientistnpc_bradley_heavy", "APCScientistHeavy" }, 
            {"scientistnpc_heavy", "HeavyScientist"},
            {"tunneldweller", "TunnelDweller"},
            {"underwaterdweller" , "UnderwaterDweller"},
            {"trainyard" , "Trainyard"},
            {"airfield" , "Airfield"}, 
            {"scientistnpc_roamtethered", "DesertScientist" },
            {"arctic_research_base", "ArcticResearchBase" },
            {"launch_site", "LaunchSite" },
            {"nuclear_missile_silo", "NuclearMissileSilo" },
            {"gingerbread", "Gingerbread" },
            {"missionprovider_fishing", "MissionProviderOutpost" },
            {"missionprovider_outpost", "MissionProviderFishing" },
            {"missionprovider_stables", "MissionProviderStables" },
            {"missionprovider_bandit", "MissionProviderBandit" },
            {"boat_shopkeeper", "BoatShopkeeper" },
            {"bandit_shopkeeper", "BanditShopkeeper" },
        };

        object GetKitInfo(string name) => Kits?.Call("GetKitInfo", name, true);
        object GiveKit(NPCPlayer npc, string name) => Kits?.Call($"GiveKit", npc, name, true);

        bool HasFStein(Settings r) => r.FrankenStein_Head != FStein.None || r.FrankenStein_Torso != FStein.None || r.FrankenStein_Legs != FStein.None;

        void GiveFStein(NPCPlayer npc, Settings r)
        {
            if (r.FrankenStein_Head != FStein.None)
            {
                npc.inventory.containerWear.capacity += 1;
                Item newItem = ItemManager.CreateByName($"frankensteins.monster.0{Convert.ToInt16(r.FrankenStein_Head)}.head", 1);
                if (newItem != null)
                {
                    SetOccupation(newItem, false, 0, 0);
                    if (!newItem.MoveToContainer(npc.inventory.containerWear))
                        newItem.Remove(0f);
                }
            }
            if (r.FrankenStein_Torso != FStein.None)
            {
                Item newItem = ItemManager.CreateByName($"frankensteins.monster.0{Convert.ToInt16(r.FrankenStein_Torso)}.torso", 1);
                if (newItem != null)
                {
                    SetOccupation(newItem, true, 504, 0);
                    if (!newItem.MoveToContainer(npc.inventory.containerWear))
                        newItem.Remove(0f);
                }
            }
            if (r.FrankenStein_Legs != FStein.None)
            {
                Item newItem = ItemManager.CreateByName($"frankensteins.monster.0{Convert.ToInt16(r.FrankenStein_Legs)}.legs", 1);
                if (newItem != null)
                {
                    SetOccupation(newItem, true, 129024, 0);
                    if (!newItem.MoveToContainer(npc.inventory.containerWear))
                        newItem.Remove(0f);
                }
            }
        }

        void SetOccupation(Item item, bool set, int under, int over)
        {
            var comp = item.info.GetComponent<ItemModWearable>();
            if (comp != null)
            {
                comp.protectionProperties = null;
                if (set)
                {
                    comp.targetWearable.occupationUnder = (Wearable.OccupationSlots)under;
                    comp.targetWearable.occupationOver = (Wearable.OccupationSlots)over;
                }
            }
        }
        
        public Dictionary<ulong, string> DisplayNames = new Dictionary<ulong, string>();
        
        void ProcessNpc(NPCPlayer npc, string NPCType, bool reload) 
        {
            var record = configData.CorpseTypes[NPCType]; 
            if (record == null || !record.enabled)
                return;

            if (!reload && LiveNpcs.ContainsKey(npc.userID))  
                return;

            LiveNpcs[npc.userID] = record;

            if (record.Health > 0)
            {
                npc._maxHealth = record.Health;
                npc.startHealth = npc._maxHealth;
                npc.InitializeHealth(record.Health, record.Health); 
            }

            if (record.DisplayName != string.Empty)
                DisplayNames[npc.userID] = record.DisplayName;
            else
                DisplayNames[npc.userID] = NPCType;

            npc.displayName = DisplayNames[npc.userID]; 
            
            if (reload)
                npc.inventory.Strip();

            if (record.Kits != null && record.Kits.Count != 0)
            {
                int kitRnd = random.Next(record.Kits.Count); 
                if (record.Kits[kitRnd] != null)
                {
                    object checkKit = GetKitInfo(record.Kits[kitRnd]);
                    if (checkKit == null)
                    {
                        if (Kits != null)
                            PrintWarning($"Kit {record.Kits[kitRnd]} does not exist.");
                    }
                    else
                    {
                        if (record.Wipe_Default_Clothing)
                        {
                            npc.inventory.containerWear.Clear();
                            ItemManager.DoRemoves();
                        }

                        if (record.Wipe_Default_Weapons)
                        {
                            npc.inventory.containerBelt.Clear();
                            ItemManager.DoRemoves();
                        }

                        if (HasFStein(record))
                            npc.inventory.containerWear.Clear();

                        GiveKit(npc, record.Kits[kitRnd]);
                        GiveFStein(npc, record);
                        npc.EquipWeapon();
                    }
                }
            }
            Info botInv = new Info() { profilename = NPCType, npc = npc };
            BotInfo[npc.userID] = botInv;
        }
        
        private Harmony harmony = new Harmony("NPCKits"); 
        public class Patches
        {
            [HarmonyPatch(typeof(ScientistNPC))]
            [HarmonyPatch("displayName", MethodType.Getter)]
            public static class ScientistNPCDisplayNames
            {
                [HarmonyPrefix]
                public static bool ScientistNPCFix(ref string __result, ScientistNPC __instance)
                {
                    if (__instance != null && nk != null && nk.DisplayNames.ContainsKey(__instance.userID))
                    {
                        __result = nk.DisplayNames[__instance.userID];
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(TunnelDweller))]
            [HarmonyPatch("displayName", MethodType.Getter)]
            public static class TunnelDwellerDisplayNames
            {
                [HarmonyPrefix]
                public static bool TunnelDwellerNPCFix(ref string __result, TunnelDweller __instance)
                {
                    if (__instance != null && nk != null && nk.DisplayNames.ContainsKey(__instance.userID))
                    {
                        __result = nk.DisplayNames[__instance.userID];
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(UnderwaterDweller))]
            [HarmonyPatch("displayName", MethodType.Getter)]
            public static class UnderwaterDwellerDisplayNames
            {
                [HarmonyPrefix]
                public static bool UnderwaterDwellerNPCFix(ref string __result, UnderwaterDweller __instance)
                {
                    if (__instance != null && nk != null && nk.DisplayNames.ContainsKey(__instance.userID))
                    {
                        __result = nk.DisplayNames[__instance.userID];
                        return false;
                    }
                    return true;
                }
            }
        }
        #endregion

        #region UI
        const string Font = "robotocondensed-regular.ttf";
        const string permAllowed = "NPCKits.allowed";
        public List<string> ValidKits = new List<string>();
        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);
        void OnPlayerDisconnected(BasePlayer player) => DestroyMenu(player, true);

        [ChatCommand("npckits")] 
        void npckits(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player.UserIDString, permAllowed) && !player.IsAdmin)
                return;

            if (args == null || args.Length == 0)
            {
                CheckKits();
                NPCKitsBGUI(player);
                NPCKitsMainUI(player, "");
                return;
            }
        }

        void CheckKits()
        {
            ValidKits.Clear();
            Kits?.Call("GetKitNames", new object[] { ValidKits });

            var names = Kits?.Call("GetAllKits");
            if (names != null)
                ValidKits.AddRange((string[])names);

            ValidKits = ValidKits.Distinct().ToList();
            ValidKits = ValidKits.OrderBy(x => x.ToString()).ToList();
        }

        void DestroyMenu(BasePlayer player, bool all)
        {
            if (all)
            {
                if (Copy.ContainsKey(player.userID))
                    Copy.Remove(player.userID);
                CuiHelper.DestroyUi(player, "NPCKitsBGUI");
            }
            CuiHelper.DestroyUi(player, "NPCKitsKitsUI");
            CuiHelper.DestroyUi(player, "NPCKitsMainUI");
            CuiHelper.DestroyUi(player, "NPCKitsDocs"); 
        }

        void NPCKitsBGUI(BasePlayer player)
        {
            if (player == null || configData == null)
                return;
            DestroyMenu(player, true);
            string guiString = string.Format("0.1 0.1 0.1 0.94");
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = guiString }, RectTransform = { AnchorMin = "0.1 0.05", AnchorMax = "0.9 0.95" }, CursorEnabled = true }, "Overlay", "NPCKitsBGUI");
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 1" }, Text = { Text = string.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.999 0.05" }, Text = { Text = string.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Command = "CloseNPCKits", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = "0.955 0.96", AnchorMax = "0.99 0.99" }, Text = { Text = "X", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = "NPCKits", FontSize = 20, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.95", AnchorMax = "0.8 1" } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        void NPCKitsMainUI(BasePlayer player, string profile) 
        {
            DestroyMenu(player, false);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.9" }, CursorEnabled = true }, "Overlay", "NPCKitsMainUI"); 
            elements.Add(new CuiElement { Parent = "NPCKitsMainUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            float top = 0.875f, bottom = 0.9f;
            bool odd = true;

            if (string.IsNullOrEmpty(profile))
            {
                elements.Add(new CuiButton { Button = { Command = "", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 0.99" }, Text = { Text = "NPC Types", FontSize = 18, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                var data = configData.CorpseTypes;
                foreach (var entry in data)
                {
                    if (odd)
                        elements.Add(new CuiPanel { Image = { Color = $"0 0 0 0.8" }, RectTransform = { AnchorMin = $"0 {top}", AnchorMax = $"0.999 {bottom}" }, CursorEnabled = true }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"NPCKitsUI {RS(entry.Key)} 0 0", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 {top}", AnchorMax = $"1 {bottom}" }, Text = { Text = $"{entry.Key}", Color = entry.Value.enabled ? "0 1 0 1" : "1 1 1 1", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    top -= 0.025f;
                    bottom -= 0.025f;
                    odd = !odd;
                }
            }
            else
            {
                elements.Add(new CuiButton { Button = { Command = "", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 0.99" }, Text = { Text = profile, FontSize = 18, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                foreach (var setting in typeof(Settings).GetFields()) 
                {
                    if (setting.Name == "Kits" || setting.Name == "DisplayName")
                        continue;

                    top -= 0.025f; bottom -= 0.025f;

                    if (odd)
                        elements.Add(new CuiPanel { Image = { Color = $"0 0 0 0.8" }, RectTransform = { AnchorMin = $"0 {top}", AnchorMax = $"0.999 {bottom}" }, CursorEnabled = true }, mainName);

                    if (setting.FieldType == typeof(int))
                    {
                        elements.Add(new CuiLabel { Text = { Text = $"{TidyName(setting.Name)}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.55 {bottom}" } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsDocs {RS(setting.Name)}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.55 {bottom}" }, Text = { Text = "", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeNum {RS(profile)} {RS(setting.Name)} false", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.55 {top + 0.003}", AnchorMax = $"0.57 {bottom - 0.003}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiLabel { Text = { Text = $"{setting.GetValue(configData.CorpseTypes[profile])}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.575 {top + 0.003}", AnchorMax = $"0.625 {bottom - 0.003}" } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeNum {RS(profile)} {RS(setting.Name)} true", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.63 {top + 0.003}", AnchorMax = $"0.65 {bottom - 0.003}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                    else if (setting.FieldType == typeof(bool))
                    {
                        elements.Add(new CuiLabel { Text = { Text = $"{TidyName(setting.Name)}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.45 {bottom}" } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsDocs {RS(setting.Name)}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.45 {bottom}" }, Text = { Text = "", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeBool {RS(profile)} {RS(setting.Name)}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.55 {top + 0.003}", AnchorMax = $"0.65 {bottom - 0.003}" }, Text = { Text = $"{setting.GetValue(configData.CorpseTypes[profile])}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                    else if (setting.FieldType == typeof(FStein))
                    {
                        elements.Add(new CuiLabel { Text = { Text = $"{TidyName(setting.Name)}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.55 {bottom}" } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsDocs {RS(setting.Name)}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.31 {top}", AnchorMax = $"0.55 {bottom}" }, Text = { Text = "", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeEnum {RS(profile)} {RS(setting.Name)} {Enum.GetNames(setting.FieldType).Length} false", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.55 {top + 0.003}", AnchorMax = $"0.57 {bottom - 0.003}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiLabel { Text = { Text = $"{setting.GetValue(configData.CorpseTypes[profile])}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.575 {top + 0.003}", AnchorMax = $"0.625 {bottom - 0.003}" } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeEnum {RS(profile)} {RS(setting.Name)} {Enum.GetNames(setting.FieldType).Length} true", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.63 {top + 0.003}", AnchorMax = $"0.65 {bottom - 0.003}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                    odd = !odd;
                }

                elements.Add(new CuiLabel { Text = { Text = $"Set Adjustment Increment", FontSize = 13, Font = Font, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.31 0.175", AnchorMax = $"0.45 0.2" } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsSetIncrement {RS(profile)} 1", Color = Increment == 1 ? configData.UI.ButtonColour2 : configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.55 0.175", AnchorMax = $"0.59 0.2" }, Text = { Text = "1", FontSize = 13, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsSetIncrement {RS(profile)} 5", Color = Increment == 5 ? configData.UI.ButtonColour2 : configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.6 0.175", AnchorMax = $"0.64 0.2" }, Text = { Text = "5", FontSize = 13, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsSetIncrement {RS(profile)} 10", Color = Increment == 10 ? configData.UI.ButtonColour2 : configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.65 0.175", AnchorMax = $"0.69 0.2" }, Text = { Text = "10", FontSize = 13, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                elements.Add(new CuiButton { Button = { Command = $"NPCKitsCopy {RS(profile)}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.88 0.14", AnchorMax = $"0.99 0.16" }, Text = { Text = "Copy profile", FontSize = 12, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                if (Copy.ContainsKey(player.userID))
                    elements.Add(new CuiButton { Button = { Command = $"NPCKitsPaste {RS(profile)}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.88 0.11", AnchorMax = $"0.99 0.13" }, Text = { Text = $"Paste", FontSize = 12, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                elements.Add(new CuiButton { Button = { Command = $"NPCKitsReloadProfile {RS(profile)}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.25 0.065", AnchorMax = $"0.35 0.095" }, Text = { Text = "Reload Profile", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsUIEditKits {RS(profile)} 0", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.45 0.065", AnchorMax = $"0.55 0.095" }, Text = { Text = "Edit Kits", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsUI", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.65 0.065", AnchorMax = $"0.75 0.095" }, Text = { Text = "Back", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            }
            CuiHelper.AddUi(player, elements);
        }

        string TidyName(string name) => name.Replace("_", " ").Replace("Percent", " %").Replace("Change", "");

        void NPCKitsKitsUI(BasePlayer player, string profile = "", int page = 0)
        {
            DestroyMenu(player, false);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.9" }, CursorEnabled = true }, "Overlay", "NPCKitsKitsUI");
            elements.Add(new CuiElement { Parent = "NPCKitsKitsUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });
            elements.Add(new CuiLabel { Text = { Text = $"Kits for {profile}", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0.9", AnchorMax = $"1 1" } }, mainName);

            float top = 0.925f, bottom = 0.95f;
            double left = 0;
            int num = 0;

            int from = page * 105;
            int to = ((page + 1) * 105) - 1;

            for (int i = from; i <= to; i++)
            {
                if (i >= ValidKits.Count)
                    break;

                if (i > from && (i - from) % 35 == 0)
                {
                    top = 0.925f;
                    bottom = 0.95f;
                    left += 0.33;
                }
                if (i - from > 104)
                    break;

                top -= 0.023f;
                bottom -= 0.023f;

                num = configData.CorpseTypes[profile].Kits.Where(x => x == ValidKits[i]).Count();
                elements.Add(new CuiLabel { Text = { Text = $"{ValidKits[i]}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"{left + 0.05} {top}", AnchorMax = $"{left + 0.15} {bottom}" } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeKit {RS(profile)} {i} {page} false", Color = num > 0 ? configData.UI.ButtonColour2 : configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"{left + 0.15} {top + 0.003}", AnchorMax = $"{left + 0.17} {bottom - 0.003}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiLabel { Text = { Text = $"{num}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"{left + 0.175} {top + 0.003}", AnchorMax = $"{left + 0.185} {bottom - 0.003}" } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsChangeKit {RS(profile)} {i} {page} true", Color = num > 0 ? configData.UI.ButtonColour2 : configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"{left + 0.19} {top + 0.003}", AnchorMax = $"{left + 0.21} {bottom - 0.003}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            }

            if (page > 0)
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsUIEditKits {RS(profile)} {page - 1}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.2 0.065", AnchorMax = $"0.3 0.095" }, Text = { Text = "<-", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            if (ValidKits.Count > to)
                elements.Add(new CuiButton { Button = { Command = $"NPCKitsUIEditKits {RS(profile)} {page + 1}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.7 0.065", AnchorMax = $"0.8 0.095" }, Text = { Text = "->", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

            elements.Add(new CuiButton { Button = { Command = $"NPCKitsCloseExtra NPCKitsKitsUI {RS(profile)}", Color = configData.UI.ButtonColour }, RectTransform = { AnchorMin = $"0.4 0.065", AnchorMax = $"0.6 0.095" }, Text = { Text = "Back", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("NPCKitsCopy")]
        private void NPCKitCopy(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), false);
            Copy[arg.Player().userID] = JsonConvert.SerializeObject(configData.CorpseTypes[RD(arg.Args[0])]);
            NPCKitsMainUI(arg.Player(), arg.Args[0]);
        }

        [ConsoleCommand("NPCKitsPaste")]
        private void NPCKitPaste(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), false);
            configData.CorpseTypes[RD(arg.Args[0])] = JsonConvert.DeserializeObject<Settings>(Copy[arg.Player().userID]);
            NPCKitsMainUI(arg.Player(), arg.Args[0]);
        }

        [ConsoleCommand("NPCKitsUI")]
        private void NPCKitsUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            DestroyMenu(player, false);
            NPCKitsMainUI(player, arg?.Args?.Length > 0 ? arg.Args[0] : "");
        }

        [ConsoleCommand("NPCKitsChangeBool")]
        private void NPCKitsChangeBool(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), false);

            var record = configData.CorpseTypes[RD(arg.Args[0])];
            var sub = record.GetType().GetField(RD(arg.Args[1]));
            var subobj = sub.GetValue(record);
            sub.SetValue(record, !(bool)subobj);
            NPCKitsMainUI(arg.Player(), arg.Args[0]);
        }

        [ConsoleCommand("NPCKitsChangeNum")] 
        private void NPCKitsChangeNum(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), false);
            bool up = Convert.ToBoolean(arg.Args[2]);
            var record = configData.CorpseTypes[RD(arg.Args[0])];
            var sub = record.GetType().GetField(RD(arg.Args[1]));
            var subobj = (int)sub.GetValue(record);
            
            if (arg.Args[1] == "Health")
            {
                int increment = ScaleIncrement(subobj, up);
                subobj = up ? subobj + increment : subobj - increment;
                if (subobj < 1)
                    subobj = 1;
            }
            else
                subobj = limitpercent(up ? subobj + Increment : subobj - Increment);

            sub.SetValue(record, subobj); 
            NPCKitsMainUI(arg.Player(), arg.Args[0]);
        }

        int limitpercent(int number) => Mathf.Max(Mathf.Min(number, 100), 0);

        int ScaleIncrement(int val, bool up)
        {
            if (up)
            {
                if (val >= 5000) return 1000;
                if (val >= 500) return 100;
                if (val >= 300) return 50; 
                return 25; 
            }
            else
            {
                if (val <= 300) return 25; 
                if (val <= 500) return 50; 
                if (val <= 5000) return 100;
                return 1000;
            }
        }

        [ConsoleCommand("NPCKitsChangeEnum")]
        private void NPCKitsChangeEnum(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), false);
            bool up = Convert.ToBoolean(arg.Args[3]);
            var record = configData.CorpseTypes[RD(arg.Args[0])];
            var sub = record.GetType().GetField(RD(arg.Args[1]));
            var subobj = (int)sub.GetValue(record);
            subobj = up ? subobj + 1 : subobj - 1;
            subobj = Mathf.Max(Mathf.Min(subobj, Convert.ToInt16(arg.Args[2]) - 1), 0);
            sub.SetValue(record, subobj);
            NPCKitsMainUI(arg.Player(), arg.Args[0]);
        }

        [ConsoleCommand("NPCKitsSetIncrement")]
        private void NPCKitsSetIncrement(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            Increment = Convert.ToInt16(arg.Args[1]);
            NPCKitsMainUI(arg.Player(), RD(arg.Args[0]));
        }

        [ConsoleCommand("NPCKitsReloadProfile")]
        private void NPCKitsReloadProfile(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || ValidKits.Count == 0)
                return;
            Reload(RD(arg.Args[0])); 
        }

        [ConsoleCommand("NPCKitsUIEditKits")]
        private void NPCKitsUIEditKits(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || ValidKits.Count == 0)
                return;
            NPCKitsKitsUI(arg.Player(), RD(arg.Args[0]), Convert.ToInt16(arg.Args[1])); 
        }

        [ConsoleCommand("NPCKitsChangeKit")]
        private void NPCKitsChangeKit(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            int num = Convert.ToInt16(arg.Args[1]);

            if (Convert.ToBoolean(arg.Args[3]) == true)
                configData.CorpseTypes[RD(arg.Args[0])].Kits.Add(ValidKits[num]);
            else
                configData.CorpseTypes[RD(arg.Args[0])].Kits.Remove(ValidKits[num]);
            NPCKitsKitsUI(arg.Player(), RD(arg.Args[0]), Convert.ToInt16(arg.Args[2]));
        }

        [ConsoleCommand("NPCKitsCloseExtra")]
        private void NPCKitsCloseExtra(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            CuiHelper.DestroyUi(arg.Player(), arg.Args[0]);
            NPCKitsMainUI(arg.Player(), RD(arg.Args[1]));
        }

        [ConsoleCommand("CloseNPCKits")]
        private void CloseNPCKits(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            DestroyMenu(arg.Player(), true);
            SaveConfig(configData); 
        }

        string RS(string input) => input.Replace(" ", "-");
        string RD(string input) => input.Replace("-", " ");

        void NPCKitsDocs(BasePlayer player, string docname, string doc)
        {
            if (player == null || configData == null)
                return;

            CuiHelper.DestroyUi(player, "NPCKitsDocs");

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.99" }, RectTransform = { AnchorMin = "0.1 0.05", AnchorMax = "0.9 0.95" }, CursorEnabled = true }, "Overlay", "NPCKitsDocs");
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 1" }, Text = { Text = string.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.999 0.05" }, Text = { Text = string.Empty } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = "NPCKits", FontSize = 20, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.95", AnchorMax = "0.8 1" } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = $"{docname.Replace("_", " ")}", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.75", AnchorMax = "0.8 0.8" } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = $"{doc}", FontSize = 14, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.05", AnchorMax = "0.8 0.95" } }, mainName);
            elements.Add(new CuiButton { Button = { Command = "NPCKitsCloseDocs", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 ", AnchorMax = $"1 1" }, Text = { Text = "", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("NPCKitsDocs")]
        private void NPCKitsDocs(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (Docs.ContainsKey(arg.Args[0]))
                NPCKitsDocs(arg.Player(), arg.Args[0], Docs[arg.Args[0]]);
        }

        [ConsoleCommand("NPCKitsCloseDocs")]
        private void NPCKitsCloseDocs(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) 
                return;
            CuiHelper.DestroyUi(arg.Player(), "NPCKitsDocs");
        }

        public Dictionary<string, string> Docs = new Dictionary<string, string>()
        {
            { "enabled", "Profile is enabled or disabled" },
            { "Health", "Spawn health for npcs from this profile." },
            { "Weapon_Drop_Percent", "The percentage chance that a killed npc from this profile will drop his weapon on the ground." },
            { "Min_Weapon_Drop_Condition_Percent", "Minimum possible condition of the dropped weapon, as a percentage." },
            { "Max_Weapon_Drop_Condition_Percent", "Maximum possible condition of the dropped weapon, as a percentage." },
            { "Dropped_Weapon_Has_Ammo_Percent_Chance", "Percentage chance that a dropped weapon is loaded with ammunition." },
            { "Wipe_Default_Clothing", "Removes vanilla clothing from npcs when they spawn, before giving out any specified kits." },
            { "Wipe_Default_Weapons", "Removes vanilla weapons from npcs when they spawn, before giving out any specified kits." },
            { "Wipe_Main_Inventory_Percent", "The percentage chance that the main inventory container of an npc is erased when the npc is killed." },
            { "Wipe_Clothing_Percent", "The percentage chance that the clothing inventory container of an npc is erased when the npc is killed." },
            { "Wipe_Belt_Percent", "The percentage chance that the belt inventory container of an npc is erased when the npc is killed." },
            { "Default_Rust_Loot_Percent", "The percentage chance of vanilla Rust loot spawning in a dead npc's corpse/backpack." },
            { "Corpse_Minutes", "The length of time, in minutes, that the corpse of a dead npc from this profile will remain." },
            { "Backpack_Minutes", "The length of time, in minutes, that an npc's lootable backpack will remain."},
            { "FrankenStein_Head", "Choose which FrankenStein head the npc has." },
            { "FrankenStein_Torso", "Choose which FrankenStein torso the npc has." },
            { "FrankenStein_Legs", "Choose which FrankenStein legs the npc has." },
        };

        #endregion

            #region Config 
        private ConfigData configData;

        class ConfigData
        {
            public bool SaveUIOnReload = true;
            public UI UI = new UI();

            public Dictionary<string, Settings> CorpseTypes = new Dictionary<string, Settings>
            {
                {"Airfield", new Settings()},
                {"ArcticResearchBase", new Settings()},
                {"NuclearMissileSilo", new Settings()},
                {"BanditShopkeeper", new Settings()},
                {"BanditTown", new Settings()},
                {"BoatShopkeeper", new Settings()},
                {"CargoShip", new Settings()},
                {"CompoundScientist", new Settings()},
                {"DesertScientist", new Settings()},
                {"DungeonScarecrow", new Settings()},
                {"Excavator", new Settings()},
                {"Gingerbread", new Settings() },
                {"HeavyScientist", new Settings()},
                {"JunkPileScientist", new Settings()},
                {"LaunchSite", new Settings()},
                {"APCScientist", new Settings() },
                {"APCScientistHeavy", new Settings() },
                {"MilitaryTunnelScientist", new Settings()},
                {"MissionProviderBandit", new Settings()},
                {"MissionProviderFishing", new Settings()},
                {"MissionProviderOutpost", new Settings()},
                {"MissionProviderStables", new Settings()},
                {"MountedScientist", new Settings()},
                {"OilRig", new Settings()},
                {"ScareCrow", new Settings()},
                {"Scientist", new Settings()},
                {"Trainyard", new Settings()},
                {"TunnelDweller", new Settings()},
                {"UnderwaterDweller", new Settings()},
            };
        }

        public class UI
        {
            public string ButtonColour = "0.7 0.32 0.17 1";
            public string ButtonColour2 = "0.7 0.5 0.17 1";
        }

        public class Settings
        {
            [JsonProperty(Order = 1)] 
            public bool enabled = false;
            [JsonProperty(Order = 2)]
            public List<string> Kits = new List<string>(); 
            [JsonProperty(Order = 3)]
            public int Health = 100;
            [JsonProperty(Order = 4)]
            public int Weapon_Drop_Percent = 0;
            [JsonProperty(Order = 5)]
            public int Min_Weapon_Drop_Condition_Percent = 100;
            [JsonProperty(Order = 6)]
            public int Max_Weapon_Drop_Condition_Percent = 100;
            [JsonProperty(Order = 7)]
            public int Dropped_Weapon_Has_Ammo_Percent_Chance = 100;
            [JsonProperty(Order = 8)]
            public bool Wipe_Default_Clothing = true;
            [JsonProperty(Order = 9)]
            public bool Wipe_Default_Weapons = true;
            [JsonProperty(Order = 10)]
            public int Wipe_Main_Inventory_Percent = 100;
            [JsonProperty(Order = 11)]
            public int Wipe_Clothing_Percent = 100;
            [JsonProperty(Order = 12)]
            public int Wipe_Belt_Percent = 100;
            [JsonProperty(Order = 13)]
            public int Default_Rust_Loot_Percent = 100; 
            [JsonProperty(Order = 14)]
            public string DisplayName = "";
            [JsonProperty(Order = 15)]
            public int Corpse_Minutes = 10;
            [JsonProperty(Order = 16)]
            public int Backpack_Minutes = 10;
            public FStein FrankenStein_Head = FStein.None;
            public FStein FrankenStein_Torso = FStein.None;
            public FStein FrankenStein_Legs = FStein.None;
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
        }

        void SaveConfig(ConfigData config) 
        {
            config.CorpseTypes = config.CorpseTypes.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            Config.WriteObject(config, true);
        }
        #endregion     
    }
}
 