using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Police", "Oxide-Russia", "1.1.0")]
    public class Police : CovalencePlugin
    {
        #region image
        private static string PoliceImage = @"[
  {
    ""name"": ""PoliceImage"",
    ""parent"": ""Hud"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.RawImage"",
        ""color"": ""1 1 1 1"",
        ""url"": ""https://i.imgur.com/nyg9MVn.png""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.652 0.009"",
        ""anchormax"": ""0.704 0.103""
      }
    ]
  }
]";
        private void PolicePanelActiv(BasePlayer player)
        {
            string active = lang.GetMessage("PoliceActive", this);
            CuiHelper.DestroyUi(player, "PolicePanelActiv");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.719 0.975", AnchorMax = "0.8 1" }
            }, "Hud", "PolicePanelActiv");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                FadeOut = 1,
                Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.25f },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70.206 -15.428", OffsetMax = "70.204 5.428" }
            }, "PolicePanelActiv", "PolicePanelBack");

            container.Add(new CuiElement
            {
                Name = "PoliceText",
                Parent = "PolicePanelActiv",
                FadeOut = 1,
                Components = {
                    new CuiTextComponent { Text = $"<color=#3aff00>{active} </color>{policeCounter.ToString()}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 0.25f },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-115.206 -15.428", OffsetMax = "115.204 5.428" }
                }
            });
            CuiHelper.AddUi(player, container);
        }

        private void PolicePanelInaktiv(BasePlayer player)
        {
            string inactive = lang.GetMessage("PoliceInactive", this);
            CuiHelper.DestroyUi(player, "PolicePanelInactiv");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.719 0.975", AnchorMax = "0.8 1" }
            }, "Hud", "PolicePanelInactiv");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                FadeOut = 1,
                Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.25f },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70.206 -15.428", OffsetMax = "70.204 5.428" }
            }, "PolicePanelInactiv", "PolicePanelBack");

            container.Add(new CuiElement
            {
                Name = "PoliceText",
                Parent = "PolicePanelInactiv",
                FadeOut = 1,
                Components = {
                    new CuiTextComponent { Text = $"<color=red>{inactive} </color>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 0.25f },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-115.206 -15.428", OffsetMax = "115.204 5.428" }
                }
            });
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region lang
        protected override void LoadDefaultMessages()
        {

            //English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PoliceOnDuty"] = "Police officers on duty: ",
                ["OnDuty"] = "On duty!",
                ["StartedDuty"] = " now on duty.",
                ["AlreadyOnDuty"] = "already on duty",
                ["PoliceNoPermission"] = "You are not a police officer",
                ["EndedDuty"] = " now off duty",
                ["OffDuty"] = "Off duty",
                ["NoPolice"] = "No police officer on duty!",
                ["AlreadyOffDuty"] = "already off duty!",
                ["CurrentlyOffDuty"] = "You are Off duty",
                ["PoliceActive"] = "Police active",
                ["PoliceInactive"] = "Police inactive",
                ["Salary"] = "Salary received: ",
                ["wait"] = "You already called the police wait: ",
                ["Help"] = " sent an emergency call at: ",
                ["Argument"] = "Argument missing (eg: /policeui off or /policeui on)",
                ["ArgumentWrong"] = "Argument wrong (eg: /policeui off or /policeui on)",
                ["helpSended"] = "Help is on the way, map marker is set, wait at current position!",
            }, this, "en");
            //German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PoliceOnDuty"] = "Polizisten im Dienst: ",
                ["OnDuty"] = "Dienst angetreten!",
                ["StartedDuty"] = " hat den Polizeidienst begonnen.",
                ["AlreadyOnDuty"] = "Du bist bereits im Dienst",
                ["wait"] = "Du hast du Polizei bereits benachrichtigt warte: ",
                ["PoliceNoPermission"] = "Du bist kein Polizist",
                ["EndedDuty"] = " hat den Polizeidienst beendet.",
                ["OffDuty"] = "Dienst beendet!",
                ["AlreadyOffDuty"] = "Du hast den Dienst bereits beendet!",
                ["CurrentlyOffDuty"] = "Du bist nicht im Dienst",
                ["PoliceActive"] = "Polizei aktiv",
                ["PoliceInactive"] = "Polizei inaktiv",
                ["Help"] = " hat einen Notruf gesendet bei: ",
                ["NoPolice"] = "Kein Polizist im Dienst!",
                ["Salary"] = "Gehalt erhalten: ",
                ["helpSended"] = "Hilfe ist unterwegs, Map Marker gesetzt, warte an dieser Position!",
                ["Argument"] = "Argument fehlt (bsp: /policeui off oder /policeui on)",
                ["ArgumentWrong"] = "Argument falsch (bsp: /policeui off or /policeui on)",
            }, this, "de");
        }
        #endregion

        #region Initialization
        float MarkerRadius = 0.2f;
        const string notifySound = "assets/bundled/prefabs/fx/invite_notice.prefab";
        public Dictionary<string, MapMarkerGenericRadius> PublicRadMarker = new Dictionary<string, MapMarkerGenericRadius>();
        public Dictionary<string, VendingMachineMapMarker> PublicVendMarker = new Dictionary<string, VendingMachineMapMarker>();
        [PluginReference] Plugin ServerRewards, LockMeUp;
        DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("Police");
        int policeCounter = 0;
        bool aktiv = true;
        Dictionary<string, string> policeArrayNew = new Dictionary<string, string>();
        ArrayList policeArray = new ArrayList();
        void Loaded() => permission.RegisterPermission("police.use", this);
        private Configuration config;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);



        class Configuration
        {
            [JsonProperty("Salary_Item")]
            public string defaultItem { get; set; }
            [JsonProperty("Item_Skin_ID")]
            public ulong ItemSkinID { get; set; }
            [JsonProperty("Item_Display_Name")]
            public string displayName { get; set; }
            [JsonProperty("Salary_Timer_in_Minutes")]
            public int defaultTimer { get; set; }
            [JsonProperty("Salary_Ammount")]
            public int defaultAmmount { get; set; }
            [JsonProperty("Use_Salary_System")]
            public bool useSalary { get; set; }
            [JsonProperty("Use_ServerRewards")]
            public bool ServerRewards { get; set; }
            [JsonProperty("ServerReward_Ammount")]
            public int RewardAmmount { get; set; }
            [JsonProperty("Use_Both_As_Salary")]
            public bool UseBoth { get; set; }
            [JsonProperty("Remove_Map_Marker_After_Minutes")]
            public int Minutes { get; set; }

            public static Configuration CreateConfig()
            {

                return new Configuration
                {
                    defaultItem = "scrap",
                    ItemSkinID = 0,
                    displayName = "",
                    defaultTimer = 60,
                    defaultAmmount = 100,
                    useSalary = true,
                    ServerRewards = false,
                    RewardAmmount = 100,
                    UseBoth = false,
                    Minutes = 5,
                };
            }
        }


        void Init()
        {

            LoadConfig();
            dataFile.Clear();
            LoadDefaultMessages();
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                CuiHelper.DestroyUi(player, "PolicePanelActiv");
                CuiHelper.DestroyUi(player, "PolicePanelInactiv");
                PolicePanelInaktiv(player);
            }
            if (config.useSalary == false)
            {
                Unsubscribe(nameof(calculateSalary));
            }
            bool GroupExists = permission.GroupExists("dutygroup");
            if (!GroupExists)
            {
                permission.CreateGroup("dutygroup", "dutygroup", 0);
            }

        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                CuiHelper.DestroyUi(player, "PolicePanelActiv");
                CuiHelper.DestroyUi(player, "PolicePanelInactiv");
                CuiHelper.DestroyUi(player, "PoliceImage");
                if (PublicRadMarker.ContainsKey(player.UserIDString))
                {
                    deleteMarkerCurrent(player.UserIDString);
                }




            }

        }

        #endregion

        #region function


        private int checkOnDutyCount()
        {
            int policeOnDutyCounter = 0;
            foreach (var data in dataFile)
            {
                policeOnDutyCounter++;

            }

            return policeOnDutyCounter;


        }

        private Item FindItem(string itemName, int ammount, ulong skin, string displayName)
        {
            var item = ItemManager.CreateByName(itemName, ammount, skin);
            if (!string.IsNullOrEmpty(displayName))
            {
                item.name = displayName;
            }

            return item;
        }

        private void calculateSalary(IPlayer player)
        {
            if (config.ServerRewards == false && !config.UseBoth)
            {
                var bplayer = (BasePlayer)player.Object;
                DateTime localDate = DateTime.Now;
                var timer1 = dataFile[player.Id].ToString();
                var parsedDate = DateTime.Parse(timer1);
                double seconds = (localDate - parsedDate).TotalSeconds;
                double scrapPerMinute = (double)config.defaultAmmount / (double)config.defaultTimer;
                double salaryAmmount = ((seconds / (double)60) * scrapPerMinute);
                double endresult = Math.Round(salaryAmmount);
                try
                {
                    bplayer.GiveItem(FindItem(config.defaultItem, (Int32)endresult, config.ItemSkinID, config.displayName), BaseEntity.GiveItemReason.PickedUp);
                }
                catch
                {
                    //salary null
                }

                if (!string.IsNullOrEmpty(config.displayName))
                {
                    bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + config.displayName);
                }
                else
                {
                    bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + config.defaultItem);
                }
            }
            else if (config.ServerRewards == true && !config.UseBoth)
            {
                var bplayer = (BasePlayer)player.Object;
                DateTime localDate = DateTime.Now;
                var timer1 = dataFile[player.Id].ToString();
                var parsedDate = DateTime.Parse(timer1);
                double seconds = (localDate - parsedDate).TotalSeconds;
                double RewardPerMinute = (double)config.RewardAmmount / (double)config.defaultTimer;
                double salaryAmmount = ((seconds / (double)60) * RewardPerMinute);
                double endresult = Math.Round(salaryAmmount);
                try
                {
                    ServerRewards?.Call("AddPoints", bplayer.UserIDString, (Int32)endresult);
                }
                catch
                {
                    //salary Null
                }

                bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + "reward points");
            }
            else if (config.UseBoth == true)
            {
                var bplayer = (BasePlayer)player.Object;
                DateTime localDate = DateTime.Now;
                var timer1 = dataFile[player.Id].ToString();
                var parsedDate = DateTime.Parse(timer1);
                double seconds = (localDate - parsedDate).TotalSeconds;



                double RewardPerMinute = (double)config.RewardAmmount / (double)config.defaultTimer;
                double salaryAmmount = ((seconds / (double)60) * RewardPerMinute);
                double endresult = Math.Round(salaryAmmount);


                double ScrapPerMinute = (double)config.defaultAmmount / (double)config.defaultTimer;
                double differenceNew2 = seconds;
                double salaryAmmount2 = ((differenceNew2 / (double)60) * ScrapPerMinute);
                double endresult2 = Math.Round(salaryAmmount2);



                try
                {
                    ServerRewards?.Call("AddPoints", bplayer.UserIDString, (Int32)endresult);
                    bplayer.GiveItem(FindItem(config.defaultItem, (Int32)endresult, config.ItemSkinID, config.displayName), BaseEntity.GiveItemReason.PickedUp);
                }
                catch
                {
                    //salary null
                }
                bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + "reward points");
                if (!string.IsNullOrEmpty(config.displayName))
                {
                    bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + config.displayName);
                }
                else
                {
                    bplayer.ChatMessage("<color=orange>" + lang.GetMessage("Salary", this) + "</color>" + endresult + " " + config.defaultItem);
                }
            }

        }



        #endregion

        #region hooks



        [Command("police")]
        private void policeCounterCall(IPlayer player, string command, string[] args)
        {
            string policeListNew = "";
            foreach (var val in policeArrayNew)
            {
                policeListNew = string.Join(", ", val.Value);
            }


            string PoliceOnDutyNew = lang.GetMessage("PoliceOnDuty", this);
            player.Message(PoliceOnDutyNew + "<color=#0097ff>" + policeListNew.ToString() + "</color>");     

        }

        [Command("callpolice")]
        private void callPolice(IPlayer player, string command, string[] args)
        {
            BasePlayer bplayer = player.Object as BasePlayer;
            if (policeCounter > 0)
            {
                string[] g = GridFromPos(bplayer.transform.position);

                if (marker(bplayer.transform.position, bplayer))
                {
                    notify(bplayer, g[0], g[1]);


                    timer.Once(config.Minutes * 60f, () =>
                    {

                        deleteMarkerCurrent(bplayer.UserIDString);

                    });
                    bplayer.ChatMessage(lang.GetMessage("helpSended", this));

                }
                else
                {
                    bplayer.ChatMessage(lang.GetMessage("wait", this) + "<color=red>" + config.Minutes.ToString() + "</color>" + " min");
                }

            }
            else
            {
                bplayer.ChatMessage(lang.GetMessage("NoPolice", this));
            }

        }

        [Command("closecase")]
        private void closeCase(IPlayer player, string command, string[] args)
        {
            BasePlayer bplayer = player.Object as BasePlayer;
            if (permission.UserHasPermission(bplayer.UserIDString, "police.use"))
            {
                RaycastHit hit;
                if (Physics.Raycast(bplayer.eyes.HeadRay(), out hit, 3f))
                {
                    BasePlayer cplayer = hit.GetEntity().ToPlayer();
                    if (cplayer == null) { return; }

                    deleteMarkerCurrent(cplayer.UserIDString);

                    return;
                }
            }


        }

        [Command("pd")]
        private void policeOnDuty(IPlayer player, string command, string[] args)
        {
            DateTime localDate = DateTime.Now;
            var bplayer = (BasePlayer)player.Object;
            aktiv = true;
            string PoliceOnDuty = lang.GetMessage("PoliceOnDuty", this);
            string OnDuty = lang.GetMessage("OnDuty", this);
            string StartedDuty = lang.GetMessage("StartedDuty", this);
            string AlreadyOnDuty = lang.GetMessage("AlreadyOnDuty", this);
            string PoliceNoPermission = lang.GetMessage("PoliceNoPermission", this);
            if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] == null && policeCounter == 0)
            {

                policeCounter++;
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelActiv(cplayer);

                }
                bool UserHasGroup = permission.UserHasGroup(bplayer.UserIDString, "dutygroup");
                if (!UserHasGroup)
                {
                    permission.AddUserGroup(bplayer.UserIDString, "dutygroup");
                }

                player.Message(OnDuty);
                policeArrayNew.Add(player.Id, player.Name);
                string policeListNew = "";
                foreach (var val in policeArrayNew)
                {
                    policeListNew = string.Join(", ", val.Value);
                }
                dataFile[player.Id] = localDate;
                server.Broadcast("<color=orange>" + bplayer.displayName + "</color>" + StartedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                dataFile.Save();
                CuiHelper.DestroyUi((BasePlayer)player.Object, "PoliceImage");
                CuiHelper.AddUi(bplayer, PoliceImage);
                aktiv = false;
            }
            if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] == null && policeCounter > 0)
            {
                player.Message(OnDuty);
                policeCounter++;
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelActiv(cplayer);

                }
                bool UserHasGroup = permission.UserHasGroup(bplayer.UserIDString, "dutygroup");
                if (!UserHasGroup)
                {
                    permission.AddUserGroup(bplayer.UserIDString, "dutygroup");
                }
                dataFile[player.Id] = localDate;
