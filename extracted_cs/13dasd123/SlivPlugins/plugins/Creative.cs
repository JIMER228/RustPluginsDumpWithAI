// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
/*
*  EULA
*  
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of This Software without the Developers consent
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*

*  Copyright Ryuk © 2023
*  Contact hikarigg38@gmail.com
*  Developers  Ryuk
*/

using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;
using ProtoBuf;
using Oxide.Core.Configuration;
using UnityEngine.UI;
using System.IO;
using Facepunch.Extend;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Creative", "Ryuk", "1.1.18")]
    [Description("Create zone, Imagine and Create bases, Save and load your bases and invite friends!")]
    public class Creative : RustPlugin
    {
        [PluginReference] Plugin CopyPaste;

        public static Creative Instance;
        public Dictionary<string, ZoneTrigger> activeZones = new Dictionary<string, ZoneTrigger>();
        private Dictionary<ulong, bool> playerInfAmmo = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerIsNoclip = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerIsLoadingBase = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> canUpgrade = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerStability = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerGodMode = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerResourceHud = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> playerAutomaticEntityEnable = new Dictionary<ulong, bool>();
        private Dictionary<ulong, int> upgradeLevels = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> skinSelected = new Dictionary<ulong, int>();
        private Dictionary<ulong, string> currentBaseName = new Dictionary<ulong, string>();
        Dictionary<BasePlayer, HashSet<SearchLight>> deployedSearchLights = new Dictionary<BasePlayer, HashSet<SearchLight>>();
        Dictionary<BasePlayer, HashSet<SamSite>> deployedSamSites = new Dictionary<BasePlayer, HashSet<SamSite>>();
        private string dataFilePath;
        private Dictionary<ulong, Dictionary<string, List<BuildingBlockData>>> savedBases = Facepunch.Pool.Get<Dictionary<ulong, Dictionary<string, List<BuildingBlockData>>>>();
        private bool isMenuOpen = false;
        private Timer checkTimer;

        private Dictionary<string, VendingMachineMapMarker> machineMarkers = new Dictionary<string, VendingMachineMapMarker>();
        private Dictionary<string, MapMarkerGenericRadius> sphereMarker = new Dictionary<string, MapMarkerGenericRadius>();

        private List<string> resourceItems = new List<string>
        {
            "wood",
            "stones",
            "sulfur",
            "cloth",
            "leather",
            "lowgradefuel",
            "metal.refined",
            "gunpowder",
            "hqm",
            "metal.fragments",
            "fat.animal",
            "skull.human",
            "gears",
            "pipes",
            "roadsigns",
            "rope",
            "semibody",
            "sheetmetal",
            "tarp",
            "techparts",
            "targeting.computer",
            "cctv.camera",
            "riflebody",
            "scrap",
            "smgbody",
            "explosives",
            "metalspring",
            "electricfuse",
            "metalblade",
            "metalpipe",
            "ladder.wooden.wall",
            "skull.wolf",
            "sewingkit",
        };

        void Loaded()
        {
            Instance = this;
        }

        private void RegisterPermissions()
        {
            permission.RegisterPermission("creative.all", this);
            permission.RegisterPermission("creative.fly", this);
            permission.RegisterPermission("creative.infammo", this);
            permission.RegisterPermission("creative.infres", this);
            permission.RegisterPermission("creative.stability", this);
            permission.RegisterPermission("creative.godmode", this);
            permission.RegisterPermission("creative.autoenablentity", this);
            permission.RegisterPermission("creative.allowclaim", this);

            permission.RegisterPermission("creative.vip", this);
            permission.RegisterPermission("creative.admin", this);
        }

        void Init()
        {
            RegisterPermissions();

            deployedSearchLights = new Dictionary<BasePlayer, HashSet<SearchLight>>();
            deployedSamSites = new Dictionary<BasePlayer, HashSet<SamSite>>();

            dataFilePath = Path.Combine(Interface.GetMod().DataDirectory, "Creatives");
            LoadData();
            LoadConfig();


            cmd.AddChatCommand(_config.claim_cmd, this, "ClaimZone");
            cmd.AddChatCommand(_config.unclaim_cmd, this, "UnclaimZone");
            cmd.AddChatCommand(_config.create_cmd, this, "team_create");
            cmd.AddChatCommand(_config.invite_cmd, this, "team_invite");
            cmd.AddChatCommand(_config.save_cmd, this, "ChatCmdSaveBase");
            cmd.AddChatCommand(_config.load_cmd, this, "ChatCmdLoadBase");
            cmd.AddChatCommand(_config.time_cmd, this, "SetTimeCommand");
            cmd.AddChatCommand(_config.list_cmd, this, "ChatCmdListBases");
            cmd.AddChatCommand(_config.undo_cmd, this, "ChatCmdUndo");
            cmd.AddChatCommand(_config.menu_cmd, this, "ChatMenu");
            cmd.AddChatCommand(_config.info_cmd, this, "ChatInfoCommand");
            cmd.AddChatCommand(_config.cost_cmd, this, "BaseCostCommand");
            cmd.AddChatCommand(_config.upg_cmd, this, "UpgradeCommand");
            cmd.AddChatCommand(_config.noclip_cmd, this, "FlyCommand");
        }

        void OnServerInitialized()
        {
            if (CopyPaste != null)
                Puts("CopyPaste plugin found");

            foreach (var player in BasePlayer.activePlayerList){
                playerInfAmmo[player.userID] = false;
                playerIsNoclip[player.userID] = false;
                playerIsLoadingBase[player.userID] = false;
                playerStability[player.userID] = false;
                canUpgrade[player.userID] = false;
                playerGodMode[player.userID] = true;
                playerResourceHud[player.userID] = _config.res_hud;
                playerAutomaticEntityEnable[player.userID] = false;
                skinSelected[player.userID] = 0;
                upgradeLevels[player.userID] = 0;
                currentBaseName[player.userID] = "autosave";

                GrantResources(player);

                checkTimer = timer.Repeat(5f, 0, () =>
                {
                    RemoveEffectsFromPlayer(player);
                });
            }

            DownloadImage();

            foreach (ItemBlueprint bp in ItemManager.GetBlueprints())            
                bp.workbenchLevelRequired = 0;


            if (_config.disable_terrain_kick)
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "antihack.terrain_protection 0");

            if (_config.disable_decay)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "decay.scale 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "decay.upkeep 0");
            }

            if (_config.always_day){
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "env.progresstime false");
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "env.time 12");
            }
            
            foreach (ItemBlueprint bp in ItemManager.bpList)
            {
                BPs.Add(bp.targetItem.itemid);
            }

            LoadPublishedCodes();
        }

        void Unload()
        {
            checkTimer?.Destroy();
            foreach (var kvp in activeZones.ToList())
            {
                UnityEngine.Object.DestroyImmediate(kvp.Value.gameObject);
            }
            activeZones.Clear();

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyButtons(player);

                if (GetPlayerNoClipStatus(player))
                {
                    player.SendConsoleCommand("noclip");
                    SetPlayerNoClipStatus(player);
                }
            }

            RemoveAllMarkers();

            Facepunch.Pool.FreeList(ref EntStable);
            Facepunch.Pool.FreeList(ref stablentites);
            Facepunch.Pool.FreeList(ref BPs);
            Facepunch.Pool.Free(ref savedBases);
        }

        private void OnServerSave()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SendReply(player, "Autosaving bases. There will be some lag.");

                Vector3 Zone_Center_Pos = GetZoneCenter(player);
                if (Zone_Center_Pos == Vector3.zero)
                    return;

                if (_config.base_system == "default")
                {
                    if (GetLoadingBaseStatus(player))
                        return;

                    SaveBase(player, "autosave");

                }else if (_config.base_system == "copypaste"){

                    if (CopyPaste == null)
                    {
                        Puts("CopyPaste plugin not loaded. Please make sure you have added this plugin!");
                        return;
                    }

                    var layers = LayerMask.GetMask("Construction", "Deployed");

                    RaycastHit hit = new RaycastHit();

                    if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 100f, layers))
                    {
                        SendReply(player, "Invalid entity. Please look at your base before save!");
                        return;
                    }

                    string new_base_name = player.userID + "_autosave";

                    var files = Interface.Oxide.DataFileSystem.GetFiles("copypaste/");

                    var options = new List<string>{"each", "false", "radius", "60", "method", "proximity", "share", "true", "tree", "false" };
                    var status = CopyPaste.Call("TryCopyFromSteamId", player.userID, new_base_name, options.ToArray());

                    if (status is string)
                    {
                        SendReply(player, "There was a problem trying to save your base.");
                        return;
                    }

                    SendReply(player, $"Successfully saved 'autosave'");
                }
            }
        }

        private void CreateMapMarker(string markerID, string text, Vector3 position, float radius, BasePlayer player)
        {
            float wrldSize = ConVar.Server.worldsize;
            wrldSize = wrldSize / 25;

            if (_config.divider_val != 0.0f)
                wrldSize = _config.divider_val;

            if (!_config.zone_mapmarkers)
                return;

            MapMarkerGenericRadius mapMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;

            if (mapMarker != null)
            {
                mapMarker.alpha = 0.6f;
                mapMarker.color1 = Color.green;
                mapMarker.color2 = Color.white;
                mapMarker.name = markerID;
                mapMarker.radius = radius / wrldSize;
                mapMarker.OwnerID = player.userID;
                mapMarker.Spawn();
                mapMarker.SendUpdate();
                sphereMarker[markerID] = mapMarker;
            }
            
            var markerPrefab = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
            if (markerPrefab != null)
            {
                markerPrefab.markerShopName = text;
                markerPrefab.SetFlag(BaseEntity.Flags.Busy, true, false, true);
                markerPrefab.Spawn();
                markerPrefab.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                machineMarkers[markerID] = markerPrefab;
            }
        }

        private void RemoveMarker(string markerID)
        {
            if (sphereMarker.ContainsKey(markerID))
            {
                MapMarkerGenericRadius marker = sphereMarker[markerID];
                if (marker != null && !marker.IsDestroyed)
                {
                    marker.Kill();
                }
                sphereMarker.Remove(markerID);
            }


            if (machineMarkers.ContainsKey(markerID))
            {
                VendingMachineMapMarker marker = machineMarkers[markerID];
                if (marker != null)
                {
                    marker.Kill();
                }
                machineMarkers.Remove(markerID);
            }
        }

        private void RemoveAllMarkers()
        {
            foreach (var Smarker in sphereMarker.Values)
            {
                if (Smarker != null)
                {
                    Smarker.Kill();
                }
            }
            sphereMarker.Clear();

            foreach (var marker in machineMarkers.Values)
            {
                if (marker != null)
                {
                    marker.Kill();
                }
            }
            machineMarkers.Clear();
        }

        private bool DoesZoneCollide(Vector3 position, float radius)
        {
            if (_config.lobby_position != Vector3.zero)
            {
                float distance_lobby = Vector3.Distance(position, _config.lobby_position);

                if (distance_lobby < radius + 60f)
                    return true;
            }

            foreach (var kvp in activeZones)
            {
                var existingZone = kvp.Value;

                float distance = Vector3.Distance(position, existingZone.transform.position);

                if (distance < radius + existingZone.GetColliderRadius())
                {
                    return true;
                }
            }

            return false;
        }

        public string GenerateRandomZoneID()
        {
            int length = 10;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            System.Random random = new System.Random();
            string randomID = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return randomID;
        }

        float getplayerradius(BasePlayer player)
        {

            if (permission.UserHasPermission(player.UserIDString, "creative.admin") || permission.UserHasPermission(player.UserIDString, "creative.vip"))
                return _config.vip_zone_radius;

            return _config.default_zone_radius; 
        }

        void DeleteEntities(BasePlayer player)
        {
            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendReply(player, "You don't have any claimed area!");
                return;
            }

            List<BaseEntity> entitiesToRemove = Facepunch.Pool.GetList<BaseEntity>();
            int entitiesCount = 0;

            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                if (entity is BasePlayer || entity.OwnerID == 0)
                    continue;

                if (Vector3.Distance(Zone_Center_Pos, entity.transform.position) <= getplayerradius(player))
                {
                    entitiesToRemove.Add(entity);
                    entitiesCount++;
                }
            }

            ServerMgr.Instance.StartCoroutine(DeleteEntitiesCoroutine(player, entitiesToRemove, entitiesCount));
        }

        private IEnumerator DeleteEntitiesCoroutine(BasePlayer player, List<BaseEntity> entitiesToRemove, int totalEntities)
        {
            float deletionDelay = 0.2f;

            for (int i = 0; i < totalEntities; i++)
            {
                if (i < entitiesToRemove.Count)
                {
                    BaseEntity entityToRemove = entitiesToRemove[i];
                    
                    if (entityToRemove != null && !entityToRemove.IsDestroyed)
                        entityToRemove.Kill();
                }

                yield return new WaitForSeconds(deletionDelay);
            }

            SendReply(player, $"Removed {totalEntities} entities/buildings in a {getplayerradius(player)} radius.");
            Facepunch.Pool.FreeList(ref entitiesToRemove);
        }

        void OnPlayerConnected(BasePlayer player){
            if (!player)
            return;

            GrantResources(player);
            playerInfAmmo[player.userID] = false;
            playerIsNoclip[player.userID] = false;
            playerIsLoadingBase[player.userID] = false;
            playerStability[player.userID] = false;
            canUpgrade[player.userID] = false;
            playerGodMode[player.userID] = true;
            playerResourceHud[player.userID] = _config.res_hud;
            playerAutomaticEntityEnable[player.userID] = false;
            skinSelected[player.userID] = 0;
            upgradeLevels[player.userID] = 0;
            currentBaseName[player.userID] = "autosave";

            CheckBuildingPlan(player);

            ShowInfoHelpGUI(player);

            ServerMgr.Instance.StartCoroutine(UnlockBlueprints(player));

            player.ClientRPCPlayer(null, player, "craftMode", 1);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);
            player.PauseFlyHackDetection(86400f);
            player.PauseSpeedHackDetection(86400f);
            player.SendNetworkUpdateImmediate();
        }

        private List<int> BPs = Facepunch.Pool.GetList<int>();

        private IEnumerator UnlockBlueprints(BasePlayer player)
        {
            var currentPlayerBps = player.PersistantPlayerInfo.unlockedItems;

            foreach (var bp in BPs)
            {
                if (!currentPlayerBps.Contains(bp))
                    currentPlayerBps.Add(bp);
            }

            var persistantPlayerInfo = player.PersistantPlayerInfo;
            persistantPlayerInfo.unlockedItems = currentPlayerBps;
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);

            yield return new WaitForSeconds(0.05f);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (GetPlayerNoClipStatus(player))
            {
                player.SendConsoleCommand("noclip");
            }

            RemoveEffectsFromPlayer(player);

            ulong playerId = player.userID;

            if (!_config.ondisconnect_keep_things)
            {
                player.inventory.Strip();

                List<string> zonesToRemove = Facepunch.Pool.GetList<string>();

                zonesToRemove.AddRange(activeZones
                    .Where(pair => pair.Value.GetOwner() == player.userID)
                    .Select(pair => pair.Key));

                foreach (string zoneID in zonesToRemove)
                {
                    if (activeZones.TryGetValue(zoneID, out var zoneTrigger))
                    {
                        RemoveMarker(zoneID);
                        DeleteEntities(player);
                        zoneTrigger.DeleteCircle();
                        activeZones.Remove(zoneID);
                        UnityEngine.Object.DestroyImmediate(zoneTrigger.gameObject);
                    }
                }

                Facepunch.Pool.FreeList(ref zonesToRemove);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            timer.Once(1f, () => RemoveEffectsFromPlayer(player));
            GrantResources(player);
        }

        private bool IsPlayerNearTerrainOrWater(Vector3 position, float minimumDistance)
        {
            Vector3 up = Vector3.up * minimumDistance;
            return Physics.Raycast(position, Vector3.down, up.magnitude, LayerMask.GetMask("Water", "Terrain", "World", "Default"));
        }

        private System.Random random = new System.Random();
        private string publishCodesFilePath = Path.Combine(Interface.Oxide.DataDirectory, "Creatives", "Published.json");
       private List<PublishedData> publishCodes = new List<PublishedData>();

        public class PublishedData
        {
            public ulong UserId { get; set; }
            public string BaseName { get; set; }
            public ulong Code { get; set; }
        }

        private void LoadPublishedCodes()
        {
            if (File.Exists(publishCodesFilePath))
            {
                string jsonData = File.ReadAllText(publishCodesFilePath);
                publishCodes = JsonConvert.DeserializeObject<List<PublishedData>>(jsonData);
            }
        }

        private void SavePublishedCodes()
        {
            string jsonData = JsonConvert.SerializeObject(publishCodes, Formatting.Indented);
            File.WriteAllText(publishCodesFilePath, jsonData);
        }


        private ulong GenerateRandomCode()
        {
            return (ulong)random.Next(1000000000, int.MaxValue) * 10UL + (ulong)random.Next(0, 999999999);
        }

        [ChatCommand("publish")]
        private void PublishCommand(BasePlayer player, string command, string[] args)
        {
            if (!currentBaseName.ContainsKey(player.userID))
                return;

            string baseName = currentBaseName[player.userID];
            PublishedData existingData = publishCodes.Find(data => data.UserId == player.userID && data.BaseName == baseName);

            if (existingData != null)
            {
                existingData.Code = GenerateRandomCode();
                SavePublishedCodes();
                SendReply(player, $"Your base has been republished with code {existingData.Code}");
            }
            else
            {
                ulong code = GenerateRandomCode();
                publishCodes.Add(new PublishedData { UserId = player.userID, BaseName = baseName, Code = code });
                SavePublishedCodes();
                SendReply(player, $"Your base has been published with code {code}");
            }
        }


        [ChatCommand("load_code")]
        private void LoadCodeCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Usage: /load_code <published code>");
                return;
            }

            ulong codeToLoad;
            if (ulong.TryParse(args[0], out codeToLoad))
            {
                var entry = publishCodes.Find(c => c.Code == codeToLoad);

                if (entry != null)
                {
                    UndoBase(player);

                    switch (_config.base_system)
                    {
                        case "default":
                            LoadBase(player.userID, entry.BaseName, player);
                            break;
                        case "copypaste":
                            Vector3 Zone_Center_Pos = GetZoneCenter(player);
                            LoadBaseCp(player.userID, entry.BaseName, player, Zone_Center_Pos);
                            break;
                    }

                    SendReply(player, $"Loaded base using code {codeToLoad}");
                }
                else
                {
                    SendReply(player, $"No base found with the code {codeToLoad}");
                }
            }
            else
            {
                SendReply(player, "Invalid code. Please use a valid 10-digit code");
            }
        }

        [ChatCommand("code_list")]
        private void CodeListCommand(BasePlayer player, string command, string[] args)
        {
            var playerEntries = publishCodes.FindAll(c => c.UserId == player.userID);

            if (playerEntries.Count > 0)
            {
                SendReply(player, "Your published bases:");

                foreach (var entry in playerEntries)
                {
                    SendReply(player, $"Name: {entry.BaseName} | Public Code: {entry.Code}");
                }
            }
            else
            {
                SendReply(player, "You haven't published any bases yet.");
            }
        }

        private void ClaimZone(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.allowclaim") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            string closestZoneID = FindClosestZone(player);
            if (string.IsNullOrEmpty(closestZoneID))
            {
                ClaimZoneFunc(player);
            }else{
                if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
                {
                    if (zoneTrigger.GetOwner() == player.userID)
                    {
                        SendReply(player, "You already own a claimed area.");
                    }
                }
            }
        }

        private void ClaimZoneFunc(BasePlayer player)
        {
            Vector3 playerPosition = player.transform.position;

            if (!IsPlayerNearTerrainOrWater(playerPosition, 5f))
            {
                SendReply(player, "You should be in the terrain before claim a zone!");
                return;
            }

            if (DoesZoneCollide(playerPosition, getplayerradius(player)))
            {
                SendReply(player, "You cannot claim a zone here; It overlaps with an existing zone.");
                return;
            }

            string zoneID = GenerateRandomZoneID();
            var zoneTrigger = new GameObject().AddComponent<ZoneTrigger>();
            float fixed_radius = getplayerradius(player) - 10;
            zoneTrigger.CreateBubble(playerPosition, fixed_radius, zoneID, player.userID);
            zoneTrigger.CenterZone = playerPosition;
            activeZones.Add(zoneID, zoneTrigger);


            CreateMapMarker(zoneID, player.displayName, playerPosition, getplayerradius(player), player);

            SendReply(player, $"You've claimed the zone! | ID: {zoneID}");
        }
    
        [ConsoleCommand("playermenu.infres")]
        private void infrescommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.infres") && !hasPerms(player))
                {
                    SendReply(player, "You do not have permission to use this command.");
                    return;
                }

                player.inventory.Strip();
                GrantResources(player);
            }
        }

        [ConsoleCommand("playermenu.basecost")]
        private void basecostcommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player != null)
            {
                player.SendConsoleCommand("chat.say", "/cost");
            }
        }

        [ConsoleCommand("playermenu.claimzone")]
        private void claimzonecommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.allowclaim") && !hasPerms(player))
                {
                    SendReply(player, "You do not have permission to use this command.");
                    return;
                }

                string closestZoneID = FindClosestZone(player);
                if (string.IsNullOrEmpty(closestZoneID))
                {
                    ClaimZoneFunc(player);
                }else{
                    if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
                    {
                        if (zoneTrigger.GetOwner() == player.userID)
                        {
                            UnclaimFunc(player);
                        }
                    }
                }

                UpdateMenu(player);
            }
        }

        [ConsoleCommand("playermenu.stability")]
        private void stabcommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.stability") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            
            if (playerStability[player.userID])
                playerStability[player.userID] = false;
            else
                playerStability[player.userID] = true;

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            List<StabilityEntity> entities = GetEntitiesInRadius(Zone_Center_Pos, getplayerradius(player));
            if (entities.Count > 0){
                foreach (var entity in entities)
                {
                    if (playerStability[player.userID])
                        entity.grounded = true;
                    else
                        entity.grounded = false;

                    entity.InitializeSupports();
                    entity.UpdateStability();
                }
            }
            UpdateMenu(player);
        }

        public List<StabilityEntity> stablentites = Facepunch.Pool.GetList<StabilityEntity>();
        
        private List<StabilityEntity> GetEntitiesInRadius(Vector3 position, float radius)
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (Vector3.Distance(entity.transform.position, position) <= radius)
                {
                    var stabilityEntity = entity as StabilityEntity;

                    if (stabilityEntity != null)
                    {
                        if (!stabilityEntity.grounded)
                        {
                            stablentites.Add(stabilityEntity);
                        }
                    }
                }
            }

            return stablentites;
        }


        private void UnclaimZone(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length != 0)
            {
                SendReply(player, "Usage: /unclaim");
                return;
            }

            UnclaimFunc(player);
        }

        private void UnclaimFunc(BasePlayer player)
        {
            string closestZoneID = FindClosestZone(player);
            if (string.IsNullOrEmpty(closestZoneID))
            {
                SendReply(player, "You do not have any zones to unclaim.");
                return;
            }

            if (playerIsLoadingBase[player.userID])
            {
                SendReply(player, "You can't unclaim while loading a base!");
                return;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (zoneTrigger.GetOwner() == player.userID || player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    DeleteEntities(player);
                    zoneTrigger.DeleteCircle();
                    activeZones.Remove(closestZoneID);
                    UnityEngine.Object.DestroyImmediate(zoneTrigger.gameObject);

                    RemoveMarker(closestZoneID);
                    
                    SendReply(player, $"You've unclaimed zone with ID: {closestZoneID}");
                }
                else
                {
                    SendReply(player, "You do not have permission to unclaim this zone.");
                }
            }
        }

        private string FindClosestZone(BasePlayer player)
        {
            string closestZoneID = null;
            float closestDistance = float.MaxValue;
            Vector3 playerPosition = player.transform.position;

            foreach (var kvp in activeZones)
            {
                var existingZone = kvp.Value;
                float distance = Vector3.Distance(playerPosition, existingZone.transform.position);

                if (distance < closestDistance)
                {
                    if (existingZone.GetOwner() == player.userID || player.Team != null && player.Team.members.Contains(existingZone.GetOwner()))
                    {
                        closestDistance = distance;
                        closestZoneID = kvp.Key;
                    }
                }
            }

            return closestZoneID;
        }

        Vector3 GetZoneCenter(BasePlayer player){
            
            var zonesToRemove = Facepunch.Pool.GetList<string>();

            zonesToRemove.AddRange(activeZones
                .Where(pair => pair.Value.GetOwner() == player.userID)
                .Select(pair => pair.Key));

            foreach (string zoneID in zonesToRemove)
            {
                if (activeZones.TryGetValue(zoneID, out var zoneTrigger))
                {
                    return zoneTrigger.GetZoneCenterZ();
                }
            }

            Facepunch.Pool.FreeList(ref zonesToRemove);

            return Vector3.zero;
        }

        bool GetPlayerInfAmmoStatus(BasePlayer player)
        {
            return playerInfAmmo[player.userID];
        }

        void SetPlayerInfAmmoStatus(BasePlayer player)
        {
            playerInfAmmo[player.userID] = !playerInfAmmo[player.userID];
        }

        void SetLoadingBaseStatus(BasePlayer player, bool value)
        {
            playerIsLoadingBase[player.userID] = value;
        }

        bool GetLoadingBaseStatus(BasePlayer player)
        {
            return playerIsLoadingBase[player.userID];
        }

        bool GetPlayerNoClipStatus(BasePlayer player)
        {
            if (!playerGodMode.ContainsKey(player.userID))
                return false;
                
            return playerIsNoclip[player.userID];
            //return player.IsFlying;
        }

        void SetPlayerNoClipStatus(BasePlayer player)
        {
            playerIsNoclip[player.userID] = !playerIsNoclip[player.userID];
        }

        bool GetZoneOwned(BasePlayer player)
        {
            string closestZoneID = FindClosestZone(player);
            if (string.IsNullOrEmpty(closestZoneID))
            {
                return false;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (zoneTrigger.GetOwner() == player.userID || player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    return true;
                }
            }

            return false;
        }

        void ChangeGTFOStatus(BasePlayer player){
            string closestZoneID = FindClosestZone(player);
            if (string.IsNullOrEmpty(closestZoneID))
            {
                SendReply(player, "You should own a zone before use GTFO.");
                return;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (zoneTrigger.GetOwner() == player.userID || player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    zoneTrigger.GTFO = !zoneTrigger.GTFO;
                    SendReply(player, $"[GTFO] Status: {zoneTrigger.GTFO} | zoneid: {closestZoneID}");
                }
                else
                {
                    SendReply(player, "You do not have permission to change GTFO mode.");
                }
            }
        }

        bool GetGTFOStatus(BasePlayer player)
        {
            string closestZoneID = FindClosestZone(player);
            if (string.IsNullOrEmpty(closestZoneID))
            {
                return true;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (zoneTrigger.GetOwner() == player.userID || player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    return zoneTrigger.GTFO;
                }
            }

            return true;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
            {
                return;
            }

            if (input.WasJustPressed(BUTTON.FIRE_THIRD))
            {
                ToggleMenu(player);
            }

            if (canUpgrade != null )
            {
                if (canUpgrade[player.userID])
                {
                    if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                        DoBuilding(player, "upgrade");

                    if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                        DoBuilding(player, "downgrade");
                }
            }

            var item = player.GetActiveItem();
            if (item == null)
            {
                return;
            }

            HeldEntity heldEntity = player.GetHeldEntity() as HeldEntity;

            if (input.WasJustPressed(BUTTON.RELOAD))
            {
                if (item.info != null && item.info.shortname != null && item.info.shortname == "hammer")
                {
                    var layers = LayerMask.GetMask("Construction", "Default", "Deployed");

                    RaycastHit hit = new RaycastHit();

                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f, layers))
                    {
                        var entity = hit.GetEntity();
                        if (entity != null)
                        {
                            if (entity.OwnerID == player.userID || (player.Team != null && player.Team.members.Contains(entity.OwnerID)))
                            {
                                ((BaseEntity)entity).Kill();
                            }
                        }
                    }
                }
            }
        }

        private void RemoveEffectsFromPlayer(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            if (player.metabolism.calories.value < 500)
                player.metabolism.calories.value = 500;

            if (player.metabolism.hydration.value < 250)
                player.metabolism.hydration.value = 250;

            player.health = 100;

            player.metabolism.temperature.value = 30;
            player.metabolism.wetness.max = 0;
        }

        int RandGenerator()
        {
            int minValue = 0;
            int maxValue = 16;
            return UnityEngine.Random.Range(minValue, maxValue + 1);
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null) return;

            var furnace = gameObject.GetComponent<BaseOven>();
            var mixingTable = gameObject.GetComponent<Recycler>();
            var turret = gameObject.GetComponent<AutoTurret>();
            var door = gameObject.GetComponent<Door>();
            var chineseLantern = gameObject.GetComponent<ChineseLantern>();
            var searchlight = gameObject.GetComponent<SearchLight>();
            var samSite = gameObject.GetComponent<SamSite>();

            int assignedLevel = upgradeLevels[player.userID];
            var buildingBlock = gameObject.GetComponent<BuildingBlock>();
            var deployable = gameObject.GetComponent<Deployable>();

            if (!deployable){
                if (buildingBlock != null)
                {
                    var targetGrade = (BuildingGrade.Enum)assignedLevel;
                    ulong skinsel = (ulong)skinSelected[player.userID];

                    buildingBlock.SetGrade(targetGrade);
                    buildingBlock.SetHealthToMax();

                    if (upgradeLevels[player.userID] == 2)
                    {
                        buildingBlock.ChangeGradeAndSkin(buildingBlock.grade, skinsel, true, true);
                    }

                    if (upgradeLevels[player.userID] == 3 && skinSelected[player.userID] > 0)
                    {
                        buildingBlock.playerCustomColourToApply = (uint)RandGenerator();
                        buildingBlock.ChangeGradeAndSkin(buildingBlock.grade, 10221, true, true);
                    }

                    if (playerStability[player.userID]){
                        if (!buildingBlock.grounded)
                        {
                            stablentites.Add(buildingBlock);
                            buildingBlock.grounded = true;
                        }
                    }

                    buildingBlock.UpdateSkin();
                    buildingBlock.SendNetworkUpdateImmediate();
                }
            }

            if (playerAutomaticEntityEnable.ContainsKey(player.userID) && !playerAutomaticEntityEnable[player.userID])
                return;

            if (furnace != null && furnace.ShortPrefabName.Contains("furnace"))
            {
                timer.Once(0.1f, () =>
                {
                    AddInfiniteFuel(furnace);
                    LightFurnace(furnace);
                });
            }
            
            if (chineseLantern != null)
            {
                timer.Once(0.1f, () =>
                {
                    AddFuelToChineseLantern(chineseLantern, 1000);
                    LightChineseLantern(chineseLantern);
                });
            }
            
            
            if (turret != null)
            {
                timer.Once(0.1f, () =>
                {
                    PowerTurret(turret);
                    AutoAuthorizePlayers(turret);
                });
            }
            
            if (door != null && door.ShortPrefabName.Contains("wall.frame.garagedoor"))
            {
                timer.Once(0.1f, () =>
                {
                    OpenGarageDoor(door);
                });
            }
            
            if (searchlight != null)
            {
                timer.Once(0.1f, () =>
                {
                    EnableSearchLight(searchlight);
                    AddSearchLight(searchlight, player);
                });
            }
            
            if (samSite != null)
            {
                timer.Once(0.1f, () =>
                {
                    EnableSamSite(samSite);
                    AddSamSite(samSite, player);
                });
            }
        }

        void EnableSearchLight(SearchLight searchlight)
        {
            searchlight.SetFlag(BaseEntity.Flags.On, true);
        }

        void EnableSamSite(SamSite samSite)
        {
            samSite.SetFlag(BaseEntity.Flags.On, true);
        }

        void AddSearchLight(SearchLight searchlight, BasePlayer player)
        {
            if (!deployedSearchLights.ContainsKey(player))
                deployedSearchLights[player] = new HashSet<SearchLight>();

            deployedSearchLights[player].Add(searchlight);
        }

        void AddSamSite(SamSite samSite, BasePlayer player)
        {
            if (!deployedSamSites.ContainsKey(player))
                deployedSamSites[player] = new HashSet<SamSite>();

            deployedSamSites[player].Add(samSite);
        }

        [ConsoleCommand("creative_runcmd")]
        private void creative_runcmd(ConsoleSystem.Arg arg) 
        {
            var args = arg.Args;
            
            if (arg.Player() == null) 
                return;
            
            var player = arg?.Player();
            
            string chatcmd = args[0].ToString();
            string replacedValues = chatcmd.Replace("{player.name}", $"{player.displayName}")
            .Replace("{player.steam}", $"{player.userID}");
            
            switch(replacedValues){
                case "twig":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 0\" ");
                break;
                case "wood":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 1\" ");
                break;
                case "stone":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 2\" ");
                break;
                case "frags":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 3\" ");
                break;
                case "hqm":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 4\" ");
                break;
                case "adobe":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 5\" ");
                break;
                case "bricks":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 6\" ");
                break;
                case "brutalist":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 7\" ");
                break;
                case "ship":
                    arg.Player().SendConsoleCommand($"chat.say \"/up 8\" ");
                break;
            }
        }

        public class ImageURL { public string Name; public string Url; }
        private readonly HashSet<string> _failedImages = new HashSet<string>();
        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();

        private readonly HashSet<ImageURL> _urls = new HashSet<ImageURL>
        {
            new ImageURL { Name = "twig", Url = "Images/blank.png" },
            new ImageURL { Name = "wood", Url = "Images/wood.png" },
            new ImageURL { Name = "stone", Url = "Images/stones.png" },
            new ImageURL { Name = "frags", Url = "Images/metal_fragments.png" },
            new ImageURL { Name = "hqm", Url = "Images/metal_refined.png" },
            new ImageURL { Name = "adobe", Url = "Images/stone_adobe_dlc.png" },
            new ImageURL { Name = "bricks", Url = "Images/stone_brick_dls.png" },
            new ImageURL { Name = "brutalist", Url = "Images/stones_brutalist_dlc.png" },
            new ImageURL { Name = "ship", Url = "Images/ship.png" },
        };

        private void DownloadImage()
        {
            ImageURL image = _urls.FirstOrDefault(x => !_images.ContainsKey(x.Name) && !_failedImages.Contains(x.Name));
            if (image != null)
            {
                ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image));
            }
            else if (_failedImages.Count > 0) Interface.Oxide.UnloadPlugin(Name);
        }

        IEnumerator ProcessDownloadImage(ImageURL image)
        {
            string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + image.Url;
            using (WWW www = new WWW(url))
            {
                yield return www;
                if (www.error != null)
                {
                    _failedImages.Add(image.Name);
                    PrintError($"Image {image.Name} was not found. Maybe you didn't upload it to the .../oxide/data/Images/ folder");
                }
                else
                {
                    Texture2D tex = www.texture;
                    _images.Add(image.Name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                DownloadImage();
            }
        }

        private void CreateButtons(BasePlayer player)
        {
            CuiElementContainer panel = new CuiElementContainer();
            var buttonColor = "0.8 0.8 0.8 0.5";

            var buttonSize = 55;
            var spacing = 2;

            var panelWidth = (buttonSize + spacing) * _images.Count - spacing;
            var panelHeight = buttonSize;

            var panelPosX = 500;
            var panelPosY = 300;

            var xOffset = panelWidth / 2;
            var yOffset = panelHeight / 2;

            var panelName = "BuildingPanel";

            var panelComponent = new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "375 80",
                    OffsetMax = "50 20"
                },
                CursorEnabled = false
            };
            panel.Add(panelComponent, "Hud.Menu", panelName);

            var startX = 0;

            foreach (var entry in _images)
            {
                var buttonName = entry.Key + "_button";
                var imageName = entry.Key + "_image";
                var imageUrl = entry.Value;

                var button = new CuiButton
                {
                    Button = { 
                        Command = $"creative_runcmd {entry.Key}",
                        Color = buttonColor
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = $"{startX} 0",
                        OffsetMax = $"{startX + buttonSize} {buttonSize}"
                    }
                };

                var image = new CuiElement
                {
                    Name = imageName,
                    Parent = buttonName,
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images[entry.Key] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                };

                var buttonElement = new CuiElement
                {
                    Name = buttonName,
                    Parent = panelName,
                    Components =
                    {
                        button.RectTransform,
                        button.Button
                    }
                };

                panel.Add(buttonElement);
                panel.Add(image);

                startX += buttonSize + spacing;
            }

            CuiHelper.AddUi(player, panel);
        }

        private void DestroyButtons(BasePlayer player)
        {
            var panelName = "BuildingPanel"; 
            CuiHelper.DestroyUi(player, panelName);
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null)
                return;

            if (oldItem != null && oldItem.info.shortname == "building.planner")
            {
                DestroyButtons(player);
            }

            CheckBuildingPlan(player);

            if (newItem == null || newItem.amount == 0)
            {
                canUpgrade[player.userID] = true;
            }
            else
            {
                canUpgrade[player.userID] = false;
            }
        }

        private const string TwigPanel = "TwigPanel";
        private const string TwigImageName = "Twig_" + TwigPanel + ".image";

        private const string WoodPanel = "WoodPanel";
        private const string WoodImage = "Wood_" + WoodPanel + ".image";

        private const string StonePanel = "StonePanel";
        private const string StoneImage = "Stone_" + StonePanel + ".image";

        private const string FragsPanel = "FragsPanel";
        private const string FragsImage = "Frags_" + FragsPanel + ".image";

        private const string HqmPanel = "HqmPanel";
        private const string HqmImage = "Hqm_" + HqmPanel + ".image";

        private void CheckBuildingPlan(BasePlayer player)
        {
            if (player == null)
                return;

            if (playerResourceHud.ContainsKey(player.userID))
            {
                if (!playerResourceHud[player.userID])
                {
                    DestroyButtons(player);
                    return;
                }

                var activeItem = player.GetActiveItem();
                if (activeItem != null && activeItem.info.shortname == "building.planner")
                {
                    CreateButtons(player);
                }
                else
                {
                    DestroyButtons(player);
                }
            }
        }

        void DoBuilding(BasePlayer player, string curr){
            var layers =  LayerMask.GetMask("Construction", "Default", "Deployed");

            RaycastHit hit = new RaycastHit();
                
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue, layers))
            {
                var entity = hit.GetEntity();
                if (entity != null)
                {
                    var buildingBlock = GetBuildingBlockInView(player);

                    if (buildingBlock == null)
                        return;

                    var currentGrade = buildingBlock.grade;
                    var nextGrade = currentGrade + 1;
                    var previousGrade = currentGrade - 1;

                    if (!Enum.IsDefined(typeof(BuildingGrade.Enum), nextGrade))
                        return;

                    if (curr == "upgrade" && currentGrade.ToString() != "TopTier")
                    {
                        buildingBlock.SetGrade(nextGrade);
                        buildingBlock.health = GetCurrentGradeMaxHealth(buildingBlock);
                        buildingBlock.SendNetworkUpdateImmediate();
                    }else if (curr == "downgrade" && currentGrade.ToString() != "Twigs"){
                        buildingBlock.SetGrade(previousGrade);
                        buildingBlock.health = GetCurrentGradeMaxHealth(buildingBlock);
                        buildingBlock.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        private string GetBuildingGrade(BaseEntity entity)
        {
            BuildingBlock buildingBlock = entity as BuildingBlock;
            if (buildingBlock != null)
            {
                return buildingBlock.grade.ToString();
            }
            return "N/A";
        }

        private BuildingBlock GetBuildingBlockInView(BasePlayer player)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(player.eyes.HeadRay(), 3f))
            {
                var entity = hit.collider.GetComponentInParent<BuildingBlock>();
                if (entity != null && entity.GetType() == typeof(BuildingBlock))
                {
                    return entity;
                }
            }
            return null;
        }

        private float[] gradeMaxHealths = { 10f, 250f, 500f, 1000f, 2000f };

        private float GetCurrentGradeMaxHealth(BuildingBlock buildingBlock)
        {
            var currentGradeIndex = (int)buildingBlock.grade;
            
            if (currentGradeIndex >= 0 && currentGradeIndex < gradeMaxHealths.Length)
            {
                var maxHealth = gradeMaxHealths[currentGradeIndex];
                return maxHealth;
            }
            
            return 0f;
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (!entity)
                return;

            if (entity.ShortPrefabName == "item_drop" || entity is DroppedItem || entity is LootContainer)
            {
                timer.Once(5f, () =>
                {
                    if (!entity.IsDestroyed)
                        entity.Kill();
                });
            }
            
            var toolCupboard = entity as BuildingPrivlidge;
            if (toolCupboard != null)
            {
                NextTick(() =>
                {
                    if (!toolCupboard.IsDestroyed)
                    {
                        var woodItem = ItemManager.CreateByItemID(-151838493, 999999); 
                        var metalFragmentsItem = ItemManager.CreateByItemID(69511070, 999999); 
                        var stoneItem = ItemManager.CreateByItemID(-2099697608, 999999); 
                        var highQualityMetalItem = ItemManager.CreateByItemID(317398316, 999999); 

                        if (woodItem != null)
                        {
                            woodItem.MoveToContainer(toolCupboard.inventory);
                        }
                        if (metalFragmentsItem != null)
                        {
                            metalFragmentsItem.MoveToContainer(toolCupboard.inventory);
                        }
                        if (stoneItem != null)
                        {
                            stoneItem.MoveToContainer(toolCupboard.inventory);
                        }
                        if (highQualityMetalItem != null)
                        {
                            highQualityMetalItem.MoveToContainer(toolCupboard.inventory);
                        }
                    }
                });
            }
            BasePlayer player = entity as BasePlayer;

            if(!player)return;

            if (playerAutomaticEntityEnable.ContainsKey(player.userID) && !playerAutomaticEntityEnable[player.userID])
                return;

            if (entity.ShortPrefabName != "elevator")
            {
                if (entity is IOEntity)
                {
                    var iOEntity = (IOEntity)entity;

                    iOEntity.UpdateHasPower(1000, 100);
                    iOEntity.IOStateChanged(1000, 100);
                    iOEntity.SendNetworkUpdate();
                }
            }
        }        

        object OnPayForPlacement(BasePlayer player, Planner planner, Construction construction)
        {            
            return true;
        }

        void AddInfiniteFuel(BaseOven furnace)
        {
            ItemDefinition woodItem = ItemManager.FindItemDefinition("wood");
            if (woodItem != null)
            {
                Item fuelItem = ItemManager.CreateByItemID(woodItem.itemid, 1);
                fuelItem.amount = 2147483647;
                furnace.inventory.AddItem(fuelItem.info, fuelItem.amount);
            }
        }

        void AddFuelToChineseLantern(ChineseLantern lantern, int amount)
        {
            ItemDefinition fuelitemz = ItemManager.FindItemDefinition("lowgradefuel");
            if (fuelitemz != null)
            {
                Item fuelItem = ItemManager.CreateByItemID(fuelitemz.itemid, 1);
                fuelItem.amount = 2147483647; 
                lantern.inventory.AddItem(fuelItem.info, fuelItem.amount);
            }
        }

        void LightChineseLantern(ChineseLantern lantern)
        {
            lantern.enabled = true;
            lantern.SetFlag(BaseEntity.Flags.On, true);
        }

        void LightFurnace(BaseOven furnace)
        {
            furnace.SetFlag(BaseEntity.Flags.On, true);
        }

        void PowerTurret(AutoTurret turret)
        {
            turret.SetIsOnline(true);
        }

        void AutoAuthorizePlayers(AutoTurret turret)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.IsConnected)
                {
                    turret.authorizedPlayers.Add(new PlayerNameID
                    {
                        userid = player.userID,
                        username = player.displayName
                    });
                }
            }
        }

        void AuthorizeAllPlayersOnTurrets()
        {
            foreach (var turret in BaseNetworkable.serverEntities.OfType<AutoTurret>())
            {
                PowerTurret(turret);
                AutoAuthorizePlayers(turret);
            }
        }

        void OpenGarageDoor(Door door)
        {
            door.SetFlag(BaseEntity.Flags.Open, true);
            door.SendNetworkUpdateImmediate();
        }

        private void team_create(BasePlayer player)
        {
            if (player.currentTeam != 0UL)
            {
                SendReply(player, $"You're already in a team!");
                return;
            }

            RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.CreateTeam();
            Team.teamLeader = player.userID;
            Team.AddPlayer(player);
        }

        private void team_invite(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Usage /invite <playername>");
            }

            string playerName = args[0];

            if (player.currentTeam == 0UL)
            {
                SendReply(player, "You should create a team before invite a player!");
                return;
            }

            RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            var Target = FindPlayersOnline(playerName);

            if (Target.Count <= 0)
            {
                SendReply(player, $"Player {playerName} not found!");
                return;
            }
            else if (Target.Count > 1)
            {
                SendReply(player, $"There are multiple players with the name {playerName}!");
                return;
            }
    
            var pTarget = Target[0];

            if (!pTarget || pTarget == null){
                SendReply(player, $"Player {playerName} not found!");
                return;
            }

            if (pTarget.currentTeam != 0UL){
                SendReply(player, $"Player {playerName} is already in another team!");
                return;
            }

            if (pTarget == player){
                SendReply(player, $"You can't invite yourself!");
                return;
            }

            Team.SendInvite(pTarget);
            SendReply(player, $"Invited {pTarget.displayName}");
            SendReply(pTarget, $"You have been invited to {player.displayName} team! \nAccept or reject in your TeamTab on Inventory!");
        }

        object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, ulong playerId)
        {
            BasePlayer player = BasePlayer.FindByID(playerId);
            
            if (player == null)
            {
                Puts($"Player with SteamID {playerId} not found.");
                return null;
            }
            
            if (team.teamLeader != playerId)
            {
                BasePlayer teamLeaderPlayer = BasePlayer.FindByID(team.teamLeader);
                
                if (teamLeaderPlayer != null)
                {
                    Vector3 teleportPosition = teamLeaderPlayer.transform.position;
                    player.Teleport(teleportPosition);
                }
                else
                {
                    Puts($"Team leader with SteamID {team.teamLeader} not found.");
                }
            }
            return null;
        }

        private static List<BasePlayer> FindPlayersOnline(string name)
        {
            List<BasePlayer> playersList = Facepunch.Pool.GetList<BasePlayer>();

            if (string.IsNullOrEmpty(name))
            {
                return playersList;
            }

            foreach (var activePlayer in BasePlayer.activePlayerList.ToList())
            {
                if (activePlayer.UserIDString.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
                else if (!string.IsNullOrEmpty(activePlayer.displayName) &&
                        activePlayer.displayName.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
                else if (activePlayer.net?.connection != null &&
                        activePlayer.net.connection.ipaddress.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
            }

            return playersList;
        }

        private void ChatCmdSaveBase(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Syntax: /save <base_name>");
                return;
            }

            string baseName = args[0];
            if (string.IsNullOrEmpty(baseName))
            {
                SendReply(player, "Please provide a valid base name.");
                return;
            }
            
            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {
                SendReply(player, "You should claim a zone before savinga base!");
                return;
            }

            int base_limit = 1;
            
            if (player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                base_limit = 999999999;
            else
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.vip"))
                    base_limit = _config.default_base_limit;
                else
                    base_limit = _config.vip_base_limit;
            }

            if (_config.base_system == "default"){
                if (GetLoadingBaseStatus(player))
                {
                    SendReply(player, "You can't save a new base while there's a base loading.");
                    return;
                }

                if (savedBases.ContainsKey(player.userID))
                {
                    var playerBases = savedBases[player.userID];
                    if (playerBases.Count < base_limit)
                    {
                        SaveBase(player, baseName);
                    }else{
                        if (playerBases.ContainsKey(baseName))
                        {
                            SendReply(player, $"The base {baseName} already exists, overwriting file!");
                            SaveBase(player, baseName);
                        }else{
                            SendReply(player, $"You reached the limit of {base_limit} saved bases per player!");
                            SendReply(player, "Please use /list and the overwrite one base with the same name.");
                        }
                    }
                }else{
                    SendReply(player, $"Saving base {baseName}!");
                    SaveBase(player, baseName);
                }
            }else if (_config.base_system == "copypaste"){

                if (CopyPaste == null)
                {
                    Puts("CopyPaste plugin not loaded. Please make sure you added this plugin!");
                    return;
                }

                var layers = LayerMask.GetMask("Construction", "Deployed");
                RaycastHit hit = new RaycastHit();

                if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 100f, layers))
                {
                    SendReply(player, "Invalid entity. Please look at your base before save!");
                    return;
                }

                string new_base_name = player.userID + "_" + baseName;

                var files = Interface.Oxide.DataFileSystem.GetFiles("copypaste/");


                if (files.Length == 0)
                {
                    var options = new List<string>{"each", "false", "radius", "60", "method", "proximity", "share", "true", "tree", "false" };
                    var status = CopyPaste.Call("TryCopyFromSteamId", player.userID, new_base_name, options.ToArray());

                    if (status is string){
                        SendReply(player, "There was a problem trying to save your base.");
                        return;
                    }

                    currentBaseName[player.userID] = new_base_name;
                    SendReply(player, $"You've successfully saved {baseName}");
                    return;
                }
                else
                {
                    var saved_bases_count = 0;
                    bool overwrite = false;
                    foreach (var file in files)
                    {
                        string regex = @"/(.*?)_";
                        Match match = Regex.Match(file, regex);

                        if (match.Success){
                            string useridfromfile = match.Groups[1].Value;

                            if (player.userID.ToString() == useridfromfile)
                            {
                                var strFileParts = file.Split('/');
                                var removejson = strFileParts[strFileParts.Length - 1].Replace(".json", "");

                                saved_bases_count += 1;

                                if (saved_bases_count >= base_limit && new_base_name == removejson)
                                {
                                    overwrite = true;
                                    SendReply(player, $"The base {baseName} already exists, overwriting file!");
                                    break;
                                }else if (saved_bases_count >= base_limit && new_base_name != removejson)
                                {
                                    overwrite = false;
                                    SendReply(player, $"You reached the limit of {base_limit} saved bases per player!");
                                    SendReply(player, "Please use /list and the overwrite one base with the same name.");
                                    break;
                                }
                            }
                        }
                        
                        if (saved_bases_count < base_limit)
                        {
                            var options = new List<string>{"each", "false", "radius", "60", "method", "proximity", "share", "true", "tree", "false" };
                            var status = CopyPaste.Call("TryCopyFromSteamId", player.userID, new_base_name, options.ToArray());

                            if (status is string){
                                SendReply(player, "There was a problem trying to save your base.");
                                return;
                            }

                            currentBaseName[player.userID] = new_base_name;
                            SendReply(player, $"You've successfully saved {baseName}");
                            return;
                        }else{
                            if (overwrite){
                                var options = new List<string>{"each", "false", "radius", "60", "method", "proximity", "share", "true", "tree", "false" };
                                var status = CopyPaste.Call("TryCopyFromSteamId", player.userID, new_base_name, options.ToArray());

                                if (status is string){
                                    SendReply(player, "There was a problem trying to save/overwrite your base.");
                                    return;
                                }

                                overwrite = false;
                                currentBaseName[player.userID] = new_base_name;
                                SendReply(player, $"You've successfully saved {baseName}");
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void ChatCmdLoadBase(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Syntax: /load <base_name>");
                return;
            }

            string baseName = args[0];
            if (string.IsNullOrEmpty(baseName))
            {
                SendReply(player, "Please provide a valid base name.");
                return;
            }

            if (GetLoadingBaseStatus(player))
            {
                SendReply(player, "You can't load a base while there's another base loading.");
                return;
            }

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {
                SendReply(player, "You should claim an area before load any base!");
                return;
            }

            UndoBase(player);

            if (_config.base_system == "default"){
                NextTick(() => LoadBase(player.userID, baseName, player));
            }else if (_config.base_system == "copypaste"){
                NextTick(() => LoadBaseCp(player.userID, baseName, player, Zone_Center_Pos));
            }
        }

        [ConsoleCommand("playermenu.undotofoundation")]
        private void undotofoundationcommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                var entitiesToRemove = Facepunch.Pool.GetList<BaseEntity>();

                Vector3 Zone_Center_Pos = GetZoneCenter(player);

                if (Zone_Center_Pos == Vector3.zero)
                {   
                    SendReply(player, "You don't have any claimed area!");
                    return;
                }

                foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
                {
                    if (entity is BasePlayer || entity.OwnerID == 0)
                        continue;

                    if (entity.ShortPrefabName.Contains("foundation", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Vector3.Distance(Zone_Center_Pos, entity.transform.position) <= getplayerradius(player))
                    {
                        entitiesToRemove.Add(entity);
                    }
                }

                foreach (var entityToRemove in entitiesToRemove)
                {
                    entityToRemove.Kill();
                }

                Facepunch.Pool.FreeList(ref entitiesToRemove);
            }
        }

        private void SetTimeCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null || args.Length != 1)
                return;

            int newTime;

            if (!int.TryParse(args[0], out newTime))
            {
                PrintToChat(player, "Invalid time format. Please use a number between 0 and 24.");
                return;
            }

            if (newTime < 0 || newTime > 24)
            {
                PrintToChat(player, "Time must be between 0 and 24.");
                return;
            }

            string commandString = $"admintime {newTime}";
            player.SendConsoleCommand(commandString);

            PrintToChat(player, $"Time set to {newTime}.");
        }

        [ConsoleCommand("playermenu.skins")]
        private void skincommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                player.SendConsoleCommand("chat.say", $"/skin");
            }
        }

        [ConsoleCommand("playermenu.undobase")]
        private void undobasecommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {   
                UndoBase(player);
            }
        }

        private void UndoBase(BasePlayer player){
            var entitiesToRemove = Facepunch.Pool.GetList<BaseEntity>();

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendReply(player, "You don't have any claimed area!");
                return;
            }

            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                if (entity is BasePlayer || entity.OwnerID == 0)
                    continue;

                if (Vector3.Distance(Zone_Center_Pos, entity.transform.position) <= (getplayerradius(player) + (getplayerradius(player) - 10)))
                {
                    entitiesToRemove.Add(entity);
                }
            }

            foreach (var entityToRemove in entitiesToRemove)
            {
                entityToRemove.Kill();
            }

            Facepunch.Pool.FreeList(ref entitiesToRemove);
        }

        private void ChatCmdListBases(BasePlayer player, string command, string[] args)
        {
            if (_config.base_system == "default")
            {
                if (savedBases.ContainsKey(player.userID))
                {
                    var playerBases = savedBases[player.userID];
                    if (playerBases.Count > 0)
                    {
                        SendReply(player, "Your saved bases:");
                        foreach (var baseName in playerBases.Keys)
                        {
                            SendReply(player, baseName);
                        }
                    }
                    else
                    {
                        SendReply(player, "You don't have any saved bases.");
                    }
                }
                else
                {
                    SendReply(player, "You don't have any saved bases.");
                }
            }
            else if (_config.base_system == "copypaste")
            {
                var files = Interface.Oxide.DataFileSystem.GetFiles("copypaste/");
                SendReply(player, "Your saved bases:");
                foreach (var file in files)
                {
                    string regex = @"/(.*?)_";
                    Match match = Regex.Match(file, regex);

                    if (match.Success){
                        string useridfromfile = match.Groups[1].Value;

                        if (player.userID.ToString() == useridfromfile)
                        {
                            var strFileParts = file.Split('/');
                            var removejson = strFileParts[strFileParts.Length - 1].Replace(".json", "");
                            var filenames = removejson.Replace(player.userID.ToString(), "");
                            var filenames2 = filenames.Replace("_", "");

                            SendReply(player, $"{filenames2}");
                        }
                    }
                }
            }
        }

        private void ChatCmdUndo(BasePlayer player, string command, string[] args)
        {
            if (GetLoadingBaseStatus(player))
            {
                SendReply(player, "You can't undo a base while there's a base loading.");
                return;
            }

            List<BaseEntity> entitiesToRemove = Facepunch.Pool.GetList<BaseEntity>();

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendReply(player, "You don't have any claimed area!");
                return;
            }

            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                if (entity is BasePlayer || entity.OwnerID == 0)
                    continue;

                if (Vector3.Distance(Zone_Center_Pos, entity.transform.position) <= getplayerradius(player))
                {
                    entitiesToRemove.Add(entity);
                }
            }

            foreach (var entityToRemove in entitiesToRemove)
            {
                entityToRemove.Kill();
            }

            SendReply(player, $"Removed {entitiesToRemove.Count} entities/buildings in a 60m radius.");
            Facepunch.Pool.FreeList(ref entitiesToRemove);
        }

        [ConsoleCommand("playermenu.viewbases")]
        private void OnViewBasesCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                CuiHelper.DestroyUi(player, "PlayerMenu");
                CuiHelper.DestroyUi(player, "CreativeTitle");
                CuiHelper.DestroyUi(player, "CreativeMenu");

                if (_config.base_system == "default")
                {
                    string playerDirPath = Path.Combine(dataFilePath, player.userID.ToString());

                    if (!Directory.Exists(playerDirPath))
                    {
                        SendReply(player, "You don't have any saved bases.");
                        return;
                    }

                    string[] playerBaseFiles = Directory.GetFiles(playerDirPath, "*.json");
                    if (playerBaseFiles.Length == 0)
                    {
                        SendReply(player, "You don't have any saved bases.");
                        return;
                    }

                    var playerBases = new Dictionary<string, List<BuildingBlockData>>();
                    foreach (string jsonFilePath in playerBaseFiles)
                    {
                        string baseName = Path.GetFileNameWithoutExtension(jsonFilePath);
                        if (File.Exists(jsonFilePath))
                        {
                            var jsonData = File.ReadAllText(jsonFilePath);
                            var buildingList = JsonConvert.DeserializeObject<List<BuildingBlockData>>(jsonData);
                            playerBases[baseName] = buildingList;
                        }
                    }

                    if (playerBases.Count > 0)
                    {
                        string closestZoneID = FindClosestZone(player);
                        if (string.IsNullOrEmpty(closestZoneID))
                        {
                            SendReply(player, "You should claim a zone before viewing all bases!");
                            return;
                        }

                        CreateBaseListMenu(player, playerBases, null);
                    }
                    else
                    {
                        SendReply(player, "You don't have any saved bases.");
                    }
                }else if (_config.base_system == "copypaste"){
                    var files = Interface.Oxide.DataFileSystem.GetFiles("copypaste/");
                    var fileList = new List<string>();

                    string closestZoneID = FindClosestZone(player);
                    if (string.IsNullOrEmpty(closestZoneID)){
                        SendReply(player, "You should claim a zone before view all bases!");
                        return;
                    }

                    foreach (var file in files)
                    {
                        string regex = @"/(.*?)_";
                        Match match = Regex.Match(file, regex);

                        if (match.Success){
                            string useridfromfile = match.Groups[1].Value;

                            if (player.userID.ToString() == useridfromfile)
                            {
                                var strFileParts = file.Split('/');
                                var removejson = strFileParts[strFileParts.Length - 1].Replace(".json", "");
                                var filenames = removejson.Replace(player.userID.ToString(), "");
                                var filenames2 = filenames.Replace("_", "");
                                fileList.Add(filenames2);
                            }
                        }
                    }

                    CreateBaseListMenu(player, null, fileList);
                }
            }
        }

        [ConsoleCommand("playermenu.closebaselist")]
        private void CloseBaseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                CuiHelper.DestroyUi(player, "BaseListMenu");
            }
        }

        [ConsoleCommand("playermenu.loadbase")]
        private void LoadBaseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                if (arg.HasArgs(1))
                {
                    string baseName = arg.Args[0];
                    player.SendConsoleCommand("chat.say", $"/load {baseName}");
                }
                else
                {
                    SendReply(player, "Syntax: /loadbase <base_name>");
                }
            }
        }

        private void CreateBaseListMenu(BasePlayer player, Dictionary<string, List<BuildingBlockData>> playerBases = null, List<string> fileList = null)
        {
            CuiElementContainer container = new CuiElementContainer();
            string panelName = "BaseListMenu";

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.7",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.1",
                    AnchorMax = "0.9 0.9"
                },
                CursorEnabled = true
            }, "Overlay", panelName);


            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "Base Selection",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.9",
                    AnchorMax = "0.9 1.0"
                }
            }, panelName);

            float yOffset = 0.85f;
            float xOffset = 0.1f;
            float buttonHeight = 0.08f;
            float buttonWidth = 0.16f;
            float spacing = 0.02f;
            int buttonsPerRow = 4;
            int buttonCount = 0;


            if (playerBases != null)
            {
                foreach (var baseName in playerBases.Keys)
                {
                    
                    if (buttonCount >= buttonsPerRow)
                    {
                        yOffset -= buttonHeight + spacing;
                        xOffset = 0.1f; 
                        buttonCount = 0;
                    }


                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"playermenu.loadbase {baseName}",
                            Color = "0.2 0.7 0.2 1",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{xOffset} {yOffset - buttonHeight}",
                            AnchorMax = $"{xOffset + buttonWidth} {yOffset}"
                        },
                        Text =
                        {
                            Text = baseName,
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, panelName);

                    xOffset += buttonWidth + spacing;
                    buttonCount++;
                }
            }

            if (fileList != null){
                foreach (var baseName in fileList)
                {
                    string new_base_name = player.userID + "_" + baseName;
                    if (buttonCount >= buttonsPerRow)
                    {
                        yOffset -= buttonHeight + spacing;
                        xOffset = 0.1f; 
                        buttonCount = 0;
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"playermenu.loadbase {new_base_name}",
                            Color = "0.2 0.7 0.2 1",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{xOffset} {yOffset - buttonHeight}",
                            AnchorMax = $"{xOffset + buttonWidth} {yOffset}"
                        },
                        Text =
                        {
                            Text = baseName,
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, panelName);

                    xOffset += buttonWidth + spacing;
                    buttonCount++;
                }
            }

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "playermenu.closebaselist",
                    Color = "0.8 0.2 0.2 1",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                },
                RectTransform =
                {
                    AnchorMin = "0.9 0.9",
                    AnchorMax = "0.95 0.95"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter
                }
            }, panelName);

            CuiHelper.DestroyUi(player, panelName);
            CuiHelper.AddUi(player, container);
        }

        private void SaveBase(BasePlayer player, string baseName)
        {
            List<BuildingBlockData> foundationData = new List<BuildingBlockData>();
            var saveData = new List<BuildingBlockData>();

            bool containsBlocks = false;

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {
                SendReply(player, "You should claim an area before saving any base!");
                return;
            }

            ulong playerID = player.userID;

            string playerDirPath = Path.Combine(dataFilePath, playerID.ToString());
            if (!Directory.Exists(playerDirPath))
            {
                Directory.CreateDirectory(playerDirPath);
            }

            foreach (var entity in BaseEntity.serverEntities)
            {
                var buildingBlock = entity as BuildingBlock;

                if (buildingBlock != null && buildingBlock.OwnerID == playerID)
                {
                    var distance = Vector3.Distance(Zone_Center_Pos, buildingBlock.transform.position);

                    if (distance <= getplayerradius(player))
                    {
                        var buildId = buildingBlock.net.ID;
                        var blockData = new BuildingBlockData
                        {
                            buildId = buildId,
                            skinId = buildingBlock.skinID,
                            Grade = buildingBlock.grade,
                            PrefabName = buildingBlock.PrefabName,
                            Position = buildingBlock.transform.position,
                            Rotation = buildingBlock.transform.rotation.eulerAngles
                        };
                        if (IsFoundationPrefab(buildingBlock.PrefabName))
                        {
                            foundationData.Add(blockData);
                        }
                        else
                        {
                            saveData.Add(blockData);
                        }
                        containsBlocks = true;
                    }
                }
            }

            foundationData.Sort((a, b) => Vector3.Distance(Zone_Center_Pos, a.Position).CompareTo(Vector3.Distance(Zone_Center_Pos, b.Position)));

           // foundationData.Sort((a, b) => Vector3.Distance(Zone_Center_Pos, a.Position).CompareTo(Vector3.Distance(Zone_Center_Pos, b.Position));

            saveData.InsertRange(0, foundationData);

            foreach (var entity in BaseEntity.serverEntities)
            {
                var buildingBlock = entity as BuildingBlock;
                var baseCombatEntity = entity as BaseCombatEntity;

                if (buildingBlock == null && baseCombatEntity != null && baseCombatEntity.OwnerID == playerID)
                {
                    var distance = Vector3.Distance(Zone_Center_Pos, baseCombatEntity.transform.position);
                    if (distance <= getplayerradius(player))
                    {
                        var buildId = baseCombatEntity.net.ID;
                        var blockData = new BuildingBlockData
                        {
                            buildId = buildId,
                            skinId = baseCombatEntity.skinID,
                            Grade = BuildingGrade.Enum.None,
                            PrefabName = entity.PrefabName,
                            Position = entity.transform.position,
                            Rotation = entity.transform.rotation.eulerAngles
                        };
                        saveData.Add(blockData);
                        containsBlocks = true;
                    }
                }
            }

            if (containsBlocks)
            {
                string userDirPath = Path.Combine(playerDirPath);
                if (!Directory.Exists(userDirPath))
                {
                    Directory.CreateDirectory(userDirPath);
                }

                string saveFilePath = Path.Combine(userDirPath, $"{baseName}.json");
                string jsonData = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(saveFilePath, jsonData);

                SendReply(player, $"Your base '{baseName}' has been saved.");
                currentBaseName[player.userID] = baseName;
            }
            else
            {
                SendReply(player, "Your base does not contain any building blocks.");
            }
        }

        private bool IsFoundationPrefab(string prefabName)
        {
            return prefabName.Contains("foundation");
        }

        public List<StabilityEntity> EntStable = Facepunch.Pool.GetList<StabilityEntity>();

        private void LoadBaseCp(ulong playerId, string baseName, BasePlayer player, Vector3 position)
        {
            RaycastHit hit;
            float distanceToGround = 0.0f;

            if (Physics.Raycast(position, Vector3.down, out hit, LayerMask.GetMask("Water", "Terrain", "World", "Default")))
            {
                distanceToGround = hit.distance;
            }

            var options = new List<string>{ "deployables", "true", "autoheight", "true" };
            float rotation = 15f;
            var status = CopyPaste.Call("TryPasteFromVector3", position, rotation, baseName, options.ToArray());

            if(status is string)
            {
                SendReply(player, "There was an error trying to load your base");
                return;
            }

            var strFileParts = baseName.Split('/');
            var removejson = strFileParts[strFileParts.Length - 1].Replace(".json", "");
            var filenames = removejson.Replace(player.userID.ToString(), "");
            var basename1 = filenames.Replace("_", "");

            currentBaseName[player.userID] = baseName;
            SendReply(player, $"Base {basename1} successfully loaded!");
        }

        private void LoadBase(ulong playerId, string baseName, BasePlayer player)
        {
            string playerDirPath = Path.Combine(dataFilePath, playerId.ToString());

            if (!Directory.Exists(playerDirPath))
            {
                SendReply(player, $"{playerDirPath}");
                SendReply(player, $"There is no saved base with the name '{baseName}' for your ID.");
                return;
            }

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero)
            {
                SendReply(player, "You should claim an area before loading any base!");
                return;
            }

            RaycastHit hit;
            float distanceToGround = 0.0f;
            Vector3 raycastz = Zone_Center_Pos;

            var Layersz = LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World");

            if (Physics.Raycast(raycastz, Vector3.down, out hit, Layersz))
            {
                distanceToGround = hit.distance;
            }

            distanceToGround -= 0.3f;
            Zone_Center_Pos.y -= distanceToGround;

            SetLoadingBaseStatus(player, true);

            string jsonFilePath = Path.Combine(playerDirPath, $"{baseName}.json");

            if (!File.Exists(jsonFilePath))
            {
                SendReply(player, $"There is no saved base data for '{baseName}' for your ID.");
                return;
            }

            var jsonData = File.ReadAllText(jsonFilePath);
            var buildingList = JsonConvert.DeserializeObject<List<BuildingBlockData>>(jsonData);
            var spawnedEntities = new List<BaseEntity>();

            int currentIndex = 0;

            Action<int> spawnNextEntity = null;
            spawnNextEntity = (index) =>
            {
                if (index >= buildingList.Count)
                {
                    foreach (var entity in EntStable)
                    {
                        entity.grounded = false;
                        entity.InitializeSupports();
                        entity.UpdateStability();
                    }
                    currentBaseName[player.userID] = baseName;
                    PrintToChat(player, $"Base '{baseName}' loaded successfully!");
                    PrintToChat(player, $"Total Entities: {spawnedEntities.Count}!");
                    SetLoadingBaseStatus(player, false);
                    DetectBuildingStructures(playerId);
                    return;
                }

                var buildingData = buildingList[index];
                var prefabName = buildingData.PrefabName;
                var positionOffset = buildingData.Position - buildingList[0].Position;
                var rotation = Quaternion.Euler(buildingData.Rotation);

                BaseEntity buildingEntity = null;

                RaycastHit terrainHit;
                Vector3 spawnPosition = Zone_Center_Pos + positionOffset;

                spawnPosition.y += 0.3f;

                try
                {
                    buildingEntity = GameManager.server.CreateEntity(prefabName, spawnPosition, rotation, true);
                }
                catch (Exception ex)
                {
                    PrintToChat(player, $"Error creating entity: {ex.Message}");
                }

                if (buildingEntity != null)
                {
                    buildingEntity.gameObject.SetActive(true);

                    BuildingBlock buildingBlock = null;
                    if (buildingEntity is BuildingBlock)
                    {
                        buildingBlock = buildingEntity as BuildingBlock;
                        buildingBlock.AttachToBuilding(BuildingManager.server.NewBuildingID());
                        buildingBlock.grade = buildingData.Grade;

                        buildingBlock.transform.rotation = rotation;
                        buildingBlock.gameObject.SetActive(true);

                        buildingBlock.ResetUpkeepTime();
                    }

                    var stabilityEntity = buildingEntity as StabilityEntity;

                    if (stabilityEntity != null)
                    {
                        if (!stabilityEntity.grounded)
                        {
                            stabilityEntity.grounded = true;
                            EntStable.Add(stabilityEntity);
                        }
                    }

                    buildingEntity.skinID = buildingData.skinId;
                    buildingEntity.OwnerID = playerId;

                    buildingEntity.Spawn();
                    spawnedEntities.Add(buildingEntity);
                    buildingEntity.UpdateNetworkGroup();
                    buildingEntity.SendNetworkUpdateImmediate();

                    var cupboard = buildingEntity as BuildingPrivlidge;

                    if (cupboard != null)
                    {
                        var tcid = BuildingManager.server.NewBuildingID();
                        cupboard.AttachToBuilding(tcid);
                        cupboard.UpdateNetworkGroup();
                        cupboard.SendNetworkUpdate();
                    }

                    buildingEntity.SendNetworkUpdate();
                    timer.Once(_config.spawn_timer, () =>
                    {
                        spawnNextEntity(index + 1);
                    });
                }
                else
                {
                    PrintToChat(player, $"Failed to create entity for prefab '{prefabName}'");
                }
            };

            spawnNextEntity(currentIndex);
        }

        private void DetectBuildingStructures(ulong playerId)
        {
            var buildingEntities = BaseNetworkable.serverEntities.OfType<BuildingBlock>();

            foreach (var buildingEntity in buildingEntities)
            {
                if (buildingEntity.OwnerID == playerId)
                {
                    var structureHealth = GetBuildingGradeHealth(buildingEntity.grade);
                    buildingEntity.health = structureHealth;
                }
            }
        }

        private float GetBuildingGradeHealth(BuildingGrade.Enum grade)
        {
            switch (grade)
            {
                case BuildingGrade.Enum.Twigs:
                    return 10f;
                case BuildingGrade.Enum.Wood:
                    return 250f;
                case BuildingGrade.Enum.Stone:
                    return 500f;
                case BuildingGrade.Enum.Metal:
                    return 1000f;
                case BuildingGrade.Enum.TopTier:
                    return 2000f;
                default:
                    return 0f;
            }
        }

        private void ToggleMenu(BasePlayer player)
        {
            if (isMenuOpen)
            {
                DestroyMenu(player);
                isMenuOpen = false;
            }
            else
            {
                CreateMenu(player);
                isMenuOpen = true;
            }
        }

        private void ChatMenu(BasePlayer player, string command, string[] args)
        {
            if (isMenuOpen)
            {
                DestroyMenu(player);
                isMenuOpen = false;
            }
            else
            {
                CreateMenu(player);
                isMenuOpen = true;
            }
        }

        private string btncolor(bool colorrtn){
            return colorrtn ? "0.47 0.63 0.41 1" : "0.97 0.43 0.4 1";
        }

        private void CreateMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.6" },
                RectTransform ={ AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            },"Overlay","PlayerMenu");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.closemenu", Color = "0.47 0.63 1 0.9" },
                Text = { Text = "X", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "586.424 309.13", OffsetMax = "635.452 356.328" }
            },"PlayerMenu","CloseMenu");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.47 0.63 1 0.9" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255.089 249.694", OffsetMax = "263.206 322.372" }
            },"Overlay","CreativeTitle");

            container.Add(new CuiElement
            {
                Name = "Text",
                Parent = "CreativeTitle",
                Components = {
                    new CuiTextComponent { Text = _config.servername, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-388.72 -33.355", OffsetMax = "388.72 36.813" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.5" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-573.295 -226.396", OffsetMax = "559.856 173.243" }
            },"Overlay","CreativeMenu");

            var fly_text = GetPlayerNoClipStatus(player) ? "FLY (ON)" : "FLY (OFF)";
            var infammo_text = GetPlayerInfAmmoStatus(player) ? "INF AMMO (ON)" : "INF AMMO (OFF)";

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.fly", Color = btncolor(GetPlayerNoClipStatus(player)) },
                Text = { Text = fly_text, Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-519.883 74.506", OffsetMax = "-319.883 124.506" }
            },"CreativeMenu","FLY");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.infammo", Color = btncolor(playerInfAmmo[player.userID]) },
                Text = { Text = infammo_text, Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310.024 74.56", OffsetMax = "-110.024 124.56" }
            },"CreativeMenu","INFAMMO");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.viewbases", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "VIEW MY BASES", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.024 74.506", OffsetMax = "99.976 124.506" }
            },"CreativeMenu","ViewBases");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.gtfo", Color = btncolor(GetGTFOStatus(player)) },
                Text = { Text = "GTFO MODE", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "110.096 74.506", OffsetMax = "310.096 124.506" }
            },"CreativeMenu","GTFO");

            var claim_txt = !GetZoneOwned(player) ? "CLAIM ZONE" : "UNCLAIM ZONE";

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.claimzone", Color = btncolor(!GetZoneOwned(player)) },
                Text = { Text = claim_txt, Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "320.276 74.506", OffsetMax = "520.276 124.506" }
            },"CreativeMenu","ClaimUnclaim");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.skins", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "SKINS", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-519.88 -2.84", OffsetMax = "-319.88 47.16" }
            },"CreativeMenu","Sins");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.undobase", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "CLEAR BASE", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310.02 -2.84", OffsetMax = "-110.02 47.16" }
            },"CreativeMenu","ClearBase");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.undotofoundation", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "CLEAR TO FOUNDATIONS", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.024 -2.84", OffsetMax = "99.976 47.16" }
            },"CreativeMenu","ClearToFoundation");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.basecost", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "BUILDING COST", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "110.1 -2.84", OffsetMax = "310.1 47.16" }
            },"CreativeMenu","BaseCost");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.infres", Color = "0.47 0.63 0.41 1" },
                Text = { Text = "INFINITE RESOURCES", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "320.28 -2.84", OffsetMax = "520.28 47.16" }
            },"CreativeMenu","InfiniteResources");

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.stability", Color = btncolor(playerStability[player.userID]) },
                Text = { Text = "STABILITY", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-519.88 -80.4", OffsetMax = "-319.88 -30.4" }
            },"CreativeMenu","Stability");

            if (_config.godmode_)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = "playermenu.godmode", Color = btncolor(playerGodMode[player.userID]) },
                    Text = { Text = "GOD MODE", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310.015 -80.241", OffsetMax = "-110.015 -30.241" }
                },"CreativeMenu","GodMode");
            }

            container.Add(new CuiButton
            {
                Button = { Command = "playermenu.entityenabled", Color = btncolor(playerAutomaticEntityEnable[player.userID]) },
                Text = { Text = "AUTO ENTITY POWER", Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.021 -80.189", OffsetMax = "99.979 -30.189" }
            },"CreativeMenu","AutoEntityBtn");

            if (_config.res_hud)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = "playermenu.resourcehud", Color = btncolor(playerResourceHud[player.userID]) },
                    Text = { Text = "RESOURCE HUD", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "110.1 -80.186", OffsetMax = "310.1 -30.186" }
                },"CreativeMenu","DisableHud");
            }

           
            CuiHelper.DestroyUi(player, "PlayerMenu");
            CuiHelper.DestroyUi(player, "CreativeTitle");
            CuiHelper.DestroyUi(player, "CreativeMenu");
            CuiHelper.AddUi(player, container);
        }

        private void DestroyMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PlayerMenu");
            CuiHelper.DestroyUi(player, "CreativeTitle");
            CuiHelper.DestroyUi(player, "CreativeMenu");

            isMenuOpen = false;
        }

        private void UpdateMenu(BasePlayer player){
            CreateMenu(player);
        }

        [ConsoleCommand("playermenu.closemenu")]
        private void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                DestroyMenu(player);
            }
        }

        private bool hasPerms(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "creative.all"))
                return true;

            if (permission.UserHasPermission(player.UserIDString, "creative.vip"))
                return true;

            if (permission.UserHasPermission(player.UserIDString, "creative.admin"))
                return true;

            return false;
        }

        [ConsoleCommand("playermenu.fly")]
        private void OnFlyCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.fly") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            player.SendConsoleCommand("noclip");
            SetPlayerNoClipStatus(player);
            UpdateMenu(player);
        }

        [ConsoleCommand("playermenu.infammo")]
        private void OnInfAmmoCommand(ConsoleSystem.Arg arg)
        {

            var player = arg.Player();
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.infammo") && !hasPerms(player))
                {
                    SendReply(player, "You do not have permission to use this command.");
                    return;
                }

                SetPlayerInfAmmoStatus(player);
                UpdateMenu(player);
            }
        }

        [ConsoleCommand("playermenu.gtfo")]
        private void OnGtfoCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                ChangeGTFOStatus(player);
                UpdateMenu(player);
            }
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            var playerinf = GetPlayerInfAmmoStatus(player);
            var heldEntity = projectile.GetItem();

            if (!playerinf || heldEntity == null)
                return;

            heldEntity.condition = heldEntity.info.condition.max;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            var playerinf = GetPlayerInfAmmoStatus(player);
            var heldEntity = player.GetActiveItem();
            var explosive = entity as TimedExplosive;

            if (heldEntity == null)
                return;

            var weapon = heldEntity.GetHeldEntity() as BaseProjectile;

            if (weapon == null)
                return;

            Vector3 Zone_Center_Pos = GetZoneCenter(player);

            if (Zone_Center_Pos == Vector3.zero || !GetGTFOStatus(player)){
                if (explosive != null)
                    if (!_config.allow_exp)
                        explosive.Kill();
               
                return;
            }

            if (!playerinf)
                return;

            heldEntity.condition = heldEntity.info.condition.max;
            weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
            weapon.SendNetworkUpdateImmediate();
        }

        private int currentPage = 0;

        private void ChatInfoCommand(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, "InfoPanelMenu");
            ShowInfoHelpGUI(player);
        }

        private void ShowInfoHelpGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            int numPages = Mathf.CeilToInt((float)_config.InfoTextLines.Count / 10);

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.8",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.1",
                    AnchorMax = "0.9 0.9"
                },
                CursorEnabled = true
            }, "Overlay", "InfoPanelMenu");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "infohelp.close",
                    Color = "0.7 0.2 0.2 1",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                },
                RectTransform =
                {
                    AnchorMin = "0.9 0.9",
                    AnchorMax = "0.95 0.95"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter
                }
            }, "InfoPanelMenu");

            string serverName = _config.servername;
            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0.1 0.92",
                    AnchorMax = "0.9 0.97"
                },
                Text =
                {
                    Text = serverName,
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter
                }
            }, "InfoPanelMenu");

            int startIndex = currentPage * 10;
            int endIndex = Mathf.Min(startIndex + 10, _config.InfoTextLines.Count);
            for (int i = startIndex; i < endIndex; i++)
            {
                container.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = $"0.1 {0.8 - (i - startIndex) * 0.08}",
                        AnchorMax = $"0.9 {0.88 - (i - startIndex) * 0.08}"
                    },
                    Text =
                    {
                        Text = _config.InfoTextLines[i],
                        FontSize = 18,
                        Align = TextAnchor.MiddleLeft
                    }
                }, "InfoPanelMenu");
            }

            if (numPages > 1)
            {
                if (currentPage < numPages - 1)
                {
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infohelp.nextpage",
                            Color = "0.2 0.6 0.2 1",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.8 0.05",
                            AnchorMax = "0.9 0.1"
                        },
                        Text =
                        {
                            Text = "Next Page",
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, "InfoPanelMenu");
                }

                if (currentPage > 0)
                {
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infohelp.prevpage",
                            Color = "0.2 0.6 0.2 1",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.1 0.05",
                            AnchorMax = "0.2 0.1"
                        },
                        Text =
                        {
                            Text = "Previous Page",
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, "InfoPanelMenu");
                }
            }

            CuiHelper.DestroyUi(player, "InfoPanelMenu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("infohelp.close")]
        private void ConsoleCloseCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "InfoPanelMenu");
        }

        [ConsoleCommand("infohelp.nextpage")]
        private void ConsoleNextPageCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            currentPage++;

            ShowInfoHelpGUI(player);
        }

        [ConsoleCommand("infohelp.prevpage")]
        private void ConsolePrevPageCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            if (currentPage > 0)
            {
                currentPage--;
            }

            ShowInfoHelpGUI(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null)
                return;
                
            GrantResources(player);
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!player)return null;
            player.inventory.Strip();
            return null;
        }

        private void GrantResources(BasePlayer player)
        {
            if (player == null)
                return;

            ulong playerId = player.userID;

            player.inventory.containerMain.capacity = 26 + (resourceItems.Count * 2);

            foreach (string resourceName in resourceItems)
            {
                int itemId = ItemManager.itemDictionary
                    .Where(kvp => kvp.Value.shortname.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault();

                if (itemId != 0)
                {
                    if (!PlayerHasItem(player, itemId))
                    {
                        Item resourceItem = ItemManager.CreateByItemID(itemId, 99999999);
                        if (resourceItem != null)
                        {
                            player.GiveItem(resourceItem);
                            for (var i = 0; i < resourceItems.Count; i++)
                            {
                                resourceItem.MoveToContainer(player.inventory.containerMain, 25 + i, false);
                            }
                        }
                    }
                }
            }
        }

        private bool PlayerHasItem(BasePlayer player, int itemId)
        {
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.itemid == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        private object OnItemCraft(ItemCraftTask task, BasePlayer owner)
        {
            ulong skin = ItemDefinition.FindSkin(task.blueprint.targetItem.itemid, task.skinID);
            Item item = null;
            try
            {
                item = ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, task.amount * task.blueprint.amountToCreate, skin);
            }
            catch (Exception e)
            {
                PrintError($"Exception creating item! targetItem: {task.blueprint.targetItem}-{task.amount * task.blueprint.amountToCreate}-{skin}; Exception: {e}");
            }

            if (item == null)
                return false;

            ItemContainer itemContainer = owner.inventory.crafting.containers.First<ItemContainer>();
            owner.inventory.GiveItem(item);
            owner.Command("note.inv", new object[]{item.info.itemid, task.amount * task.blueprint.amountToCreate});

            return true;
        }

        private void BaseCostCommand(BasePlayer player, string command, string[] args)
        {
            Vector3 playerPosition = player.transform.position;

            Dictionary<int, int> totalCost = new Dictionary<int, int>();

            foreach (var buildingBlock in BaseNetworkable.serverEntities.OfType<BuildingBlock>())
            {
                if (Vector3.Distance(buildingBlock.transform.position, playerPosition) <= getplayerradius(player))
                {
                    var buildingGrade = buildingBlock.grade;

                    foreach (var grade in buildingBlock.blockDefinition.grades)
                    {
                        if (grade.gradeBase.type == buildingGrade)
                        {
                            var costToBuild = grade.CostToBuild();

                            foreach (var itemAmount in costToBuild)
                            {
                                if (!totalCost.ContainsKey(itemAmount.itemid))
                                {
                                    totalCost[itemAmount.itemid] = 0;
                                }
                                totalCost[itemAmount.itemid] += (int)itemAmount.amount;
                            }

                            break;
                        }
                    }
                }
            }

            if (totalCost.Count > 0)
            {
                SendReply(player, "Building cost:");
                foreach (var costEntry in totalCost)
                {
                    var itemDefinition = ItemManager.FindItemDefinition(costEntry.Key);
                    if (itemDefinition != null)
                    {
                        var itemName = itemDefinition.displayName.translated;
                        SendReply(player, $"{itemName}: {costEntry.Value}");
                    }
                }
            }
            else
            {
                SendReply(player, "No building blocks found within the radius.");
            }
        }

        private bool canplayerbuild(BasePlayer player, ulong ownerUID){
            if (!player || ownerUID == null)
                return false;

            if (player.Team != null)
            {
                if (player.Team.members.Contains(ownerUID))
                {
                    return true;
                }
            }
            else
            {
                if (player.userID == ownerUID)
                {
                    return true;
                }
            }
            return false;
        }

        object CanBuild(Planner plan, Construction prefab){
            BasePlayer player = plan.GetOwnerPlayer();
            if (!player)
                return false;

            string closestZoneID = FindClosestZone(player);
            if (closestZoneID == null)
            {
                SendReply(player, "You should use /claim before start building or be in a team with claimed zone!");
                return false;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (canplayerbuild(player, zoneTrigger.GetOwner()))
                {
                    return null;
                }else{
                    SendReply(player, "You should use /claim or be in zone owner team before start building or be in a team with claimed zone!");
                    return false;
                }
            }

            foreach (var kvp in activeZones)
            {
                var existingZone = kvp.Value;
                Vector3 Zone_Center_Pos = existingZone.GetZoneCenterZ();

                if (Zone_Center_Pos == Vector3.zero)
                    return false;

                if (existingZone.IsPlayerAllowed(player))
                {
                    if (Vector3.Distance(Zone_Center_Pos, plan.transform.position) > getplayerradius(player))
                    {
                        SendReply(player, "You can't build outside your area!");
                        return false;
                    }

                    return null;
                }
            }

            SendReply(player, "You should use /claim before start building or be in a team with claimed zone!");
            return false;
        }

        private void UpgradeCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.ChatMessage("Usage: /up <grade level>");
                return;
            }

            if (args.Length == 1)
            {
                int level;
                if (int.TryParse(args[0], out level))
                {
                    if (level >= 0 && level <= 8)
                    {
                        switch(level){
                            case 5:
                                upgradeLevels[player.userID] = 2;
                                skinSelected[player.userID] = 10220;
                            break;

                            case 6:
                                upgradeLevels[player.userID] = 2;
                                skinSelected[player.userID] = 10223;
                            break;

                            case 7:
                                upgradeLevels[player.userID] = 2;
                                skinSelected[player.userID] = 10225;
                            break;
                            
                            case 8:
                                upgradeLevels[player.userID] = 3;
                                skinSelected[player.userID] = 10221;
                            break;

                            default:
                                skinSelected[player.userID] = 0;
                                upgradeLevels[player.userID] = level;
                            break;
                        }
                    }else{
                        SendReply(player,"Upgrade level should be between 0 and 8");
                    }
                }
            }
        }

        private void FlyCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.fly") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            if (player != null)
            {
                player.SendConsoleCommand("noclip", new object[] { });
                SetPlayerNoClipStatus(player);
            }
        }

        [ConsoleCommand("inventory.giveid")]
        void GiveIdCommand(BasePlayer player, string command, string[] args)
        {
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            string command = arg.cmd.Name;

            BasePlayer player = arg.Player();
            if (!player) return null;

            if (command.Equals("giveid") || command.Equals("givearm"))
            {
                Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                if (item == null) return false;

                item.amount = arg.GetInt(1, 1);
                if (!player.inventory.GiveItem(item, null))
                {
                    item.Remove(0f);
                    return false;
                }
                player.Command("note.inv", new object[] { item.info.itemid, item.amount });

                return false;
            }

            if (arg.cmd.Name.ToLower() == "lighttoggle")
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.fly") && !hasPerms(player))
                    return null;

                player.SendConsoleCommand("noclip", new object[] { });
                SetPlayerNoClipStatus(player);

                return false;
            }
            return null;
        }

        [ConsoleCommand("playermenu.godmode")]
        private void godmodecommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.godmode") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            playerGodMode[player.userID] = !playerGodMode[player.userID];

            UpdateMenu(player);
        }

        [ConsoleCommand("playermenu.resourcehud")]
        private void resourcehudcommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            playerResourceHud[player.userID] = !playerResourceHud[player.userID];

            if (!playerResourceHud[player.userID])
                DestroyButtons(player);
            else
            {
                var activeItem = player.GetActiveItem();
                if (activeItem != null && activeItem.info.shortname == "building.planner")
                    CreateButtons(player);
            }

            UpdateMenu(player);
        }

        [ConsoleCommand("playermenu.entityenabled")]
        private void entitydisabled(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.autoenablentity") && !hasPerms(player))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            playerAutomaticEntityEnable[player.userID] = !playerAutomaticEntityEnable[player.userID];

            UpdateMenu(player);
        }

        private object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || !player.userID.IsSteamId())
                return null;

            if (!permission.UserHasPermission(player.UserIDString, "creative.godmode") && !hasPerms(player))
                return null;

            if (playerGodMode.ContainsKey(player.userID) && playerGodMode[player.userID])
            {
                info.damageTypes = new DamageTypeList();
                return true;
            }

            return null;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null)
                return;

            if (entity is BuildingBlock)
            {
                BuildingBlock buildingBlock = entity.GetComponent<BuildingBlock>();

                if (buildingBlock != null){
                    BasePlayer attacker = hitInfo.Initiator as BasePlayer;
                    if (attacker != null){
                        if (buildingBlock.OwnerID != attacker.userID)
                        {
                            if (attacker.Team == null){
                                hitInfo.damageTypes.ScaleAll(0f);
                            }else{
                                if (!attacker.Team.members.Contains(buildingBlock.OwnerID))
                                {
                                    hitInfo.damageTypes.ScaleAll(0f);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                string dataJson = File.ReadAllText(dataFilePath);
                savedBases = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<string, List<BuildingBlockData>>>>(dataJson);
            }
        }

        private void SaveData()
        {
            string dataJson = JsonConvert.SerializeObject(savedBases, Formatting.Indented);
            File.WriteAllText(dataFilePath, dataJson);
        }

        private class BuildingBlockData
        {
            public NetworkableId buildId {get; set;}
            public ulong skinId {get; set;}
            public BuildingGrade.Enum Grade { get; set; }
            public string PrefabName { get; set; }
            public Vector3 Position { get; set; }
            public Vector3 Rotation { get; set; }
        }

        public class Spinner : MonoBehaviour
        {
            public float spinDuration;
            public Vector3 spinDirection;

            private float startTime;

            private void Awake()
            {
                startTime = Time.time;
            }

            private void Update()
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime >= spinDuration)
                    StopSpin();
                else
                    transform.Rotate(spinDirection * Time.deltaTime);        
            }

            public void StartSpin()
            {
                enabled = true;
            }

            public void StopSpin()
            {
                Destroy(this);
            }
        }

        public class ZoneTrigger : MonoBehaviour
        {
            private SphereCollider innerCollider;
            private List<SphereEntity> innerSpheres = Facepunch.Pool.GetList<SphereEntity>();
            public string zoneID;
            public ulong ownerUID;
            public Vector3 CenterZone;
            public bool GTFO = true;

            private bool checkingPlayers = false;
            private float checkInterval = 1f;

            void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                enabled = false;
            }

            void Update()
            {
                if (ownerUID == 1)
                    return;
                    
                if (!checkingPlayers)
                {
                    StartCoroutine(CheckPlayersInsideZone());
                }
            }

            IEnumerator<WaitForSeconds> CheckPlayersInsideZone()
            {
                checkingPlayers = true;

                while (enabled)
                {
                    Collider[] colliders = Physics.OverlapSphere(transform.position, GetColliderRadius());

                    foreach (var collider in colliders)
                    {
                        BasePlayer player = collider.GetComponentInParent<BasePlayer>();
                        if (player != null && !IsPlayerAllowed(player))
                        {
                            if (GTFO){
                                TeleportPlayerOutsideZone(player);
                            }
                        }
                    }

                    yield return new WaitForSeconds(checkInterval);
                }

                checkingPlayers = false;
            }

            void OnDestroy() => DeleteCircle();

            void OnTriggerEnter(Collider col)
            {
                 if (ownerUID == 1)
                    return;

                BaseEntity entity = col?.GetComponentInParent<BaseEntity>();

                if (entity != null && !entity.ToPlayer() && !Instance._config.allow_exp)
                {
                    entity.Kill();
                }

                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player == null)
                    return;

                if (!IsPlayerAllowed(player))
                {
                    if (GTFO){
                        TeleportPlayerOutsideZone(player);
                    }
                }
            }

            void OnTriggerExit(Collider col)
            {
                 if (ownerUID == 1)
                    return;
                    
                BaseEntity entity = col?.GetComponent<BaseProjectile>();

                if (entity != null){

                    if (!entity.ToPlayer() && !Instance._config.allow_exp)
                    {
                        entity.Kill();
                    }
                }

                var player = col?.GetComponentInParent<BasePlayer>();
                if (player == null) return;
                

                if (IsPlayerAllowed(player)){
                    if (GTFO)
                    {
                        TeleportPlayerInsideZone(player);
                    }
                }
            }

            private bool IsPlayerInsideZone(BasePlayer player)
            {
                float distance = Vector3.Distance(player.transform.position, transform.position);

                return distance < GetColliderRadius();
            }

            private void TeleportPlayerOutsideZone(BasePlayer player)
            {
                if (Instance.permission.UserHasPermission(player.UserIDString, "creative.admin"))
                    return;

                Vector3 teleportPosition = player.transform.position + (player.transform.forward * 100f);
                player.Teleport(teleportPosition);
                Instance.SendReply(player, $"[GTFO] You're not allowed to be in this zone!");
            }

            private void TeleportPlayerInsideZone(BasePlayer player)
            {
                if (Instance.permission.UserHasPermission(player.UserIDString, "creative.admin"))
                    return;

                player.Teleport(CenterZone);
                Instance.SendReply(player, $"[GTFO] You're not allowed to leave this zone");
            }

            public bool IsPlayerAllowed(BasePlayer player)
            {
                if (ownerUID == null)
                    return false;
                
                if (player.userID == ownerUID)
                {
                    return true;
                }

                if (player.Team != null)
                {
                    if (player.Team.members.Contains(ownerUID))
                    {
                        return true;
                    }
                }
                return false;
            }

            public BuildingPrivlidge GetClosestToolCupboard(Vector3 position)
            {
                float closestDistance = float.MaxValue;
                BuildingPrivlidge closestTC = null;

                foreach (var kvp in Instance.activeZones)
                {
                    var existingZone = kvp.Value;
                    float distance = Vector3.Distance(position, existingZone.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTC = existingZone.GetClosestToolCupboard();
                    }
                }

                return closestTC;
            }

            private BuildingPrivlidge GetClosestToolCupboard()
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, GetColliderRadius());

                foreach (var collider in colliders)
                {
                    BuildingPrivlidge tc = collider.GetComponentInParent<BuildingPrivlidge>();
                    if (tc != null)
                    {
                        return tc;
                    }
                }

                return null;
            }

            public void CreateBubble(Vector3 position, float initialRadius, string id, ulong ownerPlayer)
            {
                ownerUID = ownerPlayer;

                transform.position = position;
                transform.rotation = new Quaternion();

                for (int i = 0; i < 7; i++)
                {
                    var sphere = (SphereEntity)GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position, new Quaternion(), true);
                    sphere.currentRadius = initialRadius * 2;
                    sphere.lerpSpeed = 0;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    innerSpheres.Add(sphere);
                }

                var innerRB = innerSpheres[0].gameObject.AddComponent<Rigidbody>();
                innerRB.useGravity = false;
                innerRB.isKinematic = true;

                innerCollider = gameObject.AddComponent<SphereCollider>();
                innerCollider.transform.position = innerSpheres[0].transform.position;
                innerCollider.isTrigger = true;
                innerCollider.radius = initialRadius;

                gameObject.SetActive(true);
                enabled = true;
                zoneID = id;
            }

            public void DeleteCircle()
            {
                if (zoneID != null && Instance.activeZones.ContainsKey(zoneID))
                {
                    
                    foreach (SphereEntity sphere in innerSpheres)
                        sphere.Kill();

                    innerSpheres.Clear();
                    Instance.activeZones.Remove(zoneID);
                    Facepunch.Pool.FreeList(ref innerSpheres);
                }
            }

            public float GetColliderRadius()
            {
                return innerCollider != null ? innerCollider.radius : 0f;
            }

            public Vector3 GetZoneCenterZ()
            {
                return CenterZone;
            }

            public ulong GetOwner()
            {
                return ownerUID;
            }
        }

        private Configuration _config;
        private class Configuration
        {
            [JsonProperty(PropertyName = "Default User Building Zone Radius (Def: 60)")]
            public float default_zone_radius = 60f;

            [JsonProperty(PropertyName = "Vip Building Zone Radius (Def: 100)")]
            public float vip_zone_radius = 100f;

            [JsonProperty(PropertyName = "Disable player being kicked by terrain violation")]
            public bool disable_terrain_kick = true;

            [JsonProperty(PropertyName = "Disable structures decay (Def: true)")]
            public bool disable_decay = true;

            [JsonProperty(PropertyName = "Always day (12:00) (Def: true)")]
            public bool always_day = true;

            [JsonProperty(PropertyName = "Server Name")]
            public string servername = "SERVERNAME | CREATIVE | BUILDING | SANDBOX";

            [JsonProperty(PropertyName = "Information")]
            public List<string> InfoTextLines;

            [JsonProperty(PropertyName = "Enable Godmode")]
            public bool godmode_ = true;

            [JsonProperty(PropertyName = "Teleport player on save?")]
            public bool pl_tp = true;

            [JsonProperty(PropertyName = "Building Load Timer between structures (def 0.5f)")]
            public float spawn_timer = 0.5f;
            
            [JsonProperty(PropertyName = "Lobby position (player can not build here)")]
            public Vector3 lobby_position = Vector3.zero;

            [JsonProperty(PropertyName = "Load/Save base system (default / copypaste)")]
            public string base_system = "default";

            [JsonProperty(PropertyName = "Default players save base limit (def: 10)")]
            public int default_base_limit = 15;

            [JsonProperty(PropertyName = "VIP players save base limit (def: 30)")]
            public int vip_base_limit = 30;

            [JsonProperty(PropertyName = "Keep zone and building when player disconnect? (Def: false)")]
            public bool ondisconnect_keep_things = false;

            [JsonProperty(PropertyName = "Display MapMarker on zones")]
            public bool zone_mapmarkers = true;

            [JsonProperty(PropertyName = "Sphere Marker Divider value (def 0.0)")]
            public float divider_val = 0.0f;

            [JsonProperty(PropertyName = "Allow Explosives (def: false)")]
            public bool allow_exp = false;

            [JsonProperty(PropertyName = "Allow Resource hud (def: true)")]
            public bool res_hud = true;

            [JsonProperty(PropertyName = "Zone Claim Command")]
            public string claim_cmd = "claim";

            [JsonProperty(PropertyName = "Zone Un-Claim Command")]
            public string unclaim_cmd = "unclaim";

            [JsonProperty(PropertyName = "Clan Create Command")]
            public string create_cmd = "create";

            [JsonProperty(PropertyName = "Clan Invite Command")]
            public string invite_cmd = "invite";

            [JsonProperty(PropertyName = "Base Save Command")]
            public string save_cmd = "save";

            [JsonProperty(PropertyName = "Base Load Command")]
            public string load_cmd = "load";

            [JsonProperty(PropertyName = "Time Command")]
            public string time_cmd = "time";

            [JsonProperty(PropertyName = "Base List Command")]
            public string list_cmd = "list";

            [JsonProperty(PropertyName = "Base Undo Command")]
            public string undo_cmd = "base_undo";

            [JsonProperty(PropertyName = "Open Menu Command")]
            public string menu_cmd = "menu";

            [JsonProperty(PropertyName = "Open Info Menu Command")]
            public string info_cmd = "info";

            [JsonProperty(PropertyName = "Base Cost Command")]
            public string cost_cmd = "cost";

            [JsonProperty(PropertyName = "Build Grade Command")]
            public string upg_cmd = "up";

            [JsonProperty(PropertyName = "Noclip Command")]
            public string noclip_cmd = "fly";

            public static Configuration DefaultConfig()
            {
                return new Configuration()
                {
                    InfoTextLines = new List<string>()
                    {
                        "Type <color=red>/help</color> or <color=red>/info</color> to display this information menu again!",
                        "Type <color=red>/claim</color> to claim zone to build your base.",
                        "Type <color=red>/unclaim</color> to unclaim the current building zone.",
                        "Type <color=red>/save</color> <basename> to save your base.",
                        "Type <color=red>/load</color> <basename> to load your base.",
                        "Type <color=red>/base_undo</color> to undo last loaded base.",
                        "Type <color=red>/list</color> to display all your saved bases.",
                        "Type <color=red>/cost</color> to display the building cost of the base in the building zone.",
                        "Type <color=red>/skin</color> or click the skin button in the left corner of the screen to change the items skins.",
                        "Type <color=red>/invite <playername></color> to invite a player to your team.",
                        "Type <color=red>/accept</color> to accept the team invitation.",
                        "You can <color=green>upgrade</color> or <color=red>downgrade</color> building structures by clicking left or right mouse button.",
                        "Type <color=red>/bskin <grade level> <building sin> </color> to set the building skin to a paid DLC.",
                        "You can fly pressing the letter <color=red>F</color>.",
                        "You can delete or remove things using the letter <color=red>R</color>.",
                        "You can select the building grade and skin at the menu in the left corner of the screen.",
                        "Only the owner and their teammates can be inside the building zone."
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintError("Error reading config, please check!");

            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = Configuration.DefaultConfig();
            LoadPublishedCodes();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
    }
}