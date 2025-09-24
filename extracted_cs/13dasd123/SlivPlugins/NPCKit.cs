// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{   [Info("NPCKit", "steenamaroo", "1.0.1")]
    public class NPCKit : CovalencePlugin
    {
        #region Lang
        protected override void LoadDefaultMessages()
        {

            //English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["NoNPC"] = "Could not find an NPC infront of you.",
                ["RemovedNPC"] = "NPC was removed.",
                ["GiveKit"] = "You recieved kit ",
                ["Gave"] = " ",
                ["Wait"] = "You have to wait ",
                ["ToRecive"] = " to be able to recive the kit again.",
                ["KitPerm"] = "You do not have the permission for this kit",
                ["Confirm"] = "Confirm",
                ["Abort"] = "Abort",
                ["NPCName"] = "NPC Name",
                ["Create"] = "Create NPC",
                ["CreateKit"] = "Create Kit",
                ["Delete"] = "Delete NPC",
                ["Edit"] = "Edit NPC",


            }, this);

            //German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "Du hast keine Berechtigung für diesen Befehl.",
                ["NoNPC"] = "Es konnte kein NPC gefunden werden.",
                ["RemovedNPC"] = "NPC wurde entfernt.",
                ["GiveKit"] = "Kit ",
                ["Gave"] = " erhalten.",
                ["Wait"] = "Du musst noch ",
                ["ToRecive"] = " warten, bis du das kit wieder holen kannst",
                ["KitPerm"] = "Du hast keine Berechtigung für dieses Kit",
                ["Confirm"] = "Bestätigen",
                ["Abort"] = "Abbrechen",
                ["NPCName"] = "NPC Name",
                ["Create"] = "NPC Erstellen",
                ["CreateKit"] = "Kit Erstellen",
                ["Delete"] = "NPC Löschen",
                ["Edit"] = "NPC Editieren",

            }, this, "de");
        }
        #endregion

        #region Data
      
        DynamicConfigFile dataFile;
        class DynamicConfigFile
        {
            public Dictionary<int, NPCData> NPCData = new Dictionary<int, NPCData>();
            public Dictionary<string, List<MItem>> MItem = new Dictionary<string, List<MItem>>();
            public Dictionary<string, Dictionary<string, DateTime>> playerKitRecived = new Dictionary<string, Dictionary<string, DateTime>>();
            public Dictionary<string, string> kitPerm = new Dictionary<string, string>();

        }

        public class MItem
        {
            public string NPCName;
            public int ammoAmount;
            public string ammoName;
            public int amount;
            public float condition;
            public List<MItem> mods;
            public string shortName;
            public ulong skinid;
            public bool weapon;
        }

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
        public class NPCData
        {
            public Vector3 position = default(Vector3);
            public Vector3 rotation = default(Vector3);
            public string npcName;
            public Dictionary<string, double> kits = new Dictionary<string, double>();
            public Dictionary<int, string> clothing = new Dictionary<int, string>();
            public Dictionary<int, ulong> skins = new Dictionary<int, ulong>();
        }
        
       
        class Configuration
        {
            [JsonProperty("Use_MapMarker_For_NPC")]
            public bool  MapMarker{ get; set; }

            public static Configuration CreateConfig()
            {

                return new Configuration
                {
                    MapMarker = true,
                };
            }
        }

        public class Store
        {
            public int dataId = -1;    
            public VendingMachineMapMarker mapMarker = null;
        };
        #endregion

        #region Init

        Dictionary<string, string> input = new Dictionary<string, string>();
        Dictionary<string, string> input2 = new Dictionary<string, string>();
        Dictionary<string, string> inputperm = new Dictionary<string, string>();
        public Dictionary<BasePlayer, Store> Npcs = new Dictionary<BasePlayer, Store>();
        private readonly Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        public GestureCollection gestureList;
        List<string> playerInUi = new List<string>();
        public const string perm = "npckit.createnpc";
        public const string permdefault = "npckit.default";
        public const string permvip = "npckit.vip";

        void Init()
        {
            if (!permission.PermissionExists(perm, this))
            {
                permission.RegisterPermission(perm, this);
            }
            if (!permission.PermissionExists(permdefault, this))
            {
                permission.RegisterPermission(permdefault, this);
            }
            if (!permission.PermissionExists(permvip, this))
            {
                permission.RegisterPermission(permvip, this);
            }
            LoadConfig();
            LoadDefaultMessages();
            foreach(var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "KitUI");
                CuiHelper.DestroyUi(player, "EditNPCMain");
                CuiHelper.DestroyUi(player, "MainPanel");
            }

         
        }

        private void OnServerInitialized()
        {

            foreach (KeyValuePair<int, NPCData> npcs in dataFile.NPCData)
                CreateNPC(npcs.Key, npcs.Value.position, npcs.Value.rotation, npcs.Value.npcName);

          
        }

        void OnServerSave() => SaveData();
        void Loaded()
        {
            dataFile = Interface.Oxide.DataFileSystem.ReadObject<DynamicConfigFile>("NPCKit");
            Interface.Oxide.DataFileSystem.WriteObject("NPCKit", dataFile);
  

           
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("NPCKit", dataFile);
        }

        void Unload()
        {
            SaveData();

            if (dataFile.NPCData.Count == 0) return;
            foreach (BasePlayer npc in Npcs.Keys.ToList())
                RemoveNPC(npc);

            Npcs.Clear();
        }
        #endregion

        #region Functions


        private void CreateKit(BasePlayer player, string name, BasePlayer npc, double time = 0.0)
        {
            var kit = new List<MItem>();
            foreach (var check in player.inventory.AllItems()) kit.Add(ConvertToMItem(check));
            if (dataFile.MItem.ContainsKey(name))
            {
                player.ChatMessage("Kit with this name already crated");
                return;
            }
           
            dataFile.MItem.Add(name, kit);
            dataFile.NPCData[(Int32)npc.userID].kits.Add(input[player.UserIDString], time);
            dataFile.kitPerm.Add(name, inputperm[player.UserIDString]);
            player.ChatMessage($"Kit <color=yellow>{name}</color> successfully created");
            SaveData();
        }

        private void GiveKit(BasePlayer player, List<MItem> items, string kitName)
        {
   
            DateTime time = DateTime.Now;
            if (!dataFile.playerKitRecived.ContainsKey(player.UserIDString))
            {
                dataFile.playerKitRecived.Add(player.UserIDString, new Dictionary<string, DateTime>());
                SaveData();
            }
            else
            {
                dataFile.playerKitRecived[player.UserIDString].Remove(kitName);
                dataFile.playerKitRecived[player.UserIDString].Add(kitName, time);
                SaveData();
            }

            foreach (var check in items) player.GiveItem(CreateItem(check));

        }

        private Item CreateItem(MItem item)
        {
            var a = ItemManager.CreateByName(item.shortName, item.amount, item.skinid);
            a.condition = item.condition;
            if (!item.weapon) return a;
            foreach (var check in item.mods)
            {
                var b = ItemManager.CreateByName(check.shortName, check.amount, check.skinid);
                b.condition = check.condition;
                b.MoveToContainer(a.contents);
            }

            var proj = a.GetHeldEntity() as BaseProjectile;
            proj.primaryMagazine.contents = item.ammoAmount;
            proj.primaryMagazine.ammoType = ItemManager.FindItemDefinition(item.ammoName);
            proj.SendNetworkUpdateImmediate();

            return a;
        }

        private MItem ConvertToMItem(Item xItem)
        {
            var item = new MItem
            {
                skinid = xItem.skin,
                amount = xItem.amount,
                condition = xItem.condition,
                mods = new List<MItem>()
            };
            var info = xItem.info;
            item.shortName = info.shortname;
            var weapon = xItem.GetHeldEntity()?.GetComponent<BaseProjectile>();
            item.weapon = weapon != null;
            if (!item.weapon) return item;
            if (xItem.contents != null)
                foreach (var check in xItem.contents.itemList)
                    item.mods.Add(ConvertToMItem(check));
            item.ammoName = weapon.primaryMagazine.ammoType.shortname;
            item.ammoAmount = weapon.primaryMagazine.contents;
            return item;
        }


        private void DeleteNPC(BasePlayer player)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, perm)) { player.ChatMessage(lang.GetMessage("NoPermission", this)); return; }

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                BasePlayer npc = hit.GetEntity().ToPlayer();
                if (npc == null || Npcs[npc] == null) { player.ChatMessage(lang.GetMessage("NoNPC", this)); return; }
                foreach (var kit in dataFile.NPCData[(Int32)npc.userID].kits)
                {
                    dataFile.MItem.Remove(kit.Key);
                    dataFile.NPCData[(Int32)npc.userID].kits.Remove(kit.Key);
                }
                dataFile.NPCData.Remove(Npcs[npc].dataId);
                RemoveNPC(npc);
                player.ChatMessage(lang.GetMessage("RemovedNPC", this));


                
                    SaveData();
                return;
            }

            player.ChatMessage(lang.GetMessage("NoNPC", this));
        }

       

        private void FreezePlayer(IPlayer player)
        {


            GenericPosition pos = player.Position();
            timers[player.Id] = timer.Every(0.01f, () =>
            {
                if (!player.IsConnected)
                {
                    timers[player.Id].Destroy();
                    return;
                }


                player.Teleport(pos.X, pos.Y, pos.Z);

            });
        }

        private void UnfreezePlayer(IPlayer player)
        {

            if (timers.ContainsKey(player.Id))
            {
                timers[player.Id].Destroy();
            }
        }

        private void NPCCreate(BasePlayer player, string npcname)
        {
            foreach(var data in dataFile.NPCData)
            {
                if (data.Value.npcName.Equals(npcname))
                {
                    player.ChatMessage("NPC with the same name already exists.");
                    return;
                }
            }
            Dictionary<int, string> clothings = new Dictionary<int, string>();
            Dictionary<int, ulong> skins = new Dictionary<int, ulong>();
            foreach (var item in player.inventory.containerWear.itemList)
            {
                clothings.Add(item.info.GetInstanceID(), item.info.shortname);
                skins.Add(item.info.GetInstanceID(), item.skin);

            }

            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, perm)) { player.ChatMessage(lang.GetMessage("NoPermission", this)); return; }

            Vector3 rotation = player.transform.position + player.eyes.HeadForward() * 2;
            int key = UnityEngine.Random.Range(1, 99999);
            dataFile.NPCData.Add(key, new NPCData() { position = player.ServerPosition, rotation = rotation, clothing = clothings, skins = skins, npcName = npcname });
            SaveData();

            CreateNPC(key, player.ServerPosition, rotation, npcname);
        }

        private void LookAt(BasePlayer npc, Vector3 position)
        {
            SetViewAngle(npc, Quaternion.LookRotation(position - npc.eyes.transform.position));
            npc.eyes.position.Set(position.x, position.y, position.z);
            npc.eyes.position.Set(position.x, position.y, position.z);
        }
        private void SetViewAngle(BasePlayer clerk, Quaternion view)
        {
            if (view.eulerAngles == default(Vector3)) return;
            clerk.viewAngles = view.eulerAngles;
        }

        private void CreateNPC(int id, Vector3 position, Vector3 rotation, string npcname)
        {
            BasePlayer NPC = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", position, new Quaternion(0, 0, 0, 1)).ToPlayer();
            NPC.displayName = npcname;
            ulong playerid = (ulong)id;
            NPC.userID = playerid;
            NPC.UserIDString = playerid.ToString();
            NPC.enableSaving = false;
            NPC.Spawn();
            LookAt(NPC, rotation);
            NPC.SendNetworkUpdateImmediate();
            if (!Npcs.ContainsKey(NPC)) Npcs.Add(NPC, new Store());

            NPCData data;
            dataFile.NPCData.TryGetValue(id, out data);
            List<string> cloth = new List<string>();
            List<ulong> skins = new List<ulong>();
           foreach(var clothing in data.clothing)
            {
                cloth.Add(clothing.Value);
                
            }
            foreach (var skin in data.skins)
            {
                skins.Add(skin.Value);
            }
            for(int i = 0; i < cloth.Count; i++)
            {
                Puts(cloth[i]);
                ItemDefinition def = ItemManager.FindItemDefinition(cloth[i]);
                NPC.inventory.containerWear.AddItem(def, 1, skins[i]);
            }


            if (Npcs[NPC] != null)
            {
                Npcs[NPC].dataId = id;

                if (config.MapMarker)
                {
                    VendingMachineMapMarker ShopMapMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
                    ShopMapMarker.markerShopName = "Test NPC";
                    ShopMapMarker.Spawn();
                    Npcs[NPC].mapMarker = ShopMapMarker;
                }

                
            }
        }

        private void RemoveNPC(BasePlayer npc)
        {
            if (Npcs[npc] != null)
            {
                if (Npcs[npc].mapMarker != null) Npcs[npc].mapMarker.KillMessage();
      
            }

            Npcs.Remove(npc);
            npc.KillMessage();
        }

        #endregion

        #region Commands

        [Command("createnpc")][Permission(perm)]
        private void CreateNPCCommand(IPlayer iPlayer, string command, string[] args)
        {
            
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;
            MainPanel(player);
            FreezePlayer(iPlayer);
            playerInUi.Add(player.UserIDString);
        }
        
        [Command("createnpc.ui")]
        private void NPCUICreate(IPlayer iPlayer, string command, string[] args)
        {

            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            switch (args[0])
            {
                case "input":
                    if (input.ContainsKey(player.UserIDString))
                    {
                        input.Remove(player.UserIDString);
                    }
                    try
                    {
                        input.Add(player.UserIDString, args[1]);
                    }
                    catch
                    {

                    }
                 
                    break;
                case "create":
                    NPCCreate(player, input[player.UserIDString]);
                    CuiHelper.DestroyUi(player, "MainPanel");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    input.Remove(player.UserIDString);
                    break;
            }


        }

        [Command("closenpc.ui")]
        private void NPCUIClose(IPlayer iPlayer, string command, string[] args)
        {

            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            switch (args[0])
            {
                case "close":
                    CuiHelper.DestroyUi(player, "MainPanel");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    break;
                case "abort":
                    CuiHelper.DestroyUi(player, "MainPanel");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    break;
            }


        }

        [Command("kit.ui")]
        private void KitUI(IPlayer iPlayer, string command, string[] args)
        {
            DateTime timeNow = DateTime.Now;
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;
            switch (args[0])
            {
                case "kit":
                    
                    if(!permission.UserHasPermission(player.UserIDString, "npckit."+dataFile.kitPerm[args[1]]))
                    {
                        player.ChatMessage(lang.GetMessage("KitPerm", this));
                        return;
                    }
                    if (dataFile.playerKitRecived.ContainsKey(player.UserIDString))
                    {
                        if (dataFile.playerKitRecived[player.UserIDString].ContainsKey(args[1]))
                        {
                            DateTime time;
                            dataFile.playerKitRecived[player.UserIDString].TryGetValue(args[1], out time);
                            double seconds = (timeNow - time).TotalSeconds;
                            double secondsFromKit;
                            dataFile.NPCData[int.Parse(args[2])].kits.TryGetValue(args[1], out secondsFromKit);
                            if (seconds >= secondsFromKit)
                            {
                                GiveKit(player, dataFile.MItem[args[1]], args[1]);
                                player.ChatMessage(lang.GetMessage("GiveKit", this) + "<color=orange>" + args[1] + "</color=orange>" + lang.GetMessage("Gave", this));
                                return;
                            }
                            else
                            {
                                var differenz = secondsFromKit - seconds;
                                TimeSpan t = TimeSpan.FromSeconds(differenz);
                                string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                                                t.Hours,
                                                t.Minutes,
                                                t.Seconds);
                                player.ChatMessage(lang.GetMessage("Wait", this) + "<color=orange>" + answer + "</color>" + lang.GetMessage("ToRecive", this));
                                return;
                            }
                        }
                        else
                        {
                            GiveKit(player, dataFile.MItem[args[1]], args[1]);
                            player.ChatMessage(lang.GetMessage("GiveKit", this) + "<color=orange>" + args[1] + "</color=orange>" + lang.GetMessage("Gave", this));
                            return;
                        }
                        
                    }

                  GiveKit(player, dataFile.MItem[args[1]], args[1]);
                  player.ChatMessage(lang.GetMessage("GiveKit", this) + "<color=orange>" + args[1]+ "</color=orange>"  + lang.GetMessage("Gave", this));
                  return;
            }


        }


        [Command("editnpc.ui")]
        private void NPCCommand(IPlayer iPlayer, string command, string[] args)
        {
           
            BasePlayer player = iPlayer.Object as BasePlayer;         
            if (player == null) return;

            switch (args[0])
            {
                case "deleteNPC":
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
                    {
                        BasePlayer npc = hit.GetEntity().ToPlayer();
                        if (npc == null || Npcs[npc] == null) { return; }


                        foreach (var kit in dataFile.NPCData[(Int32)npc.userID].kits)
                        {
                           
                            dataFile.MItem.Remove(kit.Key);
                            dataFile.kitPerm.Remove(kit.Key);
                            foreach (KeyValuePair<string,Dictionary<string,DateTime>> kitsRecived in dataFile.playerKitRecived)
                            {
                                if (dataFile.playerKitRecived[kitsRecived.Key].ContainsKey(kit.Key))
                                {
                                    dataFile.playerKitRecived[kitsRecived.Key].Remove(kit.Key);
                                    
                                }
                         
                            }
                        }
                        
                        dataFile.NPCData.Remove(Npcs[npc].dataId);                 
                        RemoveNPC(npc);
                        player.ChatMessage(lang.GetMessage("RemovedNPC", this));
                        SaveData();
                    }
                    CuiHelper.DestroyUi(player, "EditNPCMain");
                    CuiHelper.DestroyUi(player, "KitUI");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    break;
                case "close":
                    CuiHelper.DestroyUi(player, "KitUI");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    RaycastHit hit3;
                    Physics.Raycast(player.eyes.HeadRay(), out hit3, 10f);
                    BasePlayer npc3 = hit3.GetEntity().ToPlayer();
                    npc3.SignalBroadcast(BaseEntity.Signal.Gesture, "wave", null);
                    break;
                case "edit":
                    CuiHelper.DestroyUi(player, "KitUI");
                    EditNPCMain(player);
                    break;
                case "closeedit":
                    CuiHelper.DestroyUi(player, "EditNPCMain");
                    playerInUi.Remove(player.UserIDString);
                    UnfreezePlayer(iPlayer);
                    break;
                case "createkit":
                    RaycastHit hit2;
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit2, 10f))
                    {
                        BasePlayer npc = hit2.GetEntity().ToPlayer();
                        if (npc == null || Npcs[npc] == null) { return; }
                        
                        try
                        {
                            CreateKit(player, input[player.UserIDString], npc, double.Parse(input2[player.UserIDString]));
                        }
                        catch
                        {
                            

                        }


                    }
                    
                    CuiHelper.DestroyUi(player, "EditNPCMain");
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
                    {
                        BasePlayer npc = hit.GetEntity().ToPlayer();
                        if (npc == null || Npcs[npc] == null) { return; }
                        KitUI(player, npc.userID);
                    }
                    break;
                case "inputKitName":
                    if (input.ContainsKey(player.UserIDString))
                    {
                        input.Remove(player.UserIDString);
                    }
                    try
                    {
                        input.Add(player.UserIDString, args[1]);
                        
                    }
                    catch
                    {

                    }
                    break;
                case "inputCooldown":
                    if (input2.ContainsKey(player.UserIDString))
                    {
                        input2.Remove(player.UserIDString);
                    }
                    try
                    {
                        input2.Add(player.UserIDString, args[1]);

                    }
                    catch
                    {

                    }
                    break;
                    case "abort":
                    if (input.ContainsKey(player.UserIDString))
                    {
                        input.Remove(player.UserIDString);
                    }
                    if (input2.ContainsKey(player.UserIDString))
                    {
                        input2.Remove(player.UserIDString);
                    }
                    CuiHelper.DestroyUi(player, "EditNPCMain");
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
                    {
                        BasePlayer npc = hit.GetEntity().ToPlayer();
                        if (npc == null || Npcs[npc] == null) { return; }
                        KitUI(player, npc.userID);
                    }
                    break;
                case "perm":
                    if (inputperm.ContainsKey(player.UserIDString))
                    {
                        inputperm.Remove(player.UserIDString);
                    }
                    try
                    {
                        inputperm.Add(player.UserIDString, args[1]);

                    }
                    catch
                    {

                    }
                    break;
                case "deleteKit":
                   
                    dataFile.MItem.Remove(args[1]);
                    dataFile.kitPerm.Remove(args[1]);
                    dataFile.NPCData[int.Parse(args[2])].kits.Remove(args[1]);
                    foreach (KeyValuePair<string, Dictionary<string, DateTime>> kitsRecived in dataFile.playerKitRecived)
                    {
                        if (dataFile.playerKitRecived[kitsRecived.Key].ContainsKey(args[1]))
                        {
                            dataFile.playerKitRecived[kitsRecived.Key].Remove(args[1]);

                        }

                    }
                    SaveData();
                    CuiHelper.DestroyUi(player, "KitUI");
                    KitUI(player, ulong.Parse(args[3]), int.Parse(args[4]));
                    break;



            }


        }

        [Command("removenpc")]
        private void RemoveNPC(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, perm)) { player.ChatMessage(lang.GetMessage("NoPermission", this)); return; }

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                BasePlayer npc = hit.GetEntity().ToPlayer();
                if (npc == null || Npcs[npc] == null) { player.ChatMessage(lang.GetMessage("NoNPC", this)); return; }

                dataFile.NPCData.Remove(Npcs[npc].dataId);
                RemoveNPC(npc);
                player.ChatMessage(lang.GetMessage("RemovedNPC", this));
                SaveData();
                return;
            }

            player.ChatMessage(lang.GetMessage("NoNPC", this));
        }



        #endregion

        #region Hooks

        object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {

            if (playerInUi.Contains(player.UserIDString))
            {
                return false;
            }
            return null;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            var iPlayer = player.IPlayer;
            if (input.WasJustPressed(BUTTON.USE))
            {
                try
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 3f))
                    {
                        BasePlayer npc = hit.GetEntity().ToPlayer();
                        if (npc == null || Npcs[npc] == null) { return; }

                        KitUI(player, npc.userID);
                        FreezePlayer(iPlayer);
                        playerInUi.Add(player.UserIDString);
                        npc.SignalBroadcast(BaseEntity.Signal.Gesture, "wave", null);
                        return;
                        
                    }   
                }
                catch
                {

                }
                
                

                
            }
        }

        object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (Npcs.ContainsKey(player))
            {
                info.damageTypes.ScaleAll(0);    
            }
            return null;
        }

        object OnNpcTarget(BaseAnimalNPC npc, BasePlayer player)
        {
      
            foreach(var data in dataFile.NPCData)
            {
                if (player.userID.Equals((ulong)data.Key))
                {
                 
                    return true;
                }
                else
                {
                  
                   return null;
                }
            }
            return null;
        }

        #endregion


        [Command("ToggleNextPage.KitUI")]
        private void NextPageKitUi(IPlayer iPlayer, string command, string[] args)
        {
            var bplayer = (BasePlayer)iPlayer.Object;
            if (bplayer == null || args == null) return;
            int page = Convert.ToInt16(args[1]);
            ulong id = ulong.Parse(args[0]);
            CuiHelper.DestroyUi(bplayer, "KitUI");
            KitUI(bplayer, id, page);
        }

      

        #region UI



        private void EditNPCMain(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.85", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", "EditNPCMain");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1529412 0.1333333 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-171.471 -210.746", OffsetMax = "196.643 210.746" }
            }, "EditNPCMain", "EditNPC");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-184.059 170.934", OffsetMax = "184.061 210.745" }
            }, "EditNPC", "Header");

            container.Add(new CuiElement
            {
                Name = "Image_3580",
                Parent = "Header",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/content/ui/UI.Icon.Rust.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176.5 -14", OffsetMax = "-148.5 14" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Ueberschrift",
                Parent = "Header",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("Edit", this), Font = "permanentmarker.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.646 -12.591", OffsetMax = "42.646 12.59" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.8962264 0 0 1", Command = "editnpc.ui closeedit" },
                Text = { Text = "CLOSE", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "114.14 -13.339", OffsetMax = "177.26 13.339" }
            }, "Header", "CloseButton");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.196 62.697", OffsetMax = "111.21 92.545" }
            }, "EditNPC", "InputName");

            container.Add(new CuiElement
            {
                Name = "InputKitName",
                Parent = "InputName",
                Components = {
                    new CuiInputFieldComponent { Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, CharsLimit = 0, IsPassword = false, Command = "editnpc.ui inputKitName"  },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.206 -9.556", OffsetMax = "111.204 9.557" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.195 -29.848", OffsetMax = "111.211 0" }
            }, "EditNPC", "InputCooldown");

            container.Add(new CuiElement
            {
                Name = "InputField_2272",
                Parent = "InputCooldown",
                Components = {
                    new CuiInputFieldComponent { Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, CharsLimit = 0, IsPassword = false, Command = "editnpc.ui inputCooldown"},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.206 -9.556", OffsetMax = "111.204 9.557" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "NameLabel",
                Parent = "EditNPC",
                Components = {
                    new CuiTextComponent { Text = "Kit Name", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-58.665 92.545", OffsetMax = "58.678 118.455" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cooldownlabel",
                Parent = "EditNPC",
                Components = {
                    new CuiTextComponent { Text = "Cooldown in sec", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-58.662 0", OffsetMax = "58.678 25.25" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 1 0 1", Command = "editnpc.ui createkit" },
                Text = { Text = lang.GetMessage("CreateKit", this), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.196 -136.493", OffsetMax = "-28.25 -98.507" }
            }, "EditNPC", "Bestätigen");

            container.Add(new CuiButton
            {
                Button = { Color = "1 0 0 1", Command = "editnpc.ui abort" },
                Text = { Text = lang.GetMessage("Abort", this), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "28.265 -136.493", OffsetMax = "111.211 -98.507" }
            }, "EditNPC", "Abbrechen");

            container.Add(new CuiButton
            {
                Button = { Color = "1 0 0 1", Command = "editnpc.ui deleteNPC" },
                Text = { Text = lang.GetMessage("Delete", this), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.473 -185.496", OffsetMax = "41.473 -147.504" }
            }, "EditNPC", "deleteNPCButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.764151 0.764151 0.764151 1", Command = "editnpc.ui perm default" },
                Text = { Text = "Default Perm", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.195 -81.693", OffsetMax = "-28.25 -43.707" }
            }, "EditNPC", "DefaultPermButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.2 0.2 1 1", Command = "editnpc.ui perm vip" },
                Text = { Text = "VIP Perm", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "28.266 -81.693", OffsetMax = "111.212 -43.707" }
            }, "EditNPC", "VIPPermButton");

            CuiHelper.DestroyUi(player, "EditNPCMain");
            CuiHelper.AddUi(player, container);
        }













        private void KitUI(BasePlayer player, ulong npcid = 0, int page = 1)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.85", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", "KitUI");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1529412 0.1333333 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151.271 -175.107", OffsetMax = "151.271 175.107" }
            }, "KitUI", "MainPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151.27 135.298", OffsetMax = "151.271 175.108" }
            }, "MainPanel", "Header");

            container.Add(new CuiElement
            {
                Name = "Image_3580",
                Parent = "Header",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/content/ui/UI.Icon.Rust.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-142.7 -14", OffsetMax = "-114.7 14" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Label_4319",
                Parent = "Header",
                Components = {
                    new CuiTextComponent { Text = "KITS", Font = "permanentmarker.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.937 -14.007", OffsetMax = "25.936 11.174" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.8962264 0 0 1", Command = "editnpc.ui close" },
                Text = { Text = "CLOSE", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "79.84 -13.339", OffsetMax = "142.96 13.339" }
            }, "Header", "CloseButton");
            int pos1 = 4 - (page * 4), quantity = 0;
            float top = 112.081f;
            float bottom = 75.519f;
            bool hasPerm = false;
            string Color = "";
            if (permission.UserHasPermission(player.UserIDString, perm))
            {
                hasPerm = true;
            }
            foreach (var kit in dataFile.NPCData[(Int32)npcid].kits)
            {
                pos1++;
                quantity++;
                if(pos1 > 0 && pos1 < 5)
                {
                    if(dataFile.kitPerm[kit.Key] == "default")
                    {
                        Color = "0.8392157 0.8392157 0.8392157 1";
                    }else if(dataFile.kitPerm[kit.Key] == "vip")
                    {
                        Color = "0.2 0.2 1 1";
                    }
                    container.Add(new CuiButton
                    {
                        Button = { Color = Color, Command = $"kit.ui kit {kit.Key} {npcid}" },
                        Text = { Text = kit.Key, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-80.046 {bottom}", OffsetMax = $"80.046 {top}" }
                    }, "MainPanel", "KitButton");
                    if (hasPerm)
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Color = "1 0 0 1", Command = $"editnpc.ui deleteKit {kit.Key} {npcid} {npcid} {page}" },
                            Text = { Text = "X", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"87.071 {bottom}", OffsetMax = $"111.929 {top}" }
                        }, "MainPanel", "DeleteKitButton");
                    }
                        
                    

                    top = top - 50.362f;
                    bottom = bottom - 50.762f;
                }
               
            }
            
            if (permission.UserHasPermission(player.UserIDString, perm))
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0 1 0 1", Command = "editnpc.ui edit" },
                    Text = { Text = "Edit", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -164.578", OffsetMax = "50 -136.822" }
                }, "MainPanel", "EditButton");
            }
            if (quantity > (page * 4))
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.8392157 0.8392157 0.8392157 1", Command = $"ToggleNextPage.KitUI {npcid} {page +1}" },
                    Text = { Text = "->", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "18.771 -125.207", OffsetMax = "61.629 -106.457" }
                }, "MainPanel", "NextButton");
            }
           
            if(page > 1)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.8392157 0.8392157 0.8392157 1", Command = $"ToggleNextPage.KitUI {npcid} {page -1}" },
                    Text = { Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-60.453 -125.207", OffsetMax = "-16.746 -106.457" }
                }, "MainPanel", "PrevButton");
            }
            



            CuiHelper.DestroyUi(player, "KitUI");
            CuiHelper.AddUi(player, container);
        }



        private void MainPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.85", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", "MainPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1529412 0.1333333 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-183.725 -210.746", OffsetMax = "164.425 210.746" }
            }, "MainPanel", "Formular");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-174.076 170.934", OffsetMax = "174.074 210.745" }
            }, "Formular", "Header");

            container.Add(new CuiElement
            {
                Name = "Image_3580",
                Parent = "Header",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/content/ui/UI.Icon.Rust.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-166 -14", OffsetMax = "-138 14" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Ueberschrift",
                Parent = "Header",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("Create", this), Font = "permanentmarker.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67.157 -12.591", OffsetMax = "67.157 12.59" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.8962264 0 0 1", Command = "closenpc.ui close" },
                Text = { Text = "CLOSE", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "102.44 -14", OffsetMax = "165.56 12.678" }
            }, "Header", "CloseButton");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1294118 0.1254902 0.1098039 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.202 -14.924", OffsetMax = "111.204 14.924" }
            }, "Formular", "InputName");

            container.Add(new CuiElement
            {
                Name = "InputField_2272",
                Parent = "InputName",
                Components = {
                    new CuiInputFieldComponent { Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, CharsLimit = 0, IsPassword = false, Command = "createnpc.ui input "},

                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.206 -9.556", OffsetMax = "111.204 9.557" }
                }
});

            container.Add(new CuiElement
            {
                Name = "NameLabel",
                Parent = "Formular",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("NPCName", this), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-58.67 14.924", OffsetMax = "58.672 40.834" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 1 0 1", Command = "createnpc.ui create" },
                Text = { Text = lang.GetMessage("Confirm", this), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111.203 -145.693", OffsetMax = "-28.257 -107.707" }
            }, "Formular", "Bestätigen");

            container.Add(new CuiButton
            {
                Button = { Color = "1 0 0 1", Command = "closenpc.ui abort" },
                Text = { Text = lang.GetMessage("Abort", this), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "28.257 -145.693", OffsetMax = "111.203 -107.707" }
            }, "Formular", "Abbrechen");

            CuiHelper.DestroyUi(player, "MainPanel");
            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}
