// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Network;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

// Reference: 0Harmony
#if CARBON
using HarmonyLib;
#else
using Harmony;
#endif

namespace Oxide.Plugins
{
    [Info("AdminMap", "0xF", "1.3.4")]
    partial class AdminMap : RustPlugin
    {
        #region Consts 
        const string PERMISSION_TO_USE = "adminmap.allow";
        const string PERMISSION_INVIS = "adminmap.invis";
        const string PERMISSION_PLAYER_MARKERS = "adminmap.playermarkers";
        const string PERMISSION_PLAYER_MARKERS_WL = "adminmap.playermarkers.wl";
        #endregion

        #region Vars
#if CARBON
        private static HarmonyLib.Harmony harmonyInstance;
#else
        private static HarmonyInstance harmonyInstance;
#endif
        private static AdminMap Instance;
        static MapMarkerGenericRadius scanarea;
        static string CapsuleBackgroundVerticalCRC, LetterTCRC, StashCRC, SleepingBagCRC;
        #endregion

        #region Hooks
        void Init()
        {
            Instance = this;
            AdminsDatabase.Clear();
            permission.RegisterPermission(PERMISSION_TO_USE, this);
            permission.RegisterPermission(PERMISSION_INVIS, this);
            permission.RegisterPermission(PERMISSION_PLAYER_MARKERS, this);
            permission.RegisterPermission(PERMISSION_PLAYER_MARKERS_WL, this);
            foreach (var button in config.Buttons)
                if (button.Permission != string.Empty)
                    permission.RegisterPermission($"adminmap.{button.Permission}", this);
        }

