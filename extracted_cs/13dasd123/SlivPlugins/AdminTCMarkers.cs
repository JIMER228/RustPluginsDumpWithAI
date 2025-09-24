/*
*  < ----- End-User License Agreement ----->
*  
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of This Software without the Developer’s consent
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Developer: Vergbergler (omicron.vega@gmail.com)
*
*  Copyright © 2023 Vergbergler
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using ProtoBuf;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Rust;
using Facepunch;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ConVar;
using Facepunch.Rust;
using Network;
using Network.Visibility;
using Rust.Registry;
using System.IO;
using UnityEngine.Assertions;

#pragma warning disable 0649, 0169 // RaidProtection, Instance Warning

namespace Oxide.Plugins
{
    [Info("AdminTCMarkers:", "Vergbergler", "0.2.0")]
    [Description("Allows users with permission to see all the Tool Cupboards located on the map.")]
    public class AdminTCMarkers : RustPlugin
    {
        [PluginReference] Plugin RaidProtection;
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;

        #region DATA
        private const string GENERIC_MARKER = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const string VENDING_MARKER = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";
        private const string LIMITED_NET = "mapmarker.net.limited";

        // Current list of all tool cupboards in the game
        private Dictionary<string, MarkerData> TOOL_CUPBOARD_ENTITIES = new Dictionary<string, MarkerData>();
        private Dictionary<ulong, TimedPlayer> AUTHORIZED_PLAYERS = new Dictionary<ulong, TimedPlayer>();
        private Dictionary<ulong, TimedPlayer> TEMP_AUTHORIZED_PLAYERS = new Dictionary<ulong, TimedPlayer>();
        private Stack<Coroutine> _coroutines = new Stack<Coroutine>();
        private bool PLUGIN_ACTIVE = false;

        // Permissions
        private const string PERMISSION_USE = "admintcmarkers.use";

        // Prefab IDs
        private static uint TOOL_CUPBOARD_PREFAB_ID = 2476970476;

        // Configuration file settings
        private static Configuration ConfigSettings;

        // Admin TC Marker instance
        private static AdminTCMarkers _instance;

        private class MarkerData
        {
            public VendingMachineMapMarker vending_map_marker = null;
            public MapMarkerGenericRadius generic_map_marker = null;
            public BuildingPrivlidge building_privlidge = null;
            public MarkerData() { }
        }

        private class TimedPlayer
        {
            public BasePlayer authorized_player = null;
            public float time_remaining = 0f;
            public bool is_timed = false;
        }

        #endregion

        #region OXIDE_HOOKS

        void Init()
        {
            LoadConfig();

            // Add permissions
            permission.RegisterPermission(PERMISSION_USE, this);
        }

        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------------------------------------------------------------------\n" +
            "     Загрузка плагина...\n" +
            "     Этот плагин был скачан с канала Discord [Rust Plugin Sliv]\n" +
            "     Этот плагин исправлен Инкубом под заказ [Rust Plugin Sliv]\n" +
            "     DISCORD: https://discord.gg/pFgKw6Dyyq\n" +
            "     Приятного использования!\n" +
            "-----------------------------------------------------------------------------------------");
            OnPluginLoaded(this);
        }

        void RefreshMarkers()
        {
            OnPluginUnloaded(this);
            OnPluginLoaded(this);
        }

        void OnPluginLoaded(Plugin name)
        {
            if (name.Name != this.Name)
                return;

            if (TOOL_CUPBOARD_ENTITIES.Count > 0)
                Unload();

            var OnlinePlayers = BasePlayer.activePlayerList as ListHashSet<BasePlayer>;
            foreach (BasePlayer player in OnlinePlayers)
            {
                if (permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                {
                    if (AUTHORIZED_PLAYERS.ContainsKey(player.userID) == false)
                    {
                        if (TEMP_AUTHORIZED_PLAYERS.ContainsKey(player.userID))
                            AUTHORIZED_PLAYERS.Add(player.userID, CreateTimedPlayer(player, ConfigSettings.TimeToOwn, true));
                        else
                            AUTHORIZED_PLAYERS.Add(player.userID, CreateTimedPlayer(player, 0f, false));
                    }
                }
            }

            var list = UnityEngine.GameObject.FindObjectsOfType<BuildingPrivlidge>().ToList();
            foreach (BuildingPrivlidge bp in list)
            {
                if (bp == null)
                    continue;

                CreateMarker(bp);
            }

            if (ServerMgr.Instance == null)
                return;

            PLUGIN_ACTIVE = true;
            if (TEMP_AUTHORIZED_PLAYERS.Count > 0)
            {
                _coroutines.Push(ServerMgr.Instance.StartCoroutine(CheckTimeToOwn()));
            }
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkersSlow()));
        }

        private void Unload()
        {
            OnPluginUnloaded(this);
        }

        void OnPluginUnloaded(Plugin name)
        {
            if (name.Name != this.Name)
                return;

            if (_coroutines.Count > 0)
            {
                while (_coroutines.Count > 0)
                {
                    var co = _coroutines.Pop();
                    if (co == null) continue;
                    ServerMgr.Instance.StopCoroutine(co);
                }

                _coroutines.Clear();
            }

            int deleted_count = 0;
            foreach (KeyValuePair<string, MarkerData> entry in TOOL_CUPBOARD_ENTITIES)
            {
                if (entry.Value.generic_map_marker != null)
                {
                    entry.Value.generic_map_marker.alpha = 0.0f;
                    entry.Value.generic_map_marker.Kill();
                }

                if (entry.Value.vending_map_marker != null)
                {
                    entry.Value.vending_map_marker.markerShopName = "";
                    entry.Value.vending_map_marker.Kill();
                }

                entry.Value.building_privlidge = null;
                deleted_count++;
            }

            TOOL_CUPBOARD_ENTITIES.Clear();
            AUTHORIZED_PLAYERS.Clear();
            PLUGIN_ACTIVE = false;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                RefreshMarkers();
            }
        }

        void OnGroupPermissionGranted(string name, string perm)
        {
            if (perm == PERMISSION_USE)
                RefreshMarkers();
        }

        void OnGroupPermissionRevoked(string name, string perm)
        {
            if (perm == PERMISSION_USE)
                RefreshMarkers();
        }

        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (entity == null || player == null)
                return null;

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));
            return null;
        }

        object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player, bool immediate)
        {
            if (entity == null || player == null)
                return null;

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));
            return null;
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null)
                return;

            if (entity.prefabID != TOOL_CUPBOARD_PREFAB_ID)
                return;

            BuildingPrivlidge building_privlidge = (BuildingPrivlidge)entity;
            if (building_privlidge == null)
                return;

            UpdateExistingMarker(building_privlidge);
        }

        object OnConstructionPlace(BaseEntity entity, Construction component, Construction.Target constructionTarget, BasePlayer player)
        {
            if (entity == null || player == null || TOOL_CUPBOARD_ENTITIES == null)
                return null;

            if (entity.prefabID == TOOL_CUPBOARD_PREFAB_ID)
            {
                if (permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                {
                    BuildingPrivlidge building_privlidge = (BuildingPrivlidge)entity;
                    if (building_privlidge == null)
                        return null;

                    CreateMarker(building_privlidge);
                }
            }

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));

            return null;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null || TOOL_CUPBOARD_ENTITIES == null)
                return;

            if (entity.prefabID != TOOL_CUPBOARD_PREFAB_ID)
                return;

            BuildingPrivlidge building_privlidge = (BuildingPrivlidge)entity;
            if (building_privlidge == null)
                return;

            CreateMarker(building_privlidge);

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));

            return;
        }

        object OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || TOOL_CUPBOARD_ENTITIES == null)
                return null;

            DestroyMarker(entity.GetInstanceID().ToString());

            return null;
        }

        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null)
                return null;

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));

            return null;
        }

        object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null)
                return null;

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));

            return null;
        }

        object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null)
                return null;

            // Start a coroutine to wait 1 second then refresh all the markers
            _coroutines.Push(ServerMgr.Instance.StartCoroutine(CoroutineUpdateExistingMarkers()));

            return null;
        }

        bool CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (entity.name == LIMITED_NET)
            {
                BaseEntity base_entity = (BaseEntity)entity;
                if (base_entity != null)
                {
                    if (base_entity.prefabID == TOOL_CUPBOARD_PREFAB_ID)
                    {
                        if (AUTHORIZED_PLAYERS.ContainsKey(target.userID) == false)
                        {
                            if (target.IsConnected)
                            {
                                base_entity.DestroyOnClient(target.net.connection);
                                base_entity.DestroyShared();
                            }
                        }
                    }
                }
            }

            return true;
        }

        object CanNetworkTo(MapMarkerGenericRadius radius, BasePlayer target)
        {
            if (radius.name == LIMITED_NET)
            {
                if (AUTHORIZED_PLAYERS.ContainsKey(target.userID))
                {
                    if (permission.UserHasPermission(target.UserIDString, PERMISSION_USE) == false)
                        return false;

                    return true;
                }

                radius.DestroyOnClient(target.net.connection);
                radius.DestroyShared();
                return false;
            }
            return null;
        }

        object CanNetworkTo(VendingMachineMapMarker vmarker, BasePlayer target)
        {
            if (vmarker.name == LIMITED_NET)
            {
                if (AUTHORIZED_PLAYERS.ContainsKey(target.userID))
                {
                    if (permission.UserHasPermission(target.UserIDString, PERMISSION_USE) == false)
                        return false;

                    return true;
                }

                vmarker.DestroyOnClient(target.net.connection);
                vmarker.DestroyShared();
                return false;
            }
            return null;
        }

        #endregion

        #region CUSTOM_CODE

        [ChatCommand("atcm")]
        private void chatCommand(BasePlayer player, string command, string[] args)
        {
            if (args == null || args?.Length == 0)
            {
                rust.SendChatMessage(player, "AdminTCMarkers:", "Invalid Command", player.UserIDString);
                return;
            }

            if (args[0] == "purchase")
            {
                string purchase_currency_name = GetPurchaseCurrancyName(player);
                if (ConfigSettings.FreePurchase)
                {
                    rust.SendChatMessage(player, "TC Markers:", "Purchased for FREE!", player.UserIDString);
                    AddTempAuthorization(player);
                    return;
                }
                else
                {
                    if (ConfigSettings.CurrencyType == "Item" && PurchaseWithItem(player))
                    {
                        rust.SendChatMessage(player, "TC Markers:", "Purchased for " + ConfigSettings.CurrencyCost.ToString() + " " + purchase_currency_name, player.UserIDString);
                        AddTempAuthorization(player);
                        return;
                    }
                    else if (ConfigSettings.CurrencyType != "Item" && PurchaseWithCurrency(player))
                    {
                        rust.SendChatMessage(player, "TC Markers:", "Purchased for " + ConfigSettings.CurrencyCost.ToString() + " " + purchase_currency_name + ".", player.UserIDString);
                        AddTempAuthorization(player);
                        return;
                    }
                    else
                    {
                        if (ConfigSettings.CurrencyType == "ServerRewards" && ServerRewards == null)
                        {
                            return;
                        }
                        else if (ConfigSettings.CurrencyType == "Economics" && Economics == null)
                        {
                            return;
                        }

                        rust.SendChatMessage(player, "TC Markers:", "You need at least " + ConfigSettings.CurrencyCost.ToString() + " " + purchase_currency_name + " to purchase!", player.UserIDString);
                    }
                }
            }

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                rust.SendChatMessage(player, "", "You do not have permissions to use this command!", player.UserIDString);
                return;
            }

            switch (args[0])
            {
                case "on":
                    {
                        RefreshMarkers();
                        rust.SendChatMessage(player, "AdminTCMarkers:", "Markers ON", player.UserIDString);
                    }
                    break;
                case "off":
                    {
                        Unload();
                        rust.SendChatMessage(player, "AdminTCMarkers:", "Markers OFF", player.UserIDString);
                    }
                    break;
                case "refresh":
                case "reload":
                    {
                        RefreshMarkers();
                        rust.SendChatMessage(player, "AdminTCMarkers:", "Markers Refreshed", player.UserIDString);
                    }
                    break;
                case "drop_all_purchased":
                    {
                        RefreshMarkers();
                        rust.SendChatMessage(player, "AdminTCMarkers:", "Markers Refreshed, Purchased Players Purged", player.UserIDString);
                    }
                    break;
            }
        }

        private string GetPurchaseCurrancyName(BasePlayer player)
        {
            string purchasetype = "";

            switch (ConfigSettings.CurrencyType)
            {
                case "Item":
                    {
                        if (ConfigSettings.ItemID == 0)
                        {
                            rust.SendChatMessage(player, "TC Markers:", "Invalid Item ID of " + ConfigSettings.ItemID.ToString() + ". Contact Admin.", player.UserIDString);
                            return "";
                        }
                        purchasetype = ItemManager.FindItemDefinition(ConfigSettings.ItemID).shortname;
                    }
                    break;
                case "Economics":
                case "ServerRewards":
                    {
                        purchasetype = ConfigSettings.CustomCurrencyName;
                    }
                    break;
            }

            return purchasetype;
        }

        private bool PurchaseWithItem(BasePlayer player)
        {
            bool item_found = false;
            Item found_item = null;
            ItemDefinition item_to_use = ItemManager.FindItemDefinition(ConfigSettings.ItemID);

            List<Item> all_player_items = player.inventory.AllItems().ToList();
            foreach (Item item in all_player_items)
            {
                if (item.info.itemid == ConfigSettings.ItemID)
                {
                    item_found = true;
                    found_item = item;
                    break;
                }
            }

            if (item_found == false)
                return false;

            found_item.amount -= ConfigSettings.CurrencyCost;

            if (found_item.amount <= 0)
            {
                if (player.inventory.containerMain.itemList.Contains(found_item))
                    player.inventory.containerMain.itemList.Remove(found_item);

                if (player.inventory.containerBelt.itemList.Contains(found_item))
                    player.inventory.containerBelt.itemList.Remove(found_item);
            }

            found_item.MarkDirty();
            return true;
        }

        private bool PurchaseWithCurrency(BasePlayer player)
        {
            if (ConfigSettings.CurrencyType == "ServerRewards" && ServerRewards != null)
            {
                var player_wealth = ServerRewards.Call("CheckPoints", player.userID);
                if (player_wealth == null || (int)player_wealth < ConfigSettings.CurrencyCost)
                {
                    rust.SendChatMessage(player, "TC Markers:", "You cannot afford to purchase.", player.UserIDString);
                    return false;
                }

                ServerRewards.Call("TakePoints", player.userID, ConfigSettings.CurrencyCost);
            }
            else if (ConfigSettings.CurrencyType == "Economics" && Economics != null)
            {
                var player_wealth = ServerRewards.Call("Balance", player.userID);
                if (player_wealth == null || (double)player_wealth < ConfigSettings.CurrencyCost)
                {
                    rust.SendChatMessage(player, "TC Markers:", "You cannot afford to purchase.", player.UserIDString);
                    return false;
                }

                ServerRewards.Call("Withdraw", player.userID, ConfigSettings.CurrencyCost);
            }
            else
            {
                rust.SendChatMessage(player, "TC Markers:", "Currency plugin: " + ConfigSettings.CurrencyType + " is not installed on server. Contact Admin.", player.UserIDString);
                return false;
            }

            rust.SendChatMessage(player, "TC Markers:", "Purchased for " + ConfigSettings.TimeToOwn.ToString() + " seconds! Enjoy!", player.UserIDString);
            return true;
        }

        private TimedPlayer CreateTimedPlayer(BasePlayer player, float time_remaining, bool is_timed)
        {
            TimedPlayer tp = new TimedPlayer();
            tp.authorized_player = player;
            tp.time_remaining = time_remaining;
            tp.is_timed = is_timed;
            return tp;
        }

        private void AddTempAuthorization(BasePlayer player)
        {
            TimedPlayer tp = CreateTimedPlayer(player, ConfigSettings.TimeToOwn, true);

            if (TEMP_AUTHORIZED_PLAYERS.ContainsKey(tp.authorized_player.userID) == false)
            {
                permission.GrantUserPermission(player.UserIDString, PERMISSION_USE, this);
                TEMP_AUTHORIZED_PLAYERS.Add(tp.authorized_player.userID, tp);
                rust.SendChatMessage(player, "TC Markers:", "Markers turned ON for " + ConfigSettings.TimeToOwn + " seconds!", player.UserIDString);
                RefreshMarkers();
            }
        }

        private void CreateMarker(BuildingPrivlidge privlidge)
        {
            // Avoid duplicate markers
            if (TOOL_CUPBOARD_ENTITIES.ContainsKey(privlidge.GetInstanceID().ToString()))
                return;

            // Avoid Raidable Bases
            if (ConfigSettings.RBEAuth == false)
            {
                BuildingManager.Building TCBuilding = privlidge.GetBuilding();
                if (TCBuilding != null)
                {
                    if (TCBuilding.buildingBlocks.Count > 0)
                    {
                        // Blocks of Raidable Bases do not have OwnerIDs
                        if (TCBuilding.buildingBlocks[0].OwnerID == 0)
                        {
                            return;
                        }
                    }
                }
            }

            VendingMachineMapMarker vending = new VendingMachineMapMarker();
            MapMarkerGenericRadius generic = new MapMarkerGenericRadius();

            MarkerData new_md = new MarkerData();
            new_md.building_privlidge = privlidge;

            vending = GameManager.server.CreateEntity(VENDING_MARKER, new_md.building_privlidge.ServerPosition).GetComponent<VendingMachineMapMarker>();
            vending.markerShopName = CreateTCAdvancedDetails(new_md);
            vending.enableSaving = false;
            vending.name = LIMITED_NET;

            if (ConfigSettings.ShowAdvancedDetails && GetColorMarkerVisibility(new_md.building_privlidge))
            {
                vending.Spawn();
                vending.SendNetworkUpdate();
            }

            generic = GameManager.server.CreateEntity(GENERIC_MARKER, new_md.building_privlidge.ServerPosition).GetComponent<MapMarkerGenericRadius>();
            generic.color1 = GetMarkerColor(new_md.building_privlidge.authorizedPlayers.Count);
            generic.color2 = GetMarkerColor(new_md.building_privlidge.authorizedPlayers.Count);
            generic.radius = ConfigSettings.ColorfulMarkerSize;
            generic.alpha = 1.0f;
            generic.enableSaving = false;
            generic.name = LIMITED_NET;

            if (ConfigSettings.ShowColorfulMarker && GetColorMarkerVisibility(new_md.building_privlidge))
            {
                generic.Spawn();
                generic.SendUpdate();
            }

            new_md.generic_map_marker = generic;
            new_md.vending_map_marker = vending;
            TOOL_CUPBOARD_ENTITIES.Add(privlidge.GetInstanceID().ToString(), new_md);
        }

        private void DestroyMarker(string marker_id)
        {
            if (TOOL_CUPBOARD_ENTITIES.ContainsKey(marker_id))
            {
                MarkerData marker_entry = null;
                TOOL_CUPBOARD_ENTITIES.TryGetValue(marker_id, out marker_entry);
                if (marker_entry == null)
                    return;

                if (marker_entry.generic_map_marker != null)
                {
                    marker_entry.generic_map_marker.alpha = 0.0f;
                    marker_entry.generic_map_marker.Kill();
                }

                if (marker_entry.vending_map_marker != null)
                {
                    marker_entry.vending_map_marker.markerShopName = "";
                    marker_entry.vending_map_marker.Kill();
                }

                marker_entry.building_privlidge = null;

                TOOL_CUPBOARD_ENTITIES.Remove(marker_id);
            }
        }

        private IEnumerator CoroutineUpdateExistingMarkers()
        {
            yield return new WaitForSeconds(1.0f);

            foreach (KeyValuePair<string, MarkerData> entry in TOOL_CUPBOARD_ENTITIES)
            {
                UpdateExistingMarker(entry.Value.building_privlidge);
            }
        }

        private IEnumerator CoroutineUpdateExistingMarkersSlow()
        {
            while (PLUGIN_ACTIVE)
            {
                foreach (KeyValuePair<string, MarkerData> entry in TOOL_CUPBOARD_ENTITIES)
                {
                    UpdateExistingMarker(entry.Value.building_privlidge);
                }

                yield return new WaitForSeconds(60.0f);
            }
        }

        private IEnumerator CheckTimeToOwn()
        {
            while (PLUGIN_ACTIVE)
            {
                List<TimedPlayer> expired_players = new List<TimedPlayer>();
                foreach (KeyValuePair<ulong, TimedPlayer> entry in AUTHORIZED_PLAYERS)
                {
                    if (entry.Value.is_timed == false)
                        continue;

                    entry.Value.time_remaining -= 1.0f;
                    if (entry.Value.time_remaining <= 0.0f)
                    {
                        expired_players.Add(entry.Value);
                    }
                }

                for(int i = 0; i < expired_players.Count; i++)
                {
                    if (TEMP_AUTHORIZED_PLAYERS.ContainsKey(expired_players[i].authorized_player.userID))
                    {
                        permission.RevokeUserPermission(expired_players[i].authorized_player.UserIDString, PERMISSION_USE);
                        TEMP_AUTHORIZED_PLAYERS.Remove(expired_players[i].authorized_player.userID);
                        rust.SendChatMessage(expired_players[i].authorized_player, "AdminTCMarkers:", "Markers EXPIRED", expired_players[i].authorized_player.UserIDString);
                    }
                }

                if (expired_players.Count > 0)
                    RefreshMarkers();

                expired_players.Clear();

                yield return new WaitForSeconds(1.0f);
            }
        }

        private void UpdateExistingMarker(BuildingPrivlidge building_privlidge)
        {
            if (building_privlidge == null)
                return;

            // Check to see if TC is recorded, if not make one
            if (TOOL_CUPBOARD_ENTITIES.ContainsKey(building_privlidge.GetInstanceID().ToString()) == false)
            {
                CreateMarker(building_privlidge);
            }

            // Get the MarkerData of the current TC
            MarkerData marker_entry = null;
            TOOL_CUPBOARD_ENTITIES.TryGetValue(building_privlidge.GetInstanceID().ToString(), out marker_entry);
            if (marker_entry == null)
                return;

            if (marker_entry.building_privlidge == null)
                return;

            if (marker_entry.vending_map_marker != null)
            {
                marker_entry.vending_map_marker.markerShopName = CreateTCAdvancedDetails(marker_entry);
                marker_entry.vending_map_marker.SendNetworkUpdate();
            }

            if (marker_entry.generic_map_marker != null)
            {
                marker_entry.generic_map_marker.color1 = GetMarkerColor(marker_entry.building_privlidge.authorizedPlayers.Count);
                marker_entry.generic_map_marker.color2 = GetMarkerColor(marker_entry.building_privlidge.authorizedPlayers.Count);
                marker_entry.generic_map_marker.SendUpdate();
            }
        }

        private string CreateTCAdvancedDetails(MarkerData marker_data)
        {
            if (ConfigSettings.ShowAdvancedDetails == false)
                return "";

            string output_details = "";

            if (RaidProtection != null && ConfigSettings.ShowRaidProtection)
            {
                var protection_percent = RaidProtection.Call("GetProtectionPercent", marker_data.building_privlidge);
                var protection_level = RaidProtection.Call("GetProtectionLevel", marker_data.building_privlidge);
                var protection_balance = RaidProtection.Call("GetProtectionBalance", marker_data.building_privlidge);
                var protection_hours = RaidProtection.Call("GetProtectionHours", marker_data.building_privlidge);

                output_details += $"-Raid Protection-\n";
                if (protection_percent != null) { output_details += $"Protection: " + protection_percent + "%\n"; }
                if (protection_level != null) { output_details += $"Protection Level: " + protection_level + "\n"; }
                if (protection_balance != null) { output_details += $"Protection Balance: " + protection_balance + "\n"; }
                if (protection_hours != null) { output_details += $"Protection Hours: " + protection_hours + "\n"; }
                output_details += "\n";
            }

            #region AD_SHOW_PLAYER_NAMES
            if (ConfigSettings.ShowPlayerNames)
            {
                if (marker_data.building_privlidge.authorizedPlayers.Count > 0)
                {
                    output_details += "-Authorized Players-";

                    HashSet<PlayerNameID> authorizedPlayersToRemove = new HashSet<PlayerNameID>();

                    foreach (var authorizedPlayer in marker_data.building_privlidge.authorizedPlayers)
                    {
                        string username = authorizedPlayer.username;
                        if (username.Length == 0 || username == "" || username == "Player")
                        {
                            authorizedPlayersToRemove.Add(authorizedPlayer);
                            continue;
                        }

                        output_details += $"\n{username}";
                    }

                    foreach (var playerToRemove in authorizedPlayersToRemove)
                    {
                        marker_data.building_privlidge.authorizedPlayers.Remove(playerToRemove);
                    }
                }
                else
                {
                    output_details = "No Authorized Players!";
                }
            }

            #endregion

            if (ConfigSettings.ShowPlayerNames && ConfigSettings.ShowBuildingComponents)
            {
                output_details += "\n\n";
            }

            #region AD_BUILDING_COMPONENTS
            if (ConfigSettings.ShowBuildingComponents)
            {
                if (marker_data.building_privlidge != null)
                {
                    BuildingManager.Building TCBuilding = marker_data.building_privlidge.GetBuilding();
                    if (TCBuilding != null)
                    {
                        if (TCBuilding.buildingBlocks.Count > 0)
                        {
                            output_details += "-Building Components-";

                            Dictionary<string, uint> overall_quality = new Dictionary<string, uint>();
                            overall_quality.Add("Twig", 0);
                            overall_quality.Add("Wood", 0);
                            overall_quality.Add("Stone", 0);
                            overall_quality.Add("Metal", 0);
                            overall_quality.Add("Armored", 0);

                            foreach (BuildingBlock block in TCBuilding.buildingBlocks)
                            {
                                if (block == null)
                                    continue;

                                switch (block.grade)
                                {
                                    case BuildingGrade.Enum.Twigs: { overall_quality[(string)"Twig"] += 1; } break;
                                    case BuildingGrade.Enum.Wood: { overall_quality[(string)"Wood"] += 1; } break;
                                    case BuildingGrade.Enum.Stone: { overall_quality[(string)"Stone"] += 1; } break;
                                    case BuildingGrade.Enum.Metal: { overall_quality[(string)"Metal"] += 1; } break;
                                    case BuildingGrade.Enum.TopTier: { overall_quality[(string)"Armored"] += 1; } break;
                                }
                            }

                            if (overall_quality[(string)"Twig"] > 0)
                                output_details += "\n" + overall_quality[(string)"Twig"] + " Twig";

                            if (overall_quality[(string)"Wood"] > 0)
                                output_details += "\n" + overall_quality[(string)"Wood"] + " Wood";

                            if (overall_quality[(string)"Stone"] > 0)
                                output_details += "\n" + overall_quality[(string)"Stone"] + " Stone";

                            if (overall_quality[(string)"Metal"] > 0)
                                output_details += "\n" + overall_quality[(string)"Metal"] + " Metal";

                            if (overall_quality[(string)"Armored"] > 0)
                                output_details += "\n" + overall_quality[(string)"Armored"] + " Armored";
                        }
                    }
                }
            }
            #endregion

            return output_details;
        }

        #endregion

        #region CONFIGURATION
        private Color ConfigToColor(int ConfigColor)
        {
            switch (ConfigColor)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                case 3: return Color.white;
                case 4: return Color.black;
                case 5: return Color.yellow;
                case 6: return Color.cyan;
                case 7: return Color.magenta;
                case 8: return Color.grey;
                default: return Color.white;
            }
        }

        private Color GetMarkerColor(int NumPlayersAuthed)
        {
            if (NumPlayersAuthed == 0)
                return ConfigToColor(ConfigSettings.ZeroAuthColor);

            if (NumPlayersAuthed == 1)
                return ConfigToColor(ConfigSettings.OneAuthColor);

            if (NumPlayersAuthed == 2)
                return ConfigToColor(ConfigSettings.TwoAuthColor);

            if (NumPlayersAuthed == 3)
                return ConfigToColor(ConfigSettings.ThreeAuthColor);

            if (NumPlayersAuthed == 4)
                return ConfigToColor(ConfigSettings.FourAuthColor);

            if (NumPlayersAuthed == 5)
                return ConfigToColor(ConfigSettings.FiveAuthColor);

            if (NumPlayersAuthed == 6)
                return ConfigToColor(ConfigSettings.SixAuthColor);

            if (NumPlayersAuthed == 7)
                return ConfigToColor(ConfigSettings.SevenAuthColor);

            if (NumPlayersAuthed == 8)
                return ConfigToColor(ConfigSettings.EightAuthColor);

            if (NumPlayersAuthed > 8)
                return ConfigToColor(ConfigSettings.MoreThanEightAuthColor);

            return Color.black;
        }

        private bool GetColorMarkerVisibility(BuildingPrivlidge privlidge)
        {
            if (privlidge == null)
            {
                Puts("GetMarkerVisibility:privlidge = null");
                return false;
            }

            if (ConfigSettings == null)
            {
                Puts("GetMarkerVisibility:ConfigSettings = null");
                return false;
            }

            if (ConfigSettings.ShowAdvancedDetails == false)
            {
                Puts("GetMarkerVisibility:ShowAdvancedDetails = false");
            }

            if (ConfigSettings.ShowColorfulMarker == false)
            {
                Puts("GetMarkerVisibility:ShowColorfulMarker = false");
            }

            if (privlidge.authorizedPlayers == null)
            {
                Puts("GetMarkerVisibility:privlidge.authorizedPlayers = null");
                return false;
            }

            if (privlidge.authorizedPlayers.Count == 0)
                return true;

            if (privlidge.authorizedPlayers.Count == 1)
                return true;

            if (privlidge.authorizedPlayers.Count == 2)
                return true;

            if (privlidge.authorizedPlayers.Count == 3)
                return true;

            if (privlidge.authorizedPlayers.Count == 4)
                return true;

            if (privlidge.authorizedPlayers.Count == 5)
                return true;

            if (privlidge.authorizedPlayers.Count == 6)
                return true;

            if (privlidge.authorizedPlayers.Count == 7)
                return true;

            if (privlidge.authorizedPlayers.Count == 8)
                return true;

            if (privlidge.authorizedPlayers.Count > 8)
                return true;

            Puts("GetMarkerVisibility:FailedToFindConfigSettings");
            return false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                ConfigSettings = Config.ReadObject<Configuration>();
                if (ConfigSettings == null)
                {
                    LoadDefaultConfig();
                }

                SaveConfig();
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted. \n{ex}");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            ConfigSettings = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            Config.WriteObject(ConfigSettings);
        }

        class Configuration
        {
            [JsonProperty("Show advanced details Map Marker?")]
            public bool ShowAdvancedDetails = true;

            [JsonProperty("Show Raid Protection status? (Requires ShowAdvancedDetails = true, AND RaidProtection plugin installed)")]
            public bool ShowRaidProtection = true;

            [JsonProperty("Show player names on Map Marker? (Requires ShowAdvancedDetails = true)")]
            public bool ShowPlayerNames = true;

            [JsonProperty("Show building components on Map Marker? (Requires ShowAdvancedDetails = true)")]
            public bool ShowBuildingComponents = true;

            [JsonProperty("Show colorful marker?")]
            public bool ShowColorfulMarker = true;

            [JsonProperty("Colorful marker size? (Default=0.15) (Requires ShowColorfulMarker = true)")]
            public float ColorfulMarkerSize = 0.15f;

            [JsonProperty("Show Raidable Bases with TCs on the map?")]
            public bool RBEAuth = false;

            [JsonProperty("Map Marker Color Values: 0=Red, 1=Green, 2=Blue, 3=White, 4=Black, 5=Yellow, 6=Cyan, 7=Magenta, 8=Grey, Other#=White")]
            public int DummyValue = 0;

            [JsonProperty("ZERO players TC map marker color")]
            public int ZeroAuthColor = 0;

            [JsonProperty("ONE player TC map marker color")]
            public int OneAuthColor = 1;

            [JsonProperty("TWO players TC map marker color")]
            public int TwoAuthColor = 2;

            [JsonProperty("THREE players TC map marker color")]
            public int ThreeAuthColor = 3;

            [JsonProperty("FOUR players TC map marker color")]
            public int FourAuthColor = 4;

            [JsonProperty("FIVE players TC map marker color")]
            public int FiveAuthColor = 5;

            [JsonProperty("SIX players TC map marker color")]
            public int SixAuthColor = 6;

            [JsonProperty("SEVEN players TC map marker color")]
            public int SevenAuthColor = 7;

            [JsonProperty("EIGHT players TC map marker color")]
            public int EightAuthColor = 8;

            [JsonProperty("MORE THAN EIGHT players TC map marker color")]
            public int MoreThanEightAuthColor = 8;

            [JsonProperty("Can players purchase 'Admin TC Markers' for free?")]
            public bool FreePurchase = false;

            [JsonProperty("Player purchase currency: Item, Economics, ServerRewards (Requires FreePurchase = false)")]
            public string CurrencyType = "Item";

            [JsonProperty("If using Economics or ServerRewards, whats your servers custom currency name? Examples: RP, Frooples, Gold")]
            public string CustomCurrencyName = "RP";

            [JsonProperty("If player is purchasing with an item, what is the ItemID? (Requires FreePurchase = false)")]
            public int ItemID = 0;

            [JsonProperty("How much of said currency should 'Admin TC Markers' cost? (Requires FreePurchase = false)")]
            public int CurrencyCost = 100;

            [JsonProperty("How long should the purchasing player own 'Admin TC Markers'? (Will NOT persist after server restart!)")]
            public float TimeToOwn = 600.0f;
        }

        #endregion
    }
}