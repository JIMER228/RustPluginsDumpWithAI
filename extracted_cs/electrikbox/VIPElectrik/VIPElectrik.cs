using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("VIPElectrik", "Electrik", "1.2.1")]
    [Description("add player to vip oxide group for a set of time")]

    public class VIPElectrik : RustPlugin
    {
        private TimeSpan cooldownTime;
        private StoredData data = new StoredData();
        private List<ulong> removeKeys = new List<ulong>();
        private bool isVipTimeropen;
        private string staffMsg = "<color=#18BFCA>[VIP]</color> <color=red>Staff message:</color>";
        private string mainPanelName = "vipMenu_panel";
        string timeleft;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region CONFIG

        private ConfigData settings;

        private class ConfigData
        {
            [JsonProperty("Timer Refresh (seconds)")] public int timerRefresh = 1;
            [JsonProperty("vip oxide group name")] public string oxideGroupName = "vip";
            [JsonProperty("Timer UI options")] public ConfigDataUI ui = new ConfigDataUI();
        }

        private class ConfigDataUI
        {
            [JsonProperty("Text color")] public string timerColor = "#FFA200";
            [JsonProperty("Position left")] public float posLeft = 7.5f;
            [JsonProperty("Position right")] public float posRight = 98;
            [JsonProperty("Position bottom")] public float posBottom = 4;
            [JsonProperty("Position top")] public float posTop = 25;
        }

        private bool LoadConfigVariables()
        {
            settings = Config.ReadObject<ConfigData>();
            SaveConfig(settings);
            return true;
        }


        private void SaveConfig(ConfigData settings) => Config.WriteObject(settings, true);

        protected override void LoadDefaultConfig()
        {
            settings = new ConfigData();
            SaveConfig(settings);
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region DATA

        private void LoadData()
        {
            if (data == null) data = new StoredData();
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, data);
            Puts("Data Saved");
        }

        private class DataEntry
        {
            [JsonProperty("Date")] public DateTime Date;
            [JsonProperty("Name")] public string Name;
            [JsonProperty("Time")] public string Time;
            [JsonProperty("UI Show")] public bool UIShow;
            [JsonProperty("Admin")] public string AdminWhoDidCommand;
        }

        private class StoredData
        {
            [JsonProperty("Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, DataEntry> Players = new Dictionary<ulong, DataEntry>();
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region OXIDE FUNCTIONS

        private void Init()
        {
            LoadData();
            if (!LoadConfigVariables())
            {
                Puts("No config found");
                return;
            }

            AddCovalenceCommand("vipadd", nameof(AddPlayerToVipGroup));
            AddCovalenceCommand("vipremove", nameof(AdminRemoveData));
            AddCovalenceCommand("vipui", nameof(HideUI));

            timer.Every(settings.timerRefresh, vipEnd);

            if (BasePlayer.activePlayerList.Count == 0)
                return;

            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                ToggleTimerGUI(vipPlayer);
            }
        }

        private void Unload()
        {
            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                CuiHelper.DestroyUi(vipPlayer, mainPanelName);
            }
            OnServerSave();
        }

        private void OnPlayerConnected(BasePlayer player) => ToggleTimerGUI(player);
        private void OnServerSave() => SaveData();
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region OWN FUNCTIONS

        private BasePlayer GetPlayerByNameOrId(string nameorid) => BasePlayer.activePlayerList.FirstOrDefault(x => x.displayName.Contains(nameorid, System.Globalization.CompareOptions.OrdinalIgnoreCase) || x.UserIDString == nameorid);

        private void AddPlayerToVipGroup(IPlayer user, string command, string[] args)
        {
            var player = user.Object as BasePlayer;
            var nowDate = DateTime.Now;
            int days = 0;

            if (!user.IsAdmin)
            {
                player.ChatMessage("You're not allowed to use this command");
                return;
            }

            if (args.Length < 2)
            {
                Msg(user, player, "Missing args : vipadd *steamid* *time (days)*", "\n<color=#FFAE17>Missing args:</color> /addvip *name or steamid* *time (days)*");
                return;
            }

            BasePlayer vipPlayer = GetPlayerByNameOrId(args[0]);

            if (vipPlayer == null)
            {
                Msg(user, player, "Player not found or Offline", "Player not found or Offline");
                return;
            }

            if (!int.TryParse(args[1], out days))
            {
                Msg(user, player, "Wrong time format : vipadd *steamid* *time (ex: 30)*", "\n<color=#FFAE17>Wrong time format:</color> /addvip *name or steamid* *time (ex: 30)*");
                return;
            }

            if (days > 365)
            {
                Msg(user, player, "Too many days : You can't set more than 365 days for vip*", "\n<color=#FFAE17>Too many days:</color> You can't set more than 365 days for vip*");
                return;
            }

            cooldownTime = new TimeSpan(days, 0, 0, 0);

            if (IsVIP(vipPlayer))
            {
                data.Players[vipPlayer.userID].Date += new TimeSpan(int.Parse(args[1]), 0, 0, 0);
                Puts($"Adding {args[1]} day(s) to {vipPlayer.displayName}'s VIP.");
                SaveData();
                ToggleTimerGUI(vipPlayer);
                return;
            }

            permission.AddUserGroup(vipPlayer.UserIDString, settings.oxideGroupName);

            data.Players[vipPlayer.userID] = new DataEntry
            {
                Name = vipPlayer.displayName,
                Date = nowDate.Add(cooldownTime),
                Time = cooldownTime.ToString(),
                UIShow = true,
                AdminWhoDidCommand = user.Name + " : " + nowDate.ToString()
            };

            SaveData();
            ToggleTimerGUI(vipPlayer);
            Msg(user, player, $"{vipPlayer.displayName} {vipPlayer.UserIDString} added VIP", $"<color=#FFAE17>{vipPlayer.displayName}</color> added to VIP");
        }

        private void RemovePlayerData(string[] args, IPlayer user)
        {
            removeKeys.Clear();
            BasePlayer player = user.Object as BasePlayer;
            BasePlayer vipPlayer = GetPlayerByNameOrId(args[0]);

            foreach (var element in data.Players)
            {
                if (data.Players.Count == 0) return;
                if (element.Value.Name.ToLower().Contains(args[0]) || args[0] == element.Key.ToString())
                {
                    CuiHelper.DestroyUi(vipPlayer, mainPanelName);
                    permission.RemoveUserGroup(element.Key.ToString(), settings.oxideGroupName);
                    removeKeys.Add(element.Key);
                    data.Players.Remove(element.Key);
                    SaveData();
                    Msg(user, player, $"{element.Value.Name} Removed from VIPs", $"<color=#FFAE17>{element.Value.Name}</color> removed from VIPs");
                    break;
                }
            }

            if (removeKeys.Count == 0)
            {
                Msg(user, player, "Player not found in data", "Player not found in data");
            }
            removeKeys.Clear();
        }

        private void AdminRemoveData(IPlayer user, string command, string[] args)
        {
            BasePlayer player = user.Object as BasePlayer;

            if (!user.IsAdmin)
            {
                player.ChatMessage("You're not allowed to use this command");
                return;
            }

            if (data.Players.Count == 0)
            {
                Msg(user, player, "Data file is empty", "Data file is empty");
                return;

            }
            if (args.Length == 0)
            {
                Msg(user, player, "Wrong args remove player data: vipremove *name or steamid*", "\n<color=#FFAE17>remove player data:</color> /vipremove *name or steamid*");
                return;
            }

            RemovePlayerData(args, user);
        }

        private void vipEnd()
        {
            var nowDate = DateTime.Now;
            removeKeys.Clear();

            foreach (var element in data.Players)
            {
                DateTime endDate = Convert.ToDateTime(element.Value.Date);

                if (endDate <= nowDate)
                {
                    BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                    CuiHelper.DestroyUi(vipPlayer, mainPanelName);
                    permission.RemoveUserGroup(element.Key.ToString(), settings.oxideGroupName);
                    removeKeys.Add(element.Key);
                    Puts($"{element.Value.Name} Removed from vip");
                }
            }

            if (removeKeys.Count > 0)
            {
                removeKeys.ForEach(key => data.Players.Remove(key));
                SaveData();
            }
        }

        private void Msg(IPlayer user, BasePlayer player, string putsMSG, string chatMSG)
        {
            if (user.IsServer)
            {
                Puts(putsMSG);
                return;
            }
            player.ChatMessage($"{staffMsg} {chatMSG}");
            Puts($"{putsMSG}");
        }

        private string VipGetPlayerTimeLeft(DateTime element)
        {
            var nowDate = DateTime.Now;
            DateTime endDate = element;
            TimeSpan timeLeftTimer = endDate - nowDate;
            string getTimeleft = timeLeftTimer.ToString(@"dd\:hh\:mm\:ss");

            return getTimeleft;
        }

        private void HideUI(IPlayer user, string command, string[] args)
        {
            BasePlayer player = user.Object as BasePlayer;
            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                if (player == vipPlayer) { element.Value.UIShow = !element.Value.UIShow; }
            }
            SaveData();
            BuildUI(player);
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region UI

        void ToggleTimerGUI(BasePlayer player)
        {
            BuildUI(player);
            timer.Every(settings.timerRefresh, () => BuildUI(player));
        }

        void BuildUI(BasePlayer player)
        {
            foreach (var element in data.Players)
            {
                try
                {
                    if (player.UserIDString == element.Key.ToString() && player.IsConnected && element.Value.UIShow)
                    {
                        timeleft = VipGetPlayerTimeLeft(element.Value.Date);

                        CuiElementContainer container = new CuiElementContainer();
                        UI_AddAnchor(container, mainPanelName, "Hud", "0 0", "0 0");
                        UI_AddPanel(container, "vip_panel", mainPanelName, "0 0 0 0.4",
                            $"{settings.ui.posLeft} {settings.ui.posBottom}",
                            $"{settings.ui.posRight} {settings.ui.posTop}");

                        UI_AddText(container, "vip_panel", "0 0 0 0.9", $"VIP :  {timeleft}",
                            TextAnchor.MiddleCenter, 11, "1 -1", "0 0");

                        UI_AddText(container, "vip_panel", "1 1 1 1", $"VIP :  <color={settings.ui.timerColor}>{timeleft}</color>",
                            TextAnchor.MiddleCenter, 11, "0 0", "0 0");

                        CuiHelper.DestroyUi(player, mainPanelName);
                        CuiHelper.AddUi(player, container);
                        break;
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, mainPanelName);
                    }
                }
                catch (Exception) { }
            }
        }

        private static void UI_AddAnchor(CuiElementContainer container, string name, string parentName, string anchorMin, string anchorMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parentName,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = "0 0",
                        OffsetMax = "0 0",
                    },
					// new CuiNeedsCursorComponent()
				},
            });
        }

        private static void UI_AddPanel(CuiElementContainer container, string name, string parentName, string color, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parentName,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = color,
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                },
            });
        }

        private static void UI_AddText(CuiElementContainer container, string parentName, string color, string text, TextAnchor textAnchor, int fontSize, string offsetMin, string offsetMax)
        {
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = text,
                    FontSize = fontSize,
                    Font = "robotocondensed-bold.ttf",
                    Align = textAnchor,
                    Color = color
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax
                },
            },
            parentName, CuiHelper.GetGuid());
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region API 

        private bool IsVIP(BasePlayer player)
        {
            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                if (player == vipPlayer) return true;
            }
            return false;
        }

        private bool IsVipTimerShow(BasePlayer player)
        {
            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                if (player == vipPlayer) { return element.Value.UIShow; }
            }
            return false;
        }

        TimeSpan VIPTimeLeft(BasePlayer player)
        {
            foreach (var element in data.Players)
            {
                BasePlayer vipPlayer = GetPlayerByNameOrId(element.Value.Name);
                if (player == vipPlayer)
                {
                    var nowDate = DateTime.Now;
                    DateTime endDate = element.Value.Date;
                    TimeSpan timeLeftTimer = endDate - nowDate;
                    return timeLeftTimer;
                }
            }
            return TimeSpan.Zero;
        }
        #endregion
    }
}