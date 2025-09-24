// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using CompanionServer;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SelfWipe", "MuB-Studios", "1.0.5")]
    [Description("This plugin allows players to wipe their datas  themself!")]
    public class SelfWipe : RustPlugin
    {
        [PluginReference] private Plugin ZLevelsRemastered, Backpacks, NoEscape;
        public string UsePermission = "selfwipe.use";
        private Configuration _config;
        private Dictionary<ulong, List<BuildingPrivlidge>> TCList = new Dictionary<ulong, List<BuildingPrivlidge>>();
      
        #region GUI
        void CreateWipeMenu(BasePlayer player)
        {
            var c = new CuiElementContainer();

            var PanelMain = c.Add(new CuiPanel
            {
                Image = { Color = "0.00 0.00 0.00 0.86" },
                RectTransform = { AnchorMin = "0.0 0.0", AnchorMax = "0.0 0.0" },
                CursorEnabled = true,
            }, "GUI", "quickMenu");

            c.Add(new CuiButton
            {
                Button = { Command = "wipe", Color = "0.00 0.00 0.00 0.00" },
                RectTransform = { AnchorMin = "0.0 0.0", AnchorMax = "0.0 0.0" },
                Text = { Text = "Test" },
            }, PanelMain);
        }
        #endregion
        
        #region Config

        private class Configuration
        {
            
            [JsonProperty(PropertyName = "Button list", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ButtonSettings> ButtonList = new List<ButtonSettings>()
            {
                new ButtonSettings()
                {
                    Text = "Wipe All",
                    Cmd = "SELF_WIPE ALL"
                },
                new ButtonSettings()
                {
                    Text = "Wipe Blueprints",
                    Cmd = "SELF_WIPE BP"
                },
                new ButtonSettings()
                {
                    Text = "Wipe Buildings",
                    Cmd = "SELF_WIPE BUILD"
                },
                new ButtonSettings()
                {
                    Text = "Wipe Inventory",
                    Cmd = "SELF_WIPE INVENTORY"
                    
                },
                new ButtonSettings()
                {
                    Text = "Wipe Backpack",
                    Cmd = "SELF_WIPE BACKPACK"
                    
                },
            };

            [JsonProperty("Chat Command")]
            public string wipeCommand = "wipe";
            
            internal class ButtonSettings
            {

                [JsonProperty(PropertyName = "Close when pressed?", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public bool IsClose = true;
                [JsonProperty(PropertyName = "Command", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string Cmd = "";
                [JsonProperty(PropertyName = "Text", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string Text = "";

                [JsonProperty(PropertyName = "Button Color", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string ButtonColor = "0 0 0 1";

                [JsonProperty(PropertyName = "Text color", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string ColorText = "1 1 1 1";

                [JsonProperty(PropertyName = "Enable button", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public bool IsEnable = true;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion
      

        #region Hooks

        void Init()
        {
            permission.RegisterPermission(UsePermission, this);

            AddCovalenceCommand(_config.wipeCommand, nameof(WipeCommand));
        }
        private void OnServerInitialized(bool initial)
        {
            foreach (var check in UnityEngine.Object.FindObjectsOfType<BuildingPrivlidge>())
            {
                if(!TCList.ContainsKey(check.OwnerID))
                    TCList.Add(check.OwnerID, new List<BuildingPrivlidge>());

                TCList[check.OwnerID].Add(check);
            }
        }


        private void OnEntitySpawned(BuildingPrivlidge privlidge)
        {
            if(!TCList.ContainsKey(privlidge.OwnerID))
                TCList.Add(privlidge.OwnerID, new List<BuildingPrivlidge>());

            TCList[privlidge.OwnerID].Add(privlidge);
        }
        
        private void OnEntityKill(BuildingPrivlidge privlidge)
        {
            if(!TCList.ContainsKey(privlidge.OwnerID))return;
            TCList[privlidge.OwnerID].Remove(privlidge);
        }
        
        private void ClearInventory(BasePlayer player)
        {
            string InventoryWipeMessage = lang.GetMessage("InventoryWipeSuccesful", this);
            player.inventory.containerBelt.Clear();
            player.inventory.containerMain.Clear();
            player.inventory.containerWear.Clear();
            PrintToChat(player, InventoryWipeMessage);
        }

        private void ClearBlueprints(BasePlayer player)
        {
            string BlueprintWipeMessage = lang.GetMessage("BlueprintWipeSuccesful", this);
            player.blueprints.Reset();
            SendReply(player, BlueprintWipeMessage);
        }

        #endregion
        
        
        #region Command

        private void WipeCommand(IPlayer iplayer, string cmd, string[] args)
        {
            if (iplayer.IsServer) return;
            BasePlayer player = iplayer.Object as BasePlayer;
            if (iplayer == null) return;

            try
            {
                switch (args[0].ToLower())
                {
                    case "blueprint":
                        ClearBlueprints(player);
                        break;
                    case "inventory":
                        ClearInventory(player);
                        break;
                    case "buildings":
                        ClearBuilds(player);
                        break; 
                   
                }
            }
            catch
            {
                if(!permission.UserHasPermission(player.UserIDString, UsePermission))return;
                ShowMainUI(player);
            }
          
        }

        [ChatCommand("guitest")]
        private void GUITestCMD(BasePlayer player)
        {
            CreateWipeMenu(player);
        }
        #endregion
        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BlueprintWipeSuccesful"] = "You have wiped all of your Blueprints!",
                ["InventoryWipeSuccesful"] = "You have wiped all of your Inventory!",
                ["BuildingDataSuccesfull"] = "You have wiped all of your Building!",
                ["BackPackWipeSuccesful"] = "You have wiped all of your Backpacks!",
                ["NoPermissionToUse"] = "You don't have permission to use this command!",
            }, this);
        }
        #endregion
        

        private void ShowMainUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 0", OffsetMax = "50 0" }
            }, "Overlay", "Panel_5811");
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Close = "Panel_5811"},
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-1000 -1000", OffsetMax = $"1000 1000" }
            }, "Panel_5811", "CLOSE");
            var count = _config.ButtonList.Count(p => p.IsEnable);
            var width = 49.996 - -50.004;
            var pos = -50.004 - (count * width + (count - 1) * 1) / 2;
            foreach (var check in _config.ButtonList)
            {
                if(!check.IsEnable)continue;
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0.5450981" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{pos} -50", OffsetMax = $"{pos + width} 50" }
                }, "Panel_5811", "Panel_9687");

              var button = new CuiButton
              {
                  Button = { Color = check.ButtonColor, Command = check.Cmd},
                  Text = { Text = check.Text, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = check.ColorText },
                  RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -16.668", OffsetMax = "50 16.668" }
              };
              if (check.IsClose)
                  button.Button.Close = "Panel_5811";
                  
              container.Add(button, "Panel_9687", "Button_5074");
              pos += width + 1;

          
            }
           

            CuiHelper.DestroyUi(player, "Panel_5811");
            CuiHelper.AddUi(player, container);
        }

        [ChatCommand("wipeui")]
        private void cmdChatwipeui(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, UsePermission))return;
            if (NoEscape != null && NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString))
                return;
            ShowMainUI(player);
        }
        
        [ConsoleCommand("SELF_WIPE")]
        private void cmdConsoleSELF_WIPE(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (NoEscape != null && NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString))
                return;
            switch (arg.Args[0])
            {
                case "ALL":
                {
                    ClearBlueprints(player);
                    ClearBuilds(player);
                    ClearInventory(player);
                    ClearBackpack(player);
                    break;
                }
                case "BP":
                {
                    ClearBlueprints(player);
                    break;
                }
                case "BUILD":
                {
                    ClearBuilds(player);
                    break;
                }
                case "INVENTORY":
                {
                    ClearInventory(player);
                    break;
                }
                case "BACKPACK":
                    ClearBackpack(player);
                    break;
              
            }
            
        }

        private void ClearBackpack(BasePlayer player)
        {
            Backpacks?.Call("API_EraseBackpack", player.userID);
            PrintToChat(player, lang.GetMessage("BackPackWipeSuccesful", this));
        }
       
        private void ClearBuilds(BasePlayer player)
        {
            if (!TCList.ContainsKey(player.userID)) return;
            foreach (var check in TCList[player.userID])
            {
                var lists = new List<StabilityEntity>();
                Vis.Entities(check.transform.position, 30, lists);
                foreach (var block in lists)
                {
                    var tc = block.GetBuildingPrivilege();
                    if (tc == null || tc != check) continue;
                    block.Invoke(block.KillMessage, 0.01f);
                }
                var listsss = new List<DecayEntity>();
                Vis.Entities(check.transform.position, 30, listsss);
                foreach (var block in listsss)
                {
                    var tc = block.GetBuildingPrivilege();
                    if (tc == null || tc != check) continue;
                    block.Invoke(block.KillMessage, 0.01f);
                }
                if(check == null)continue;
                if(check.GetBuilding() == null)continue;
                foreach (var block in check.GetBuilding().buildingBlocks)
                {
                    block.Invoke(block.KillMessage, 0.01f);
                }
            }
        }
        
    }
}
