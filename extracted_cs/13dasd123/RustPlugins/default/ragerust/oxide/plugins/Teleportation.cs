using VLB;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Physics = UnityEngine.Physics;
using ConVar;
using Time = UnityEngine.Time;
using System.Collections.Generic;
using Color = UnityEngine.Color;
using Oxide.Core.Libraries.Covalence;
using System;
using Oxide.Core;
using System.Drawing;
using Rust;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Teleportation", "BadMandarin", "1.1.4")]
    [Description("Teleportation")]
    class Teleportation : RustPlugin
    {

        private string GetShortMonumentName(string fullName)
        {
            var split = fullName.Split('/');
            return split[split.Length - 1].Replace(".prefab", string.Empty);
        }
        
                
        [ChatCommand("autotp")]
        private void ChatCommandAtp(BasePlayer player, string command, string[] args)
        {
            var data = PluginData.GetForPlayer(player.userID);
            if (data == null)
                return;

            data.AutoAccept = !data.AutoAccept;
            SendMessage(player, "ATP.MODE", GetMessage(player, data.AutoAccept ? "ENABLED" : "DISABLED"));
        }
        
        private string GetImage(string name)
        {
            string ID = (string)ImageLibrary?.Call("GetImage", name);
            if (ID == "")
                ID = (string)ImageLibrary?.Call("GetImage", name) ?? ID;
        
            return ID;
        }

                
                
        private string CanTeleportPlayer(BasePlayer player)
        {
            if (player.IsSwimming() && _config.SettingsResolution.CantTeleportSwimming) 
                return GetMessage(player, "CAN.TP.SWIMMING");
             
            if (!player.IsAlive()) 
                return GetMessage(player, "CAN.TP.ALIVE");
            
            if (_config.SettingsResolution.CantTeleportMounted && player.isMounted) 
                return GetMessage(player, "CAN.TP.MOUNTED");
                
            if (_config.SettingsResolution.CantTeleportParent && player.HasParent()) 
                return GetMessage(player, "CAN.TP.PARENT");
            
            if (_config.SettingsResolution.CantTeleportWater && player.IsSwimming()) 
                return GetMessage(player, "CAN.TP.WATER");

            if (player.metabolism.radiation_poison.value > _config.SettingsResolution.SettingsRadiation.NeedRadiationAmount && _config.SettingsResolution.SettingsRadiation.CantTeleportRadiation) 
                return GetMessage(player, "CAN.TP.RADIATION");

            if (player.metabolism.bleeding.value > _config.SettingsResolution.SettingsBleeding.NeedBleedingAmount && _config.SettingsResolution.SettingsBleeding.CantTeleportBleeding) 
                return GetMessage(player, "CAN.TP.BLEEDING");

            if (player.IsWounded() && _config.SettingsResolution.CantTeleportWounded) 
                return GetMessage(player, "CAN.TP.WOUNDED");
            
            if (player.IsWounded() && _config.SettingsResolution.CantTeleportCrawling) 
                return GetMessage(player, "CAN.TP.CRAWLING");

            if (player.metabolism.temperature.value < _config.SettingsResolution.SettingsTemperature.MinNeedTemperature && _config.SettingsResolution.SettingsTemperature.CantTeleportTemperature) 
                return GetMessage(player, "CAN.TP.TEMPERATURE");
             
            var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
            if (privilege != null && !player.IsBuildingAuthed() && _config.SettingsResolution.CantTeleportInBuild) 
                return GetMessage(player, "CAN.TP.BUILDINGBLOCK");
             
            return "";
        }

        private void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));
        
        private float GetTeleportTimeHome(ulong uid) 
        {
            float min = 9999f;
            bool set = false;
            foreach (var privilege in _config.SettingsHome.TeleportationTime)
            {
                if (permission.UserHasPermission(uid.ToString(), privilege.Key))
                {
                    min = Mathf.Min(min, privilege.Value);
                    set = true;
                }
                
            }
            
            return set ? min : -1f;
        }
        
        [ConsoleCommand("tp")]
        void ConsoleCmdTp(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandTp(player, "", arg.Args);
        }

        private class PlayerData
        {
            public List<Home> HomesList = new List<Home>();
            public double CooldownHome = 0f;
            public double CooldownTeleport = 0f;
            public bool AutoAccept = true;

            public Dictionary<string, double> MonumentsCooldown = new Dictionary<string, double>();

            public class Home
            {
                public Home(Vector3 homePos, string homeName, uint sleepingBagId = 0)
                {
                    HomePos = homePos;
                    HomeName = homeName;
                    SleepingBagId = sleepingBagId;
                }

                public Vector3 HomePos;
                public string HomeName;
                public uint SleepingBagId;
            }

            public double GetMonumentCooldown(string monumentName)
            {
                double time = 0;
                MonumentsCooldown.TryGetValue(monumentName, out time);

                return time;
            }
            
            public Home GetHome(string name)
            {
                return HomesList.Find(x => x.HomeName == name);
            }

            public Home GetHome(Vector3 pos)
            {
                return HomesList.Find(x => Vector3.Distance(x.HomePos, pos) < 1f);
            }
        }
        private const string LayerNotification = "UI.Teleportation.Notification";
        
        void OnServerSave()
        {
            PluginData.SaveData();
        }
        
        public class SettingsRadiation
        {
            [JsonProperty("Кол-во радиации, при которой будет вызываться запрет на телепортацию [Amount of radiation that will cause a ban on teleportation]")]
            public float NeedRadiationAmount = 5f;

            [JsonProperty("Запретить телепортацию при радиации? [Prohibit teleportation during radiation?]")]
            public bool CantTeleportRadiation = true;
        }
        private const string LayerBlur = "UI.Teleportation.Layer";
        
        private void Teleport(BasePlayer player, BasePlayer target)
        {
            //if (target.GetComponentInParent<Component>() != null ||
            //    target.GetComponentInParent<HotAirBalloon>() != null || target.GetComponentInParent<Spawnable>() != null || (target.GetParentEntity() != null && target.GetParentEntity().name.Contains("cave")) || (target.GetParentEntity() != null && target.GetParentEntity().name.Contains("junkpile_water")) || (target.GetParentEntity() != null && target.GetParentEntity().name.Contains("cargo")))
            if(target.HasParent())
            {
                player.PauseFlyHackDetection(10f);
                player.SetParent(target.GetParentEntity());
                player.SendNetworkUpdateImmediate();
            }
            
            Teleport(player, target.transform.position, target);
        }
        
        private List<BuildingBlock> GetFoundation(Vector3 position)
        {
            RaycastHit hit;
            var entities = new List<BuildingBlock>();

            if (Physics.Raycast(position + new Vector3(0f, 0.2f, 0f), Vector3.down, out hit, 3f, blockLayer) && hit.GetEntity().IsValid())
            {
                var entity = hit.GetEntity();
                if (_config.SettingsHome.AvailableFoundations.Contains(entity.PrefabName))
                {
#if DEBUG
                    Puts($"  GetFoundation() found {entity.PrefabName} at {entity.transform.position}");
#endif
                    entities.Add(entity as BuildingBlock);
                }
            }
            else
            {
#if DEBUG
                Puts("  GetFoundation() none found.");
#endif
            }

            return entities;
        }

        [ConsoleCommand("tpr")]
        void ConsoleCmdTpr(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandTpr(player, "", arg.Args);
        }
        
        [ChatCommand("tpa")]
        private void ChatCommandTpa(BasePlayer player, string command, string[] args)
        {
            var teleportMgr = TeleportMgr.GetManager(player);
            if (teleportMgr == null)
                return;
            
            if (teleportMgr.HasRequest())
            {
                SendMessage(player, "TPA.HAVE.REQUEST");
                return;
            }

            var pendingRequest = teleportMgr.IncomingRequests.FirstOrDefault(x => x.RequestConfirmed == false);
            if (pendingRequest == null)
            {
                SendMessage(player, "TPA.ZERO.REQUEST");
                return;
            }

            if (pendingRequest.RequestConfirmed)
            {
                SendMessage(player, "TPA.REQUEST.CONFIRMED");
                return;
            }

            var pendingPlayer = pendingRequest.Player;

            var pendingTeleportMgr = TeleportMgr.GetManager(pendingPlayer);
            if (pendingTeleportMgr == null)
                return;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            if (player.IsWounded() && _config.SettingsResolution.CantTeleportWounded)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "TP.CANCEL.BLOOD.OR.WOUNDED");
                return;
            }

            if (player.metabolism.bleeding.value > _config.SettingsResolution.SettingsBleeding.NeedBleedingAmount
                && _config.SettingsResolution.SettingsBleeding.CantTeleportBleeding)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "TP.CANCEL.BLOOD.OR.WOUNDED");
                return;
            }

            if (_config.SettingsResolution.CantTeleportMounted && player.isMounted)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "CAN.TP.MOUNTED"); 
                return;
            }
            
            if (_config.SettingsResolution.CantTeleportParent && player.HasParent())
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "CAN.TP.PARENT"); 
                return;
            }
            
            if (_config.SettingsResolution.CantTeleportWater && player.IsSwimming())
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "CAN.TP.WATER"); 
                return;
            }

            if (player.IsBuildingBlocked() && _config.SettingsResolution.CantTeleportInBuild)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "TP.CANCEL.BUILD.DONT.AUTH"); 
                return;
            }

            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendMessageWithoutLang(player, ret);
                return;
            }
            
            SendMessage(player, "TPA.PLAYER.ACCEPT", pendingPlayer.displayName);
            SendMessage(pendingPlayer, "TPA.TARGET.ACCEPT");
            
            CuiHelper.DestroyUi(player, LayerNotification);
            
            //teleportMgr.IncomingRequest = null;
            pendingTeleportMgr.AcceptRequest();
            
        }
        
        private float GetTeleportCooldownTpr(ulong uid)
        {
            float min = 9999f;
            bool set = false;
            
            foreach (var privilege in _config.SettingsTeleport.CooldownTeleportation)
            {
                if (permission.UserHasPermission(uid.ToString(), privilege.Key))
                {
                    min = Mathf.Min(min, privilege.Value);
                    set = true;
                }
                
            } 
            
            return set ? min : -1f;
        }  
        
        private string CheckFoundation(ulong userID, Vector3 position)
        {
            if (CheckInsideBattery(position) != null)
                return "FOUNDATION.NOT.FOUND";
            
            if (UnderneathFoundation(position))
                return "FOUNDATION.NOT.FOUND";
            
#if DEBUG
                Puts($"CheckFoundation() looking for foundation at {position}");
#endif
            var entities = GetFoundation(position);

            entities.RemoveAll(x => !x.IsValid() || x.IsDestroyed);
            if (entities.Count == 0) 
                return "FOUNDATION.NOT.FOUND";

            if (!_config.SettingsHome.CheckFoundationForOwner) 
                return null;
            
            for (var i = 0; i < entities.Count; i++)
            {
                if (IsFriend(userID, entities[i].OwnerID)) return null;
            }

            return "FOUNDATION.NOT.OWNED";
        }
        
        private void AddImage(string name)
        {
            if (HasImage(name))
                return;
            
            ImageLibrary?.Call("AddImage", name, name);
            _imagesLoading.Add(name);
        }

        private static class PlayerHelper
        {
            private static bool FindPlayerPredicate(BasePlayer player, string nameOrUserId)
            {
                return player.displayName.IndexOf(nameOrUserId, StringComparison.OrdinalIgnoreCase) != -1 ||
                       player.UserIDString == nameOrUserId;
            }

            public static bool Find(string nameOrUserId, out BasePlayer target)
            {
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        target = activePlayer;
                        return true;
                    }
                }

                foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (FindPlayerPredicate(sleepingPlayer, nameOrUserId))
                    {
                        target = sleepingPlayer;
                        return true;
                    }
                }

                target = (BasePlayer) null;
                return false;
            }

            public static bool FindOnlineMultiple(string nameOrUserId, BasePlayer player, out BasePlayer target)
            {
                if (nameOrUserId == string.Empty)
                {
                    _plugin.SendMessage(player, "PLAYER.NOT.FOUND");

                    target = (BasePlayer) null;
                    return false;
                }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                List<BasePlayer> activePlayers = new List<BasePlayer>();
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        activePlayers.Add(activePlayer);
                    }
                }

                switch (activePlayers.Count)
                {
                    case 0:
                        _plugin.SendMessage(player, $"PLAYER.NOT.FOUND.PARAM", nameOrUserId);
                        target = null;
                        return false;

                    case 1:
                        target = activePlayers[0];
                        return true;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                    default:
                        var text = "";
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                        foreach (var activePlayer in activePlayers)
                        {
                            text += $" {activePlayer.displayName},";
                        }

                        _plugin.SendMessage(player, "PLAYER.FIND.MULTIPLE"); 
                        target = null;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                        return false;
                }
            }
            
            public static bool FindOnlineAndOfflineMultiple(string nameOrUserId, BasePlayer player, out BasePlayer target)
            {
                if (nameOrUserId == string.Empty)
                {
                    _plugin.SendMessage(player, "PLAYER.NOT.FOUND");

                    target = (BasePlayer) null;
                    return false;
                }

                List<BasePlayer> activePlayers = new List<BasePlayer>();
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        activePlayers.Add(activePlayer);
                    }
                }
                foreach (BasePlayer activePlayer in BasePlayer.sleepingPlayerList)
                {
                    if (FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        activePlayers.Add(activePlayer);
                    }
                }

                switch (activePlayers.Count)
                {
                    case 0:
                        _plugin.SendMessage(player, $"PLAYER.NOT.FOUND.PARAM", nameOrUserId);
                        target = null;
                        return false;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                    case 1:
                        target = activePlayers[0];
                        return true;

                    default:
                        var text = "";
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                        foreach (var activePlayer in activePlayers)
                        {
                            text += $" {activePlayer.displayName},";
                        }

                        _plugin.SendMessage(player, "PLAYER.FIND.MULTIPLE"); 
                        target = null;

                        return false;
                }
            }

            public static bool FindOnline(string nameOrUserId, out BasePlayer target)
            {
                if (nameOrUserId == string.Empty)
                {
                    target = (BasePlayer) null;
                    return false;
                }
 
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        target = activePlayer;
                        return true;
                    }
                }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                target = (BasePlayer) null;
                return false;
            }

        }
        
        private void StartPluginLoad()
        {
            if (ImageLibrary != null)
            {
                //Load your images here
                AddImage(TeleportImage);
                CheckStatus();
            }
            else
            {
                PrintError($"ImageLibrary not found! Please, check your plugins list.");
            }
        }
        
        private MonumentInfo FindMonument(string shortPrefabName)
        {
            return TerrainMeta.Path.Monuments.Find(x => x.name.Contains(shortPrefabName));
        }
        
        object OnSleepingBagValidCheck(SleepingBag bag, ulong targetPlayerID, bool ignoreTimers)
        {
            if (_config.SettingsMonument.AvailableMonuments.Exists(x => x.CreatedBag != null && x.CreatedBag == bag))
            {
                var player = BasePlayer.FindByID(targetPlayerID);
                if (player != null && player.IsHostile())
                    return false;
                
                return true;
            }
            
            return null;
        }

        [ChatCommand("home")] 
        private void ChatCommandHome(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "HOME.HELP");
                return;
            }

            if (GetTeleportTimeHome(player.userID) <= 0)
                return;

            var data = PluginData.GetForPlayer(player.userID);
            var homeName = string.Join(" ", args);

            var home = data.GetHome(homeName);
            
            if (home == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "HOME.NOT.FOUND.OR.REMOVED");
                return; 
            }
            
            var foundationList = GetFoundation(home.HomePos);
            if (foundationList == null || foundationList.Count == 0)
            {
                SendMessage(player, "FOUNDATION.NOT.FOUND");
                return; 
            }
            
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendMessageWithoutLang(player, ret);
                return;
            }

            if (_config.SettingsHome.EnableSleepingBags)
            {
                if (home.SleepingBagId == 0)
                {
                    data.HomesList.Remove(home);
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendMessage(player, "HOME.NOT.FOUND.BAG");
                    return;
                }
                
                var bag = BaseNetworkable.serverEntities.Find(home.SleepingBagId);
                if (bag == null)
                {
                    data.HomesList.Remove(home);
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendMessage(player, "HOME.NOT.FOUND.BAG");
                    return;
                }
            }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            if (data.CooldownHome > GetCurrentTime())
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "HOME.COOLDOWN", TimeHelper.FormatTime(TimeSpan.FromSeconds(data.CooldownHome - GetCurrentTime())));
                return;
            }

            var message = CanTeleportPlayer(player);
            if (!string.IsNullOrEmpty(message))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessageWithoutLang(player, message);
                return;
            }
            
            string err = CheckInsideBlock(home.HomePos);
            if (err != null)
            {
                SendMessage(player, "HOME.REMOVED.INSIDE.BLOCK", home.HomeName);
                data.HomesList.Remove(home);
                return;
            }
            
            var teleportMgr = TeleportMgr.GetManager(player);
            if (teleportMgr == null)
                return;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            if (teleportMgr.HasRequest())
            {
                SendMessage(player, "TPR.PLAYER.CANCEL.REQUEST");
                return;
            }
            
            teleportMgr.InitRequest(home.HomePos, true);
            SendMessage(player, "HOME.TELEPORTATION", TimeHelper.FormatTime(TimeSpan.FromSeconds(GetTeleportTimeHome(player.userID)), language: lang.GetLanguage(player.UserIDString)));
        }

        private void SendMessageWithoutLang(BasePlayer player, string message,
            Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, message, GetMessage(player, "TELEPORTATION.TITLE"));
            else 
                player.SendConsoleCommand("chat.add", channel, 0, message);
        }

        public class SettingsResolution
        {
            [JsonProperty("Запретить телепортацию в воде? [Prohibit teleportation in water?]")]
            public bool CantTeleportSwimming = true;
            
            [JsonProperty("Запретить телепортацию, если упал? [Prohibit teleportation if dropped?]")]
            public bool CantTeleportWounded = true;
            
            [JsonProperty("Запретить телепортацию, если ползает? [Prohibit teleportation if crawling?]")]
            public bool CantTeleportCrawling = true;

            [JsonProperty("Запретить телепортацию, если человек находится в билдинг зоне [Prohibit teleportation if a person is in the building zone]")]
            public bool CantTeleportInBuild = true;
            
            [JsonProperty("Запретить телепортацию, если человек сидит на обьекте [Prohibit teleportation if a person is mounted]")]
            public bool CantTeleportMounted = true;
            
            [JsonProperty("Запретить телепортацию, если человек находиться на движущемся обьекте [Prohibit teleportation if a person is on moving object]")]
            public bool CantTeleportParent = true;
            
            [JsonProperty("Запретить телепортацию, если человек находиться в воде [Prohibit teleportation if a person is in water]")]
            public bool CantTeleportWater = true;

            [JsonProperty("Настройки радиации [Settings Radiation]", ObjectCreationHandling = ObjectCreationHandling.Replace)] 
            public SettingsRadiation SettingsRadiation = new SettingsRadiation();
            
            [JsonProperty("Настройки кровотечения [Settings Bleeding]", ObjectCreationHandling = ObjectCreationHandling.Replace)] 
            public SettingsBleeding SettingsBleeding = new SettingsBleeding();
            
            [JsonProperty("Настройки температуры [Settings Temperature]", ObjectCreationHandling = ObjectCreationHandling.Replace)] 
            public SettingsTemperature SettingsTemperature = new SettingsTemperature();
        }
        
                private string GetSizedImage(string name, int size) => GetImage($"{name}_{size}");
        private readonly int buildingLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        
        private const string AdminPermission = "teleportation.admin";
        
        private static void DrawMarker(BasePlayer player, Vector3 position, string text, int length = 1)
        {
            if (player == null || player.IsNpc) return;

            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendEntityUpdate(); 
            player.SendConsoleCommand("ddraw.text", length, Color.white, position, text);
            player.SendConsoleCommand("camspeed 0");
                    
            if (player.Connection.authLevel < 2)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    
            player.SendEntityUpdate();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }
        
        private string CheckInsideBlock(Vector3 targetLocation)
        {
            List<BuildingBlock> blocks = Facepunch.Pool.GetList<BuildingBlock>();
            Vis.Entities(targetLocation + new Vector3(0, 0.25f), 0.1f, blocks, blockLayer);
            bool inside = blocks.Count > 0;
            Facepunch.Pool.FreeList(ref blocks);

            return inside ? "FOUNDATION.HOME.INSIDE.BLOCK" : null;
        }
                
                private static PluginData _data;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["ENABLED"] = "enabled",
                ["DISABLED"] = "disabled",
                ["ATP.MODE"] = "Auto friend accept teleportation was: {0}.",
                ["TPM.ADMIN.REMOVE.NOT.FOUND"] = "Monument not found.",
                ["TPM.ADMIN.REMOVE.SUCCESS"] = "You successfuly removed the monument from config.",
                ["TPM.PLAYER.CANCEL"] = "Teleport to monument cancelled.",
                ["TPM.NO.AVAILABLE.MONUMENTS"] = "There is no available monuments.",
                ["TPM.LIST.FOUND"] = "Available monuments: {0}",
                ["TPM.HELP"] = "Usage: /tpm 'monument name'.\nUse: /tpm list - to see all available monuments.",
                ["TPM.HOSTILE"] = "You can't teleport to monument while hostile.",
                ["TPM.NOT.FOUND"] = "Monument not found!",
                ["TPM.COOLDOWN"] = "Teleportation has been canceled! Reason: cooldown {0}",
                ["TPM.TP.SUCCESS"] = "You successfully teleport to monument. Time before teleporting {0}",
                ["TPM.SUCCESS"] = "You have successfully teleported to monument",
                ["TPM.ADMIN.SUCCESS"] = "Monument was successfully added into your config.",
                ["TPM.ADMIN.CLOSEST.NOT.FOUND"] = "The system could not find the nearest monument to you!",
                ["TPM.ADMIN.EXISTS"] = "Such a monument already exists!",
                ["TPR.PLAYER.CANCEL"] = "You have successfully canceled teleportation to player {0}",
                ["TPR.TARGET.CANCEL"] = "Player {0} has canceled teleportation to you!",
                ["HOME.PLAYER.CANCEL"] = "You canceled teleportation to your home!",
                ["PLAYER.NOT.FOUND"] = "Player not found", 
                ["TPA.ZERO.REQUEST"] = "You have no pending teleport request!",
                ["TP.CANCEL.BLOOD.OR.WOUNDED"] = "Teleport canceled! You can't teleport while wounded!",
                ["TP.CANCEL.BUILD.DONT.AUTH"] = "You can't teleport while in a building blocked zone!",
                ["TPA.PLAYER.ACCEPT"] = "You've accepted the teleport request of {0}!",
                ["TPA.TARGET.ACCEPT"] = "Player has accepted your teleport request!! Teleportation has started!",
                ["TPR.HELP"] = "Use: /tpr nickname", 
                ["TPR.COOLDOWN"] = "Your teleport requests are currently on cooldown. You'll have to wait {0} to send your next teleport request.",
                ["TELEPORTATION.TITLE"] = "[TELEPORTATION]",
                ["TPR.PLAYER.CANCEL.REQUEST"] = "You can't initiate another teleport while you have a teleport pending!",
                ["TPR.TARGET.CANCEL.REQUEST"] = "You can't request a teleport to someone who's about to teleport!",
                ["TPR.PLAYER.SEND"] = "Teleportation request to {0} was sent successfully",
                ["TPR.TARGET.SEND"] = "{0} sent you teleportation request\nTo accept type: /tpa\nTo refuse type: /tpc",
                ["REMOVE.HOME.HELP"] = "Use: /removehome name",
                ["REMOVE.HOME.NONE"] = "Couldn't find your home with that name!",
                ["REMOVE.HOME.SUCCESS"] = "You have removed your home {0}!", 
                ["FOUNDATION.NOT.FOUND"] = "Foundation not found!",
                ["FOUNDATION.NOT.OWNED"] = "Foundation is not owned by you or your friends!",
                ["FOUNDATION.HOME.INSIDE.BLOCK"] = "You can't create home inside other blocks!",
                ["SETHOME.HELP"] = "Use: /sethome name",
                ["BLOOD.OR.WOUNDED"] = "You are bleeding or you wounded!",
                ["HOME.HELP"] = "Use: /home name",
                ["HOME.NOT.FOUND.OR.REMOVED"] = "Home location not found or has been deleted!",
                ["HOME.NOT.FOUND.BAG"] = "Teleportation is not possible! Sleeping bag not found!",
                ["HOME.COOLDOWN"] = "Teleportation has been canceled! Reason: cooldown {0}",
                ["HOME.REMOVED.INSIDE.BLOCK"] = "Your home '{0}' was removed because it was inside a foundation!",
                ["CAN.TP.SWIMMING"] = "Teleportation in water is prohibited!",
                ["CAN.TP.ALIVE"] = "You can't teleport while being dead!",
                ["CAN.TP.RADIATION"] = "You are irradiated! Teleportation has been canceled!",
                ["CAN.TP.BLEEDING"] = "You are bleeding! Teleportation canceled!",
                ["CAN.TP.TEMPERATURE"] = "You're too cold to teleport!",
                ["CAN.TP.WOUNDED"] = "You can't teleport while wounded!",
                ["CAN.TP.MOUNTED"] = "You can't teleport while mounting vehicle!",
                ["CAN.TP.PARENT"] = "You can't teleport while you are on moving object.",
                ["CAN.TP.WATER"] = "You can't teleport while swimming.",
                ["CAN.TP.BUILDINGBLOCK"] = "You can't teleport while in a building blocked zone!",
                ["SETHOME.ALREADY.EXISTS"] = "A location with this name already exists!",
                ["SETHOME.CANCEL.RADIUS"] = "You already have home location nearby!", 
                ["SETHOME.CANCEL.MAXHOUSE"] = "Unable to set your home here, you have reached the maximum amount of homes!",
                ["SETHOME.SUCCESS"] = "You have saved the current location {0} as your home!",
                ["TP.HOMELIST.ZERO"] = "Player have no houses!",
                ["TP.HOMELIST.FOUND"] = "Houses list: {0}",
                ["HOMELIST.ZERO"] = "You have no houses!",
                ["HOMELIST.FOUND"] = "Houses list: {0}",
                ["HOME.TELEPORTATION"] = "You successfully teleport to your home. Time before teleporting {0}",
                ["REQUEST.CHECK.DISCONNECT"] = "Teleport request has been canceled! Reason: player left server",
                ["REQUEST.CHECK.DEAD"] = "Teleport request has been canceled! Reason: player died!",
                ["REQUEST.CHECK.WOUNDED"] = "Teleport request has been canceled! Reason: player wounded!",
                ["REQUEST.CHECK.TIME.ENDED"] = "Teleport request has been canceled! Reason: time out!",
                ["TP.CHECK.BUILD"] = "Teleport request has been canceled! Reason: building blocked zone",
                ["TP.HOME.SUCCESS"] = "You teleported to your home!",
                ["TP.PLAYER.SUCCESS"] = "You have successfully teleported to {0}",
                ["TP.TARGET.SUCCESS"] = "Player {0} successfully teleported to you!",    
                ["MARKER.BAG"] = "<size=24>{0}</size>\n<size=16>{1}</size>",
                ["MARKER.BAG.NAMEBAG"] = "{0}",
                ["MARKER.BAG.COOLDOWN"] = "COUNTDOWN: {0}",
                ["MARKER.BAG.NOTCOOLDOWN"] = "AVAILABLE FOR TELEPORTATION",
                ["TELEPORTATION.UI"] = "TIME LEFT BEFORE TELEPORT: {0}",
                ["PLAYER.NOT.FOUND.PARAM"] = "Player {0} not found",
                ["PLAYER.FIND.MULTIPLE"] = "Several players were found: {0}",
                ["TPR.UI.REQUEST"] = "REQUEST FROM PLAYER {0}",
                ["TPR.UI.REQUEST.YES"] = "YES", 
                ["TPR.UI.REQUEST.NO"] = "NO",
                ["SETHOME.CANCEL.BUILD"] = "You cannot set your home location while in building blocked zone!"
            }, this);   
            
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["ENABLED"] = "включено",
                ["DISABLED"] = "отключено",
                ["ATP.MODE"] = "Автоматическое принятие запроса на телепорт: {0}.",
                ["TPM.ADMIN.REMOVE.NOT.FOUND"] = "Монумент не найден.",
                ["TPM.ADMIN.REMOVE.SUCCESS"] = "Вы успешно удалили монумент из конфига.",
                ["TPM.PLAYER.CANCEL"] = "Телепорт на монумент был отменён.",
                ["TPM.NO.AVAILABLE.MONUMENTS"] = "Нет доступных монументов.",
                ["TPM.LIST.FOUND"] = "Доступные монументы: {0}",
                ["TPM.HELP"] = "Использование: /tpm 'название монумента'.\nИспользуйте: /tpm list - чтобы увидеть список доступных монументов.",
                ["TPM.HOSTILE"] = "Не возможно телепортироватся на город в враждебном состоянии.",
                ["TPM.NOT.FOUND"] = "Монумент не найден!",
                ["TPM.COOLDOWN"] = "Телепортация отменена! Причина: откат {0}",
                ["TPM.TP.SUCCESS"] = "Вы успешно телепортируетесь на монумент. Время перед телепортом {0}",
                ["TPM.SUCCESS"] = "Вы успешно телепортированы на монумент.",
                ["TPM.ADMIN.SUCCESS"] = "Вы успешно добавили новый монумент в конфиг.",
                ["TPM.ADMIN.CLOSEST.NOT.FOUND"] = "Система не смогла определить ближайший монумент к вам!",
                ["TPM.ADMIN.EXISTS"] = "Такой монумент уже есть в конфиге!",
                ["TPR.PLAYER.CANCEL"] = "Вы успешно отменили телепортацию к игроку {0}",
                ["TPR.TARGET.CANCEL"] = "Игрок {0} отменил телепортацию к Вам!",
                ["HOME.PLAYER.CANCEL"] = "Вы отменили телепортацию домой!",
                ["PLAYER.NOT.FOUND"] = "Игрок не найден",
                ["TPA.ZERO.REQUEST"] = "У вас нет входящих запросов на телепортацию!",
                ["TP.CANCEL.BLOOD.OR.WOUNDED"] = "Телепортация была отменена! Причина кровоток или вы упали!",
                ["TP.CANCEL.BUILD.DONT.AUTH"] = "В билдинг зоне телепортация запрещена!",
                ["TPA.PLAYER.ACCEPT"] = "Вы приняли запрос игрока {0}",
                ["TPA.TARGET.ACCEPT"] = "Игрок принял ваш запрос! Телепортация началась!",
                ["TPR.HELP"] = "Используйте: /tpr ник", 
                ["TPR.COOLDOWN"] = "Телепортация невозможна! У вас кулдаун: {0}",
                ["TELEPORTATION.TITLE"] = "[TELEPORTATION]",
                ["TPR.PLAYER.CANCEL.REQUEST"] = "Телепортация невозможна! У Вас уже есть запрос!",
                ["TPR.TARGET.CANCEL.REQUEST"] = "Телепортация невозможна! У игрока уже есть запрос!",
                ["TPR.PLAYER.SEND"] = "Запрос к {0} был успешно отправлен",
                ["TPR.TARGET.SEND"] = "{0} отправил вам запрос на телепортацию\nЧтобы принять используйте /tpa\nЧтобы отказаться используйте /tpc",
                ["REMOVE.HOME.HELP"] = "Используйте: /removehome название", 
                ["REMOVE.HOME.NONE"] = "Данного дома не существует",
                ["REMOVE.HOME.SUCCESS"] = "Вы удалили дом под названием {0}",
                ["FOUNDATION.NOT.FOUND"] = "Фундамент не найден!",
                ["FOUNDATION.NOT.OWNED"] = "Фундамент не пренадлежит вам или вашим друзьям!",
                ["FOUNDATION.HOME.INSIDE.BLOCK"] = "Вы не можете создавать дом внутри другого блока!",
                ["SETHOME.HELP"] = "Используйте: /sethome название",
                ["BLOOD.OR.WOUNDED"] = "У вас кровотечение или вы упали!",
                ["HOME.HELP"] = "Используйте: /home название",
                ["HOME.NOT.FOUND.OR.REMOVED"] = "Точка телепортации ненайдена или была удалена!",
                ["HOME.NOT.FOUND.BAG"] = "Телепортация невозможна! Спальник не обнаружен!",
                ["HOME.COOLDOWN"] = "Телепортация была отменена! Причина: кулдаун {0}",
                ["HOME.REMOVED.INSIDE.BLOCK"] = "Ваш дом '{0}' был удалён потому, что он был внутри фундамента!",
                ["CAN.TP.SWIMMING"] = "Телепортация в воде запрещена",
                ["CAN.TP.ALIVE"] = "Телепортация мертвым запрещена",
                ["CAN.TP.RADIATION"] = "Вы облучены! Телепортация была отменена!",
                ["CAN.TP.BLEEDING"] = "Вы истекаете кровью! Телепортация отменена!",
                ["CAN.TP.TEMPERATURE"] = "Вы замерзли! Телепортация отменена!",
                ["CAN.TP.WOUNDED"] = "Вы получили ранение! Телепортация отменена!",
                ["CAN.TP.MOUNTED"] = "Вы сидите! Телепортация отменена!",
                ["CAN.TP.PARENT"] = "Вы на движущемся объекте! Телепортация отменена!",
                ["CAN.TP.WATER"] = "Вы в воде! Телепортация отменена!",
                ["CAN.TP.CRAWLING"] = "Вы получили ранение! Телепортация отменена!",
                ["CAN.TP.BUILDINGBLOCK"] = "Вы находитесь в зоне действия шкафа! Телепортация отменена!",
                ["SETHOME.ALREADY.EXISTS"] = "Данный дом уже существует",
                ["SETHOME.CANCEL.RADIUS"] = "У вас уже установлен дом в этом месте!",
                ["SETHOME.CANCEL.MAXHOUSE"] = "Вы превысили максимальное количество сетхомов!",
                ["SETHOME.SUCCESS"] = "Вы успешно установили дом {0}",
                ["TP.HOMELIST.ZERO"] = "У игрока нет домов!",
                ["TP.HOMELIST.FOUND"] = "Список домов игрока: {0}",
                ["HOMELIST.ZERO"] = "У вас нет домов!",
                ["HOMELIST.FOUND"] = "Список ваших домов: {0}",
                ["HOME.TELEPORTATION"] = "Вы успешно телепортируетесь на дом. До телепортации {0}",
                ["REQUEST.CHECK.DISCONNECT"] = "Запрос на телепортацию был отменён! Причина: цель покинула сервер",
                ["REQUEST.CHECK.DEAD"] = "Запрос на телепортацию был отменён! Причина: цель погибла!",
                ["REQUEST.CHECK.WOUNDED"] = "Запрос на телепортацию был отменён! Причина: цель упала!",
                ["REQUEST.CHECK.CRAWLING"] = "Запрос на телепортацию был отменён! Причина: цель упала!",
                ["REQUEST.CHECK.TIME.ENDED"] = "Запрос на телепортацию был отменён! Причина: окончилось время ожидания!",
                ["TP.CHECK.BUILD"] = "Телепортация была отменена! Причина: зона шкафа",
                ["TP.HOME.SUCCESS"] = "Вы успешно были телепортированы домой!",
                ["TP.PLAYER.SUCCESS"] = "Вы успешно телепортировались к {0}",
                ["TP.TARGET.SUCCESS"] = "Игрок {0} успешно телепортировался к Вам!",    
                ["MARKER.BAG"] = "<size=24>{0}</size>\n<size=16>{1}</size>",
                ["MARKER.BAG.NAMEBAG"] = "{0}",
                ["MARKER.BAG.COOLDOWN"] = "ОТКАТ: {0}",
                ["MARKER.BAG.NOTCOOLDOWN"] = "ДОСТУПЕН ДЛЯ ТЕЛЕПОРТАЦИИ",
                ["TELEPORTATION.UI"] = "ДО КОНЦА ТЕЛЕПОРТАЦИИ ОСТАЛОСЬ: {0}",
                ["PLAYER.NOT.FOUND.PARAM"] = "Игрок {0} был не найден",
                ["PLAYER.FIND.MULTIPLE"] = "Было найдено несколько игроков: {0}",
                ["TPR.UI.REQUEST"] = "ЗАПРОС ОТ ИГРОКА {0}",
                ["TPR.UI.REQUEST.YES"] = "ДА",
                ["TPR.UI.REQUEST.NO"] = "НЕТ",
                ["SETHOME.CANCEL.BUILD"] = "Вы не можете установить сетхом в билдинг зоне!"
            }, this, "ru");  
              
        }
        
        private static double GetCurrentTime()
        {
            return new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        }

        public class SettingsTemperature
        { 
            [JsonProperty("Минимальное кол-во температуры, для отмены тп [Minimum temperature, for canceling TP]")]
            public float MinNeedTemperature = 3f;

            [JsonProperty("Запретить телепортацию при низкой температуре? [Prohibit teleportation at low temperatures?]")]
            public bool CantTeleportTemperature = true;
        }
        
        object CanRenameBed(BasePlayer player, SleepingBag bed, string bedName)
        {
            if (_config.SettingsMonument.AvailableMonuments.Exists(x => x.CreatedBag != null && x.CreatedBag == bed))
                return false;
            
            return null;
        }
        
        private void SetHome(BasePlayer player, string name)
        {
            var userId= player.userID;
            var playerPosition = player.GetNetworkPosition();
            var playerData = PluginData.GetForPlayer(player.userID);
            if (playerData == null)
                return;

            foreach (var home in playerData.HomesList)
            {
                if (home.HomeName == name)
                {
                    SendMessage(player, "SETHOME.ALREADY.EXISTS");
                    return;
                }
                
                if (Vector3.Distance(playerPosition, home.HomePos) > 30) 
                    continue;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                SendMessage(player, "SETHOME.CANCEL.RADIUS");
                return;
            }
            
            if (GetHomeLimit(userId) == playerData.HomesList.Count)
            {
                SendMessage(player, "SETHOME.CANCEL.MAXHOUSE");
                return; 
            }

            SleepingBag sleepingBag = null;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            if (_config.SettingsHome.EnableSleepingBags)
            {
                sleepingBag = CreateSleepingBag(player, playerPosition, $"{name}");
                
                //List<BuildingBlock> foundations = new List<BuildingBlock>();
                //Vis.Components(sleepingBag.transform.position, .3f, foundations);
                
                var foundation = GetFoundation(sleepingBag.transform.position);
                sleepingBag.buildingID = foundation[0].buildingID;
                
                sleepingBag.gameObject.AddComponent<MarkerBag>();
                sleepingBag.skinID = 214;
            }
                
            playerData.HomesList.Add(new PlayerData.Home(playerPosition, name, sleepingBag?.net.ID ?? 0));

            SendMessage(player, "SETHOME.SUCCESS", name);
        }

        private static PluginConfig _config;
        
        [ConsoleCommand("UI_Teleportation")]
        private void ConsoleCommandTeleportation(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;

            if (player == null) return;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            switch (arg.GetString(0))
            {
                case "close":

                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);
                    
                    break;
            }
        }
        
        
        private void FullLoad()
        {
            RegisterPermissions();
            
            if(permission.PermissionExists("teleportation.default"))
                permission.GrantGroupPermission("default", "teleportation.default", this);

            _initiated = true;
            
            for(int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity is SleepingBag == false)
                    continue;

                var bag = (SleepingBag)entity;
                if (bag.skinID == 214)
                {
#if DEBUG
                    Puts("Found sleeping bag with custom skin at " + bag.transform.position);
#endif
                    bag.gameObject.AddComponent<MarkerBag>();
                }
            }

            if (_config.SettingsMonument.AvailableMonuments.Count > 0)
            {
                if (_config.SettingsMonument.EnableSleepingBags == false)
                {
                    Unsubscribe(nameof(OnSleepingBagValidCheck));
                    Unsubscribe(nameof(CanRenameBed));
                }
                
                for (var i = 0; i < _config.SettingsMonument.AvailableMonuments.Count; i++)
                {
                    var monument = _config.SettingsMonument.AvailableMonuments[i];
                    var foundMonument = FindMonument(monument.ShortPrefabName);
                    if (foundMonument == null)
                    {
                        PrintError($"Failed to find monument from config! Name: '{monument.ShortPrefabName}'.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(monument.ShortCutCommand) == false)
                    {
                        cmd.AddChatCommand(monument.ShortCutCommand, this, (player, s, arg3) =>  ChatCommandTpMonument(player, "", new string[] { monument.Name } ));
                    }

                    if (_config.SettingsMonument.EnableSleepingBags) 
                    {
                        var bag = CreateSleepingBag(null, ParentPosition.GetFinalPosition(foundMonument.transform, monument.PositionOffset) - new Vector3(0, 100f, 0), monument.Name);
                        if (bag != null)
                        {
                            bag.SetPublic(false);
                            bag.OwnerID = 0;
                            bag.deployerUserID = 0;
                            bag.spawnOffset +=  new Vector3(0, 100f, 0);
                            bag.unlockTime = Time.realtimeSinceStartup + monument.CoolDownPublic;
                            bag.secondsBetweenReuses = monument.CoolDownPublic; 
                        }
                        else
                        {
                            PrintError($"Failed to create sleeping bag for monument! Name: '{monument.ShortPrefabName}'.");
                        }
                        
                        monument.CreatedBag = bag;
                    }
                    
                    monument.MonumentInfo = foundMonument;
                }
                
            }
            
            
        }

                
        
        
        private class MarkerBag : MonoBehaviour
        {
            public static List<MarkerBag> MarkerBags = new List<MarkerBag>();
            
            public SleepingBag SleepingBag;
            public BasePlayer OwnerPlayer;

            public void Awake()
            {
                SleepingBag = GetComponent<SleepingBag>();
                if (SleepingBag == null)
                {
                    _plugin.PrintError("Failed to init marker bag component! Reason: 'sleeping bag not found'");
                    Destroy(this);
                    return;
                }

                UpdatePlayer(BasePlayer.FindByID(SleepingBag.OwnerID));

                MarkerBags.Add(this);
            }

            private void OnDestroy()
            {
                MarkerBags.Remove(this);
            }

            public void UpdatePlayer(BasePlayer player)
            {
                if (player == null || player.userID != SleepingBag.OwnerID)
                    return;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                OwnerPlayer = player;
                CancelInvoke(nameof(DrawText));
                InvokeRepeating(nameof(DrawText), 0.5f, 1f); 
            }

            private void DrawText()
            {
                if (OwnerPlayer == null)
                {
                    CancelInvoke(nameof(DrawText));
                    return;
                }

                try
                {
                    var playerPos = OwnerPlayer.transform.position;

                    if (Vector3.Distance(playerPos, SleepingBag.transform.position) > 5f)
                        return;

                    if (SleepingBag.IsVisible(playerPos, 5f) == false)
                        return;

                    DrawMarker(OwnerPlayer, transform.position + new Vector3(0, 1.25f, 0), GetMessage(OwnerPlayer));
                }
                catch(Exception e)
                {
                    _plugin.PrintError("Failed to draw text for sleeping bag! Destroying...");
                    Destroy(this);
                }
            }

            private string GetMessage(BasePlayer player)
            {
                return _plugin.GetMessage(player, "MARKER.BAG",
                    _plugin.GetMessage(player, "MARKER.BAG.NAMEBAG", SleepingBag.niceName.ToUpper()),
                    SleepingBag.unlockTime > Time.realtimeSinceStartup
                        ? _plugin.GetMessage(player, "MARKER.BAG.COOLDOWN",
                            TimeHelper.FormatTime(
                                TimeSpan.FromSeconds(SleepingBag.unlockTime - Time.realtimeSinceStartup), language: _plugin.lang.GetLanguage(player.UserIDString)).ToUpper())
                        : _plugin.GetMessage(player, "MARKER.BAG.NOTCOOLDOWN")
                );
            }
        }
        
        [ConsoleCommand("tpc")]
        void ConsoleCmdTpc(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandTpc(player, "", arg.Args);
        }

        private bool HasImage(string name) => (bool)(ImageLibrary?.Call("HasImage", name) ?? false);
        private readonly int deployedLayer = LayerMask.GetMask("Deployed");
        
        [ConsoleCommand("tpa")]
        void ConsoleCmdTpa(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandTpa(player, "", arg.Args);
        }

        public class SettingMonument
        {
            [JsonProperty("Список доступных монументов [Available monument to teleport]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<AvailableMonument> AvailableMonuments = new List<AvailableMonument>()
            {
                new AvailableMonument()
                {
                    PositionOffset = new Vector3(-30.2480469f, 2.18783569f, 14.8937988f),
                    ShortPrefabName = "compound",
                    Name = "compound",
                    ShortCutCommand = "compound",
                    CoolDown = 300f,
                    CoolDownPublic = 30f,
                    TeleportTime = 30f
                }
            };

            [JsonProperty("Включить систему спальников? [Enable sleeping bag system?]")]
            public bool EnableSleepingBags = true;
            
            public class AvailableMonument
            {
                [JsonProperty("Оффсет позиция относительно центра РТ [Position offset from center of RT]")]
                public Vector3 PositionOffset;
                
                [JsonProperty("Краткое имя монумента НЕ МЕНЯТЬ [Short name of the monument DO NOT EDIT]")]
                public string ShortPrefabName;
                
                [JsonProperty("Отображаемое имя монумента [Display name of the monument]")]
                public string Name;
                
                [JsonProperty("Краткая команда телепорта (можно оставить пустым) [Short version of command (can be empty)]")]
                public string ShortCutCommand;
                
                [JsonProperty("Откат для телепорта отдельный [Individual teleport cooldown]")]
                public float CoolDown;
                
                [JsonProperty("Общий откат на телепорт [Common teleport cooldown]")]
                public float CoolDownPublic;
                
                [JsonProperty("Время телепорта [Teleport time]")]
                public float TeleportTime;

                [JsonIgnore] public SleepingBag CreatedBag;
                [JsonIgnore] public MonumentInfo MonumentInfo;
            }
        }
        
        private float GetTeleportTimeTpr(ulong uid)
        {
            float min = 9999f;
            bool set = false;
            foreach (var privilege in _config.SettingsTeleport.TeleportationTime)
            {
                if (permission.UserHasPermission(uid.ToString(), privilege.Key))
                {
                    min = Mathf.Min(min, privilege.Value);
                    set = true;
                }
                
            }
            
            return set ? min : -1f;
        } 
        
        private int GetHomeLimit(ulong uid)    
        {
            int max = -1;
            foreach (var privilege in _config.SettingsHome.CountHome)
                if (permission.UserHasPermission(uid.ToString(), privilege.Key)) 
                    max = Mathf.Max(max, privilege.Value);
            return max; 
        } 
        
        private string CheckInsideBattery(Vector3 targetLocation)
        {
            var batteries = new List<ElectricBattery>();
            Vis.Entities(targetLocation, 0.35f, batteries, deployedLayer);
            return batteries.Count > 0 ? "TPTargetInsideBlock" : null;
        }
        
        private const string EffectPrefab = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        
        
        
        private void SendMessage(BasePlayer player, string message, string arg1 = "", string arg2 = "", Chat.ChatChannel channel = Chat.ChatChannel.Global)
        { 
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, GetMessage(player, message, arg1, arg2), GetMessage(player, "TELEPORTATION.TITLE"));
            else 
                player.SendConsoleCommand("chat.add", channel, 0, GetMessage(player, message, arg1, arg2));
        } 

        private class TeleportMgr : MonoBehaviour
        {
            public static Dictionary<BasePlayer, TeleportMgr> PlayerToManager = new Dictionary<BasePlayer, TeleportMgr>();
            public static List<TeleportMgr> ManagersList = new List<TeleportMgr>();
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            private BasePlayer _player;
            private double teleportTime;
            public Request Request = null;
            public List<Request> IncomingRequests = new List<Request>();

            private double TimeTeleportation => teleportTime - GetCurrentTime();

            private float TimeTP = 30f;
            private float TimeHome = 30f;

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                if (_player == null)
                {
                    _plugin.PrintError("Failed to find player for teleport component! Deleting...");
                    Destroy(this);
                    return;
                }

                teleportTime = 0f;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                PlayerToManager.Add(_player, this);
                ManagersList.Add(this);
            }

            private void OnDestroy()
            {
                if (_player != null)
                    PlayerToManager.Remove(_player);

                ManagersList.Remove(this);
            }

            private void TeleportTick()
            {
                if (Request == null)
                {
                    CancelTeleport();
                    return;
                }

                try
                {

                    string ret = "";

                    if (Request.RequestConfirmed == false)
                    {
                        ret = CheckRequest();
                        if (string.IsNullOrEmpty(ret) == false)
                        {
                            _plugin.SendMessage(_player, ret);
                            CancelTeleport();
                        }

                        return;
                    }

                    ret = CheckTeleport();
                    if (string.IsNullOrEmpty(ret) == false)
                    {
                        _plugin.SendMessage(_player, ret);
                        CancelTeleport();
                        return;
                    }

                    if (teleportTime - 1 > GetCurrentTime())
                    {
                        UpdateUI();
                        return;
                    }

                    _plugin.Teleport(_player, Request.Target == null ? Request.Position : Request.Target.ServerPosition);

                    var data = PluginData.GetForPlayer(_player.userID);
                    if (data == null)
                    {
                        CancelTeleport();
                        return;
                    }

                    if (Request.IsHome)
                    {
                        float coolDown = _plugin.GetTeleportCooldownHome(_player.userID);
                        var home = data.GetHome(Request.Position);
                        if (home == null)
                        {
                            _plugin.SendMessage(_player, "HOME.NOT.FOUND.OR.REMOVED");
                            CancelTeleport();
                            return;
                        }

                        if (_config.SettingsHome.EnableSleepingBags)
                        {

                            var bag = (SleepingBag) BaseNetworkable.serverEntities.Find(home.SleepingBagId);
                            if (bag != null)
                                bag.unlockTime = Time.realtimeSinceStartup + coolDown;
                        }

                        data.CooldownHome = GetCurrentTime() + coolDown;
                        _plugin.SendMessage(_player, "TP.HOME.SUCCESS");
                    }
                    else if (Request.Monument != null)
                    {
                        data.MonumentsCooldown.TryAdd(Request.Monument.Name, GetCurrentTime() + Request.Monument.CoolDown);

                        if (Request.Monument.CoolDownPublic > 0 && Request.Monument.CreatedBag != null)
                        {
                            Request.Monument.CreatedBag.unlockTime = Time.realtimeSinceStartup + Request.Monument.CoolDownPublic;
                        }
                        
                        _plugin.SendMessage(_player, "TPM.SUCCESS");
                    }
                    else
                    {
                        _plugin.SendMessage(_player, "TP.PLAYER.SUCCESS", Request.Target.displayName);
                        _plugin.SendMessage(Request.Target, "TP.TARGET.SUCCESS", _player.displayName);

                        data.CooldownTeleport = GetCurrentTime() + _plugin.GetTeleportCooldownTpr(_player.userID);
                    }

                }
                catch (Exception e)
                {
                    _plugin.PrintError("Failed to update teleport tick! " + e);
                    Destroy(this);
                }

                CancelTeleport();
            }

            public bool HasRequest()
            {
                return Request != null;
            }

            public void InitRequest(Vector3 teleportPos, bool isHome, BasePlayer target = null, SettingMonument.AvailableMonument monument = null)
            {
                Request = new Request(_player, isHome, teleportPos, target, monument);

                teleportTime = GetCurrentTime() + (isHome ? _plugin.GetTeleportTimeHome(_player.userID) : _plugin.GetTeleportTimeTpr(_player.userID));

                if (monument != null)
                {
                    Request.RequestConfirmed = true;
                    teleportTime = GetCurrentTime() + monument.TeleportTime;
                    
                    ShowUI();
                }
                
                if (isHome)
                {
                    Request.RequestConfirmed = true;
                    teleportTime = GetCurrentTime() + _plugin.GetTeleportTimeHome(_player.userID);
                    
                    ShowUI();
                }
                
                if(IsInvoking(nameof(TeleportTick)) == false)
                    InvokeRepeating(nameof(TeleportTick), 0.5f, 1f);
            }

            public void AcceptRequest()
            {
                if (Request == null)
                    return;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                Request.RequestConfirmed = true;
                teleportTime = GetCurrentTime() + _plugin.GetTeleportTimeTpr(_player.userID);
                ShowUI();
            }

            private string CheckTeleport()
            {
                if (_player.IsAlive() == false) 
                    return "CAN.TP.ALIVE";
                
                if (_config.SettingsResolution.CantTeleportMounted && _player.isMounted) 
                    return "CAN.TP.MOUNTED";
                
                if (_config.SettingsResolution.CantTeleportParent && _player.HasParent()) 
                    return "CAN.TP.PARENT";
                
                if (_config.SettingsResolution.CantTeleportWater && _player.IsSwimming()) 
                    return "CAN.TP.WATER";

                if (_config.SettingsResolution.CantTeleportWounded && _player.IsWounded()) 
                    return "CAN.TP.WOUNDED";
            
                if (_config.SettingsResolution.CantTeleportCrawling && _player.IsWounded()) 
                    return "CAN.TP.CRAWLING";
                
                if (_config.SettingsResolution.CantTeleportSwimming && _player.IsSwimming()) 
                    return "CAN.TP.SWIMMING";
                
                if (_config.SettingsResolution.SettingsRadiation.CantTeleportRadiation && _player.metabolism.radiation_poison.value > _config.SettingsResolution.SettingsRadiation.NeedRadiationAmount) 
                    return "CAN.TP.RADIATION";

                if (_config.SettingsResolution.SettingsBleeding.CantTeleportBleeding && _player.metabolism.bleeding.value > _config.SettingsResolution.SettingsBleeding.NeedBleedingAmount) 
                    return "CAN.TP.BLEEDING";

                if (_config.SettingsResolution.SettingsTemperature.CantTeleportTemperature && _player.metabolism.temperature.value < _config.SettingsResolution.SettingsTemperature.MinNeedTemperature) 
                    return "CAN.TP.TEMPERATURE";
                
                if (_config.SettingsResolution.CantTeleportInBuild && Request.Player.IsBuildingBlocked() && Request.IsHome)
                    return "TP.CHECK.BUILD";
                
                if (_config.SettingsResolution.CantTeleportInBuild && Request.IsHome == false && Request.Target != null && Request.Target.IsBuildingBlocked())
                    return "TP.CHECK.BUILD";
                
                return string.Empty;
            }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            private string CheckRequest()
            {
                if (Request.RequestTimeout > 30)
                    return "REQUEST.CHECK.TIME.ENDED";
                
                if (Request.Target.IsConnected == false)
                    return "REQUEST.CHECK.DISCONNECT";

                if (Request.Target.IsDead())
                    return "REQUEST.CHECK.DEAD";

                if (Request.Target.IsWounded() && _config.SettingsResolution.CantTeleportWounded)
                    return "REQUEST.CHECK.WOUNDED";
                
                if (Request.Target.IsCrawling() && _config.SettingsResolution.CantTeleportCrawling)
                    return "REQUEST.CHECK.WOUNDED";

                return "";
            }
            
            private void ShowUI()
            {
                
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform = { AnchorMin = "0.3447913 0.113889", AnchorMax = "0.640625 0.17037", OffsetMax = "0 0" },
                            Image = { Color = "0.9686275 0.9215686 0.8823529 0.13529412" }
                        }, 
                        "Hud", 
                        LayerNotification
                    },
                    {
                        new CuiLabel() 
                        {  
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.98"},
                            Text =
                            {
                                Text = _plugin.GetMessage(_player, "TELEPORTATION.UI", 
                                    TimeHelper.FormatTime(TimeSpan.FromSeconds(TimeTeleportation), language: _plugin.lang.GetLanguage(_player.UserIDString)).ToUpper()), 
                                Font = "RobotoCondensed-Bold.ttf", 
                                Align = TextAnchor.MiddleCenter, 
                                FontSize = 18, 
                                Color = GetColor("#FFFFFF", 0.7f)
                            },
                        }, 
                        LayerNotification, 
                        LayerNotification + ".UpdateText"
                    }
                };

                CuiHelper.DestroyUi(_player, LayerNotification);
                CuiHelper.AddUi(_player, container);
            } 

            private void UpdateUI()
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiLabel() 
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.98"},
                            Text =
                            {
                                Text = _plugin.GetMessage(_player, "TELEPORTATION.UI", TimeHelper.FormatTime(TimeSpan.FromSeconds(TimeTeleportation), 
                                    language: _plugin.lang.GetLanguage(_player.UserIDString)).ToUpper()), 
                                Font = "RobotoCondensed-Bold.ttf", 
                                Align = TextAnchor.MiddleCenter, 
                                FontSize = 18, 
                                Color = GetColor("#FFFFFF", 0.7f)
                            },
                        }, 
                        LayerNotification, 
                        LayerNotification + ".UpdateText"
                    }
                };

                CuiHelper.DestroyUi(_player, LayerNotification + ".UpdateText");
                CuiHelper.AddUi(_player, container);
            }

            public void CancelTeleport() 
            {
                if (Request.Target != null)
                {
                    var manager = TeleportMgr.GetManager(Request.Target);
                    if (manager != null)
                    {
                        manager.IncomingRequests.Remove(Request);
                    }
                    CuiHelper.DestroyUi(Request.Target, LayerNotification);
                }
                
                Request = null;
                CancelInvoke(nameof(TeleportTick)); 
                CuiHelper.DestroyUi(_player, LayerNotification);
            }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            public static TeleportMgr GetManager(BasePlayer player)
            {
                TeleportMgr manager = null;
                if (PlayerToManager.TryGetValue(player, out manager) == false)
                    return null;

                return manager;
            }
        }
        
        void OnNewSave(string fileName)
        {
            if (_config.AutoWipe)
            {
                PrintWarning("Wipe detected! wiping data file...");
                _data.PlayersData = new Dictionary<ulong, PlayerData>();
                PluginData.SaveData();
            }
        }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                _config = GetDefaultConfig();
            }
        }
                
        private class ParentPosition
        {
            public static Vector3 GetOffsetPosition(Transform parent, Vector3 child)
            {
                return parent.InverseTransformPoint(child);
            }

            public static Vector3 GetFinalPosition(Transform parent, Vector3 offset)
            {
                return parent.TransformPoint(offset);
            }
        }
        
        private static class TimeHelper
        {
            public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
            { 
                string result = string.Empty;  
                switch (language)
                {
                    case "ru":
                        int i = 0;
                        if (time.Days != 0 && i < maxSubstr)
                        { 
                            if (!string.IsNullOrEmpty(result)) 
                                result += " ";
 
                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }

                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }

                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }



                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr) 
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }

                        break;
                    case "en": 
                        int b = 0; 
                        if (time.Days != 0 && b < maxSubstr)
                        { 
                            if (!string.IsNullOrEmpty(result)) 
                                result += " ";
 
                            result += $"{Format(time.Days, "d", "d", "d")}";
                            b++;
                        }

                        if (time.Hours != 0 && b < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "h", "h", "h")}";
                            b++;
                        }

                        if (time.Minutes != 0 && b < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "m", "m", "m")}";
                            b++;
                        }
                        
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && b < maxSubstr) 
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "s", "s", "s")}";
                                b++;
                            }
                        }
                        break;
                }

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }

            private static DateTime Epoch = new DateTime(1970, 1, 1);
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            public static double GetTimeStamp()
            {
                return DateTime.Now.Subtract(Epoch).TotalSeconds;
            }
        }

        private const string TeleportImage = "https://i.imgur.com/kTyCOMy.png";
        
         
                [PluginReference] private Plugin IQChat, ImageLibrary, Friends, Clans;

        [ChatCommand("homelist")]
        private void ChatCommandHomeList(BasePlayer player, string command, string[] args)
        {
            var data = PluginData.GetForPlayer(player.userID);
            if (data == null)
                return;

            if (data.HomesList.Count < 1)
            {
                SendMessage(player, "HOMELIST.ZERO");
                return;
            }

            string homes = "";
            for (var i = data.HomesList.Count - 1; i >= 0; i--)
            {
                var home = data.HomesList[i];
                homes += $"{home.HomeName}";

                if (i != 0) homes += ", ";
            }

            SendMessage(player, "HOMELIST.FOUND", homes);
        }

        void Unload()
        {
            PluginData.SaveData();

            for (var i = 0; i < MarkerBag.MarkerBags.Count; i++)
                UnityEngine.Object.Destroy(MarkerBag.MarkerBags[i]);
            
            for (var i = 0; i < TeleportMgr.ManagersList.Count; i++)
                UnityEngine.Object.Destroy(TeleportMgr.ManagersList[i]);
            
            for (var i = 0; i < _config.SettingsMonument.AvailableMonuments.Count; i++)
            {
                var monument = _config.SettingsMonument.AvailableMonuments[i];
                if(monument.CreatedBag != null)
                    monument.CreatedBag.Kill();
            }
        }
        
        [ChatCommand("tpc")]
        private void ChatCommandTpc(BasePlayer player, string command, string[] args)
        {
            var teleportMgr = TeleportMgr.GetManager(player);
            if (teleportMgr == null)
                return;
            
            if (teleportMgr.Request != null)
            {
                if (teleportMgr.Request.IsHome)
                {
                    teleportMgr.CancelTeleport();
                    SendMessage(player, "HOME.PLAYER.CANCEL");
                    return;
                }

                if (teleportMgr.Request.Monument != null)
                {
                    teleportMgr.CancelTeleport();
                    return;
                }

                if (teleportMgr.Request.Target != null)
                {
                    //var objTarget = TeleportMgr.GetManager(teleportMgr.Request.Target);
                    SendMessage(teleportMgr.Request.Target, "TPR.TARGET.CANCEL", player.displayName);
                    //objTarget.CancelTeleport();
                }

                SendMessage(player, "TPR.PLAYER.CANCEL", teleportMgr.Request.Target.displayName);
                teleportMgr.CancelTeleport();
                
                return;
            }

            var pendingRequest = teleportMgr.IncomingRequests.FirstOrDefault();
            if (pendingRequest != null)
            {
                if (pendingRequest.Player != null)
                {
                    var objTarget = TeleportMgr.GetManager(pendingRequest.Player);
                    
                    SendMessage(player, "TPR.PLAYER.CANCEL", pendingRequest.Player.displayName);
                    SendMessage(pendingRequest.Player, "TPR.TARGET.CANCEL", player.displayName);
                    
                    objTarget.CancelTeleport();
                }

                //teleportMgr.IncomingRequests.Remove(pendingRequest);
                //teleportMgr.CancelTeleport();
                return; 
            }
        }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }
        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        
        [ChatCommand("tp")]
        private void ChatCommandTp(BasePlayer player, string command, string[] args)
        {
            if(player.IsAdmin == false && permission.UserHasPermission(player.UserIDString, AdminPermission) == false)
                return;
            
            switch (args.Length)
            {
                case 0: 
                    
                break;
                
                case 1:
                    
                    string name = args[0];
                    BasePlayer target = null;

                    if(!PlayerHelper.FindOnlineAndOfflineMultiple(name, player, out target))
                    {
                        return;
                    }
                    if (target == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendMessage(player, "PLAYER.NOT.FOUND");
                        return;
                    }

                    Teleport(player, target);
                    break;
                case 2:
                    
                    string name1 = args[0];
                    string name2 = args[1];
                    BasePlayer target1 = null;

                    if(!PlayerHelper.FindOnlineAndOfflineMultiple(name1, player, out target1))
                    {
                        return;
                    }
                    
                    BasePlayer target2 = null;

                    if(!PlayerHelper.FindOnlineAndOfflineMultiple(name2, player, out target2))
                    {
                        return;
                    }

                    if (target1 == null || target2 == null)
                    {
                        SendMessage(player, "PLAYER.NOT.FOUND");
                        return;
                    }

                    Teleport(target1, target2);
                    break;
                case 3:

                    float x = float.Parse(args[0]);
                    float y = float.Parse(args[1]);
                    float z = float.Parse(args[2]);

                    Teleport(player, x, y, z);
                    break;
            }
        }
        
        public class Request
        {
            public bool IsHome = false;
            public SettingMonument.AvailableMonument Monument;
            public BasePlayer Target = null;
            public BasePlayer Player = null;
            public Vector3 Position = Vector3.zero;
            public RealTimeSince RequestTimeout = 0;
            public bool RequestConfirmed = false;

            public Request(BasePlayer player, bool isHome, Vector3 position = default(Vector3), BasePlayer target = null, SettingMonument.AvailableMonument monument = null)
            {
                this.IsHome = isHome;
                this.Monument = monument;
                this.Target = target;
                this.Player = player;
                this.Position = position;
            }
        }
        
        private class PluginData
        {
            private const string DataPath = "Teleportation/Data";

            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, PlayerData> PlayersData = new Dictionary<ulong, PlayerData>();

            public static PlayerData GetForPlayer(ulong player)
            {
                if (_data == null)
                    return null;

                PlayerData data = null;
                if (_data.PlayersData.TryGetValue(player, out data) == false && player.ToString().Length >= 17)
                {
                    data = new PlayerData();
                    _data.PlayersData.Add(player, data);
                }
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                return data;
            }
            
            public static void SaveData()
            {
                if (_data == null) return;
                Interface.Oxide.DataFileSystem.WriteObject(DataPath, _data);
            }

            public static void LoadData()
            {
                if (Interface.Oxide.DataFileSystem.ExistsDatafile(DataPath))
                {
                    _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(DataPath);
                }
                else
                {
                    _data = new PluginData()
                    {
                        
                    };
                }

                if (_data == null)
                {
                    _data = new PluginData()
                    {
                        
                    };
                }
            }
        }
        
        private readonly int triggerLayer = LayerMask.GetMask("Trigger");
        private readonly int blockLayer = LayerMask.GetMask("Construction");
        
        bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (playerid == ownerid) 
                return true;
            
            if (_config.UseFriends && Friends != null && Friends.IsLoaded)
            {
                var fr = Friends?.CallHook("AreFriends", playerid, ownerid);
                if (fr != null && fr is bool && (bool)fr)
                {
                    return true;
                }
            }
            
            if (_config.UseClans && Clans != null && Clans.IsLoaded)
            {
                string playerclan = (string)Clans?.CallHook("GetClanOf", playerid);
                string ownerclan = (string)Clans?.CallHook("GetClanOf", ownerid);
                if (playerclan != null && ownerclan != null && playerclan == ownerclan)
                {
                    return true;
                }
            }
            
            if (_config.UseTeams)
            {
                RelationshipManager.PlayerTeam playerTeam;
                if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerid, out playerTeam))
                {
                    if (playerTeam.members.Contains(ownerid))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private string GetMessage(BasePlayer player, string message, string arg1 = "", string arg2 = "")
        {
            return string.Format(lang.GetMessage(message, this, player.UserIDString), arg1, arg2);
        }
        
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                
            };
        }
        
        [ChatCommand("tpm")]
        private void ChatCommandTpMonument(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendMessage(player, "TPM.HELP");
                return;
            }

            switch (args[0])
            {
                case "add":
                {
                    if (player.IsAdmin == false)
                        break;
                    
                    var closestMonument = FindClosestMonument(player.transform.position);
                    if (closestMonument == null)
                    {
                        SendMessage(player, "TPM.ADMIN.CLOSEST.NOT.FOUND");
                        break;
                    }
                    
                    var monumentInfo = _config.SettingsMonument.AvailableMonuments.Find(x => closestMonument.name.Contains(x.ShortPrefabName));
                    if (monumentInfo != null)
                    {
                        SendMessage(player, "TPM.ADMIN.EXISTS");
                        break;
                    }

                    Vector3 positionOffset = ParentPosition.GetOffsetPosition(closestMonument.transform, player.transform.position);
                    
                    string name = GetShortMonumentName(closestMonument.name);
                    
                    SleepingBag bag = null;
                    if (_config.SettingsMonument.EnableSleepingBags)
                    {
                        bag = CreateSleepingBag(null, ParentPosition.GetFinalPosition(closestMonument.transform, positionOffset) - new Vector3(0, 100f, 0), name);
                        bag.SetPublic(false);
                        bag.OwnerID = 0;
                        bag.deployerUserID = 0;
                        bag.spawnOffset += new Vector3(0, 100f, 0);
                        bag.unlockTime = Time.realtimeSinceStartup + 30f;
                        bag.secondsBetweenReuses = 30f;
                    }

                    monumentInfo = new SettingMonument.AvailableMonument()
                    {
                        ShortPrefabName = name,
                        CoolDown = 300f,
                        CoolDownPublic = 30f,
                        TeleportTime = 30f,
                        Name = name,
                        PositionOffset = positionOffset,
                        ShortCutCommand = "",
                        CreatedBag = bag,
                        MonumentInfo = closestMonument,
                    };
                    
                    _config.SettingsMonument.AvailableMonuments.Add(monumentInfo);
                    SaveConfig();
                    
                    SendMessage(player, "TPM.ADMIN.SUCCESS");
                    
                    break;
                }

                case "remove":
                {
                    if (player.IsAdmin == false)
                        break;

                    if (args.Length < 2)
                        break;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                    string name = string.Join(" ", args.Skip(1));
                    
                    var monumentInfo = _config.SettingsMonument.AvailableMonuments.Find(x => x.Name == name);
                    if (monumentInfo == null)
                    {
                        SendMessage(player, "TPM.ADMIN.REMOVE.NOT.FOUND");
                        break;
                    }

                    _config.SettingsMonument.AvailableMonuments.Remove(monumentInfo);
                    SaveConfig();
                    SendMessage(player, "TPM.ADMIN.REMOVE.SUCCESS");
                
                    break;
                }

                case "list":
                {
                    if (_config.SettingsMonument.AvailableMonuments.Count < 1)
                    {
                        SendMessage(player, "TPM.NO.AVAILABLE.MONUMENTS");
                        break;
                    }
                    
                    string monuments = "";
                    for (var i = _config.SettingsMonument.AvailableMonuments.Count - 1; i >= 0; i--)
                    {
                        var availableMonument = _config.SettingsMonument.AvailableMonuments[i];
                        monuments += $"\n/tpm {availableMonument.Name}" + (string.IsNullOrEmpty(availableMonument.ShortCutCommand) ? "" : $" (/{availableMonument.ShortCutCommand})");
                    }

                    SendMessage(player, "TPM.LIST.FOUND", monuments);
                    
                    break;
                }

                default:
                {
                    if (args.Length < 1)
                    {
                        SendMessage(player, "TPM.HELP");
                        break;
                    }

                    if (player.IsHostile())
                    {
                        SendMessage(player, "TPM.HOSTILE");
                        break;
                    }
                    
                    string name = string.Join(" ", args);
                    
                    var monumentInfo = _config.SettingsMonument.AvailableMonuments.Find(x => x.Name == name);
                    if (monumentInfo == null)
                    {
                        SendMessage(player, "TPM.NOT.FOUND");
                        break;
                    }

                    if (monumentInfo.MonumentInfo == null)
                    {
                        SendMessage(player, "TPM.NOT.FOUND");
                        break;
                    }
                    
                    var data = PluginData.GetForPlayer(player.userID);
                    if (data == null)
                        break;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
                    double cooldown = data.GetMonumentCooldown(monumentInfo.Name);
                    if (cooldown > GetCurrentTime())
                    {
                        SendMessage(player, "TPM.COOLDOWN", TimeHelper.FormatTime(TimeSpan.FromSeconds(cooldown - GetCurrentTime())));
                        break;
                    }

                    var teleportMgr = TeleportMgr.GetManager(player);
                    if (teleportMgr == null)
                        break;
                    
                    teleportMgr.InitRequest(ParentPosition.GetFinalPosition(monumentInfo.MonumentInfo.transform, monumentInfo.PositionOffset), false, null, monumentInfo);
                    SendMessage(player, "TPM.TP.SUCCESS", TimeHelper.FormatTime(TimeSpan.FromSeconds(monumentInfo.TeleportTime)));
                    
                    break;
                }
            }
        }
        private List<string> _imagesLoading = new List<string>();
        
        public class SettingsHome 
        {
            [JsonProperty("Время на телепортацию ('Пермишен': Секунд) [Teleportation Time('Permission': seconds)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> TeleportationTime = new Dictionary<string, float>()
            {
                ["teleportation.default"] = 10f
            };

            [JsonProperty("Учитывать дистанцию при телепортации? [Take into account the distance?]")]
            public bool ConsiderDistance = false;
            
            [JsonProperty("КД на телепортацию('Пермишен': Секунд) [Cooldown Teleportation('Permission': seconds)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> CooldownTeleportation = new Dictionary<string, float>()
            {
                ["teleportation.default"] = 30f
            };
            
            [JsonProperty("Количество домов['Пермишен': Количество] [Count Home ('Permission': seconds)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> CountHome = new Dictionary<string, int>()
            {
                ["teleportation.default"] = 3
            };

            [JsonProperty("Множитель телепортации(для дистанции) [Multiplier teleportation for distance]")]
            public double Multiplier = 10;
            
            [JsonProperty("Проверять фундамент на владельца? [Check foundation for owner?]")]
            public bool CheckFoundationForOwner = true;
            
            [JsonProperty("Список разрешенных для установки дома блоков [Valid building blocks for home]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string[] AvailableFoundations = new []
            {
                "assets/prefabs/building core/foundation/foundation.prefab",
                "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab"
            };
            
            [JsonProperty("Включить систему спальников? [Enable sleeping bag system?]")]
            public bool EnableSleepingBags = true;
        }
        
        private static Teleportation _plugin;
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
        private void CheckStatus()
        {
            int loadedImages = 0;
            foreach (var value in _imagesLoading)
            {
                if (HasImage(value) == false)
                    continue;

                loadedImages++;
            }
            
            if (loadedImages < _imagesLoading.Count - 1 && (bool)ImageLibrary.Call("IsReady") == false)
            {
                PrintError($"Plugin is not ready! Loaded: {loadedImages}/{_imagesLoading.Count} images.");
                timer.Once(10f, CheckStatus);
            }
            else
            {
                FullLoad();
                PrintWarning("Plugin succesfully loaded! Author: BadMandarin.");
            }
        }
        
        [ChatCommand("tphome")]
        private void ChatCommandTpHome(BasePlayer player, string command, string[] args)
        {
            if(player.IsAdmin == false && permission.UserHasPermission(player.UserIDString, AdminPermission) == false)
                return;
            
            string name = args[0];
            BasePlayer target = null;
            PlayerData data = null;
            
            switch (args.Length)
            {
                case 1:
                    
                    if(!PlayerHelper.FindOnlineAndOfflineMultiple(name, player, out target))
                    {
                        return;
                    }
                    
                    if (target == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendMessage(player, "PLAYER.NOT.FOUND");
                        return;
                    }

                    data = PluginData.GetForPlayer(target.userID);
                    if (data == null)
                        break;
                    
                    if (data.HomesList.Count < 1)
                    {
                        SendMessage(player, "TP.HOMELIST.ZERO");
                        return;
                    }

                    string homes = "";
                    for (var i = data.HomesList.Count - 1; i >= 0; i--)
                    {
                        var home = data.HomesList[i];
                        homes += $"{home.HomeName}";

                        if (i != 0) homes += ", ";
                    }

                    SendMessage(player, "TP.HOMELIST.FOUND", homes);
                    
                    break;
                case 2:
                    
                    if(!PlayerHelper.FindOnlineAndOfflineMultiple(name, player, out target))
                    {
                        return;
                    }
                    
                    if (target == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendMessage(player, "PLAYER.NOT.FOUND");
                        return;
                    }
                    
                    data = PluginData.GetForPlayer(target.userID);
                    if (data == null)
                        break;
                    
                    if (data.HomesList.Count < 1)
                    {
                        SendMessage(player, "TP.HOMELIST.ZERO");
                        return;
                    }

                    var playerHome = data.HomesList.Find(x => x.HomeName.Contains(string.Join(" ", args.Skip(1))));
                    if (playerHome == null)
                        break;
                    
                    Teleport(player, playerHome.HomePos);
                    
                    break;
                case 3:

                    break;
            }
        }
        
        private SleepingBag CreateSleepingBag(BasePlayer player, Vector3 pos, string name)  
        {
            SleepingBag sleepingBag =  
                GameManager.server.CreateEntity("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab", pos, Quaternion.identity) as SleepingBag;
            if (sleepingBag == null) 
                return null; 
            
            sleepingBag.deployerUserID = player?.userID ?? 0; 
            sleepingBag.niceName = name; 
            sleepingBag.OwnerID = player?.userID ?? 0;

            sleepingBag.gameObject.AddComponent<DestroyOnGroundMissing>();
            sleepingBag.gameObject.AddComponent<GroundWatch>();

            sleepingBag.Spawn();
            
            return sleepingBag;
        }
        
        private float GetTeleportCooldownHome(ulong uid)
        {
            float min = 9999f;
            bool set = false;
            foreach (var privilege in _config.SettingsHome.CooldownTeleportation)
            {
                if (permission.UserHasPermission(uid.ToString(), privilege.Key))
                {
                    min = Mathf.Min(min, privilege.Value);
                    set = true;
                }
            }
            
            return set ? min : -1f;
        } 
        
        private static string GetColor(string hex, float alpha = 1f)
        {
            if (hex.Length != 7) hex = "#FFFFFF";
            if (alpha < 0 || alpha > 1f) alpha = 1f;

            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;

            return $"{r} {g} {b} {alpha}";
        }

        private MonumentInfo FindClosestMonument(Vector3 position)
        {
            MonumentInfo info = null;
            float minDistance = float.MaxValue;
            var monuments = TerrainMeta.Path.Monuments;

            foreach (var monument in monuments)
            {
                float distance = Vector3.Distance(position, monument.transform.position);
                if (distance > minDistance)
                    continue;

                minDistance = distance;
                info = monument;
            }

            return info;
        }

                
                private class PluginConfig
        {
            [JsonProperty("Автоматически удалять DATA файл при вайпе? [Automatically delete DATA file when wipe?]")]
            public bool AutoWipe = true;
            
            [JsonProperty("Использовать плагин друзей? [Use friends plugin?]")]
            public bool UseFriends = false;
            
            [JsonProperty("Использовать плагин кланов? [Use clans plugin?]")]
            public bool UseClans = false;
            
            [JsonProperty("Использовать систему друзей игры? [Use rust teams system?]")]
            public bool UseTeams = true;
            
            [JsonProperty("Настройки дома[SETTINGS HOME]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SettingsHome SettingsHome = new SettingsHome();
            
            [JsonProperty("Настройки монументов[SETTINGS MONUMENTs]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SettingMonument SettingsMonument = new SettingMonument();
            
            [JsonProperty("Настройки телепортации[SETTINGS TELEPORT]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SettingsTeleport SettingsTeleport = new SettingsTeleport();
            
            [JsonProperty("Настройки запретов[SETTINGS RESOLUTION]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SettingsResolution SettingsResolution = new SettingsResolution()
            {
                SettingsBleeding = new SettingsBleeding(),
                SettingsRadiation = new SettingsRadiation(),
                SettingsTemperature = new SettingsTemperature(),
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        private void RegisterPermissions()
        {
            if(!permission.PermissionExists(AdminPermission))
                permission.RegisterPermission(AdminPermission, this);
            
            foreach (var perm in _config.SettingsHome.CooldownTeleportation)
            {
                if(!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);
            }
            
            foreach (var perm in _config.SettingsHome.CountHome)
            {
                if(!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);
            }
            
            foreach (var perm in _config.SettingsHome.TeleportationTime)
            {
                if(!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);
            }
            
            foreach (var perm in _config.SettingsTeleport.CooldownTeleportation)
            {
                if(!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);
            }
            
            foreach (var perm in _config.SettingsTeleport.TeleportationTime)
            { 
                if(!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);
            }
        }
        
        
                void OnServerInitialized()
        {
            _plugin = this;
            PluginData.LoadData();
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
            StartPluginLoad();
        }
        
        [ChatCommand("sethome")]
        private void ChatCommandSetHome(BasePlayer player, string command, string[] args)
        {
            if (GetHomeLimit(player.userID) <= 0)
                return;
            
            var foundationList = GetFoundation(player.ServerPosition);
            if (foundationList == null || foundationList.Count == 0)
            {
                SendMessage(player, "FOUNDATION.NOT.FOUND");
                return; 
            }
            
            var foundation = foundationList?.First();
            if (foundation == null)
            {
                SendMessage(player, "FOUNDATION.NOT.FOUND");
                return;
            }
            
            string err = CheckFoundation(player.userID, player.ServerPosition);
            if (err != null)
            {
                SendMessage(player, err);
                return;
            }
            
            err = CheckInsideBlock(player.ServerPosition);
            if (err != null)
            {
                SendMessage(player, err);
                return;
            }
                
            if (player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null &&
                !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "SETHOME.CANCEL.BUILD"); 
                return;
            }
            
            if (args.Length != 1)
            {
                SendMessage(player, "SETHOME.HELP"); 
                return;
            }

            if (player.IsWounded() && _config.SettingsResolution.CantTeleportWounded ||
                player.metabolism.bleeding.value > _config.SettingsResolution.SettingsBleeding.NeedBleedingAmount
                && _config.SettingsResolution.SettingsBleeding.CantTeleportBleeding)
            {
                SendMessage(player, "BLOOD.OR.WOUNDED");
                return;
            }
            
            SetHome(player, args[0]);
        } 
		   		 		  						  	   		  		 			  	 	 		  	 				   		 
        private void Teleport(BasePlayer player, Vector3 position, BasePlayer target = null)
        {
            if (player.IsDead() && player.IsConnected)
            {
                player.RespawnAt(position, Quaternion.identity);
                return;
            }

            if (target == null)
            {
                if (player.HasParent())
                {
                    player.SetParent(null);
                    player.SendNetworkUpdate();
                }
            }
            else
            {
                if (target.HasParent())
                {
                    player.SetParent(target.GetParentEntity());
                    player.SendNetworkUpdate();
                }
            }

            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendMessageWithoutLang(player, ret);
                return;
            }

            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");

            player.StartSleeping();
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try
            {
                player.ClearEntityQueue(null);
            }
            catch
            {
                //ignored
            }

            player.SendFullSnapshot();
        }
        
        private bool UnderneathFoundation(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + new Vector3(0f, 3f, 0f), Vector3.down, out hit, 5f, Layers.Mask.Construction, QueryTriggerInteraction.Ignore))
            {
                var block = hit.GetEntity() as BuildingBlock;

                if (block.IsValid() && (block.prefabID == 72949757 || block.prefabID == 3234260181))
                {
                    return hit.point.y > position.y;
                }
            }
            return false;
        }

        [ChatCommand("removehome")]
        private void ChatCommandRemoveHome(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "REMOVE.HOME.HELP");
                return;
            }

            string homeName = string.Join(" ", args);
            
            var data = PluginData.GetForPlayer(player.userID);
            var home = data.GetHome(homeName);
            
            if (home == null)
            {
                SendMessage(player, "REMOVE.HOME.NONE");
                return;
            }

            if (home.SleepingBagId != 0)
            {
                var bag = BaseNetworkable.serverEntities.Find(home.SleepingBagId);
                if (bag != null)
                    bag.Kill();
            }

            data.HomesList.Remove(home);
            SendMessage(player, "REMOVE.HOME.SUCCESS", homeName);
        }
        
        private const string Layer = "UI.Teleportation";
        
        
        
        
        [ConsoleCommand("home")]
        void ConsoleCmdHome(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandHome(player, "", arg.Args);
        }

        [ChatCommand("tpr")]
        private void ChatCommandTpr(BasePlayer player, string command, string[] args)
        {
            if (GetTeleportTimeTpr(player.userID) <= 0)
                return;
            
            if (args.Length < 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessage(player, "TPR.HELP");
                return;
            }
            
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendMessageWithoutLang(player, ret);
                return;
            }
            
            var message = CanTeleportPlayer(player);
            if (string.IsNullOrEmpty(message) == false)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendMessageWithoutLang(player, message);
                return;
            }

            var data = PluginData.GetForPlayer(player.userID);
            if (data == null)
                return;
            
            if (data.CooldownTeleport >= GetCurrentTime())  
            { 
                SendMessage(player, "TPR.COOLDOWN", TimeHelper.FormatTime(TimeSpan.FromSeconds(data.CooldownTeleport - GetCurrentTime()), language: lang.GetLanguage(player.UserIDString)));
                return;
            } 

            BasePlayer target = null; 
            if(!PlayerHelper.FindOnlineMultiple(string.Join(" ", args), player, out target))
            {
                return;
            }
            
            if (target == null) 
                return;
 
            if (target == player) 
                return; 
            
            var teleportMgr = TeleportMgr.GetManager(player);
            if (teleportMgr == null)
                return;

            if (teleportMgr.HasRequest())
            {
                SendMessage(player, "TPR.PLAYER.CANCEL.REQUEST");
                return;
            }

            var targetTeleportMgr = TeleportMgr.GetManager(target);
            if (targetTeleportMgr == null)
                return;

            teleportMgr.InitRequest(target.transform.position, false, target);
            targetTeleportMgr.IncomingRequests.Add(teleportMgr.Request);
            
            SendMessage(player, "TPR.PLAYER.SEND", target.displayName);
            SendMessage(target, "TPR.TARGET.SEND", player.displayName);

            var targetData = PluginData.GetForPlayer(target.userID);
            if (targetData?.AutoAccept == true && IsFriend(player.userID, target.userID))
            {
                ChatCommandTpa(target, "", new string[] { "" });
                return;
            }

            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel 
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMax = "0 0" },
                Image = { Color = GetColor("#FFFFFF", 0.4f) }
            }, "Hud", LayerNotification);
            
            container.Add(new CuiElement()
            { 
                Parent = LayerNotification,
                Name = LayerNotification + ".Main",
                Components =
                {
                    new CuiRawImageComponent() 
                    {
                        Png = GetImage(TeleportImage)
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"0 0", 
                        AnchorMax = $"1 1",  
                        OffsetMin = "-200 81",
                        OffsetMax = "180 117.6" 
                    },
                }
            });


            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-151 81", OffsetMax = "132 117.6"},
                Image = { Color = "0.9686275 0.9215686 0.8823529 0.13529412"}
            }, LayerNotification);   
             
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-151 81", OffsetMax = "132 117.6" },
                Text = { Text = GetMessage(player, "TPR.UI.REQUEST" , player.displayName), Font = "RobotoCondensed-Bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 16, Color = GetColor("#FFFFFFB2")},
            }, LayerNotification); 
 
            container.Add(new CuiButton() 
            {  
                RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0 1", OffsetMax = "49 0"},
                Button = { Color = "0 0 0 0", Command = "chat.say /tpc"},  
                Text = { Text = GetMessage(player, "TPR.UI.REQUEST.NO"), Align = TextAnchor.MiddleCenter, Color = GetColor("#D12C2C"), FontSize = 16}
            }, LayerNotification + ".Main");
             
            container.Add(new CuiButton() 
            {   
                RectTransform = { AnchorMin = "0.98 0", AnchorMax = "1 1", OffsetMin = "-49 0" },  
                Button = { Color = "0 0 0 0", Command = "chat.say /tpa"},    
                Text = { Text = GetMessage(player, "TPR.UI.REQUEST.YES"), Align = TextAnchor.MiddleCenter, Color = GetColor("#C1FF9B"), FontSize = 16}
            }, LayerNotification + ".Main");

            CuiHelper.AddUi(target, container);
        }

        public class SettingsBleeding
        {
            [JsonProperty("Кол-во кровотечение, при котором будет вызываться запрет на телепортацию [The amount of bleeding that will trigger a ban on teleportation]")]
            public float NeedBleedingAmount = 5f;

            [JsonProperty("Запретить телепортацию при кровотечении? [Prohibit teleportation during bleeding?]")]
            public bool CantTeleportBleeding = true;
        }

        public class SettingsTeleport
        {
            [JsonProperty("Время на телепортацию ('Пермишен': Секунд) [Teleportation Time('Permission': seconds)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> TeleportationTime = new Dictionary<string, float>()
            {
                ["teleportation.default"] = 10f
            };
             
            [JsonProperty("Учитывать дистанцию при телепортации? [Take into account the distance?]")]
            public bool ConsiderDistance = false;
            
            [JsonProperty("КД на телепортацию('Пермишен': Секунд) [Cooldown Teleportation('Permission': seconds)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> CooldownTeleportation = new Dictionary<string, float>()
            {
                ["teleportation.default"] = 30f
            };
             
            [JsonProperty("Множитель телепортации(для дистанции) [Multiplier teleportation for distance]")]
            public double Multiplier = 10;
        }
        
        [ConsoleCommand("tpm")]
        void ConsoleCmdTpm(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;
            
            if (arg.HasArgs() == false) 
                return;
            
            ChatCommandTpMonument(player, "", arg.Args);
        }
        
                
        
        private static bool _initiated = false;
        
        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2f, () => OnPlayerConnected(player));
                return;
            }

            if (!player.IsConnected) return;

            player.GetOrAddComponent<TeleportMgr>();
        }
        
            }
}