;
                policeArrayNew.Add(player.Id, player.Name);
                string policeListNew = "";
                foreach (var val in policeArrayNew)
                {
                    policeListNew = string.Join(", ", val.Value);
                }
                server.Broadcast("<color=orange>" + player.Name + "</color>" + StartedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                dataFile.Save();
                CuiHelper.DestroyUi((BasePlayer)player.Object, "PoliceImage");
                CuiHelper.AddUi(bplayer, PoliceImage);
                aktiv = false;
            }
            else if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] != null && aktiv)
            {
                player.Message(AlreadyOnDuty);
            }
            else if (!permission.UserHasPermission(player.Id, "police.use"))
            {
                player.Message(PoliceNoPermission);
            }



        }
        [Command("pde")]
        private void policeOffDuty(IPlayer player, string command, string[] args)
        {

            aktiv = true;
            var bplayer = (BasePlayer)player.Object;
            string PoliceOnDuty = lang.GetMessage("PoliceOnDuty", this);
            string OffDuty = lang.GetMessage("OffDuty", this);
            string EndedDuty = lang.GetMessage("EndedDuty", this);
            string AlreadyOffDuty = lang.GetMessage("AlreadyOffDuty", this);
            string PoliceNoPermission = lang.GetMessage("PoliceNoPermission", this);
            if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] != null && policeCounter == 1)
            {

                policeCounter--;
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelInaktiv(cplayer);

                }
                player.Message(OffDuty);
                if (config.useSalary == true)
                {
                    calculateSalary(player);
                }
                dataFile.Remove(player.Id);
                bool UserHasGroup = permission.UserHasGroup(bplayer.UserIDString, "dutygroup");
                if (UserHasGroup)
                {
                    permission.RemoveUserGroup(bplayer.UserIDString, "dutygroup");
                }
                policeArrayNew.Remove(player.Id);
                string policeListNew = "";
                foreach (var val in policeArrayNew)
                {
                    policeListNew = string.Join(", ", val.Value);
                }
                dataFile.Save();
                CuiHelper.DestroyUi((BasePlayer)player.Object, "PoliceImage");
                server.Broadcast("<color=orange>" + player.Name + "</color>" + EndedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                aktiv = false;
            }

            if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] != null && policeCounter > 1)
            {
                policeCounter--;
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelActiv(cplayer);

                }
                player.Message(OffDuty);
                if (config.useSalary == true)
                {
                    calculateSalary(player);
                }

                dataFile.Remove(player.Id);
                policeArrayNew.Remove(player.Id);
                string policeListNew = "";
                foreach (var val in policeArrayNew)
                {
                    policeListNew = string.Join(", ", val.Value);
                }
                dataFile.Save();
                server.Broadcast("<color=orange>" + player.Name + "</color>" + EndedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                CuiHelper.DestroyUi((BasePlayer)player.Object, "PoliceImage");
                aktiv = false;
            }
            else if (permission.UserHasPermission(player.Id, "police.use") && dataFile[player.Id] == null && aktiv)
            {
                player.Message(AlreadyOffDuty);
            }
            else if (!permission.UserHasPermission(player.Id, "police.use"))
            {
                player.Message(PoliceNoPermission);
            }

        }


        [Command("policeui")]
        private void policeUIOff(IPlayer player, string command, string[] args)
        {
            var bplayer = player.Object as BasePlayer;
            if (args.Length == 1)
            {
                if (args[0].Equals("off"))
                {
                    CuiHelper.DestroyUi(bplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(bplayer, "PolicePanelInactiv");
                }
                else if (args[0].Equals("on"))
                {
                    if (policeCounter == 0)
                    {
                        PolicePanelInaktiv(bplayer);
                    }
                    else
                    {
                        PolicePanelActiv(bplayer);
                    }


                }
                else
                {
                    player.Message(lang.GetMessage("ArgumentWrong", this));
                }
            }
            else
            {
                player.Message(lang.GetMessage("Argument", this));
            }
        }



        void OnPlayerDisconnected(BasePlayer player, string reason)
        {

            string EndedDuty = lang.GetMessage("EndedDuty", this);
            string PoliceOnDuty = lang.GetMessage("PoliceOnDuty", this);
            if (permission.UserHasPermission(player.UserIDString, "police.use") && dataFile[player.UserIDString] != null)
            {
                if (policeCounter == 1)
                {
                    policeCounter--;
                    foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                    {
                        CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                        CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                        PolicePanelInaktiv(cplayer);
                    }
                    if (config.useSalary == true)
                    {
                        calculateSalary(player.IPlayer);
                    }

                    dataFile.Remove(player.UserIDString);
                    policeArrayNew.Remove(player.UserIDString);
                    string policeListNew = "";
                    foreach (var val in policeArrayNew)
                    {
                        policeListNew = string.Join(", ", val.Value);
                    }
                    CuiHelper.DestroyUi(player, "PoliceImage");
                    server.Broadcast("<color=orange>" + player.displayName + "</color>" + EndedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                }
                if (policeCounter > 1 && policeCounter != 0)
                {
                    policeCounter--;
                    foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                    {
                        CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                        CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                        PolicePanelActiv(cplayer);
                    }

                    if (config.useSalary == true)
                    {
                        calculateSalary(player.IPlayer);
                    }
                    dataFile.Remove(player.UserIDString);
                    policeArrayNew.Remove(player.UserIDString);
                    string policeListNew = "";
                    foreach (var val in policeArrayNew)
                    {
                        policeListNew = string.Join(", ", val.Value);
                    }
                    CuiHelper.DestroyUi(player, "PoliceImage");
                    server.Broadcast("<color=orange>" + player.displayName + "</color>" + EndedDuty + "\n" + PoliceOnDuty + "<color=#0097ff>" + policeListNew + "</color>");
                }

            }
        }

        void OnServerShutdown()
        {
            dataFile.Clear();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            string CurrentlyOffDuty = lang.GetMessage("CurrentlyOffDuty", this);
            if (player.isSpawned && permission.UserHasPermission(player.UserIDString, "police.use"))
            {
                player.ChatMessage(CurrentlyOffDuty);
            }
            if (policeCounter == 0)
            {
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelInaktiv(cplayer);
                }
            }
            else if (policeCounter > 0)
            {
                foreach (var cplayer in BasePlayer.activePlayerList.ToList())
                {
                    CuiHelper.DestroyUi(cplayer, "PolicePanelActiv");
                    CuiHelper.DestroyUi(cplayer, "PolicePanelInactiv");
                    PolicePanelActiv(cplayer);
                }
            }
        }




        public void notify(BasePlayer bplayer, string location1, string location2)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList.ToList())
            {
                if (permission.UserHasPermission(player.UserIDString, "police.use"))
                {
                    player.ChatMessage("<color=#345ae7>" + bplayer.displayName + "</color>" + lang.GetMessage("Help", this) + "<color=#345ae7>" + location1 + location2 + "</color>");
                    RunEffect(player.transform.position, notifySound, player);
                }
            }

        }

        public bool marker(Vector3 pos, BasePlayer player)
        {

            if (!PublicRadMarker.ContainsKey(player.UserIDString))
            {
                VendingMachineMapMarker MapMarkerVendingCustom;
                MapMarkerGenericRadius MapMarkerCustom;
                MapMarkerVendingCustom = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", pos) as VendingMachineMapMarker;
                MapMarkerCustom = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos) as MapMarkerGenericRadius;
                MapMarkerVendingCustom.markerShopName = player.displayName;
                MapMarkerCustom.alpha = 1.0f;
                MapMarkerCustom.color1 = Color.blue;
                MapMarkerCustom.color2 = Color.black;
                MapMarkerCustom.radius = MarkerRadius;
                MapMarkerVendingCustom.Spawn();
                MapMarkerCustom.Spawn();
                MapMarkerCustom.SendUpdate();
                PublicRadMarker[player.UserIDString] = MapMarkerCustom;
                PublicVendMarker[player.UserIDString] = MapMarkerVendingCustom;
                return true;
            }
            else
            {
                return false;
            }


        }

        private void deleteMarkerCurrent(string id)
        {
            if (PublicVendMarker.ContainsKey(id))
            {
                PublicVendMarker[id].Kill();
                PublicVendMarker.Remove(id);
            }

            if (PublicRadMarker.ContainsKey(id))
            {
                PublicRadMarker[id].Kill();
                PublicRadMarker.Remove(id);
            }
        }


        private static void RunEffect(Vector3 position, string prefab, BasePlayer player = null)
        {
            var effect = new Effect();
            effect.Init(Effect.Type.Generic, position, Vector3.zero);
            effect.pooledString = prefab;

            if (player != null)
            {
                EffectNetwork.Send(effect, player.net.connection);
            }
            else
            {
                EffectNetwork.Send(effect);
            }
        }
        float grids, size;
        float f(float i) => Mathf.Floor(i);

        void OnServerInitialized()
        {
            grids = Mathf.Floor((float)World.Size / 146);
            size = TerrainMeta.Size.x / 1024f;

        }
        string[] GridFromPos(Vector3 pos)
        {
            Vector2 n1 = new Vector2(TerrainMeta.NormalizeX(pos.x), TerrainMeta.NormalizeZ(pos.z));
            Vector2 m = n1 * size * 7;
            double l = f(m.x) + 1;
            double n2 = f(m.y) + 1;
            n2 = (float)(f(grids) - n2);

            string s1 = string.Empty;
            float n6 = (float)(l / 26.0);
            float n7 = (float)(l % 26.0);
            n7 = n7 < 1 ? -1f : n7;
            char ch;
            if (n6 > 1.0)
            {
                ch = Convert.ToChar(64 + (int)n6);
                s1 = ch.ToString();
            }
            ch = Convert.ToChar(64 + (int)n7);
            return new string[] { s1 + ch.ToString(), n2.ToString() };
        }
        #endregion


    }
}