        void OnServerInitialized(bool initial)
        {
            CapsuleBackgroundVerticalCRC = FileStorage.server.Store(Convert.FromBase64String("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TYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoUQZMiaFIETYqgSRE0KYImRdCkCJoU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"), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
            LetterTCRC = FileStorage.server.Store(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAThSURBVHgB7d3Pi1V1GMDh96RoZYmWRUW1KggtComgH1ZYFC2kIFq0bdH/IEEUtHIV7aJli0CCFtG+RbSoFi0kLRgRSSSFMCkxU0/v4U4YUTD3Xhq/57zPAy9nFjMgOO9nzjl37pkIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAZfR9f3fOIzldANOWi35bzqs5H+Ss9Fe8H0yOqheXi709D0/l7F2dXf/xqRdztnRddyGYjI1BKbnwW/KwJ64s/EM5G9bwpcP3yh05x4LJEICJy4Uf/o8fzHl2dYbl3xyLWUsoGBEBmJhc+GFJh5/qw7I/kfNkztaAfyEAE5BLP1y3PxezU3oLz5oJwIjl4t+Shw9zng9YwDXBmB0Iy88SBGDcdgcsQQDGzV15liIAUJgAQGECAIUJABQmAFCYAEBhAgCFCQAUJgBQmABAYQIAhQkAFCYAUJgAQGECAIV5JFhD+r7flofXY/Zs/uvW8CV3xfp6J/+NZ2M+fc7JnM+6rvsmaIo/DNKQXK4v8vB4TNPlnD0ZgS+DZrgEaEQu/00x3eUfDN9r+4KmCEA7bozp87jyxggAFCYAUJgAQGECAIUJABQmAFCYAEBhAgCFCQAUJgBQmABAYQIAhQkAFCYAUJgAtONcTN/5oCkC0Iiu607n4WhM21dBUzwTsC0v5ryZszPW9kzAO3M2xfr5JWaP9prH8EzAEzkf5xwMmuKZgCPW9/2hmD1AdL3ck2cqK8FkuASAwgQAChMAKEwAoDABgMIEAAoTAChMAKAwAYDCBAAKEwAoTACgMAGAwgQAChMAKEwAoDABgMIEAAoTAChMAKAwAYDCBAAKEwAoTACgMAGAwgQAChMAKEwAoDABgMIEAAoTAChMAKAwAYDCBAAKEwAoTACgMAGAwgSAeWwIJkUAmMeWYFIEgHlsDyZFAJjHA8GkCADzeDqYFAFgHvv6vr83mAwBYB7DqwCfZARuDSZBAJjXrpwjGYE3cu4LRq0LRisX8FDMFvJq+jXnWM5POX3OmdXjP13KOZzzXtd1Z4ImbAxYzg0596/OWuzOeSlogkuAcetjfPYGzRCAcfs9xsdvEzZEAMbtfMASBGDcBIClCMC4/RywBAEYtx8DliAA43YiYAkCMG5HA5YgAOP2dcASBGDEuq4b7gGcDFiQAIzf5wELEoDxOxiwIO8GHLm+76+N2WXAthiHy3np4unCjXAGMHK5TMNvA74bsABnABOQZwFb87CSsyPa5wygIc4AJiAX6mweXotxvj2Yq0gAJiIj8GkeDgTMQQCmZX/OW9H2mcCFoBkCMCF5FtDnvJ0fvpxzPNr0XQD/r7wxeH3O/pzjfTvO5TwTNMOrABOXCzfccX8h55WcfbG+f9/vYszesPR9zrc5H+UZyuGgGQJQSMZguOTbmfNozmM5D+fcnnNzLG6433AqZi9DHsn5IWYLP3y8kgv/R9AsAeCvs4QhAjtWj5tj9vDOTX/7tOF7ZVj232J2I+90zBb/VC75pQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKbpT6jRW4l52+/oAAAAAElFTkSuQmCC"), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
            StashCRC = FileStorage.server.Store(Convert.FromBase64String("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hBCTRUtrPb08obWuo0GhsYHw8s17z3/rys5njpftf6VLtvefuqeT6O9r8j8KAOgKoRt0204SoL/6zXsF4cExxnhz0dhv78/rV48XbW+Up+dyrcXaMAfRLaN3bGCYlK56/uref3/h2t1/5nz8Dj9Y1gp0XG95H27/XgBgdeMT4NoKiEinGs1DT+KttMI7vZ8BV1zkcmP9CSnk2SI34uRaJnzwuAC3j+vdb1y++++u3Z3+29V1xWrCDZ3Ksw95108VgPvmgWO6sLixmkSXrX5wnrd98m/xmYgxHk7L9qWdSZm+cXe+A6wZo9Pv78y//7XX7v7+4bz+b3TU3NKFZPmTmvh9+3H3ChtgSJcjNlavczoh9XCcPjzcI0e7+q4wWn5ymJu140X7lRDjt1cTt6vv/VS2k/+kt8sbIF2NbDU0nZdofkBV9zN3swKiXY0GcOe3+uHm/uI9X/zPAwDv9Vo/m38SeGyP7bE9tsf22AD4vwAc2zoiYRDfAAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDIzLTEyLTMwVDE0OjA5OjU4KzAwOjAwrQFNqAAAACV0RVh0ZGF0ZTptb2RpZnkAMjAyMy0xMi0zMFQxNDowOTo1OCswMDowMNxc9RQAAAAodEVYdGRhdGU6dGltZXN0YW1wADIwMjMtMTItMzBUMTQ6MTA6MDErMDA6MDBrqmt2AAAAE3RFWHRkYzpmb3JtYXQAaW1hZ2UvcG5n/7kbPgAAABV0RVh0cGhvdG9zaG9wOkNvbG9yTW9kZQAzVgKzQAAAACZ0RVh0cGhvdG9zaG9wOklDQ1Byb2ZpbGUAc1JHQiBJRUM2MTk2Ni0yLjEcL2wLAAAAEHRFWHR4bXA6Q29sb3JTcGFjZQAxBQ7I0QAAACh0RVh0eG1wOkNyZWF0ZURhdGUAMjAxNS0xMC0wMVQxMzoyMjozNS0wNDowMMoWcMcAAAAxdEVYdHhtcDpDcmVhdG9yVG9vbABBZG9iZSBQaG90b3Nob3AgQ0MgMjAxNCAoV2luZG93cykySyUpAAAAKnRFWHR4bXA6TWV0YWRhdGFEYXRlADIwMTUtMTAtMDFUMTM6MjI6MzUtMDQ6MDBCTB9AAAAAKHRFWHR4bXA6TW9kaWZ5RGF0ZQAyMDE1LTEwLTAxVDEzOjIyOjM1LTA0OjAwfuhM/gAAABd0RVh0eG1wOlBpeGVsWERpbWVuc2lvbgA1MTKsIfVRAAAAF3RFWHR4bXA6UGl4ZWxZRGltZW5zaW9uADUxMjEuFCcAAABLdEVYdHhtcE1NOkRvY3VtZW50SUQAYWRvYmU6ZG9jaWQ6cGhvdG9zaG9wOmZhMzUwN2E4LTY4NjAtMTFlNS04ZGZiLWM3ZjFjNTA5NzQxNIt3ndUAAAA9dEVYdHhtcE1NOkluc3RhbmNlSUQAeG1wLmlpZDozZjk1Njc0Zi1jNmQxLWZhNDAtYTAzNS0zZTllZDYyOGQwYjHhhUjkAAAARXRFWHR4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQAeG1wLmRpZDpiZGJhMTYxNC1mYjRiLWUxNDQtODI4My03YjA2MTNhN2EzYTDY1hHxAAAAAElFTkSuQmCC"), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
            SleepingBagCRC = FileStorage.server.Store(Convert.FromBase64String("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"), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
            ServerMgr.Instance.StartCoroutine(this.Loop());
            ServerMgr.Instance.StartCoroutine(this.PermissionsLoop());
            scanarea = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", Vector3.zero) as MapMarkerGenericRadius;
            scanarea.enableSaving = false;
            scanarea.radius = 0f;
            scanarea.alpha = 0f;
            scanarea.color1 = config.ScanArea.Color;
            scanarea.Spawn();

            foreach (var admin in BasePlayer.activePlayerList
                   .Select(AdminsDatabase.Get)
                   .OfType<AdminsDatabase.AdminData>())
                admin.RenderAdminMapPanel();
        }

        void Loaded()
        {
#if CARBON
            harmonyInstance = new HarmonyLib.Harmony(this.Name);
#else
            harmonyInstance = HarmonyInstance.Create(this.Name);
#endif
            harmonyInstance.Patch(AccessTools.Method(typeof(BasePlayer), "Server_RemovePointOfInterest"), new HarmonyMethod(GetType(), "Server_RemovePointOfInterest"));
            harmonyInstance.Patch(AccessTools.Method(typeof(BasePlayer), "TeamUpdate"), new HarmonyMethod(GetType(), "TeamsBypass"));
            harmonyInstance.Patch(AccessTools.Method(typeof(BasePlayer), "ClearTeam"), new HarmonyMethod(GetType(), "TeamsBypass"));
            OnLoaded();
        }

        void Unload()
        {
            harmonyInstance.UnpatchAll(this.Name);
            AdminsDatabase.Clear();
            scanarea.Kill();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            AdminsDatabase.Get(player)?.RenderAdminMapPanel();
        }

        object OnMapMarkerAdd(BasePlayer player, MapNote note)
        {
            AdminsDatabase.AdminData adminData = AdminsDatabase.Get(player);

            if (adminData != null && adminData.HasActiveMap)
            {
                if (player.serverInput.IsDown(BUTTON.DUCK))
                {
                    player.Teleport(GetGroundPosition(note.worldPosition));
                }
                else
                {

                    adminData.SelectedPlayer = null;
                    adminData.SelectedCupboard = null;
                    if (!adminData.IsMapActive(Maps.Cupboards))
                    {
                        adminData.scannerMarker = note;
                        SendAdminMapData();
                        SendLocalMarkerUpdate(player, scanarea, note.worldPosition, 20.25f * config.ScanArea.Radius);
                    }
                }
                return false;
            }
            return null;
        }

        public static bool Server_RemovePointOfInterest(BaseEntity.RPCMessage msg)
        {
            BasePlayer sender = msg.player;
            int index = msg.read.Int32();
            AdminsDatabase.AdminData adminData = AdminsDatabase.Get(sender);
            if (adminData != null && adminData.HasActiveMap)
            {
                if (adminData.IsMapActive(Maps.Cupboards))
                {
                    var cupboards = GetAllBuildingPrivlidges();
                    if (index >= cupboards.Count()) return false;
                    adminData.SelectedCupboard = cupboards.ElementAt(index);
                }
                else if (adminData.IsMapActive(Maps.Stashes))
                {
                    if (index >= adminData.RenderedStashes.Count) return false;
                    StashContainer stash = adminData.RenderedStashes.ElementAtOrDefault(index);
                    if (stash == null)
                        return false;

                    sender.Teleport(stash.transform.position);
                }
                else if (adminData.IsMapActive(Maps.SleepingBags))
                {
                    if (index >= adminData.RenderedSleepingBags.Count) return false;
                    SleepingBag bag = adminData.RenderedSleepingBags.ElementAtOrDefault(index);
                    if (bag == null)
                        return false;

                    sender.Teleport(bag.transform.position);
                }
                else
                {
                    MapNote scannerMarker = adminData?.scannerMarker;
                    if (scannerMarker != null)
                    {
                        if (index >= adminData.RenderedPlayers.Count) return false;
                        adminData.SelectedPlayer = adminData.RenderedPlayers.ElementAt(index);
                        return false;
                    }
                }

            }
            msg.read.Position -= 4;
            return true;
        }
        public static bool TeamsBypass(BasePlayer __instance)
        {
            if (AdminsDatabase.Get(__instance)?.IsMapActive(Maps.Text) == true)
                return false;
            return true;
        }
        object OnMapMarkersClear(BasePlayer player, List<MapNote> notes)
        {
            if (AdminsDatabase.Get(player)?.IsMapActive(Maps.Text) == true)
                return false;
            return null;
        }
        void OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            AdminsDatabase.AdminData adminData = AdminsDatabase.Get(player);
            if (adminData == null || !adminData.HasActiveMap) return;
            if (player.serverInput.current.aimAngles == player.serverInput.previous.aimAngles)
                return;
            if (adminData.SelectedPlayer != null)
                adminData.SelectedPlayer = null;
            if (adminData.SelectedCupboard != null)
                adminData.SelectedCupboard = null;

        }
        partial void OnLoaded();
        #endregion

        #region Commands
        [ChatCommand("amap")]
        private void MainCMD(BasePlayer player, string command, string[] args)
        {
            AdminsDatabase.AdminData adminData = AdminsDatabase.Get(player);
            if (adminData == null)
            {
                player.ChatMessage(GetMessage(LangKeys.NO_PERMS, player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                if (adminData.HasActiveMap)
                {
                    adminData.DisableMap();
                    adminData.Dispose();
                }
                else
                {
                    adminData.EnableMap(Maps.Markers);
                }
                return;
            }
            else if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "d":
                        adminData.EnableMap(Maps.Markers);
                        break;
                    case "t":
                        adminData.EnableMap(Maps.Text);
                        break;
                    case "c":
                        adminData.EnableMap(Maps.Cupboards);
                        break;
                    case "s":
                        adminData.EnableMap(Maps.Sleepers);
                        break;
                    case "stash":
                        adminData.EnableMap(Maps.Stashes);
                        break;
                    case "ss":
                        adminData.searchQuery = (args.Length > 1 ? string.Concat(args.Skip(1)) : string.Empty);
                        if (adminData.searchQuery != string.Empty)
                        {
                            Func<BasePlayer, bool> func = (p => p.displayName.ToLower().Contains(adminData.searchQuery));
                            Func<BuildingPrivlidge, bool> func2 = priv => HasAuthorizadedPlayer(priv, adminData.searchQuery);
                            IEnumerable<BasePlayer> online = BasePlayer.activePlayerList.Where(func);
                            IEnumerable<BasePlayer> offline = BasePlayer.sleepingPlayerList.Where(func);
                            IEnumerable<BuildingPrivlidge> cupboards = GetAllBuildingPrivlidges().Where(func2);
                            player.ChatMessage(
                                string.Format(GetMessage(LangKeys.SEARCH_QUERY_RESULT, player.UserIDString) +
                                $"\nActive (<color=yellow>{online.Count()}</color>): {string.Join(", ", online.Select(p => p.displayName))}" +
                                $"\nSleeping (<color=yellow>{offline.Count()}</color>): {string.Join(", ", offline.Select(p => p.displayName))}" +
                                $"\nCupboards - <color=yellow>{cupboards.Count()}</color>",
                                $"<color=yellow>{adminData.searchQuery}</color>"));
                        }
                        else player.ChatMessage(GetMessage(LangKeys.SEARCH_QUERY_CLEAR, player.UserIDString));
                        break;
                    case "wl":
                        adminData.ToggleMod(MapMods.WithoutLabels);
                        break;
                    case "off":
                        adminData.DisableMap();
                        adminData.Dispose();
                        break;
                }
                return;
            }
        }

        [ConsoleCommand("adminmap.gui.cmd")]
        private void guicmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player.UserIDString == null || !permission.UserHasPermission(player.UserIDString, PERMISSION_TO_USE))
                return;

            AdminsDatabase.AdminData adminData = AdminsDatabase.Get(player);
            if (adminData == null) return;
            if (arg.Args.Length < 1) return;

            switch (arg.GetString(0))
            {
                case "command":
                    string commands = arg.FullString.Substring(arg.GetString(0).Length + 1).Replace("\\\"", "\"").Replace("{steamid}", adminData.SelectedPlayer.UserIDString).Replace("{username}", adminData.SelectedPlayer.displayName).Replace("{admin.steamid}", adminData.currentPlayer.UserIDString).Replace("{admin.username}", adminData.currentPlayer.displayName);
                    foreach (var command in commands.Split(';'))
                        player.SendConsoleCommand(command);
                    break;
                case "showinfo":
                    ShowNote(player, adminData.SelectedPlayer.UserIDString);
                    break;
                case "panel":
                    var buttonIndex = arg.GetInt(1, -1);
                    if (buttonIndex == -1) break;
                    switch (buttonIndex)
                    {
                        case 0:
                            if (adminData.IsMapActive(Maps.Markers))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.Markers);
                            break;
                        case 1:
                            if (adminData.IsMapActive(Maps.Text))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.Text);
                            break;
                        case 2:
                            if (adminData.IsMapActive(Maps.Sleepers))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.Sleepers);
                            break;
                        case 3:
                            if (adminData.IsMapActive(Maps.Cupboards))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.Cupboards);
                            break;
                        case 4:
                            if (adminData.IsMapActive(Maps.Stashes))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.Stashes);
                            break;
                        case 5:
                            if (adminData.IsMapActive(Maps.SleepingBags))
                                adminData.DisableMap();
                            else
                                adminData.EnableMap(Maps.SleepingBags);
                            break;
                        case 6:
                            adminData.ToggleMod(MapMods.WithoutLabels);
                            break;
                    }
                    break;
            }

            adminData.SelectedPlayer = null;
            adminData.SelectedCupboard = null;
        }

        #endregion

        #region Methods

        private bool HasAuthorizadedPlayer(BuildingPrivlidge buildingPrivilege, string searchQuery)
        {
            foreach (var p in buildingPrivilege.authorizedPlayers)
                if (p.username.ToLower().Contains(searchQuery))
                    return true;
            return false;
        }

        void ShowNote(BasePlayer player, string text)
        {
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.entitySource = RelationshipManager.ServerInstance;
            player.inventory.loot.AddContainer(new ItemContainer { itemList = new List<Item> { new Item { info = ItemManager.FindItemDefinition("note"), name = player.displayName, text = text } } });
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer<string>(null, player, "RPC_OpenLootPanel", "photoframe");
        }

        static void RestoreState(BasePlayer player)
        {
            if (player.currentTeam != 0UL)
                player.TeamUpdate();
            else
                player.ClearTeam();
            if (player.State.pings == null)
                player.State.pings = new List<MapNote>();
            player.SendPingsToClient();
            player.SendMarkersToClient();
            SendLocalMarkerUpdate(player, scanarea, Vector3.zero, 0);
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            if (UnityEngine.Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos)) + 2.5f;

            return sourcePos;
        }

        string GetHexColorForTeam(ulong teamID)
        {
            return Convert.ToString((int)Math.Floor((Math.Abs(Math.Sin(teamID) * 16777215))), 16);
        }

        static IEnumerable<BuildingPrivlidge> GetAllBuildingPrivlidges()
        {
            return ServerBuildingManager.server.buildingDictionary.SelectMany(b => b.Value.buildingPrivileges);
        }

        MapNote GetTCNote(BuildingPrivlidge privlidge, string label = null) => new MapNote
        {
            label = label,
            noteType = 1,
            isPing = true,
            icon = 2,
            colourIndex = Mathf.Clamp(privlidge.authorizedPlayers.Count - 1, 0, 5),
            worldPosition = privlidge.transform.position
        };

        MapNote GetStashNote(StashContainer stash, string label = null) => new MapNote
        {
            label = label,
            noteType = 1,
            isPing = true,
            icon = 11,
            colourIndex = Mathf.Clamp(stash.inventory.itemList.Count-1, 0, 5),
            worldPosition = stash.transform.position
        };

        MapNote GetPlayerNote(BasePlayer player, int forceColor = -1, bool withLabel = true) => new MapNote
        {
            label = withLabel ? player.displayName : null,
            noteType = 1,
            isPing = true,
            icon = 6,
            colourIndex = forceColor > -1 ? forceColor : (player.IsSleeping() ? 3 : 2),
            worldPosition = player.transform.position
        };
        MapNote GetVirtualMarker(BasePlayer player, int forceColor = -1) => new MapNote
        {
            label = player.displayName,
            noteType = 1,
            isPing = true,
            icon = 6,
            colourIndex = forceColor > -1 ? forceColor : (player.IsSleeping() ? 3 : 2),
            worldPosition = Vector3.negativeInfinity
        };

        static void SendLocalMarkerUpdate(BasePlayer player, MapMarkerGenericRadius marker, Vector3 position, float radius)
        {
            if (player == null || marker == null) return;
            SendInfo playerSendInfo = new SendInfo(player.Connection);
            NetWrite netWrite = Network.Net.sv.StartWrite();
            Network.Connection connection = player.net.connection;
            connection.validate.entityUpdates = connection.validate.entityUpdates + 1U;
            netWrite.PacketID(Message.Type.Entities);
            netWrite.UInt32(player.net.connection.validate.entityUpdates);
            BaseNetworkable.SaveInfo saveInfo = new BaseNetworkable.SaveInfo
            {
                forConnection = player.net.connection,
                forDisk = false
            };
            using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
            {
                marker.Save(saveInfo);
                saveInfo.msg.baseEntity.pos = position;
                saveInfo.msg.ToProto(netWrite);
            }
            netWrite.Send(playerSendInfo);
            marker.ClientRPCEx<Vector3, float, Vector3, float, float>(playerSendInfo, null, "MarkerUpdate", new Vector3(marker.color1.r, marker.color1.g, marker.color1.b), 1, -Vector3.one, marker.color1.a, (1024f / TerrainMeta.Size.x * 0.2f) * radius);
        }

        void SendPingsWithPlayers(BasePlayer player, bool withLabels = true)
        {
            using (MapNoteList mapNoteList = Facepunch.Pool.Get<MapNoteList>())
            {
                mapNoteList.notes = Facepunch.Pool.GetList<MapNote>();
                if (player.State.pings != null)
                    mapNoteList.notes.AddRange(player.State.pings);
                foreach (var p in BasePlayer.activePlayerList)
                    if (p != player && !permission.UserHasPermission(p.UserIDString, PERMISSION_INVIS))
                        mapNoteList.notes.Add(GetPlayerNote(p, -1, withLabels));
                player.ClientRPCPlayer<MapNoteList>(null, player, "Client_ReceivePings", mapNoteList);
                mapNoteList.notes.Clear();
            }
        }
        void SendMarkersToPlayersWithPerms()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (permission.UserHasPermission(player.UserIDString, PERMISSION_PLAYER_MARKERS))
                    SendPingsWithPlayers(player, true);
                else if (permission.UserHasPermission(player.UserIDString, PERMISSION_PLAYER_MARKERS_WL))
                    SendPingsWithPlayers(player, false);
            }
        }

        void SendAdminMapData()
        {
            IEnumerable<AdminsDatabase.AdminData> admins = BasePlayer.activePlayerList
                   .Select(AdminsDatabase.Get)
                   .OfType<AdminsDatabase.AdminData>();
            if (admins.Count() == 0) return;

            // Text Player Map
            IEnumerable<AdminsDatabase.AdminData> textAdmins = admins.Where(a => a.IsMapActive(Maps.Text));
            if (textAdmins.Count() > 0)
            {
                var tmsettings = config.TextMapSettings;
                using (PlayerTeam playerTeam = Facepunch.Pool.Get<PlayerTeam>())
                {
                    playerTeam.teamLeader = 0;
                    playerTeam.teamID = 0;
                    playerTeam.teamName = string.Empty;
                    playerTeam.teamLifetime = 0;
                    playerTeam.teamPings = Facepunch.Pool.GetList<MapNote>();
                    foreach (var admin in textAdmins)
                    {
                        playerTeam.members = Facepunch.Pool.GetList<PlayerTeam.TeamMember>();
                        foreach (var player in BasePlayer.activePlayerList)
                        {
                            if (player == admin.currentPlayer || permission.UserHasPermission(player.UserIDString, PERMISSION_INVIS)) continue;
                            PlayerTeam.TeamMember teamMember = Facepunch.Pool.Get<PlayerTeam.TeamMember>();

                            string color = tmsettings.SoloColor;
                            if (admin.searchQuery != string.Empty && player.displayName.ToLower().Contains(admin.searchQuery.ToLower()))
                                color = tmsettings.SearchColor;
                            else if (player.IsSleeping())
                                color = tmsettings.SleepersColor;
                            else if (player.currentTeam > 0)
                            {
                                if (tmsettings.UseGeneratedTeamsColors)
                                    color = GetHexColorForTeam(player.currentTeam);
                                else
                                    color = tmsettings.TeamsColor;
                            }
                            teamMember.displayName = $"\t\t\t<size={tmsettings.FontSize * 10}><color=#{color}>{player.displayName}</color></size>\t\t\t";
                            teamMember.healthFraction = 100;
                            teamMember.position = player.transform.position;
                            teamMember.online = true;
                            teamMember.wounded = false;
                            teamMember.userID = player.userID;
                            playerTeam.members.Add(teamMember);
                        }
                        admin.currentPlayer.ClientRPCEx<PlayerTeam>(new SendInfo(admin.currentPlayer.Connection), null, "CLIENT_ReceiveTeamInfo", playerTeam);
                        var scannerMarker = admin.scannerMarker;
                        if (scannerMarker != null)
                        {
                            using (MapNoteList markers = Facepunch.Pool.Get<MapNoteList>())
                            {
                                markers.notes = Facepunch.Pool.GetList<MapNote>();
                                admin.RenderedPlayers.Clear();
                                foreach (var player in BasePlayer.activePlayerList)
                                {
                                    if (player == admin.currentPlayer || permission.UserHasPermission(player.UserIDString, PERMISSION_INVIS)) continue;
                                    if (Vector3.Distance(player.transform.position, scannerMarker.worldPosition) < 150 * config.ScanArea.Radius)
                                    {
                                        if (markers.notes.Count < 12)
                                        {
                                            var colorIndex = -1;
                                            if (admin.searchQuery != string.Empty && player.displayName.ToLower().Contains(admin.searchQuery.ToLower()))
                                                colorIndex = 5;
                                            markers.notes.Add(GetVirtualMarker(player, colorIndex));
                                            admin.RenderedPlayers.Add(player);
                                        }
                                    }
                                }
                                admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceiveMarkers", markers);
                            }

                        }
                    }
                }
            }


            // Marker Player Map
            IEnumerable<AdminsDatabase.AdminData> markerAdmins = admins.Where(a => a.IsMapActive(Maps.Markers) || a.IsMapActive(Maps.Sleepers));
            if (markerAdmins.Count() > 0)
            {
                foreach (var admin in markerAdmins)
                {
                    var withSearch = admin.searchQuery != string.Empty;
                    bool usingSleeperMap = admin.IsMapActive(Maps.Sleepers);
                    bool renderWithoutLabels = admin.IsModActive(MapMods.WithoutLabels);
                    MapNoteList pings = Facepunch.Pool.Get<MapNoteList>();
                    pings.notes = Facepunch.Pool.GetList<MapNote>();
                    MapNoteList markers = Facepunch.Pool.Get<MapNoteList>();
                    markers.notes = Facepunch.Pool.GetList<MapNote>();
                    IEnumerable<BasePlayer> playersToRender = (usingSleeperMap ? BasePlayer.sleepingPlayerList : BasePlayer.activePlayerList);
                    IEnumerable<BasePlayer> searchResult = null;
                    if (withSearch)
                        searchResult = playersToRender.Where(player => player.displayName.ToLower().Contains(admin.searchQuery.ToLower()));
                    var scannerMarker = admin.scannerMarker;
                    admin.RenderedPlayers.Clear();
                    foreach (var player in playersToRender)
                    {
                        if (player == admin.currentPlayer || permission.UserHasPermission(player.UserIDString, PERMISSION_INVIS)) continue;
                        bool? existInSeachQuery = searchResult?.Contains(player);
                        var colorIndex = -1;
                        if (existInSeachQuery.HasValue && existInSeachQuery.Value)
                            colorIndex = 5;
                        if (scannerMarker != null && Vector3.Distance(player.transform.position, scannerMarker.worldPosition) < 150 * config.ScanArea.Radius)
                        {
                            if (markers.notes.Count < 12)
                            {
                                if (renderWithoutLabels || usingSleeperMap)
                                {
                                    if (!(usingSleeperMap && existInSeachQuery.HasValue && !existInSeachQuery.Value))
                                        markers.notes.Add(GetVirtualMarker(player, colorIndex));
                                    pings.notes.Add(GetPlayerNote(player, colorIndex, existInSeachQuery.HasValue && existInSeachQuery.Value));
                                }
                                else
                                    markers.notes.Add(GetPlayerNote(player, colorIndex, true));
                                admin.RenderedPlayers.Add(player);
                            }
                        }
                        else
                        {
                            bool withLabels = true;
                            if (renderWithoutLabels || usingSleeperMap)
                                withLabels = false;
                            if (existInSeachQuery.HasValue && existInSeachQuery.Value)
                                withLabels = true;
                            pings.notes.Add(GetPlayerNote(player, colorIndex, withLabels));
                        }

                    }
                    admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceivePings", pings);
                    admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceiveMarkers", markers);
                    markers.Dispose();
                    pings.Dispose();
                }
            }

            // Marker Cupboards Map
            IEnumerable<AdminsDatabase.AdminData> cupboardAdmins = admins.Where(a => a.IsMapActive(Maps.Cupboards));
            if (cupboardAdmins.Count() > 0)
            {
                foreach (var admin in cupboardAdmins)
                {
                    using (MapNoteList markers = Facepunch.Pool.Get<MapNoteList>())
                    {
                        IEnumerable<BuildingPrivlidge> privlidges = GetAllBuildingPrivlidges();
                        IEnumerable<BuildingPrivlidge> searchResult = null;
                        if (admin.searchQuery != string.Empty)
                            searchResult = privlidges.Where(priv => HasAuthorizadedPlayer(priv, admin.searchQuery));
                        markers.notes = Facepunch.Pool.GetList<MapNote>();
                        foreach (var priv in privlidges)
                        {
                            string label = null;
                            if (searchResult != null && searchResult.Contains(priv))
                            {
                                var __1 = priv.authorizedPlayers.Where(authPlayer => authPlayer.username.ToLower().Contains(admin.searchQuery.ToLower()));
                                var __2 = priv.authorizedPlayers.Where(authPlayer => !__1.Contains(authPlayer));
                                label = $"{string.Join(", ", __1.Select(p => p.username))}\n{string.Join(", ", __2.Select(p => p.username))}";
                            }
                            markers.notes.Add(GetTCNote(priv, label));
                        }
                        admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceiveMarkers", markers);
                    }
                }
            }
            
            // Marker Stashes Map
            IEnumerable<AdminsDatabase.AdminData> stashAdmins = admins.Where(a => a.IsMapActive(Maps.Stashes));
            if (stashAdmins.Count() > 0)
            {
                foreach (var admin in stashAdmins)
                {
                    using (MapNoteList markers = Facepunch.Pool.Get<MapNoteList>())
                    {
                        markers.notes = Facepunch.Pool.GetList<MapNote>();
                        admin.RenderedStashes.Clear();
                        IEnumerable<StashContainer> stashes = BaseNetworkable.serverEntities.OfType<StashContainer>().Where(stash => stash.inventory.itemList.Count > 0);
                        if (admin.searchQuery != string.Empty && ulong.TryParse(admin.searchQuery, out ulong searchUserId))
                            stashes = stashes.Where(stash => stash.OwnerID == searchUserId);
                        foreach (var stash in stashes)
                        {
                            markers.notes.Add(GetStashNote(stash));
                            admin.RenderedStashes.Add(stash);
                        }
                        admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceiveMarkers", markers);
                    }
                }
            }
            
            // Marker SleepingBags Map
            IEnumerable<AdminsDatabase.AdminData> sleepingbagsAdmins = admins.Where(a => a.IsMapActive(Maps.SleepingBags));
            if (sleepingbagsAdmins.Count() > 0)
            {
                foreach (var admin in sleepingbagsAdmins)
                {
                    using (MapNoteList markers = Facepunch.Pool.Get<MapNoteList>())
                    {
                        markers.notes = Facepunch.Pool.GetList<MapNote>();
                        admin.RenderedSleepingBags.Clear();
                        IEnumerable<SleepingBag> sleepingBags = BaseNetworkable.serverEntities.OfType<SleepingBag>(); 
                        IEnumerable<SleepingBag> searchResult = null;
                        if (admin.searchQuery != string.Empty && ulong.TryParse(admin.searchQuery, out ulong searchUserId))
                             searchResult = sleepingBags.Where(stash => stash.OwnerID == searchUserId);
                        foreach (var bag in sleepingBags)
                        {
                            var colorIndex = 3; 
                            bool? existInSeachQuery = searchResult?.Contains(bag);
                            if (existInSeachQuery.HasValue && existInSeachQuery.Value)
                                colorIndex = 5;

                            markers.notes.Add(new MapNote
                            {
                                label = null,
                                noteType = 1,
                                isPing = true,
                                icon = 7,
                                colourIndex = colorIndex,
                                worldPosition = bag.transform.position
                            });
                            admin.RenderedSleepingBags.Add(bag);
                        }
                        admin.currentPlayer.ClientRPCPlayer<MapNoteList>(null, admin.currentPlayer, "Client_ReceiveMarkers", markers);
                    }
                }
            }
        }


        IEnumerator Loop()
        {
            while (this.IsLoaded)
            {
                SendAdminMapData();
                yield return UnityEngine.CoroutineEx.waitForSeconds(config.LoopIntervals.Admin);
            }
            yield break;
        }
        IEnumerator PermissionsLoop()
        {
            while (this.IsLoaded)
            {
                SendMarkersToPlayersWithPerms();
                yield return UnityEngine.CoroutineEx.waitForSeconds(config.LoopIntervals.MarkersByPermission);
            }
            yield break;
        }
        #endregion

        #region Classes

        public enum Maps
        {
            Markers = 1,
            Text,
            Sleepers,
            Cupboards,
            Stashes,
            SleepingBags,
        }

        public enum MapMods
        {
            WithoutLabels = 1,
        }

        public static class AdminsDatabase
        {
            public class AdminData : IDisposable
            {
                public BasePlayer currentPlayer;
                public AdminData(BasePlayer player)
                {
                    currentPlayer = player;
                    RenderAdminMapPanel();
                }
                public void Dispose()
                {
                    if (currentPlayer == null || !currentPlayer.IsConnected)
                        return;

                    SelectedPlayer = null;
                    SelectedCupboard = null;
                    RestoreState(currentPlayer);
                    CuiHelper.DestroyUi(currentPlayer, "AdminMap / Panel");
                }

                public Maps active_map;
                public MapMods active_mods;

                public bool IsMapActive(Maps map)
                {
                    return active_map == map;
                }

                public bool IsModActive(MapMods map)
                {
                    return (this.active_mods & map) == map;
                }

                public void EnableMap(Maps map)
                {
                    if (this.IsMapActive(map))
                    {
                        return;
                    }
                    this.active_map = map;
                    RestoreState(currentPlayer);
                    RenderAdminMapPanel();
                    Instance.SendAdminMapData();
                }

                public void DisableMap()
                {
                    active_map = 0;
                    RestoreState(currentPlayer);
                    RenderAdminMapPanel();
                    Instance.SendAdminMapData();
                }

                public void ToggleMod(MapMods mod)
                {
                    if (this.IsModActive(mod))
                    {
                        this.active_mods &= ~mod;
                    }
                    else
                    {
                        this.active_mods |= mod;
                    }
                    RenderAdminMapPanel();
                    Instance.SendAdminMapData();
                }

                public bool HasActiveMap => this.active_map > 0;

                public string searchQuery = string.Empty;
                public MapNote scannerMarker;
                private BasePlayer _selectedPlayer;
                private BuildingPrivlidge _selectedCupboard;

                public BasePlayer SelectedPlayer
                {
                    get
                    {
                        return _selectedPlayer;
                    }
                    set
                    {
                        _selectedPlayer = value;
                        if (_selectedPlayer != null)
                            RenderGuiForPlayers();
                        else
                            CuiHelper.DestroyUi(currentPlayer, "AdminMap / Buttons");
                    }
                }

                public BuildingPrivlidge SelectedCupboard
                {
                    get
                    {
                        return _selectedCupboard;
                    }
                    set
                    {
                        _selectedCupboard = value;
                        if (_selectedCupboard != null)
                            RenderCupboardInfoGui(_selectedCupboard);
                        else
                            CuiHelper.DestroyUi(currentPlayer, "AdminMap / CupboardInfoGui");
                    }
                }
                public List<BasePlayer> RenderedPlayers { get; set; } = new List<BasePlayer>();
                public List<StashContainer> RenderedStashes { get; set; } = new List<StashContainer>();
                public List<SleepingBag> RenderedSleepingBags { get; set; } = new List<SleepingBag>();


                public virtual void RenderAdminMapPanel()
                {
                    var disabledButtonBackground = "0.31 0.302 0.294 1";
                    var disabledButtonIcon = "0.565 0.545 0.529 1";
                    var enabledButtonBackground = "0.3568628 0.4431373 0.2235294 1";
                    var enabledButtonIcon = "0.5490196 0.7764707 0.1921569 1";

                    var cui = new CUI();

                    int height = 7 * 41;

                    var __4 = cui.AddContainer(
                        anchorMin: "0 0.5",
                        anchorMax: "0 0.5",
                        offsetMin: $"4 -{height/2}",
                        offsetMax: $"49 {height/2}",
                        parent: "Hud.Menu",
                        name: "AdminMap / Panel");

                    cui.AddHImage(
                          CapsuleBackgroundVerticalCRC,
                          color: "0 0 0 1",
                          parent: __4);

                    int buttonIndex = 0;

                    var __5 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsMapActive(Maps.Markers) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddColorPanel(
                        color: IsMapActive(Maps.Markers) ? enabledButtonIcon : disabledButtonIcon,
                        sprite: "assets/content/ui/map/icon-map_pin.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "0 1",
                        offsetMax: "0 1",
                        parent: __5);

                    buttonIndex++;

                    var __6_1 = cui.AddColorPanel(
                        color: IsMapActive(Maps.Text) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __5);

                    cui.AddHImage(
                        LetterTCRC,
                        color: IsMapActive(Maps.Text) ? enabledButtonIcon : disabledButtonIcon,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "0 0",
                        offsetMax: "0 0",
                        parent: __6_1);

                    cui.AddButton(
                      $"adminmap.gui.cmd panel {buttonIndex}",
                       parent: __6_1);

                    buttonIndex++;

                    var __5_1 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsMapActive(Maps.Sleepers) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                       offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddColorPanel(
                        color: IsMapActive(Maps.Sleepers) ? enabledButtonIcon : disabledButtonIcon,
                        sprite: "assets/content/ui/map/icon-map_sleep.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "0 0",
                        offsetMax: "0 0",
                        parent: __5_1);

                    buttonIndex++;

                    var __5_2 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsMapActive(Maps.Cupboards) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddColorPanel(
                        color: IsMapActive(Maps.Cupboards) ? enabledButtonIcon : disabledButtonIcon,
                        sprite: "assets/content/ui/map/icon-map_home.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "-4 -5",
                        offsetMax: "4 3",
                        parent: __5_2);

                    buttonIndex++;

                    var __5_3 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsMapActive(Maps.Stashes) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddHImage(
                        StashCRC,
                        color: IsMapActive(Maps.Stashes) ? enabledButtonIcon : disabledButtonIcon,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "6 6",
                        offsetMax: "-6 -6",
                        parent: __5_3);

                    buttonIndex++;

                    var __55 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsMapActive(Maps.SleepingBags) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddHImage(
                        SleepingBagCRC,
                        color: IsMapActive(Maps.SleepingBags) ? enabledButtonIcon : disabledButtonIcon,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "6 6",
                        offsetMax: "-6 -6",
                        parent: __55);

                    buttonIndex++;

                    var __5_4 = cui.AddButton(
                        $"adminmap.gui.cmd panel {buttonIndex}",
                        color: IsModActive(MapMods.WithoutLabels) ? enabledButtonBackground : disabledButtonBackground,
                        sprite: "assets/icons/circle_closed.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0.5 1",
                        anchorMax: "0.5 1",
                        offsetMin: $"-20 -{(buttonIndex + 1) * 40 + 2.2f}",
                        offsetMax: $"20 -{buttonIndex * 40 + 2.2f}",
                        parent: __4);

                    cui.AddColorPanel(
                        color: IsModActive(MapMods.WithoutLabels) ? enabledButtonIcon : disabledButtonIcon,
                        sprite: "assets/content/ui/hypnotized.png",
                        imageType: UnityEngine.UI.Image.Type.Simple,
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "4 4",
                        offsetMax: "-4 -4",
                        parent: __5_4);

                    buttonIndex++;

                    cui.RenderWithDestroy(currentPlayer);
                }

                public void RenderGuiForPlayers()
                {
                    CUI cui = new CUI();
                    var container = cui.AddContainer(
                        anchorMin: "0.12 0.96",
                        anchorMax: "0.12 0.96",
                        offsetMin: $"0 -70",
                        offsetMax: $"{5 * 70} -0",
                        parent: "Hud.Menu",
                        name: "AdminMap / Buttons");
                    var nicknameContainer = cui.AddContainer(
                           anchorMin: "0 1",
                           anchorMax: "0 1",
                           offsetMin: "30 0",
                           offsetMax: "450 40",
                           parent: container);
                    cui.AddText(
                           text: SelectedPlayer.ToString(),
                           font: CUI.Font.DroidSansMono,
                           fontSize: 14,
                           align: TextAnchor.MiddleLeft,
                           parent: nicknameContainer);
                    cui.AddButton(
                           command: $"adminmap.gui.cmd showinfo",
                           parent: nicknameContainer);

                    int buttonCounter = 0;

                    foreach (var button in config.Buttons)
                    {
                        if (button.Permission != string.Empty && !Instance.permission.UserHasPermission(currentPlayer.UserIDString, $"adminmap.{button.Permission}"))
                            continue;

                        string[] commands = button.Command.Split(';');
                        for (int i = 0; i < commands.Length; i++)
                        {
                            string command = commands[i];
                            if (command.First() == '/')
                                commands[i] = command.Replace(command, $"chat.say \"{command}\"");
                        }
                        button.Command = string.Join(";", commands);
                        var circleContaner = cui.AddContainer(
                           anchorMin: "0 0",
                           anchorMax: "0 1",
                           offsetMin: $"{buttonCounter * 70} 0",
                           offsetMax: $"{(buttonCounter + 1) * 70} 0",
                           parent: container);
                        cui.AddText(
                           text: button.Label,
                           font: CUI.Font.RobotoCondensedBold,
                           fontSize: 10,
                           align: TextAnchor.MiddleCenter,
                           color: "1 1 1 1",
                           anchorMin: "0 0",
                           anchorMax: "1 1",
                           parent: circleContaner);
                        cui.AddButton(
                            color: button.ColorString,
                            sprite: "assets/icons/circle_open.png",
                            imageType: UnityEngine.UI.Image.Type.Simple,
                            command: $"adminmap.gui.cmd command {button.Command}",
                            parent: circleContaner);
                        buttonCounter++;
                    }

                    cui.RenderWithDestroy(currentPlayer);
                }

                private void RenderCupboardInfoGui(BuildingPrivlidge cupboard)
                {
                    var cui = new CUI();
                    var __1 = cui.AddColorPanel(
                        color: "0.05 0.05 0.05 0.85",
                        sprite: "assets/content/ui/ui.background.rounded.png",
                        imageType: UnityEngine.UI.Image.Type.Tiled,
                        anchorMin: "0 0",
                        anchorMax: "0 0",
                        offsetMin: "200 10",
                        offsetMax: "400 360",
                        parent: "Hud.Menu",
                        name: "AdminMap / CupboardInfoGui");
                    List<global::ItemAmount> list = Facepunch.Pool.GetList<global::ItemAmount>();
                    cupboard.CalculateUpkeepCostAmounts(list);
                    cui.AddText(
                        text:
                            $"<b>Authorized players:</b>\n" +
                            string.Concat(cupboard.authorizedPlayers.Select(player => (searchQuery != string.Empty && player.username.ToLower().Contains(searchQuery.ToLower()) ? $"<color=yellow>{player.username}</color>\n" : $"{player.username}\n"))) +
                            "<b>Upkeep Cost:</b>\n" +
                            string.Concat(list.Select(itemAmount => $"{(int)itemAmount.amount} {itemAmount.itemDef.displayName.translated}\n")),
                        font: CUI.Font.RobotoCondensedRegular,
                        fontSize: 16,
                        align: TextAnchor.MiddleCenter,
                        color: "1 1 1 1",
                        anchorMin: "0 0",
                        anchorMax: "1 1",
                        offsetMin: "10 30",
                        offsetMax: "-10 -10",
                        parent: __1);
                    Facepunch.Pool.FreeList<global::ItemAmount>(ref list);
                    var buttonContaner = cui.AddColorPanel(
                          color: "0.7 0 0 0.5",
                          anchorMin: "0 0",
                          anchorMax: "1 0",
                          offsetMin: $"0 0",
                          offsetMax: $"0 20",
                          parent: __1);
                    cui.AddText(
                       text: "Teleport",
                       font: CUI.Font.RobotoCondensedBold,
                       fontSize: 10,
                       align: TextAnchor.MiddleCenter,
                       color: "1 1 1 1",
                       anchorMin: "0 0",
                       anchorMax: "1 1",
                       parent: buttonContaner);
                    cui.AddButton(
                        command: $"teleportpos ({cupboard.transform.position.x},{cupboard.transform.position.y},{cupboard.transform.position.z})",
                        parent: buttonContaner);
                    cui.RenderWithDestroy(currentPlayer);
                }

            }

            static Dictionary<ulong, AdminData> Database = new Dictionary<ulong, AdminData>();
            public static AdminData Get(BasePlayer player)
            {
               
                AdminData data = null;
                Database.TryGetValue(player.userID, out data);
                if (player.IsConnected && player.UserIDString != null && Instance.permission.UserHasPermission(player.UserIDString, PERMISSION_TO_USE))
                {
                    if (data == null)
                        Database.Add(player.userID, data = new AdminData(player));
                }
                else if (data != null)
                {
                    if (Database.Remove(player.userID))
                    {
                        data.Dispose();
                        return null;
                    }
                }
                return data;
            }
            public static void Clear()
            {
                foreach (var admin in Database.Values)
                    admin.Dispose();
                Database.Clear();
            }
        }
        #endregion

        #region Config
        static Configuration config;
        public class Configuration
        {
            public class ScanAreaSettings
            {
                [JsonProperty(PropertyName = "Radius")]
                public float Radius { get; set; } = 2f;
                [JsonProperty(PropertyName = "Color")]
                public string ColorString { get; set; } = "0 0.7 0 0.6";

                [JsonIgnore]
                public Color Color
                {
                    get
                    {
                        string[] colorSplit = ColorString.Split(' ');
                        return new Color(float.Parse(colorSplit[0]), float.Parse(colorSplit[1]), float.Parse(colorSplit[2]), float.Parse(colorSplit[3]));
                    }
                }
            }
            [JsonProperty(PropertyName = "Scan Area Settings")]
            public ScanAreaSettings ScanArea { get; set; } = new ScanAreaSettings();
            public class Intervals
            {
                [JsonProperty(PropertyName = "Admin Loop")]
                public float Admin { get; set; } = 1f;

                [JsonProperty(PropertyName = "Markers by permissions")]
                public float MarkersByPermission { get; set; } = 3f;
            }

            [JsonProperty(PropertyName = "Loop intervals (in seconds)")]
            public Intervals LoopIntervals { get; set; } = new Intervals();
            public class Button
            {
                public Button() { }
                public Button(string permission, string label, string command)
                {
                    this.Permission = permission;
                    this.Label = label;
                    this.Command = command;
                }
                public Button(string permission, string label, string command, string color) : this(permission, label, command)
                {
                    this.ColorString = color;
                }

                [JsonProperty(PropertyName = "Permission (adminmap.<perm>)")]
                public string Permission { get; set; } = string.Empty;

                [JsonProperty(PropertyName = "Label")]
                public string Label { get; set; } = string.Empty;

                [JsonProperty(PropertyName = "Command")]
                public string Command { get; set; } = string.Empty;

                [JsonProperty(PropertyName = "Color")]
                public string ColorString { get; set; } = "1 1 1 1";
            }

            public class TMSettings
            {

                [JsonProperty(PropertyName = "Font size")]
                public int FontSize { get; set; } = 16;
                [JsonProperty(PropertyName = "Use color generation for teams?")]
                public bool UseGeneratedTeamsColors { get; set; } = true;
                [JsonProperty(PropertyName = "Color for searches")]
                public string SearchColor { get; set; } = "00ffff";
                [JsonProperty(PropertyName = "Color for teams")]
                public string TeamsColor { get; set; } = "ffaf4d";
                [JsonProperty(PropertyName = "Color for solo players")]
                public string SoloColor { get; set; } = "9bd92f";
                [JsonProperty(PropertyName = "Color for sleepers")]
                public string SleepersColor { get; set; } = "404040";
            }

            [JsonProperty(PropertyName = "Text Map Settings")]
            public TMSettings TextMapSettings { get; set; } = new TMSettings();

            [JsonProperty(PropertyName = "Command Buttons")]
            public IList<Button> Buttons { get; set; } = new ReadOnlyCollection<Button>(new List<Button>
            {
                new Button(string.Empty, "TP", "teleport {steamid}"),
                new Button(string.Empty, "TP2ME", "teleport {steamid} {admin.steamid}"),
                new Button(string.Empty, "INV", "/viewinv {username}"),
                new Button(string.Empty, "SPECTATE", "spectate {steamid}"),
                new Button(string.Empty, "KILL", "kill {steamid}", "1 0 0 1"),
                new Button(string.Empty, "KICK", "kick {steamid}", "1 0 0 1"),
            });

            public static Configuration DefaultConfig() => new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }

        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Language
        public static class LangKeys
        {
            public const string NO_PERMS = nameof(NO_PERMS);
            public const string ENABLE = nameof(ENABLE);
            public const string DISABLE = nameof(DISABLE);
            public const string TEXT_MAP_ENABLED = nameof(TEXT_MAP_ENABLED);
            public const string SLEEPERS_MAP_ENABLED = nameof(SLEEPERS_MAP_ENABLED);
            public const string CUPBOARDS_MAP_ENABLED = nameof(CUPBOARDS_MAP_ENABLED);
            public const string SEARCH_QUERY_RESULT = nameof(SEARCH_QUERY_RESULT);
            public const string SEARCH_QUERY_CLEAR = nameof(SEARCH_QUERY_CLEAR);
            public const string WL_ENABLED = nameof(WL_ENABLED);
            public const string WL_DISABLED = nameof(WL_DISABLED);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.NO_PERMS] = "You dont have permission to use this command.",
                [LangKeys.ENABLE] = "Admin map enabled",
                [LangKeys.DISABLE] = "Admin map disabled",
                [LangKeys.TEXT_MAP_ENABLED] = "Text ADMIN MAP ENABLED",
                [LangKeys.SLEEPERS_MAP_ENABLED] = "SLEEPERS ADMIN MAP ENABLED",
                [LangKeys.CUPBOARDS_MAP_ENABLED] = "CUPBOARDS ADMIN MAP ENABLED",
                [LangKeys.WL_ENABLED] = "MARKERS WITHOUT LABLES ENABLED",
                [LangKeys.WL_DISABLED] = "MARKERS WITHOUT LABLES DISABLED",
                [LangKeys.SEARCH_QUERY_RESULT] = "The query \"{0}\" found:",
                [LangKeys.SEARCH_QUERY_CLEAR] = "The search query has been cleared."
            }, this);
        }
        private static string GetMessage(string langKey, string userID) => Instance.lang.GetMessage(langKey, Instance, userID);
        private static string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);

        #endregion
    }

    #region 0xF UI Library
    partial class AdminMap
    {
        public class CUI
        {
            public CuiElementContainer ElementContainer { get; set; } = new CuiElementContainer();

            readonly string[] FontNames = new string[] {
            "RobotoCondensed-Bold.ttf",
            "RobotoCondensed-Regular.ttf",
            "DroidSansMono.ttf",
            "PermanentMarker.ttf"
        };

            public enum Font
            {
                RobotoCondensedBold,
                RobotoCondensedRegular,
                DroidSansMono,
                PermanentMarker
            }
            public string AddText(
               string text = "Text",
               string color = "1 1 1 1",
               Font font = Font.RobotoCondensedRegular,
               int fontSize = 14,
               TextAnchor align = TextAnchor.UpperLeft,
               VerticalWrapMode overflow = VerticalWrapMode.Overflow,
               string anchorMin = "0 0",
               string anchorMax = "1 1",
               string offsetMin = "0 0",
               string offsetMax = "0 0",
               float fadeIn = 0f,
               float fadeOut = 0f,
               string parent = "Hud",
               string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiTextComponent
                         {
                             Text = text,
                             Color = color,
                             Font = FontNames[(int)font],
                             VerticalOverflow = overflow,
                             FontSize = fontSize,
                             Align = align,
                             FadeIn = fadeIn
                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    FadeOut = fadeOut,
                    Parent = parent,
                    Name = name,

                };
                Add(element);
                return name;
            }
            public string AddOutlinedText(
               string text = "Text",
               string color = "1 1 1 1",
               Font font = Font.RobotoCondensedRegular,
               int fontSize = 14,
               TextAnchor align = TextAnchor.UpperLeft,
               VerticalWrapMode overflow = VerticalWrapMode.Overflow,
               string outlineColor = "0 0 0 1",
               float outlineWidth = 1,
               string anchorMin = "0 0",
               string anchorMax = "1 1",
               string offsetMin = "0 0",
               string offsetMax = "0 0",
               float fadeIn = 0f,
               float fadeOut = 0f,
               string parent = "Hud",
               string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiTextComponent
                         {
                             Text = text,
                             Color = color,
                             Font = FontNames[(int)font],
                             VerticalOverflow = overflow,
                             FontSize = fontSize,
                             Align = align,
                             FadeIn = fadeIn
                         },
                         new CuiOutlineComponent
                         {
                             Color = outlineColor,
                             Distance = $"{outlineWidth} {-outlineWidth}"
                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                             OffsetMax =  offsetMax
                         },
                    },
                    FadeOut = fadeOut,
                    Parent = parent,
                    Name = name
                };
                Add(element);
                return name;
            }

            public string AddInputfield(
              string command,
              string text = "Enter text here...",
              string color = "1 1 1 1",
              Font font = Font.RobotoCondensedRegular,
              int fontSize = 14,
              TextAnchor align = TextAnchor.UpperLeft,
              string anchorMin = "0 0",
              string anchorMax = "1 1",
              string offsetMin = "0 0",
              string offsetMax = "0 0",
              bool needsKeyboard = true,
              bool autoFocus = false,
              bool isPassword = false,
              int charsLimit = 0,
              string parent = "Hud",
              string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiInputFieldComponent
                         {
                             Text = text,
                             Color = color,
                             Font = FontNames[(int)font],
                             FontSize = fontSize,
                             Align = align,
                             Autofocus = autoFocus,
                             Command = command,
                             IsPassword = isPassword,
                             CharsLimit = charsLimit,
                             NeedsKeyboard = needsKeyboard,
                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                             OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name
                };
                Add(element);
                return name;
            }

            public string AddPanel(
               string color = "0 0 0 0",
               string sprite = "assets/content/ui/ui.background.tile.psd",
               string material = "assets/icons/iconmaterial.mat",
               UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple,
               string anchorMin = "0 0",
               string anchorMax = "1 1",
               string offsetMin = "0 0",
               string offsetMax = "0 0",
               float fadeIn = 0f,
               float fadeOut = 0f,
               bool cursorEnabled = false,
               bool keyboardEnabled = false,
               string parent = "Hud",
               string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiPanel panel = new CuiPanel
                {
                    Image =
                {
                    Color = color,
                    Sprite = sprite,
                    Material = material,
                    ImageType = imageType,
                    FadeIn = fadeIn,
                },
                    RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax,
                },
                    CursorEnabled = cursorEnabled,
                    KeyboardEnabled = keyboardEnabled,
                    FadeOut = fadeOut
                };
                ElementContainer.Add(panel, parent, name);
                return name;
            }
            public string AddButton(
                string command,
                string color = "0 0 0 0",
                string sprite = "assets/content/ui/ui.background.tile.psd",
                string material = "assets/icons/iconmaterial.mat",
                UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple,
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                float fadeIn = 0f,
                float fadeOut = 0f,
                string parent = "Hud",
                 string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiButton button = new CuiButton
                {
                    Button =
                {
                    Close = "",
                    Command = command,
                    Color = color,
                    Sprite = sprite,
                    Material = material,
                    ImageType = imageType,
                    FadeIn = fadeIn,
                },
                    RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax,
                },
                    FadeOut = fadeOut,
                };
                ElementContainer.Add(button, parent, name);
                return name;
            }


            public string AddImage(
                string content,
                string color = "1 1 1 1",
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                float fadeIn = 0f,
                float fadeOut = 0f,
                string parent = "Hud",
                 string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiRawImageComponent()
                         {
                             Color = color,
                             Png = content,
                             Sprite = "assets/content/textures/generic/fulltransparent.tga",
                             FadeIn = fadeIn
                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name,
                    FadeOut = fadeOut
                };
                Add(element);
                return name;
            }
            public string AddHImage(string content,
                string color = "1 1 1 1",
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                string parent = "Hud",
                 string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiRawImageComponent()
                         {
                             Color = color,
                             Png = content,
                             Sprite = "assets/content/textures/generic/fulltransparent.tga",
                             Material = "assets/icons/iconmaterial.mat"
                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name
                };
                Add(element);
                return name;
            }
            public string AddIcon(
                int itemId,
                ulong skin = 0,
                string color = "1 1 1 1",
                string sprite = "assets/content/ui/ui.background.tile.psd",
                string material = "assets/icons/iconmaterial.mat",
                UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple,
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                string parent = "Hud",
                 string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiImageComponent()
                         {
                             Color = color,
                             ItemId = itemId,
                             SkinId = skin,
                             Sprite = sprite,
                             Material = material,
                             ImageType = imageType,

                         },
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name
                };
                Add(element);
                return name;
            }
            public string AddColorPanel(
                string color = "0 0 0 0",
                string sprite = "assets/content/ui/ui.background.tile.psd",
                string material = "assets/icons/iconmaterial.mat",
                UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple,
                string outlineColor = "0 0 0 0",
                float outlineWidth = 2,
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                string parent = "Hud",
                 string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new CuiImageComponent
                         {
                             Color = color,
                             Sprite = sprite,
                             Material = material,
                             ImageType = imageType,

                         },
                         new CuiOutlineComponent
                         {
                             Color = outlineColor,
                             Distance = $"{outlineWidth} {-outlineWidth}"
                         },
                         new  CuiRectTransformComponent
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name
                };
                Add(element);
                return name;
            }
            public string AddContainer(
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0",
                string parent = "Hud",
                string name = null)
            {
                if (name == null)
                    name = CuiHelper.GetGuid();
                CuiElement element = new CuiElement
                {
                    Components =
                    {
                         new  CuiRectTransformComponent()
                         {
                              AnchorMin = anchorMin,
                              AnchorMax = anchorMax,
                              OffsetMin = offsetMin,
                              OffsetMax =  offsetMax
                         },
                    },
                    Parent = parent,
                    Name = name,
                };
                Add(element);
                return name;
            }

            public void Add(CuiElement element)
            {
                // element.Name = $"{element.Parent}/{element.Name}";
                ElementContainer.Add(element);
            }

            public void Render(BasePlayer player) => CuiHelper.AddUi(player, ElementContainer);
            public void RenderWithDestroy(BasePlayer player, int countElements = 1)
            {

                if (countElements == 0) return;
                if (ElementContainer.Count > 0)
                {
                    for (int i = 0; i < (countElements == -1 ? ElementContainer.Count : countElements); i++)
                    {
                        var element = ElementContainer.ElementAt(i);
                        if (element != null && element.Name != null && element.Name != string.Empty)
                            CuiHelper.DestroyUi(player, element.Name);
                    }

                }
                Render(player);
            }
        }
    }
    #endregion
}