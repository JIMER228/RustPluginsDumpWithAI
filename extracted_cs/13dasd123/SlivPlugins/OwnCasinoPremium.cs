
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.Text;
using System;
using CompanionServer.Handlers;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Own Casino Premium", "NooBlet", "1.4.1")]
    [Description("Make your own Casino, Premium version")]
    public class OwnCasinoPremium : RustPlugin
    {
        #region Vars       
        private DynamicConfigFile casinoFile = Interface.Oxide.DataFileSystem.GetDatafile("Casino_data");
        private static OwnCasinoPremium plugin;
        private const ulong skinIDwheel = 2893506595;
        private const ulong skinIDblackjack = 2893501916;
        private const ulong skinIDterminal1 = 2893522810;
        private const ulong skinIDterminal2 = 2893510492;
        private const ulong skinIDslots = 2893493101;
        private const ulong skinIDchips = 2893518750;
        private const ulong skinIDchips4 = 2893495658;
        private const ulong skinIDchips3 = 2893495594;
        private const ulong skinIDchips2 = 2893495521;
        private const ulong skinIDmoney = 1829167394;
        private const string wheelprefab = "assets/prefabs/misc/casino/bigwheel/big_wheel.prefab";
        private const string terminalprefab = "assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab";
        private const string chairprefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        private const string couchprefab = "assets/prefabs/deployable/sofa/sofa.deployed.prefab";
        private const string slotsprefab = "assets/prefabs/misc/casino/slotmachine/slotmachine.prefab";
        private const string chipsprefab = "assets/prefabs/deployable/card table/cardtable.static_configa.prefab";
        private const string chipsprefab3 = "assets/prefabs/deployable/card table/cardtable.static_configb.prefab";
        private const string chipsprefab4 = "assets/prefabs/deployable/card table/cardtable.static_configd.prefab";
        private const string chipsprefab2 = "assets/prefabs/deployable/card table/cardtable.static_configc.prefab";
        private const string blackjackprefab = "assets/content/vehicles/trains/caboose/blackjackmachine/blackjackmachine.static.prefab";
        private const string MLRSRocketPrefab = "assets/content/vehicles/mlrs/rocket_mlrs.prefab";


        private const string permUse = "OwnCasinoPremium.use";
        private const string scrapBoxPerm = "OwnCasinoPremium.scrapboxuse";
        private bool bigpunish = false;
        private bool removepriv = false;
        private int Wheelspintime = 45;
        private bool OwnerAllowbet = true;
        private bool usebuildinbetitem = true;
        private Dictionary<ItemContainer, ItemDefinition[]> originalAllowedItems = new Dictionary<ItemContainer, ItemDefinition[]>();
        private List<object> itemWhitelist = new List<object>
        {
            "scrap",

        };



        static List<string> effects = new List<string>
        {
        "assets/bundled/prefabs/fx/item_break.prefab",
        "assets/bundled/prefabs/fx/impacts/stab/rock/stab_rock_01.prefab"
        };

        #endregion Vars

        #region Config

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file");
        }
        private void LoadConfiguration()
        {
            CheckCfg<bool>("Allow Owner To bet on own Casino", ref OwnerAllowbet);
            CheckCfg<int>("Spin Frequency in seconds", ref Wheelspintime);
            CheckCfg<List<object>>("BigWheel Item Whitelist", ref itemWhitelist);
            CheckCfg<bool>("Use buildin custom bet item", ref usebuildinbetitem);
            CheckCfg<bool>("Punish Exploiters with MLRS", ref bigpunish);
            //CheckCfg<bool>("Remove Xploiters permissions", ref removepriv);
            SaveConfig();
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
            {
                var = (T)Config[Key];
            }
            else
            {
                Config[Key] = var;
            }

        }



        #endregion config       

        #region Hooks 
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null) { return; }          
            RunChecks(go.ToBaseEntity());            
        }

        private void RunChecks(BaseEntity baseEntity)
        {
            CheckDeployWheel(baseEntity);
            CheckDeployTermanal1(baseEntity);
            CheckDeployTermanal2(baseEntity);
            CheckDeployslots(baseEntity);
            CheckDeploypoker(baseEntity);
            CheckDeploypoker4(baseEntity);
            CheckDeploypoker3(baseEntity);
            CheckDeploypoker2(baseEntity);
            CheckDeployblackjack(baseEntity);
        }
        private void OnServerInitialized()
        {
            plugin = this;
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(scrapBoxPerm, this);
            LoadConfiguration();
            BigWheelGame.spinFrequencySeconds = Wheelspintime;
            

        }
        private void Loaded()
        {
            CheckchildClass();
            if (!usebuildinbetitem)
            {
                Unsubscribe("CanMoveItem");
            }
        }
        void Unload()
        {
            BigWheelGame.spinFrequencySeconds = 45f;
            ResetContainer();
        }

        private object CanMoveItem(Item item, PlayerInventory playerLoot, ItemContainerId targetContainerID, int targetSlot, int amount)
        {
            var basePlayer = item?.GetOwnerPlayer();

            if (basePlayer != null)
            {
                if (!basePlayer.IsNpc)
                {
                    ItemContainer targetContainer = playerLoot.FindContainer(targetContainerID);
                    BigWheelBettingTerminal bigWheelBettingTerminal;
                    try
                    {
                        bigWheelBettingTerminal = (BigWheelBettingTerminal)targetContainer?.entityOwner;
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    if (bigWheelBettingTerminal != null)
                    {

                        if (bigWheelBettingTerminal.OwnerID == 0)
                        {
                            ResetContainer(targetContainer);
                            return null;
                        }

                        if (!itemWhitelist.Contains(item.info.shortname))
                            return false;

                        if (!OwnerAllowbet)
                        {
                            if (!CanPlaceBet(bigWheelBettingTerminal.OwnerID, basePlayer)) { return false; }
                        }

                        // Insure only one type of item being used
                        if (targetContainer.itemList.Count > 0)
                        {
                            foreach (Item containerItem in targetContainer.itemList)
                            {
                                if (containerItem.info.shortname != item.info.shortname)
                                {
                                    return false;
                                }
                            }
                        }

                        if (!targetContainer.onlyAllowedItems.Contains(item.info))
                        {
                            if (!originalAllowedItems.ContainsKey(targetContainer))
                            {
                                originalAllowedItems.Add(targetContainer, targetContainer.onlyAllowedItems);
                            }
                            ItemDefinition[] allowedItems = new ItemDefinition[targetContainer.onlyAllowedItems.Length + 1];

                            // Add scrap item to allowed items list
                            for (int i = 0; i < allowedItems.Length - 1; i++)
                            {
                                allowedItems[i] = targetContainer.onlyAllowedItems[i];
                            }
                            allowedItems[allowedItems.Length - 1] = item.info;
                            targetContainer.SetOnlyAllowedItems(allowedItems);
                        }
                    }
                }
            }
            return null;
        }

        void OnBigWheelWin(BigWheelGame bigWheel, Item scrap, BigWheelBettingTerminal terminal, int multiplier)
        {
            if (bigWheel.OwnerID != 0)
            {

            }

        }
        void OnBigWheelLoss(BigWheelGame wheel, Item item)
        {
            if (wheel.OwnerID != 0)
            {
                BuildingPrivlidge tc = wheel.GetBuildingPrivilege();
               
                if (tc == null) { return; }
               
                List<DecayEntity> boxes = new List<DecayEntity>();
              
                foreach (var i in tc.GetBuilding().decayEntities)
                {
                    if (i.skinID == 2729495286)
                    {
                        boxes.Add(i);
                    }
                }
                if(boxes.Count == 0) { return; }
                if (boxes.Count > 1)
                {
                    if (bigpunish)
                    {
                        foreach (var p in BasePlayer.GetConnectionsWithin(tc.transform.position, 50))
                        {
                            var player = findPlayer(p.ownerid.ToString());
                            float currenthealth = player.health;
                            float maxhealth = player.MaxHealth();
                            player.SetMaxHealth(100000000000f);
                            player.health = 100000000000f;
                            timer.Once(20f, () =>
                            {
                                player.health = currenthealth;
                                player.SetMaxHealth(maxhealth);
                            });
                        }
                        foreach (var b in boxes)
                        {
                            dropMLRS(boxes);
                        }
                        return;
                    }
                    if (removepriv)
                    {
                        permission.RevokeUserPermission(tc.OwnerID.ToString(),permUse);
                     
                        foreach (var i in tc.GetBuilding().decayEntities)
                        {
                            if (i.skinID == 2893506595||i.skinID == 2893501916 || i.skinID == 2893522810 || i.skinID == 2893510492 || i.skinID == 2893493101 || i.skinID == 2893518750 || i.skinID == 2893495658 || i.skinID == 2893495594 || i.skinID == 2893495521)
                            {
                                i.Kill();
                            }
                        }
                        killxploidboxes(boxes);


                    }
                    else
                    {
                       
                        killxploidboxes(boxes);
                    }
                  
                   
                }
                else
                {
                    var container = boxes.FirstOrDefault()?.GetComponent<StorageContainer>();
                    var scrap = ItemManager.CreateByPartialName(item.info.shortname, item.amount, item.skin);

                    scrap.MoveToContainer(container.inventory);
                }
            }
        }

      

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            CheckHit(player, info?.HitEntity);      
        }

        bool CanPickupEntity(BasePlayer player, BaseMountable entity)
        {

            if (entity.skinID == skinIDterminal1 && player.userID == entity.OwnerID)
            {
                timer.Once(0.2f, () =>
                {
                    if (entity.IsValid() == true && entity.IsDestroyed == false)
                    {
                        entity.Kill();
                    }
                });
                GiveItems(player, "chair");
                return false;
            }
            if (entity.skinID == skinIDterminal2 && player.userID == entity.OwnerID)
            {
                timer.Once(0.2f, () =>
                {
                    if (entity.IsValid() == true && entity.IsDestroyed == false)
                    {
                        entity.Kill();
                    }
                });
                GiveItems(player, "sofa");
                return false;
            }
            return true;
        }



        #endregion Hooks

        #region Helpers

        private void dropMLRS(List<DecayEntity> boxes)
        {
            foreach (var b in boxes)
            {
                var mlrs = GameManager.server.CreateEntity(MLRSRocketPrefab, b.transform.position + Vector3.down);
                mlrs.Spawn();

            }
        }

        private static void killxploidboxes(List<DecayEntity> boxes)
        {
            foreach (var b in boxes)
            {
                b.Kill();
            }
           
        }

        private void CheckchildClass()
        {
            timer.Every(600f, () =>
            {
                foreach (var w in BigWheelGame.serverEntities)
                {
                    if(w == null) { continue; }
                    var wheel = w?.GetComponent<BigWheelGame>();
                    if (wheel == null) continue;
                    if (wheel.OwnerID != 0)
                    {
                        if (!wheel.HasComponent<WheelComponent>())
                        {
                            wheel.gameObject.AddComponent<WheelComponent>();
                            Puts("wheel checked and fixed");
                        }
                    }
                }
                foreach (var w in CardTable.serverEntities)
                {
                    if(w == null) { continue;}
                    var card = w?.GetComponent<CardTable>();
                    if (card == null) continue;
                    if (card.OwnerID != 0)
                    {
                        if (!card.HasComponent<pokerComponent>())
                        {
                            card.gameObject.AddComponent<pokerComponent>();
                            Puts("table checked and fixed");
                        }
                    }
                }
                foreach (var w in SlotMachine.serverEntities)
                {if (w == null)
                    {
                        continue;
                    }
                    var slots = w?.GetComponent<SlotMachine>();
                    if (slots == null) continue;
                    if (slots.OwnerID != 0)
                    {
                        if (!slots.HasComponent<slotsComponent>())
                        {
                            slots.gameObject.AddComponent<slotsComponent>();
                            Puts("slots checked and fixed");
                        }
                    }
                }
                foreach (var b in BlackjackMachine.serverEntities)
                {
                    if(b == null) { continue;}
                    var blackjack = b?.GetComponent<BlackjackMachine>();
                    if (blackjack == null) continue;
                    if (blackjack.OwnerID != 0)
                    {
                        if (!blackjack.HasComponent<BlackjackMachine>())
                        {
                            blackjack.gameObject.AddComponent<blackjackComponent>();
                            Puts("blackjack checked and fixed");
                        }
                    }
                }

            });
          
        }
        private void ResetContainer(ItemContainer itemContainer = null)
        {
            if (itemContainer == null)
            {
                foreach (var keyValuePair in originalAllowedItems)
                {
                    keyValuePair.Key.SetOnlyAllowedItems(keyValuePair.Value);
                }
            }
            else
            {
                ItemDefinition[] itemDefinitions;
                originalAllowedItems.TryGetValue(itemContainer, out itemDefinitions);
                if (itemDefinitions != null)
                {
                    itemContainer.SetOnlyAllowedItems(itemDefinitions);
                }
            }
        }

        private void CheckHit(BasePlayer player, BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }
            if (player.userID == entity.OwnerID)
            {
                if (IsWheel(entity.skinID))
                {

                    timer.Once(0.2f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<WheelComponent>()?.TryPickup(player);
                        }
                    });
                }

                if (Isslots(entity.skinID))
                {
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            Puts("isslot");
                            entity.GetComponent<slotsComponent>()?.TryPickup(player);
                        }
                    });
                }
                if (Ispoker(entity.skinID))
                {
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<pokerComponent>()?.TryPickup(player);
                        }
                    });
                }
                if (Ispoker2(entity.skinID))
                {
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<pokerComponent>()?.TryPickup(player);
                        }
                    });
                }
                if (Ispoker3(entity.skinID))
                {
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<pokerComponent>()?.TryPickup(player);
                        }
                    });
                }
                if (Ispoker4(entity.skinID))
                {
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<pokerComponent>()?.TryPickup(player);
                        }
                    });
                }
                if (Isblackjack(entity.skinID))
                {
                  
                    timer.Once(1f, () =>
                    {
                        if (entity.IsValid() == true)
                        {
                            entity.GetComponent<blackjackComponent>()?.TryPickup(player);
                        }
                    });
                }
            }
        }

        private BasePlayer findPlayer(string name)
        {
            BasePlayer target = BasePlayer.FindAwakeOrSleeping(name);
            return target;
        }

        private SleepingBag[] FindSleepingBags(BasePlayer basePlayer)
        {
            var bags = SleepingBag.FindForPlayer(basePlayer.userID, true);
            return bags.Where((SleepingBag b) => b.deployerUserID == basePlayer.userID).ToArray();
        }

        private bool CheckTCAuth(ulong ownerid, BasePlayer player)
        {
            var tc = player.GetBuildingPrivilege();
            foreach(var p in tc.authorizedPlayers)
            {              
                if(p.userid == player.userID)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CheckTeam(ulong ownerid,BasePlayer target)
        {
            if(target.Team != null)
            {
                foreach(var p in target.Team.members)
                {
                    if(p == ownerid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        bool CanPlaceBet(ulong ownerid , BasePlayer player)
        {
            foreach ( var b in FindSleepingBags(player))
            {
                if(b.GetBuildingPrivilege().OwnerID == ownerid)
                {
                    return false;
                }
            }
            if (CheckTeam(ownerid, player)) { return false; }

            if (CheckTCAuth(ownerid,player)) { return false; }    
            
            return true;
        }

        #endregion Helpers

        #region Wheel Stuff
        private void CheckDeployWheel(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!IsWheel(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;


            SpawnWheel(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();

            NextTick(() => { entity?.Kill(); });
        }

        private bool IsWheel(ulong skin)
        {
            return skin == skinIDwheel;
        }

        private void SpawnWheel(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x, position.y + 1, position.z + 0.2f);
            var wheel = GameManager.server.CreateEntity(wheelprefab, newpos, rotation);
            if (wheel == null)
            {
                return;
            }
            var transform = wheel.transform;
            var old = transform.eulerAngles;

            old.z -= 90;
            old.y -= 90;
            transform.eulerAngles = old;
            wheel.OwnerID = ownerID;
            wheel.gameObject.AddComponent<WheelComponent>();
            wheel.skinID = skinIDwheel;

            wheel.Spawn();
        }
        #endregion Wheel Stuff

        #region Termanal1 Stuff

        private void CheckDeployTermanal1(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!IsTermanal1(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;
            SpawnTerminal1(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }
        private bool IsTermanal1(ulong skin)
        {
            return skin == skinIDterminal1;
        }

        private void SpawnTerminal1(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x + 0.8f, position.y, position.z - 0.3f);
            var Terminal = GameManager.server.CreateEntity(terminalprefab, newpos, rotation);
            var chair = GameManager.server.CreateEntity(chairprefab, position, rotation);
            if (Terminal == null && chair == null)
            {
                return;
            }
            chair.OwnerID = ownerID;
            chair.skinID = skinIDterminal1;
            chair.Spawn();
            Terminal.SetParent(chair);
           // Terminal.OwnerID = ownerID;
            Terminal.transform.localPosition = new Vector3(0.8f, 0, 0.2f);
            Terminal.transform.localRotation = Quaternion.Euler(new Vector3(0, -90, 0));
            Terminal.SetFlag(BigWheelBettingTerminal.Flags.On, true);
            Terminal.SendNetworkUpdateImmediate();
            Terminal.Spawn();

        }

        #endregion Termanal1 stuff

        #region Termanal2 Stuff

        private void CheckDeployTermanal2(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!IsTerminal2(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;
            SpawnTerminal2(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }
        private bool IsTerminal2(ulong skin)
        {
            return skin == skinIDterminal2;
        }

        private void SpawnTerminal2(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x + 0.8f, position.y, position.z - 0.3f);
            var Terminal1 = GameManager.server.CreateEntity(terminalprefab, newpos, rotation);
            var Terminal2 = GameManager.server.CreateEntity(terminalprefab, newpos, rotation);
            var sofa = GameManager.server.CreateEntity(couchprefab, position, rotation);
            if (Terminal1 == null && sofa == null && Terminal2 == null)
            {
                return;
            }
            sofa.OwnerID = ownerID;
            sofa.skinID = skinIDterminal2;
            sofa.Spawn();
            Terminal1.SetParent(sofa);
           // Terminal1.OwnerID = ownerID;
            Terminal1.transform.localPosition = new Vector3(-1.2f, 0, 0.7f);
            Terminal1.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
            Terminal1.SetFlag(BigWheelBettingTerminal.Flags.On, true);
            Terminal1.SendNetworkUpdateImmediate();
            Terminal1.Spawn();
            Terminal2.SetParent(sofa);
           // Terminal2.OwnerID = ownerID;
            Terminal2.transform.localPosition = new Vector3(1.2f, 0, 0.7f);
            Terminal2.transform.localRotation = Quaternion.Euler(new Vector3(0, -90, 0));
            Terminal2.SetFlag(BigWheelBettingTerminal.Flags.On, true);
            Terminal2.SendNetworkUpdateImmediate();
            Terminal2.Spawn();
        }

        #endregion Termanal1 stuff

        #region slots Stuff
        private void CheckDeployslots(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!Isslots(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;
            Spawnslots(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }

        private bool Isslots(ulong skin)
        {
            return skin == skinIDslots;
        }

        private void Spawnslots(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x - 0.2f, position.y, position.z);
            var slots = GameManager.server.CreateEntity(slotsprefab, newpos, rotation);
            if (slots == null)
            {
                return;
            }

            slots.OwnerID = ownerID;
            slots.gameObject.AddComponent<slotsComponent>();
            slots.skinID = skinIDslots;
            slots.Spawn();

        }
        #endregion slots Stuff

        #region poker Stuff
        private void CheckDeploypoker(BaseEntity entity)
        {           
            if (entity == null)
            {
                return;
            }

            if (!Ispoker(entity.skinID))
            {
                return;
            }
           
            var transform = entity.transform;
            Spawnpoker(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
          
        }

        private bool Ispoker(ulong skin)
        {
            return skin == skinIDchips;
        }

        private void Spawnpoker(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x, position.y, position.z);
            var poker = GameManager.server.CreateEntity(chipsprefab, newpos, rotation);
            if (poker == null)
            {
                return;
            }

            poker.OwnerID = ownerID;
            poker.gameObject.AddComponent<pokerComponent>();
            poker.skinID = skinIDchips;
            poker.Spawn();
        }
        #endregion poker Stuff

        #region poker4 Stuff
        private void CheckDeploypoker4(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!Ispoker4(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;
            Spawnpoker4(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }

        private bool Ispoker4(ulong skin)
        {
            return skin == skinIDchips4;
        }

        private void Spawnpoker4(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x, position.y, position.z);
            var poker = GameManager.server.CreateEntity(chipsprefab4, newpos, rotation);  
            if (poker == null)
            {
                return;
            }

            poker.OwnerID = ownerID;
            poker.gameObject.AddComponent<pokerComponent>();
            poker.skinID = skinIDchips4;
            poker.Spawn();
        }
        #endregion poker4 Stuff

        #region poker3 Stuff
        private void CheckDeploypoker3(BaseEntity entity)
        {
            if (entity == null)
            {               
                return;
            }

            if (!Ispoker3(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;           
            Spawnpoker3(transform.position, transform.rotation, entity.OwnerID);           
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }

        private bool Ispoker3(ulong skin)
        {
            return skin == skinIDchips3;
        }

        private void Spawnpoker3(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x, position.y, position.z);
            var poker = GameManager.server.CreateEntity(chipsprefab3, newpos, rotation);
           

            if (poker == null)
            {
               
                return;
            }

            poker.OwnerID = ownerID;
            poker.gameObject.AddComponent<pokerComponent>();
            poker.skinID = skinIDchips3;
            poker.Spawn();
        }
        #endregion poker3 Stuff

        #region poker2 Stuff
        private void CheckDeploypoker2(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!Ispoker2(entity.skinID))
            {
                return;
            }

            var transform = entity.transform;

            Spawnpoker2(transform.position, transform.rotation, entity.OwnerID);
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }

        private bool Ispoker2(ulong skin)
        {
            return skin == skinIDchips2;
        }

        private void Spawnpoker2(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x - 0.2f, position.y, position.z);
            var poker = GameManager.server.CreateEntity(chipsprefab2, newpos, rotation);
           
            if (poker == null)
            {
                return;
            }

            poker.OwnerID = ownerID;
            poker.gameObject.AddComponent<pokerComponent>();
            poker.skinID = skinIDchips2;
            poker.Spawn();
        }
        #endregion poker2 Stuff

        #region blackjack Stuff
        private void CheckDeployblackjack(BaseEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!Isblackjack(entity.skinID))
            {
                return;
            }
            var player = findPlayer(entity.OwnerID.ToString());

            var transform = entity.transform;
            Quaternion q = new Quaternion();
            q = player.eyes.rotation;

            q.Set(0, q.y, 0, q.w);
            Spawnblackjack(transform.position, q, entity.OwnerID); 
            entity.transform.position -= new Vector3(0, 3, 0);
            entity.SendNetworkUpdate();
            NextTick(() => { entity?.Kill(); });
        }

        private bool Isblackjack(ulong skin)
        {
            return skin == skinIDblackjack;
        }

        private void Spawnblackjack(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            var newpos = new Vector3(position.x, position.y, position.z);
            var blackjack = GameManager.server.CreateEntity(blackjackprefab, newpos, rotation);
            
            if (blackjack == null)
            {
                return;
            }

            blackjack.OwnerID = ownerID;
            blackjack.gameObject.AddComponent<blackjackComponent>();
            blackjack.skinID = skinIDblackjack;
            
            blackjack.Spawn();
        }
        #endregion blackjack Stuff

        #region Commands

        [ChatCommand("casino")]
        private void casinocommand(BasePlayer player, string cmd, string[] args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string[] commands =
            {
                  "/casino <arg>",
                  "",
                  "Arguments:",
                  "giveall     - give one of each item",
                  "wheel       - give Bigwheel",
                  "chair       - give single seat Terminal",
                  "sofa        - give double seat Terminal ",
                  "poker       - give a Poker Table",
                  "slots       - give one Slot Machine",
                  "box         - give a scrap box",
            };

            if (player == null) { Puts("Command cant be send From console!!"); return; }
            if (!permission.UserHasPermission(player.UserIDString, permUse)) { player.ChatMessage(string.Format(GetLang("NoUse", player.UserIDString))); return; }
            if (args.Length < 1)
            {
                stringBuilder.AppendLine($"_____________________________");
                foreach (var s in commands)
                {
                    stringBuilder.AppendLine($"{s}");

                }

                PrintPluginMessageToChat(player, stringBuilder.ToString().TrimEnd(), "Commands");
                return;
            }
            switch (args[0].ToLower())
            {
                case "wheel":
                    GiveItems(player, "wheel");
                    break;
                case "chair":
                    GiveItems(player, "chair");
                    break;
                case "sofa":
                    GiveItems(player, "sofa");
                    break;               
                case "poker":
                    GiveItems(player, "poker");
                    break;
                case "poker6":
                    GiveItems(player, "poker");
                    break;
                case "poker4":
                    GiveItems(player, "poker4");
                    break;
                case "poker3":
                    GiveItems(player, "poker3");
                    break;
                case "poker2":
                    GiveItems(player, "poker2");
                    break;
                case "slots":
                    GiveItems(player, "slots");
                    break;
                case "box":
                    if (permission.UserHasPermission(player.UserIDString, scrapBoxPerm))
                    {
                        GiveItems(player, "box");
                    }
                    else
                    {
                        player.ChatMessage(GetLang("ScrapBoxNoPerm", player.UserIDString));
                    }
                    
                    break;
                case "giveall":
                    GiveItems(player, "giveall");
                    break;
                case "blood":
                    GiveItems(player, "blood");
                    break;
                case "blackjack":
                    GiveItems(player, "blackjack");
                    break;
               

            }
        }
      
        [ConsoleCommand("casino.give")]
        private void consolegiveCommand(ConsoleSystem.Arg args)
        {
            BasePlayer player = findPlayer(args.Args[1]);
            if (args.Args.Length < 2) { return; }
            if (player == null) { return; }
            switch (args.Args[0].ToLower())
            {
                case "wheel":
                    GiveItems(player, "wheel");
                    break;
                case "chair":
                    GiveItems(player, "chair");
                    break;
                case "sofa":
                    GiveItems(player, "sofa");
                    break;
                case "poker":
                    GiveItems(player, "poker");
                    break;
                case "poker6":
                    GiveItems(player, "poker");
                    break;
                case "poker4":
                    GiveItems(player, "poker4");
                    break;
                case "poker3":
                    GiveItems(player, "poker3");
                    break;
                case "poker2":
                    GiveItems(player, "poker2");
                    break;
                case "slots":
                    GiveItems(player, "slots");
                    break;
                case "box":
                    if (permission.UserHasPermission(player.UserIDString, scrapBoxPerm))
                    {
                        GiveItems(player, "box");
                    }
                    else
                    {
                        player.ChatMessage(GetLang("ScrapBoxNoPerm", player.UserIDString));
                    }
                    break;
                case "giveall":
                    GiveItems(player, "giveall");
                    break;
                case "blood":
                    GiveItems(player, "blood");
                    break;
                case "blackjack":
                    GiveItems(player, "blackjack");
                    break;
            }
        }
        private void PrintPluginMessageToChat(BasePlayer player, string message, string msgtype)
        {
            player.ChatMessage("<b><size=16>[<color=#ffa500ff>" + this.Name + " " + msgtype + "</color>]</size></b>\n" + message);
        }


        #endregion Commands

        #region Item Creation
        public void GiveItems(BasePlayer player, string itemname)
        {
            var items = CreateItems();

            if (items != null && player != null)
            {
                switch (itemname)
                {
                    case "wheel":
                        player.GiveItem(items[0]);
                        break;
                    case "chair":
                        player.GiveItem(items[1]);
                        break;
                    case "sofa":
                        player.GiveItem(items[2]);
                        break;
                    case "poker":
                        player.GiveItem(items[3]);
                        break;
                    case "poker6":
                        player.GiveItem(items[3]);
                        break;
                    case "poker4":
                        player.GiveItem(items[4]);
                        break;
                    case "poker3":
                        player.GiveItem(items[5]);
                        break;
                    case "poker2":
                        player.GiveItem(items[6]);
                        break;
                    case "slots":
                        player.GiveItem(items[7]);
                        break;
                    case "box":
                        player.GiveItem(items[8]);
                        break;
                    case "blackjack":
                        player.GiveItem(items[9]);
                        break;
                    case "blood":
                        player.GiveItem(items[10]);
                        break;
                    case "giveall":
                        foreach (var item in items)
                        {
                            if (Array.IndexOf(items,  item) == 10)
                                continue;                           
                            
                            if (Array.IndexOf(items, item) == 8)
                            {
                                if (permission.UserHasPermission(player.UserIDString, scrapBoxPerm))
                                {
                                    player.GiveItem(item);
                                }

                            }
                            else
                            {
                                player.GiveItem(item);
                            }
                        }
                        break;
                }
                player.ChatMessage(string.Format(GetLang("GiveItems", player.UserIDString)));

            }
        }

        private Item[] CreateItems()
        {
            var wheel = ItemManager.CreateByName("sign.wooden.large", 1);
            var terminal1 = ItemManager.CreateByName("chair", 1);
            var terminal2 = ItemManager.CreateByName("sofa", 1);
            var poker = ItemManager.CreateByName("table", 1);            
            var poker4 = ItemManager.CreateByName("table", 1);
            var poker3 = ItemManager.CreateByName("table", 1);
            var poker2 = ItemManager.CreateByName("table", 1);

            var slots = ItemManager.CreateByName("arcade.machine.chippy", 1);
            var box = ItemManager.CreateByName("box.wooden.large", 1, 2729495286);
            var blood = ItemManager.CreateByName("blood", 100, 2834920066);
            var blackjack = ItemManager.CreateByName("arcade.machine.chippy", 1);
            if (wheel != null)
            {
                wheel.name = "Big Wheel";
                wheel.skin = skinIDwheel;
            }
            if (terminal1 != null)
            {
                terminal1.name = "Single Terminal";
                terminal1.skin = skinIDterminal1;
            }
            if (terminal2 != null)
            {
                terminal2.name = "Dubble Terminal";
                terminal2.skin = skinIDterminal2;
            }
            if (poker != null)
            {
                poker.name = "Poker Table 6 Seats";               
                poker.skin = skinIDchips;
                
            }
            if (poker4 != null)
            {
                poker4.name = "Poker Table 4 Seats";
                poker4.skin = skinIDchips4;

            }
            if (poker3 != null)
            {
                poker3.name = "Poker Table 3 Seats";
                poker3.skin = skinIDchips3;

            }
            if (poker2 != null)
            {
                poker2.name = "Poker Table 2 Seats";
                poker2.skin = skinIDchips2;

            }
            if (slots != null)
            {
                slots.name = "Slot Machine";
                slots.skin = skinIDslots;
            }
            if (box != null)
            {
                box.name = "PayOut Box";
                box.skin = 2729495286;
            }
            if (blood != null)
            {
                blood.name = "Blood";
            }
            if (blackjack != null)
            {
                blackjack.name = "Blackjack Machine";
                blackjack.skin = skinIDblackjack;
            }

            Item[] items = { wheel, terminal1, terminal2, poker, poker4, poker3, poker2, slots, box,blackjack,blood};
            return items;
        }
        #endregion Item Ctreation

        #region Lang API

        private string GetLang(string key, string id) => lang.GetMessage(key, this, id);
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoUse"] = "You dont have permission to use this command",
                ["GiveItems"] = "Casino Items Given!",
                ["ScrapBoxNoPerm"] = "You dont have Permission to spawn the ScrapBox",

            }, this, "en");
        }

        #endregion Lang API

        #region Classes

        private class WheelComponent : MonoBehaviour
        {
            private BigWheelGame wheel;

            private void Awake()
            {
                wheel = GetComponent<BigWheelGame>();
                InvokeRepeating("CheckGround", 5f, 5f);


            }
            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(wheel.transform.position + new Vector3(0, -0.6f, 0), Vector3.down,
                    out rhit, 21f, LayerMask.GetMask("Terrain", "Construction"));

                var entity = rhit.GetEntity();
                if (entity != null)
                {
                    if (isGroundMissing(entity))
                    {
                        GroundMissing();
                    }
                }
                else
                {
                    GroundMissing();
                }
            }
            bool isGroundMissing(BaseEntity entity)
            {
                if (entity.ShortPrefabName.Contains("foundation")) { return false; }
                if (entity.ShortPrefabName.Contains("floor")) { return false; }

                return true;
            }


            private void GroundMissing()
            {
                foreach (var effect in effects) { Effect.server.Run(effect, wheel.transform.position); }
                wheel.Kill();
            }
            public void TryPickup(BasePlayer player)
            {
                foreach(var t in wheel.terminals)
                {                   
                    if (t.inventory.IsLocked()) { player.ChatMessage("cant pickup wheel while bet is in progress"); return; }
                }
               
                wheel.Kill();
                plugin.GiveItems(player, "wheel");
            }

            public void DoDestroy()
            {
                Destroy(this);
            }
        }

        

        private class slotsComponent : MonoBehaviour
        {
            private SlotMachine slots;
           

            private void Awake()
            {
                slots = GetComponent<SlotMachine>();

                InvokeRepeating("CheckGround", 5f, 5f);
            }
           
            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(slots.transform.position + new Vector3(0, 0.1f, 0), Vector3.down,
                    out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;
                if (distance > 0.2f) { GroundMissing(); }
            }

            private void GroundMissing()
            {
                foreach (var effect in effects) { Effect.server.Run(effect, slots.transform.position); }
                this.DoDestroy();
            }
            public void TryPickup(BasePlayer player)
            {
                slots.Kill();
                plugin.GiveItems(player, "slots");
            }


            public void DoDestroy()
            {
                var entity = slots;
                entity.DismountAllPlayers();
                try
                {
                    entity.DismountAllPlayers();
                    entity.Kill();
                }
                catch { }
            }
        }
        private class pokerComponent : MonoBehaviour
        {
            private CardTable poker;
            private void Awake()
            {
                poker = GetComponent<CardTable>();
                InvokeRepeating("CheckGround", 5f, 5f);
            }

            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(poker.transform.position + new Vector3(0, 0.1f, 0), Vector3.down,
                    out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;
                if (distance > 0.2f) { GroundMissing(); }
            }

            private void GroundMissing()
            {
                foreach (var effect in effects) { Effect.server.Run(effect, poker.transform.position); }
                this.DoDestroy();
            }
            public void TryPickup(BasePlayer player)
            {
                poker.Kill();
                if (poker.skinID == skinIDchips) { plugin.GiveItems(player, "poker"); }
                else
                if (poker.skinID == skinIDchips4) { plugin.GiveItems(player, "poker4"); }
                else
                if (poker.skinID == skinIDchips3) { plugin.GiveItems(player, "poker3"); }
                else
                if (poker.skinID == skinIDchips2) { plugin.GiveItems(player, "poker2"); }
            }

            public void DoDestroy()
            {
                var entity = poker;
                entity.DismountAllPlayers();
                try
                {
                    entity.DismountAllPlayers();
                    entity.Kill();
                }
                catch { }
            }
        }
        private class blackjackComponent : MonoBehaviour
        {
            private BlackjackMachine bb;
          
            private void Awake()
            {
                bb = GetComponent<BlackjackMachine>();
                InvokeRepeating("CheckGround", 5f, 5f);
                
            }

            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(bb.transform.position + new Vector3(0, 0.1f, 0), Vector3.down,
                    out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;
                if (distance > 0.2f) { GroundMissing(); }
            }

            private void GroundMissing()
            {
                foreach (var effect in effects) { Effect.server.Run(effect, bb.transform.position); }
                this.DoDestroy();
            }
            public void TryPickup(BasePlayer player)
            {
                bb.Kill();
                plugin.GiveItems(player, "blackjack");
            }

            public void DoDestroy()
            {
                var entity = bb;
                entity.DismountAllPlayers();
                try
                {
                    entity.DismountAllPlayers();
                    entity.Kill();
                }
                catch { }
            }
        }

        #endregion Classes

    }
}