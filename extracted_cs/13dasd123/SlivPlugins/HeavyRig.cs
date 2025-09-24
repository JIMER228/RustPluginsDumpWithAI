using Facepunch.Utility;
using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Heavy Rig Wave Event", "NooBlet", "1.2.0")]
    [Description("Spawns a Heavy Oirig Event")]
    public class HeavyRig : RustPlugin
    {
        #region Vars

        string crate = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab";
        public bool EventActive = false;
        BaseEntity largeReader = null;
        BaseEntity smallReader = null;
        private Configuration _config;
        public static string _cardName;
        public List<HackableLockedCrate> spawnedCrates = new List<HackableLockedCrate>();
        public Dictionary<Vector3,float> LargeCorrections = new Dictionary<Vector3,float>
        {
              { new Vector3(2.5f, 37f, 1f), 0f },
              { new Vector3(12f, 37f, 1f), -90f },
              { new Vector3(16f, 37f, 12f), -90f },
              { new Vector3(16f, 42f, 12f), -90f }
        };
        public Dictionary<Vector3,float> SmallCorrections = new Dictionary<Vector3, float>
        {
              { new Vector3(14f, 28f,5f), 180f },
              { new Vector3(18.8f, 27.2f,1.5f), -90f },           
        };
        public Dictionary<Vector3, float> SmallReaderCorrection = new Dictionary<Vector3, float>
        {
              { new Vector3(24f, 27.2f, -10.78f), 0f },              
        };
        public Dictionary<Vector3, float> LargeReaderCorrection = new Dictionary<Vector3, float>
        {
              { new Vector3(-14.5f, 38f-1.35f,5.85f), 180f },
        };

        #endregion Vars

        #region Hooks
        
        void OnServerInitialized(bool initial)
        {
            _cardName = _config.CardName;
            largeReader = SetReader(SpawnCratePos(LargeReaderCorrection.FirstOrDefault().Key, "Large Oil Rig").pos, SpawnCratePos(LargeReaderCorrection.FirstOrDefault().Key, "Large Oil Rig").rot);
            smallReader = SetReader(SpawnCratePos(SmallReaderCorrection.FirstOrDefault().Key, "Oil Rig").pos, SpawnCratePos(SmallReaderCorrection.FirstOrDefault().Key, "Oil Rig").rot);
        }
        void Unload()
        {
            largeReader.Kill();
            smallReader.Kill();
            foreach(var c in spawnedCrates)
            {
                if (c != null) c.Kill();
            }
        }       

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
           if(crate.OwnerID == 0304  ||crate._name == "0304") { return false; }
            if (!EventActive) { return null; } else
            {
                TerrainMeta.Path.Monuments.ForEach(monument =>
                {
                    if (monument == null) return;
                    if (Vector3.Distance(crate.transform.position, monument.transform.position) < 100)
                    {
                        if (monument.displayPhrase.english.Contains("Oil Rig"))
                        {
                            if (monument.displayPhrase.english.StartsWith("Large"))
                            {
                                if (EventActive) { StartHackcycle(spawnedCrates); }
                               // Puts("Large Hacked");
                            }
                            else
                            {
                                if (EventActive) { StartHackcycle(spawnedCrates); }
                               // Puts("Small Hacked");
                            }

                        }
                    }
                });
            }
            return null;
        }

        void HeavyOilRigWaveEventStarted()
        {
            Puts("HeavyOilRigWaveEventStarted");
        }
        void HeavyOilRigWaveEventStopped()
        {
            Puts("HeavyOilRigWaveEventStopped");
        }
        object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if(cardReader.OwnerID == 0304 || cardReader._name =="0304")
            {               
                if (card.skinID == 1988408422)
                {
                    if (EventActive) { player.ChatMessage(GetLang("EventActiveMessage",player)); return false; }
                    if (!CrateReady(player.transform.position)) { player.ChatMessage(GetLang("CrateNotReady",player)); return false; }
                    cardReader.GrantCard();
                    Puts("swiped");
                    player.GetActiveItem().OnBroken();
                    Activateevent(GetRig(player.transform.position));
                    BroadcastEvent(player, GetRig(player.transform.position));
                    Interface.CallHook("HeavyOilRigWaveEventStarted");
                    timer.Once(10f, () =>
                    {
                        cardReader.ResetIOState();
                    });
                    timer.Once(300f, () =>
                    {
                        foreach(var c in spawnedCrates)
                        {
                            if (!c.IsBeingHacked())
                            {
                                c.Kill();
                            }
                        }
                        Interface.CallHook("HeavyOilRigWaveEventStopped");
                        EventActive = false;                     
                    });
                    return true;
                }
                else
                {                    
                    cardReader.CancelAccess();
                    cardReader.FailCard();
                    return false;
                }
               
            }
            return null;
        }      

        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || !_config.EnableSpawn) return;

            var customItem = _config.Drop.Find(x => x.ShortPrefabName.Contains(container.ShortPrefabName));
            if (customItem == null || !(Random.Range(0f, 100f) <= customItem.DropChance)) return;

            timer.In(0.21f, () =>
            {
                if (container.inventory == null) return;

                var count = Random.Range(customItem.MinAmount, customItem.MaxAmount + 1);

                if (container.inventory.capacity <= container.inventory.itemList.Count)
                    container.inventory.capacity = container.inventory.itemList.Count + count;

                for (var i = 0; i < count; i++)
                {
                    var item = Item?.ToItem();
                    if (item == null) break;

                    item.MoveToContainer(container.inventory);
                }
            });
        }

        object OnEntityKill(HackableLockedCrate entity)
        {
            if (spawnedCrates.Contains(entity)) { spawnedCrates.Remove(entity); }
            return null;
        }

        #endregion Hooks

        #region Methods
        private bool CrateReady(Vector3 position)
        {
            foreach(var c in HackableLockedCrate.serverEntities)
            {
                var crate = c as HackableLockedCrate;
                if (crate == null) continue;
                if (Vector3.Distance(position, crate.transform.position) <= 100)
                {
                    if (crate.IsBeingHacked() || crate.IsFullyHacked())
                    {
                        return false;
                    }
                    else 
                    { 
                        return true; 
                    }
                }
            }
            return false;
        }

        private void BroadcastEvent(BasePlayer player, string v)
        {
            string monument = "";
            if (v == "large")
            {
                monument = "Large OilRig";
            }
            else
            {
                monument = "Small OilRig";
            }
            player.ChatMessage(GetLang("ActivateEventPlayerMessage",player));
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p == null) { continue; }
                timer.Repeat(1f, 4, () =>
                {
                    p.ShowToast(GameTip.Styles.Server_Event, $"{GetLang("ActivateEvent",p)} <color=green>{monument}</color>");
                });
            }
        }
        private void Activateevent(string rig)
        {            
            if (rig == "large")
            {
                foreach (var pos in LargeCorrections)
                {
                    var entity = GameManager.server.CreateEntity(crate, SpawnCratePos(pos.Key, "Large Oil Rig").pos, SpawnCratePos(pos.Key, "Large Oil Rig").rot);
                    var hack = entity?.GetComponent<HackableLockedCrate>();
                   // hack.OwnerID = 0304;
                    hack._name = "0304";
                    float addedRotationY = pos.Value;
                    Quaternion addedRotation = Quaternion.Euler(0f, addedRotationY, 0f);
                    hack.transform.localRotation *= addedRotation;
                    hack.Spawn();
                    spawnedCrates.Add(hack);
                }
                EventActive = true;
            }
            else if (rig == "small")
            {
                foreach (var pos in SmallCorrections)
                {
                    var entity = GameManager.server.CreateEntity(crate, SpawnCratePos(pos.Key, "Oil Rig").pos, SpawnCratePos(pos.Key, "Oil Rig").rot);
                    var hack = entity?.GetComponent<HackableLockedCrate>();
                   // hack.OwnerID = 0304;
                    hack._name = "0304";
                    float addedRotationY = pos.Value;
                    Quaternion addedRotation = Quaternion.Euler(0f, addedRotationY, 0f);
                    hack.transform.localRotation *= addedRotation;
                    hack.Spawn();
                    spawnedCrates.Add(hack);
                }
               EventActive = true;
            }
            //timer.Once(30f, () =>
            //{
            //    var list = spawnedCrates.ToList();
            //    var max = list.Count;
            //    for (int n = max; n > 0; n--)
            //    {
            //        var c = list[n - 1];
            //        if (c == null) { continue; }
            //        c.Kill();
            //        spawnedCrates.Remove(c);
            //    }
               
            //});
        }      

        public string GetRig(Vector3 pos)
        {
            var rig = "";
            TerrainMeta.Path.Monuments.ForEach(monument =>
            {
                if (monument == null) return;
                if (Vector3.Distance(pos, monument.transform.position) < 100)
                {
                    if (monument.displayPhrase.english.Contains("Oil Rig"))
                    {
                        if (monument.displayPhrase.english.StartsWith("Large"))
                        {
                            rig = "large";
                        }
                        else
                        {
                            rig = "small";
                        }
                    }
                }
            });
            return rig;
        }

        private String getOilrigName(BasePlayer player)
        {
            var rig = "";
            TerrainMeta.Path.Monuments.ForEach(monument =>
            {
                if (monument == null) return;
                if (Vector3.Distance(player.transform.position, monument.transform.position) < 100)
                {
                    if (monument.displayPhrase.english.Contains("Oil Rig"))
                    {
                        if (monument.displayPhrase.english.StartsWith("Large"))
                        {
                            rig = "Large Oil Rig";
                        }
                        else
                        {
                            rig = "Small Oil Rig";
                        }
                    }
                }
            });
            return rig;
        }
        public GetVecs SpawnCratePos(Vector3 correction,string rig)
        {
            Vector3 pos = new Vector3(0, 0, 0);
            Quaternion rot = new Quaternion(0,0,0,0);
            TerrainMeta.Path.Monuments.ForEach(monument =>
            {
                if (monument == null) return;               
                if (monument.displayPhrase.english == rig)
                {
                    var correct = correction;
                    if (correct == Vector3.zero) return;

                    var transform = monument.transform;
                    rot = transform.rotation;
                    pos = transform.position + rot * correct;                   
                }
            });
            
            return new GetVecs { pos = pos,rot = rot};
        }

        public void StartHackcycle(List<HackableLockedCrate> list)
        {
            int currentIndex = 0;

            void HackNextCrate()
            {
                if (currentIndex < list.Count)
                {
                    var crate = list[currentIndex];
                    if (crate != null) { crate.StartHacking(); }
                    currentIndex++;
                }
                else
                {                   
                    list.Clear();
                    EventActive=false;  
                    spawnedCrates.Clear();
                }
            }

            timer.Repeat(30f, list.Count, HackNextCrate);
        }

        BaseEntity SetReader(Vector3 pos , Quaternion rot)
        {
            CardReader changed = null;
            var reader = GameManager.server.CreateEntity("assets/prefabs/io/electric/switches/cardreader.prefab", pos,rot);
            if (reader == null) { Puts("reader null"); return null; }            
            reader.gameObject.SetActive(true);
            if (reader is CardReader)
                changed = reader as CardReader;
            if (changed != null)
            {
                //changed.OwnerID = 0304;
                changed._name = "0304";
                changed.UpdateHasPower(10, 1);
                changed.accessLevel = 3;
            }
            float addedRotationY = 0;
            if (GetRig(reader.transform.position) == "large")
            {
                addedRotationY = LargeReaderCorrection.FirstOrDefault().Value;
            }
            else
            {
                addedRotationY = SmallReaderCorrection.FirstOrDefault().Value;
            }
            
            Quaternion addedRotation = Quaternion.Euler(0f, addedRotationY, 0f);          
            reader.transform.localRotation *= addedRotation;

            reader.Spawn();           
            SpawnRefresh(reader);
            reader.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            reader.SendNetworkUpdateImmediate();          
            return reader;
        }
       
        void SpawnRefresh(BaseNetworkable entity1)
        {
            UnityEngine.Object.Destroy(entity1.GetComponent<Collider>());
        }
        #endregion Methods

        #region Config
                
        private class Configuration
        {           

            [JsonProperty(PropertyName = "Enable Card spawn?")]
            public bool EnableSpawn = true;

            [JsonProperty(PropertyName = "Drop Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<DropInfo> Drop = new List<DropInfo>
            {
                new DropInfo
                {
                    ShortPrefabName = "crate_elite",
                    MinAmount = 1,
                    MaxAmount = 1,
                    DropChance = 10
                },
                 new DropInfo
                {
                    ShortPrefabName = "codelockedhackablecrate",
                    MinAmount = 1,
                    MaxAmount = 1,
                    DropChance = 10
                },
            };

            [JsonProperty(PropertyName = "Card Name")]
            public string CardName = "Wave Card";
        }       

        public class DropInfo
        {
            [JsonProperty(PropertyName = "Object Short prefab name")]
            public string ShortPrefabName;

            [JsonProperty(PropertyName = "Minimum item to drop")]
            public int MinAmount;

            [JsonProperty(PropertyName = "Maximum item to drop")]
            public int MaxAmount;

            [JsonProperty(PropertyName = "Item Drop Chance")]
            public float DropChance;
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

        #region Lang

        private string GetLang(string key, BasePlayer player)
        {
            return lang.GetMessage(key, this)
                .Replace("{playername}", player.displayName);
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CrateNotReady"] = "Main Crate not Active or Hacked",
                ["ActivateEvent"] = "A Player Activated the Wave Event at :",
                ["ActivateEventPlayerMessage"] = "You have 5min's to hack main crate , or event will fail",
                ["EventActiveMessage"] = "Event already running",               

            }, this, "en");
        }

        #endregion Lang       

        #region Commands

        [ChatCommand("testcard")]
        private void testcardCommand(BasePlayer target, string command, string[] args)
        {
            if (!target.IsAdmin) { return; }
            var item = Item?.ToItem();
            if (item == null) return;

            target.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }

        [ConsoleCommand("givecard")]
        private void giveplayercardCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.GetString(0);
            var item = Item?.ToItem();
            if (item == null) return;
            BasePlayer.FindAwakeOrSleeping(player).GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }

        #endregion Commands

        #region Classes
        public class GetVecs
        {
            public Vector3 pos { get; set; }
            public Quaternion rot { get; set; }
        }

        public class ItemConfig
        {
            [JsonProperty(PropertyName = "DisplayName")]
            public string DisplayName;

            [JsonProperty(PropertyName = "Discription")]
            public string Discription;

            [JsonProperty(PropertyName = "ShortName")]
            public string ShortName;

            [JsonProperty(PropertyName = "SkinID")]
            public ulong SkinID;

            public Item ToItem()
            {
                var newItem = ItemManager.CreateByName(ShortName, 1, SkinID);
                if (newItem == null)
                {
                    Debug.LogError($"Error creating item with shortName '{ShortName}'!");
                    return null;
                }

                if (!string.IsNullOrEmpty(DisplayName)) newItem.name = DisplayName;
               
                return newItem;
            }

            public bool IsSame(Item item)
            {
                return item != null && item.info.shortname == ShortName && item.skin == SkinID;
            }
        }

        public new ItemConfig Item = new ItemConfig
        {
            DisplayName = _cardName,
            Discription = "Access Card For OiRig Wave Event",
            ShortName = "keycard_red",
            SkinID = 1988408422,

        };

        #endregion Classes
    }
}
