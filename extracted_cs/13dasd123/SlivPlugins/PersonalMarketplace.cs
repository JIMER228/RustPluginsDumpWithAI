using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using UnityEngine;

/* ChangeLog:
- Updated: For May 4th Rust update changes
*/

namespace Oxide.Plugins
{
    [Info("Personal Marketplace", "ZEODE", "1.2.3")]
    [Description("Deploy, craft, pickup and use personal drone marketplaces.")]
    public class PersonalMarketplace: CovalencePlugin
    {
        #region Instance refs

        [PluginReference] Plugin Friends, Clans, WaterBases;

        #endregion

        #region Consts

        private static PersonalMarketplace Instance;

        private const ulong itemSkinID = 2859284352;
        private const string placeableItem = "mailbox";

        private const string itemName = "Marketplace";
        private const string vendingName = "Market Vending Machine";

        private const string marketPrefab = "assets/prefabs/misc/marketplace/marketplace.prefab";
        private const string marketTerminal = "assets/prefabs/misc/marketplace/marketterminal.prefab";
        private const string vendingPrefab = "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab";
        private const string errorSound = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        private const string successSound = "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab";

        private const string breakSound = "assets/bundled/prefabs/fx/item_break.prefab";

        private const string permAdmin = "personalmarketplace.admin";
        private const string permDeploy = "personalmarketplace.deploy";
        private const string permPickup = "personalmarketplace.pickup";
        private const string permCraft = "personalmarketplace.craft";

        private const float lookDistance = 5f;
        private const float ceilingDistance = 100f;

        #endregion Consts

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permDeploy, this);
            permission.RegisterPermission(permPickup, this);
            permission.RegisterPermission(permCraft, this);

            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
                
                if (storedData == null)
                {
                    Puts("Data is null. Creating blank data file...");
                    storedData = new StoredData();
                    SaveData();
                }
            }
            catch (Exception ex)
            {
                if (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonReaderException)
                {
                    Puts("Data file invalid. Creating blank data file...");
                    storedData = new StoredData();
                    SaveData();
                    return;
                }
                throw;
            }
        }

        private void OnServerInitialized(bool initial)
        {
            Instance = this;

            bool doSave = false;
            foreach (var key in storedData.MarketData.Keys.ToList())
            {
                var market = BaseNetworkable.serverEntities.Find(new NetworkableId(key)) as Marketplace;
                if (market == null)
                {
                    Puts($"INFO: Missing/Destroyed Marketplace with ID {key}, data entry removed.");
                    storedData.MarketData.Remove(key);
                    doSave = true;
                    continue;
                }

                if (config.deploy.addVending)
                {
                    var vending = storedData.MarketData[key].Vending;
                    if (vending == null)
                        continue;
                    
                    int count = 1;
                    for (int i = 0; i < vending.Count; i++)
                    {
                        var machine = BaseNetworkable.serverEntities.Find(new NetworkableId(vending[i].NetID)) as VendingMachine;
                        if (machine != null)
                        {
                            if (!storedData.MarketData[key].Vending[i].Name.Contains(vendingName))
                            {
                                storedData.MarketData[key].Vending[i].Name = $"{vendingName} {count}";
                                doSave = true;
                            }

                            machine._name = storedData.MarketData[key].Vending[i].Name;
                            count++;
                        }
                    }
                }
            }
            if (doSave) SaveData();

            NextTick(()=> SetupMarketplaces());
        }

        private void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType(typeof(MarketplaceComponent)).ToList())
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }

            Instance = null;
            SaveData();
        }

        private void OnNewSave()
        {
            PrintWarning("Server wipe detected, clearing all player data.");
            storedData = new StoredData();
            SaveData();
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null || plan.GetOwnerPlayer() == null)
            {
                return;
            }

            var player = plan?.GetOwnerPlayer();
            var entity = go?.ToBaseEntity();
            if (player == null || entity == null)
            {
                return;
            }

            if (IsMarketplace(entity.skinID))
            {
                float condition = config.pickup.initialCondition;
                var item = plan.GetItem();
                if (item != null)
                {
                    condition = item.maxCondition;
                }
                if (!permission.UserHasPermission(player.UserIDString, permDeploy))
                {
                    KillGive(player, entity, condition, "Permission");
                    return;
                }
                if (config.deploy.privDeploy && !IsAuthorized(player, entity))
                {
                    KillGive(player, entity, condition, "BuildPriv");
                    return;
                }
                if (!config.deploy.deployMultiple)
                {
                    foreach (var key in storedData.MarketData.Keys)
                    {
                        if (storedData.MarketData[key].OwnerID == player.userID)
                        {
                            KillGive(player, entity, condition, "NoMultiples");
                            return;
                        }
                    }
                }
                if (config.deploy.onFoundation && !OnFoundation(entity))
                {
                    KillGive(player, entity, condition, "OnFoundation");
                    return;
                }
                if (IsNearWorldCollider(entity))
                {
                    KillGive(player, entity, condition, "RoomToDeploy");
                    return;
                }
                if (InBuilding(entity))
                {
                    KillGive(player, entity, condition, "InsideBuilding");
                    return;
                }

                var marketplace = GameManager.server.CreateEntity(marketPrefab, entity.transform.position, entity.transform.rotation) as BaseEntity;
                if (marketplace == null) return;
                marketplace.transform.forward = player.transform.position - marketplace.transform.position;
                marketplace.transform.rotation = Quaternion.Euler(entity.transform.rotation.eulerAngles.x, marketplace.transform.rotation.eulerAngles.y, entity.transform.rotation.eulerAngles.z);
                marketplace.skinID = itemSkinID;
                marketplace.OwnerID = player.userID;
                marketplace.creatorEntity = player;
                marketplace.Spawn();

                marketplace.gameObject.AddComponent<MarketplaceComponent>();

                NextTick(() =>
                {
                    entity.Kill();
                });

                foreach (var child in marketplace.children)
                {
                    child.skinID = itemSkinID;
                    child.OwnerID = player.userID;
                    child.creatorEntity = player;
                }

                if (config.deploy.addVending)
                {
                    AddVendingMachines(player, marketplace, condition);
                    Message(player, "HitToRotate");
                }
                else
                {
                    AddPlayerData(player.userID, player.displayName, marketplace.net.ID.Value, condition, new List<Vending>());
                }
            }
        }

        private object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin == itemSkinID && targetItem.skin == itemSkinID)
            {
                return false;
            }
            return null;
        }

        private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            return CanStackItem(item.item, targetItem.item);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (!IsMarketplace(item.skin))
            {
                return null;
            }
            BasePlayer player = item.GetOwnerPlayer();
            var entity = container.entityOwner;
            if (entity is RepairBench)
            {
                if (config.options.showTips)
                    player.ShowToast(GameTip.Styles.Blue_Long, lang.GetMessage("NoRepair", this));
                
                return ItemContainer.CanAcceptResult.CannotAccept;
            }
            else if (entity is Recycler)
            {
                if (config.options.showTips)
                    player.ShowToast(GameTip.Styles.Blue_Long, lang.GetMessage("NoRecycle", this));
                
                return ItemContainer.CanAcceptResult.CannotAccept;
            }

            return null;
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            BaseEntity entity = info?.HitEntity as BaseEntity;
            if (entity == null)
                return;

            if (!IsMarketplace(entity.skinID))
            {
                if (entity._name == null)
                    return;
                
                if (!entity._name.Contains(vendingName))
                    return;
            }

            if (entity.name == vendingPrefab && config.deploy.addVending)
            {
                if (config.deploy.vendingAuth && !IsAuthorized(player, entity))
                {
                    PlaySound(errorSound, player);
                    Message(player, "BuildPriv");
                    return;
                }
                else if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                {
                    if (!IsFriend(player.userID, entity.OwnerID))
                    {
                        PlaySound(errorSound, player);
                        Message(player, "OwnerTeamRotate");
                        return;
                    }
                }
                else if (entity.OwnerID != player.userID)
                {
                    PlaySound(errorSound, player);
                    Message(player, "OwnerRotate");
                    return;
                }

                entity.transform.rotation = Quaternion.LookRotation(-entity.transform.forward, entity.transform.up);
                entity.SendNetworkUpdateImmediate();
                return;
            }
            else if (entity.name == marketTerminal)
            {
                var marketplace = entity?.GetParentEntity();
                if (marketplace == null)
                {
                    return;
                }

                if (!config.pickup.canPickup)
                {
                    PlaySound(errorSound, player);
                    Message(player, "NoPickup");
                    return;
                }
                else if (!permission.UserHasPermission(player.UserIDString, permPickup))
                {
                    PlaySound(errorSound, player);
                    Message(player, "Permission");
                    return;
                }
                else if (config.pickup.privPickup && !IsAuthorized(player, marketplace))
                {
                    PlaySound(errorSound, player);
                    Message(player, "BuildPriv");
                    return;
                }
                else if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                {
                    if (!IsFriend(player.userID, marketplace.OwnerID))
                    {
                        PlaySound(errorSound, player);
                        Message(player, "OwnerTeam");
                        return;
                    }
                }

                if (config.deploy.addVending && !IsVendingEmpty(player, marketplace))
                {
                    PlaySound(errorSound, player);
                    Message(player, "VendingNotEmpty");
                    return;
                }

                PickupMarketplace(player, marketplace);
            }
        }

        private object OnEntityTakeDamage(VendingMachine entity, HitInfo info)
        {
            if (entity?._name == null)
                return null;
            
            if (config.deploy.addVending && config.deploy.blockVendingDamage)
            {
                if (entity._name.Contains(vendingName))
                    return true;
            }
            
            return null;
        }

        #endregion

        #region Main

        private void SetupMarketplaces()
        {
            foreach (var marketplace in UnityEngine.Object.FindObjectsOfType<Marketplace>())
            {
                if (IsMarketplace(marketplace.skinID))
                {
                    if (!marketplace.GetComponent<MarketplaceComponent>())
                        marketplace.gameObject.AddComponent<MarketplaceComponent>();

                    // Check data file for missing marketplace data and replace it if needed
                    if (!storedData.MarketData.ContainsKey(marketplace.net.ID.Value))
                    {
                        IPlayer iplayer = players.FindPlayerById(marketplace.OwnerID.ToString());
                        if (iplayer != null)
                        {
                            List<Vending> vending = new List<Vending>();
                            if (config.deploy.addVending)
                            {
                                var layerMask = LayerMask.GetMask("Deployed");
                                var center = marketplace.transform.position + new Vector3(0, 1f, 0);
                                Vector3 halfExtents = new Vector3(4f, 4.7f, 4.7f);
                                Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask, QueryTriggerInteraction.UseGlobal);
                                if (hitColliders.Length != 0)
                                {
                                    int count = 1;
                                    for (int i = 0; i < hitColliders.Length; i++)
                                    {
                                        var hitEntity = hitColliders[i].GetComponentInParent<BaseEntity>();
                                        if (hitEntity == null) continue;
                                        
                                        if (hitEntity.name.Contains("vendingmachine"))
                                        {
                                            if (!hitEntity._name.Contains(vendingName))
                                                hitEntity._name = $"{vendingName} {count}";
                                            
                                            vending.Add(new Vending { Name = hitEntity._name, NetID = hitEntity.net.ID.Value });
                                            if (count == 4) break;
                                            count++;
                                            continue;
                                        }
                                    }
                                }
                                if (vending == null) vending = new List<Vending>();
                                AddPlayerData(Convert.ToUInt64(iplayer.Id), iplayer.Name, marketplace.net.ID.Value, 100, vending);
                            }
                            else
                            {
                                AddPlayerData(Convert.ToUInt64(iplayer.Id), iplayer.Name, marketplace.net.ID.Value, 100, new List<Vending>());
                            }
                        }
                    }
                }
            }
        }

        private void GiveMarketplace(BasePlayer player, string type, float condition = -1)
        {
            var mpItem = CreatePlaceableItem();
            if (mpItem != null && player != null)
            {
                if (condition == -1)
                {
                    mpItem.maxCondition = config.pickup.initialCondition;
                }

                mpItem.maxCondition = condition;
                if (type == "pickup")
                {
                    if (condition <= 0)
                    {
                        mpItem.maxCondition = condition;
                        mpItem.condition = 0;
                        Message(player.IPlayer, "ItemBroken");
                        PlaySound(breakSound, player);
                    }
                    Message(player.IPlayer, "Pickup");
                    player.GiveItem(mpItem);
                    return;
                }
                else if (type == "refund")
                {
                    player.GiveItem(mpItem);
                    return;
                }
                else if (type == "receive")
                {
                    player.GiveItem(mpItem);
                    Message(player.IPlayer, "Receive");
                    return;
                }
                else if (type == "craft")
                {
                    player.GiveItem(mpItem);
                    Message(player.IPlayer, "Crafted");
                    return;
                }
            }
        }

        private void PickupMarketplace(BasePlayer player, BaseEntity market)
        {
            if (player == null || market == null)
            {
                return;
            }

            ulong marketId = market.net.ID.Value;
            float condition = 0;
            List<Vending> vending = new List<Vending>();
            if (storedData.MarketData.ContainsKey(marketId))
            {
                condition = storedData.MarketData[marketId].Condition;
                vending = storedData.MarketData[marketId].Vending;
            }

            if (vending == null)
                return;
            
            if (config.deploy.addVending)
            {
                foreach (var vend in vending)
                {
                    var machine = BaseNetworkable.serverEntities.Find(new NetworkableId(vend.NetID));
                    if (machine != null)
                    {
                        NextTick(() => {
                            if (machine != null || !machine.IsDestroyed)
                                machine.Kill();
                        });
                    }
                }
            }

            NextTick(() => {
                if (!market.IsDestroyed)
                    market.Kill();
            });

            if (config.pickup.loseCondition)
                condition = condition - config.pickup.pickupPenalty;
            
            RemovePlayerData(marketId);
            GiveMarketplace(player, "pickup", condition);
        }

        private void AddVendingMachines(BasePlayer player, BaseEntity marketplace, float condition)
        {
            var position = marketplace.transform.position;
            List<Vending> vending = new List<Vending>();

            var vend1 = GameManager.server.CreateEntity(vendingPrefab, position) as VendingMachine;
            if (vend1 == null) return;
            vend1.OwnerID = player.userID;
            vend1._name = vendingName + " 1";
            vend1.creatorEntity = player;
            vend1.dropsLoot = config.deploy.dropVendingLoot;
            vend1.transform.localPosition = new Vector3(4.76f, 0.64f, 0.73f);
            vend1.transform.localRotation = new Quaternion(0.0f, 1.0f, 0.0f, 1.0f);
            DestroyGroundWatch(vend1);
            vend1.SetFlag(BaseEntity.Flags.Reserved1, true, true, true);
            vend1.SetParent(marketplace, false, false);
            vend1.Spawn();
            vend1.SetParent(null, true, true);
            vending.Add(new Vending { Name = vend1._name, NetID = vend1.net.ID.Value });

            var vend2 = GameManager.server.CreateEntity(vendingPrefab, position) as VendingMachine;
            if (vend2 == null) return;
            vend2.OwnerID = player.userID;
            vend2._name = vendingName + " 2";
            vend2.creatorEntity = player;
            vend2.dropsLoot = config.deploy.dropVendingLoot;
            vend2.transform.localPosition = new Vector3(4.76f, 0.64f, -0.73f);
            vend2.transform.localRotation = new Quaternion(0.0f, 1.0f, 0.0f, 1.0f);
            DestroyGroundWatch(vend2);
            vend2.SetFlag(BaseEntity.Flags.Reserved1, true, true, true);
            vend2.SetParent(marketplace, false, false);
            vend2.Spawn();
            vend2.SetParent(null, true, true);
            vending.Add(new Vending { Name = vend2._name, NetID = vend2.net.ID.Value });

            var vend3 = GameManager.server.CreateEntity(vendingPrefab, position) as VendingMachine;
            if (vend3 == null) return;
            vend3.enableSaving = true;
            vend3.OwnerID = player.userID;
            vend3._name = vendingName + " 3";
            vend3.creatorEntity = player;
            vend3.dropsLoot = config.deploy.dropVendingLoot;
            vend3.transform.localPosition = new Vector3(-4.76f, 0.64f, 0.73f);
            vend3.transform.localRotation = new Quaternion(0.0f, -1.0f, 0.0f, 1.0f);
            DestroyGroundWatch(vend3);
            vend3.SetFlag(BaseEntity.Flags.Reserved1, true, true, true);
            vend3.SetParent(marketplace, false, false);
            vend3.Spawn();
            vend3.SetParent(null, true, true);
            vending.Add(new Vending { Name = vend3._name, NetID = vend3.net.ID.Value });

            var vend4 = GameManager.server.CreateEntity(vendingPrefab, position) as VendingMachine;
            if (vend4 == null) return;
            vend4.enableSaving = true;
            vend4.OwnerID = player.userID;
            vend4._name = vendingName + " 4";
            vend4.creatorEntity = player;
            vend4.dropsLoot = config.deploy.dropVendingLoot;
            vend4.transform.localPosition = new Vector3(-4.76f, 0.64f, -0.73f);
            vend4.transform.localRotation = new Quaternion(0.0f, -1.0f, 0.0f, 1.0f);
            DestroyGroundWatch(vend4);
            vend4.SetFlag(BaseEntity.Flags.Reserved1, true, true, true);
            vend4.SetParent(marketplace, false, false); // Parent to marketplace temporarily (stop parent entity errors)
            vend4.Spawn();
            vend4.SetParent(null, true, true); // Unparent from marketplace but keep world position (stop parent entity errors)
            vending.Add(new Vending { Name = vend4._name, NetID = vend4.net.ID.Value });

            AddPlayerData(player.userID, player.displayName, marketplace.net.ID.Value, condition, vending);
        }

        #endregion

        #region Helpers

        private void DestroyGroundWatch(BaseEntity entity)
        {
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
        }

        // Method for other plugins related to storage boxes or Marketplaces to check whether a entity is a
        // Personal Marketplace item or not and handle accordingly, to avoid conflicts (such as Chest Stacks).
        object MarketplaceCheck(ulong skinId) => (skinId == itemSkinID) ? true : (object)null;

        private bool IsVendingEmpty(BasePlayer player, BaseEntity marketplace)
        {
            foreach (var key in storedData.MarketData.Keys)
            {
                if (key == marketplace.net.ID.Value)
                {
                    var vending = storedData.MarketData[key].Vending;
                    if (vending == null)
                        return true;
                    
                    foreach (var vend in vending)
                    {
                        var machine = BaseNetworkable.serverEntities.Find(new NetworkableId(vend.NetID)) as VendingMachine;
                        if (machine != null)
                        {
                            if (!machine.IsInventoryEmpty())
                                return false;
                        }
                    }
                    break;
                }
            }
            return true;
        }

        private bool OnFoundation(BaseEntity entity)
        {
            RaycastHit hit;
            var heightOffset = new Vector3(0, 0.5f, 0);
            var mpPosition = entity.transform.position;
            if (Physics.Raycast(mpPosition + heightOffset, Vector3.down, out hit, 0.6f, LayerMask.GetMask("Terrain", "Construction")) && hit.GetEntity().IsValid())
            {
                if (hit.GetEntity().name.Contains("foundation"))
                    return true;
            }
            return false;
        }

        private bool InBuilding(BaseEntity entity)
        {
            var layerMask = LayerMask.GetMask("Construction");
            var center = entity.transform.position + new Vector3(0, ((ceilingDistance / 2) + 0.1f), 0);
            Vector3 halfExtents = new Vector3(14, ceilingDistance, 8) / 2;
            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask, QueryTriggerInteraction.UseGlobal);
            if (hitColliders.Length != 0)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    var hitEntity = hitColliders[i].GetComponentInParent<BaseEntity>();
                    if (hitEntity == null || hitEntity == entity)
                        continue;

                    return true;
                }
            }
            return false;
        }

        private bool IsNearWorldCollider(BaseEntity entity)
        {
            var layerMask = LayerMask.GetMask("Deployed", "Construction", "Terrain", "Tree");
            var center = entity.transform.position + new Vector3(0, 4.1f, 0);
            Vector3 halfExtents = new Vector3(14, 8, 8) / 2;
            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask, QueryTriggerInteraction.UseGlobal);
            if (hitColliders.Length != 0)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    var hitEntity = hitColliders[i].GetComponentInParent<BaseEntity>();
                    if (hitEntity == null || hitEntity == entity)
                        continue;
                    
                    return true;
                }
            }
            return false;
        }

        private Item CreatePlaceableItem()
        {
            var mpItem = ItemManager.CreateByName(placeableItem, 1, itemSkinID);
            if (mpItem != null)
                mpItem.name = itemName;
                
            return mpItem;
        }

        public void KillGive(BasePlayer player, BaseEntity entity, float condition, string messageKey)
        {
            if (entity.IsValid() == true && !entity.IsDestroyed)
            {
                NextTick(() => {
                    entity.Kill();
                });
            }
            GiveMarketplace(player, "refund", condition);
            Message(player, messageKey);
        }

        private void DropDestroyed(Vector3 position, float condition)
        {
            var mpItem = CreatePlaceableItem();
            mpItem.maxCondition = condition;

            if (config.destroy.dropCondition)
                mpItem.maxCondition = condition - config.pickup.pickupPenalty;
            
            mpItem?.Drop(position, Vector3.down);
        }

        private void PlaySound(string sound, BasePlayer player)
        {
            if (player == null)
                return;
            
            if (config.options.enableSounds)
            {
                if (!player.IPlayer.IsServer)
                {
                    Effect.server.Run(sound, player.transform.position);
                    return;
                }
            }
        }

        private bool IsMarketplace(ulong skinId)
        {
            if (skinId == null || skinId == 0)
                return false;
            
            if (skinId == itemSkinID)
                return true;
            
            return false;
        }

        private bool IsAuthorized(BasePlayer player, BaseEntity block)
        {
            if (block.GetBuildingPrivilege()?.IsAuthed(player) == true)
                return true;

            return false;
        }

        private bool IsFriend(ulong playerId, ulong targetId)
        {
            if (playerId == null || targetId == null)
                return false;
            
            if (playerId == targetId)
                return true;
            
            if (Clans)
            {
                var result = Clans?.Call("IsMemberOrAlly", playerId, targetId);
                if (result != null && Convert.ToBoolean(result))
                    return true;
            }
            if (Friends)
            {
                var result = Friends?.Call("AreFriends", playerId, targetId);
                if (result != null && Convert.ToBoolean(result))
                    return true;
            }
            RelationshipManager.PlayerTeam team;
            RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out team);

            if (team == null)
                return false;
            
            if (team.members.Contains(targetId))
                return true;

            return false;
        }

        private object RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            var hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            var distance = lookDistance;
            object target = false;
            foreach (var hit in hits)
            {
                var ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }
            return target;
        }

        private IPlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in covalence.Players.Connected)
            {
                if (activePlayer.Id == nameOrIdOrIp)
                    return activePlayer;
                
                if (activePlayer.Name.Contains(nameOrIdOrIp))
                    return activePlayer;
                
                if (activePlayer.Name.ToLower().Contains(nameOrIdOrIp.ToLower()))
                    return activePlayer;
                
                if (activePlayer.Address == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        }
        #endregion

        #region Commands
        [Command("marketplace.give")]
        private void ChatGiveCmd(IPlayer player, string command, string[] args)
        {
            var self = player.Object as BasePlayer;
            if (self == null && !player.IsServer) return;
            if (!player.HasPermission(permAdmin))
            {
                Message(player, "Permission");
                PlaySound(errorSound, self);
                return;
            }
            else if (args.Length < 1 && player.IsServer)
            {
                Message(player, "ConsoleUsage");
                return;
            }
            else if (args.Length < 1 && player.IsConnected)
            {
                GiveMarketplace(self, "receive", config.pickup.initialCondition);
                PlaySound(successSound, self);
                return;
            }
            var t = FindPlayer(args[0]);
            var target = t.Object as BasePlayer;
            if (target == null)
            {
                Message(player, "PlayerNotFound", args[0]);
                PlaySound(errorSound, self);
                return;
            }
            GiveMarketplace(target, "receive", config.pickup.initialCondition);
            PlaySound(successSound, self);
            Message(player, "PlayerGiven", target);
        }

        [Command("marketplace.craft")]
        private void CraftCmd(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
            {
                Message(player, "InGameOnly");
                return;
            }
            var self = player.Object as BasePlayer;
            if (self == null) return;
            if (!config.craft.craftTrue)
            {
                Message(player, "NoCraft");
                PlaySound(errorSound, self);
                return;
            }
            else if (config.craft.craftTrue && !player.HasPermission(permCraft))
            {
                Message(player, "Permission");
                PlaySound(errorSound, self);
                return;
            }

            var required = new Dictionary<string, int>();
            foreach (var component in config.craft.cost)
            {
            	ItemDefinition compDefinition = ItemManager.FindItemDefinition(component.Key);
                if (self.inventory.GetAmount(compDefinition.itemid) < component.Value)
                {
                    if (!required.ContainsKey(component.Key))
                    {
                        required.Add(component.Key, 0);
                    }
                    required[component.Key] += component.Value;
                }
            }
            if (required.Count == 0)
            {
                foreach (var item in config.craft.cost)
                {
                	ItemDefinition itemDefinition = ItemManager.FindItemDefinition(item.Key);
                    self.inventory.Take(null, itemDefinition.itemid, item.Value);
                }
                GiveMarketplace(self, "craft", config.pickup.initialCondition);
                PlaySound(successSound, self);
                return;
            }
            else
            {
                string list = String.Empty;
                foreach (var item in required)
                {
                    list = list + ($" + {item.Key} x {item.Value}\n");
                }
                Message(player, "CraftCost", list);
                PlaySound(errorSound, self);
            }
        }

        [Command("marketplace.pickup")]
        private void PickupCmd(IPlayer player, string command, string[] args)
        {
            var self = player.Object as BasePlayer;
            if (self == null && !player.IsServer) return;
            if (player.IsServer)
            {
                Message(player, "InGameOnly");
                return;
            }
            else if (!config.pickup.canPickup)
            {
                Message(player, "NoPickup");
                PlaySound(errorSound, self);
                return;
            }
            else if (!player.HasPermission(permPickup))
            {
                Message(player, "Permission");
                PlaySound(errorSound, self);
                return;
            }
            else if (args.Length > 1)
            {
                Message(player, "PickupUse");
                return;
            }
            var entity = RaycastAll<BaseEntity>(self.eyes.HeadRay()) as BaseEntity;
            if (entity == null)
            {
                Message(player, "NullEnt");
                PlaySound(errorSound, self);
                return;
            }
            else if (entity.name == marketTerminal)
            {
                entity = entity.GetParentEntity();
            }
            else if (!IsMarketplace(entity.skinID))
            {
                Message(player, "NotMarket");
                PlaySound(errorSound, self);
                return;
            }
            else if (config.pickup.privPickup && !IsAuthorized(self, entity))
            {
                Message(player, "BuildPriv");
                PlaySound(errorSound, self);
                return;
            }
            else if (config.options.useFriends || config.options.useClans || config.options.useTeams)
            {
                if (!IsFriend(self.userID, entity.OwnerID))
                {
                    Message(player, "OwnerTeam");
                    PlaySound(errorSound, self);
                    return;
                }
            }
            if (config.deploy.addVending && !IsVendingEmpty(self, entity))
            {
                PlaySound(errorSound, self);
                Message(player, "VendingNotEmpty");
                return;
            }
            PickupMarketplace(self, entity);
        }

        [Command("marketplace.clear")]
        void ClearData(IPlayer player, string command, string[] args)
        {
            var self = player.Object as BasePlayer;
            if (self == null && !player.IsServer) return;
            if (!player.HasPermission(permAdmin))
            {
                Message(player, "Permission");
                PlaySound(errorSound, self);
                return;
            }
            else if (args?.Length > 1 && player.IsConnected)
            {
                Message(player, "ClearUsage");
                PlaySound(errorSound, self);
                return;
            }
            else if (args?.Length < 1)
            {
                storedData.MarketData.Clear();
                SaveData();
                Message(player, "DataCleared");
                PlaySound(successSound, self);
                return;
            }
            else if (args?.Length == 1)
            {
                var ownerId = args[0];
                bool success = false;
                var keys = storedData.MarketData.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (storedData.MarketData[key].OwnerID.ToString() == ownerId)
                    {
                        storedData.MarketData.Remove(key);
                        success = true;
                    }
                }
                if (success)
                {
                    SaveData();
                    Message(player, "UserCleared", ownerId);
                    PlaySound(successSound, self);
                    return;
                }
                Message(player, "NoPlayerData", ownerId);
                PlaySound(errorSound, self);
            }
        }
        #endregion

        #region Mono

        private class MarketplaceComponent : MonoBehaviour
        {
            public BaseEntity marketplace;
            public BasePlayer player;
            public ulong marketId;
            public Vector3 position;

            void Awake()
            {
                marketplace = gameObject.GetComponent<Marketplace>();
                marketplace.SendNetworkUpdateImmediate();
                marketplace.UpdateNetworkGroup();

                player = marketplace.creatorEntity as BasePlayer;
                marketId = marketplace.net.ID.Value;
                position = marketplace.transform.position;

                if (marketplace == null)
                    DestroyImmediate(this);

                if (config.destroy.floorCheck)
                    InvokeRepeating(nameof(GroundCheck), 3f, 3f);
            }

            void GroundCheck()
            {
                RaycastHit hit;
                var mpPosition = position + new Vector3(0, 0.5f, 0);
                Physics.Raycast(mpPosition, Vector3.down, out hit, 5f, LayerMask.GetMask("Terrain", "Construction", "Default"));
                if (hit.distance > 0.6f)
                    GroundMissing();
            }

            void GroundMissing()
            {
                if (!storedData.MarketData.ContainsKey(marketplace.net.ID.Value))
                    return;
                
                var condition = storedData.MarketData[marketId].Condition;
                var vending = storedData.MarketData[marketId].Vending;
                if (config.deploy.addVending && vending != null)
                {
                    foreach (var vend in vending)
                    {
                        var machine = BaseNetworkable.serverEntities.Find(new NetworkableId(vend.NetID));
                        Instance.NextTick(() => {
                        if (!machine.IsDestroyed)
                            machine.Kill();
                        
                        });
                    }
                }

                marketplace.Kill();
                
                Instance.RemovePlayerData(marketId);

                if (config.destroy.dropItem)
                {
                    Instance.DropDestroyed(position, condition);
                }

                if (config.options.enableSounds)
                {
                    Instance.PlaySound(breakSound, player);
                }
            }

            void OnDestroy()
            {
                CancelInvoke(nameof(GroundCheck));
            }
        }

        #endregion Mono

        #region Language

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Pickup"] = "You picked up a Marketplace!",
                ["Receive"] = "You received a Marketplace!",
                ["Crafted"] = "You crafted a Marketplace!",
                ["PlayerGiven"] = "{0} received a Marketplace!",
                ["ItemBroken"] = "Your Marketplace has broken and is not repariable!",
                ["Permission"] = "You do not have permission to do that!",
                ["BuildPriv"] = "You need building privilege to do that!",
                ["NoCraft"] = "Marketplace crafting is disabled!",
                ["NoPickup"] = "Marketplace pickup is disabled!",
                ["PickupUse"] = "Hit a terminal with a hammer or type: /marketplace.pickup while looking at it.",
                ["OwnerTeam"] = "Only the owner or their allies can pickup this Marketplace!",
                ["CraftCost"] = "Not enough resources to craft:\n{0}",
                ["NullEnt"] = "No marketplace near, try standing closer.",
                ["PlayerNotFound"] = "Can't find a player with the name/SteamID: {0}",
                ["NoPlayerData"] = "Can't find a player with the SteamID: {0}",
                ["NoMultiples"] = "You may only deploy one marketplace at any time!",
                ["OnFoundation"] = "Marketplaces must be placed on foundations!",
                ["InsideBuilding"] = "Marketplaces must have a clear line of sight to the sky!",
                ["RoomToDeploy"] = "There is not room to deploy a marketplace here!",
                ["NotMarket"] = "You are not looking at a marketplace!",
                ["ClearUsage"] = "Usage:\n - /marketplace.clear to clear ALL players data\n - /marketplace.clear <Steam64Id> to clear data for that player",
                ["DataCleared"] = "ALL Marketplace data cleared!",
                ["UserCleared"] = "Marketplace data cleared for: {0}",
                ["NoRepair"] = "You cannot repair a Marketplace!",
                ["NoRecycle"] = "You cannot recycle a Marketplace!",
                ["ConsoleUsage"] = "Error: Please specify a player name or SteamID!",
                ["InGameOnly"] = "Error: This command is only for use in game!",
                ["VendingNotEmpty"] = "One or more vending machines are not empty!",
                ["HitToRotate"] = "Hit marketplace vending machines with a hammer to rotate",
                ["OwnerTeamRotate"] = "Only the owner or their allies can rotate this vending machine",
                ["OwnerRotate"] = "Only the owner can rotate this vending machine"
            }, this, "en");
        }

        private string GetLang(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        public void Message(IPlayer player, string messageKey, params object[] args)
        {
            if (player == null) return;
            var message = string.Format(GetLang(messageKey, player.Id, args));
            if (config.options.useChatPrefix)
                player.Reply(config.options.chatPrefix + message);
            else
                player.Reply(message);

            BasePlayer basePlayer = player.Object as BasePlayer;
            if (config.options.showTips)
                basePlayer.ShowToast(GameTip.Styles.Blue_Long, message, args.ToString());
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null) return;
            var message = string.Format(GetLang(messageKey, player.UserIDString, args));
            if (config.options.useChatPrefix)
                player.ChatMessage(config.options.chatPrefix + message);
            else
                player.ChatMessage(message);

            if (config.options.showTips)
                player.ShowToast(GameTip.Styles.Blue_Long, message, args.ToString());
        }

        #endregion Language

        #region Stored Data

        private static StoredData storedData;

        private class StoredData
        {
            public Dictionary<ulong, MarketplaceData> MarketData = new Dictionary<ulong, MarketplaceData>();
        }

        private class MarketplaceData
        {
            public string PlayerName;
            public ulong OwnerID;
            public float Condition;
            public List<Vending> Vending = new List<Vending>();
        }

        private class Vending
        {
            public string Name;
            public ulong NetID;
        }

        private void AddPlayerData(ulong playerId, string playerName, ulong marketId, float condition, List<Vending> vending)
        {
            if(!storedData.MarketData.ContainsKey(marketId))
            {
                storedData.MarketData.Add(marketId, new MarketplaceData());
                storedData.MarketData[marketId].PlayerName = playerName;
                storedData.MarketData[marketId].OwnerID = playerId;
                storedData.MarketData[marketId].Condition = condition;
                storedData.MarketData[marketId].Vending = vending;
                SaveData();
            }
        }

        private void RemovePlayerData(ulong marketId)
        {
            if (storedData.MarketData.ContainsKey(marketId))
            {
                storedData.MarketData.Remove(marketId);
                SaveData();
            }
            return;
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        #endregion Stored Data

        #region Config

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Options")]
            public Options options;
            [JsonProperty(PropertyName = "Deploy Settings")]
            public Deploy deploy;
            [JsonProperty(PropertyName = "Pickup Settings")]
            public Pickup pickup;
            [JsonProperty(PropertyName = "Craft Settings")]
            public Craft craft;
            [JsonProperty(PropertyName = "Destroy Settings")]
            public Destroy destroy;
            
            public class Options
            {
                [JsonProperty(PropertyName = "Use Friends")]
                public bool useFriends;
                [JsonProperty(PropertyName = "Use Clans")]
                public bool useClans;
                [JsonProperty(PropertyName = "Use Teams")]
                public bool useTeams;
                [JsonProperty(PropertyName = "Plugin sound effects")]
                public bool enableSounds;
                [JsonProperty(PropertyName = "Show ToolTips")]
                public bool showTips;
                [JsonProperty(PropertyName = "Use Chat Prefix")]
                public bool useChatPrefix;
                [JsonProperty(PropertyName = "Chat Prefix")]
                public string chatPrefix;
            }
            public class Deploy
            {
                [JsonProperty(PropertyName = "Building privilege required to deploy")]
                public bool privDeploy;
                [JsonProperty(PropertyName = "Permission required to deploy")]
                public bool permDeploy;
                [JsonProperty(PropertyName = "Force deploy on foundation")]
                public bool onFoundation;
                [JsonProperty(PropertyName = "Players can deploy multiple marketplaces")]
                public bool deployMultiple;
                [JsonProperty(PropertyName = "Add vending machines to Marketplaces")]
                public bool addVending;
                [JsonProperty(PropertyName = "Building privilege required to access vending loot")]
                public bool vendingAuth;
                [JsonProperty(PropertyName = "Block all damage to vending machines")]
                public bool blockVendingDamage;
                [JsonProperty(PropertyName = "Vending machine drops loot if destroyed")]
                public bool dropVendingLoot;
            }
            public class Pickup
            {
                [JsonProperty(PropertyName = "Players can pickup own marketplaces")]
                public bool canPickup;
                [JsonProperty(PropertyName = "Building privilege required to pickup own marketplace")]
                public bool privPickup;
                [JsonProperty(PropertyName = "Require permission to pickup")]
                public bool permPickup;
                [JsonProperty(PropertyName = "Lose condition when players pickup")]
                public bool loseCondition;
                [JsonProperty(PropertyName = "Initial Marketplace condition")]
                public float initialCondition;
                [JsonProperty(PropertyName = "Amount of condition to lose")]
                public float pickupPenalty;
            }
            public class Craft
            {
                [JsonProperty(PropertyName = "Players can craft a Marketplace")]
                public bool craftTrue;
                [JsonProperty(PropertyName = "Require permission to craft")]
                public bool permCraft;
                [JsonProperty(PropertyName = "Cost to craft:")]
                public Dictionary<string, int> cost;
            }

            public class Destroy
            {
                [JsonProperty(PropertyName = "Destroy if floor underneath destroyed")]
                public bool floorCheck;
                [JsonProperty(PropertyName = "Drop marketplace on floor if destroyed")]
                public bool dropItem;
                [JsonProperty(PropertyName = "Lose condition when dropped")]
                public bool dropCondition;
            }
            public VersionNumber Version { get; set; }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                options = new ConfigData.Options
                {
                    useFriends = true,
                    useClans = true,
                    useTeams = true,
                    enableSounds = true,
                    showTips = true,
                    useChatPrefix = true,
                    chatPrefix = "[Personal Marketplace]: "
                },
                deploy = new ConfigData.Deploy
                {
                    privDeploy = true,
                    permDeploy = true,
                    onFoundation = false,
                    deployMultiple = false,
                    addVending = false,
                    vendingAuth = false,
                    blockVendingDamage = true,
                    dropVendingLoot = true
                },
                pickup = new ConfigData.Pickup
                {
                    canPickup = true,
                    permPickup = true,
                    privPickup = true,
                    loseCondition = true,
                    initialCondition = 100,
                    pickupPenalty = 20
                },
                craft = new ConfigData.Craft
                {
                    craftTrue = true,
                    permCraft = true,
                    cost = new Dictionary<string, int>
                    {
                        {"scrap", 500},
                        {"metal.fragments", 2500},
                        {"metal.refined", 350},
                        {"gears", 20},
                        {"techparts", 20},
                        {"dropbox", 8},
                        {"targeting.computer", 8},
                        {"electric.rf.receiver", 1},
                        {"electric.rf.broadcaster", 1}
                    }
                },
                destroy = new ConfigData.Destroy
                {
                    floorCheck = true,
                    dropItem = true,
                    dropCondition = false
                },
                Version = Version
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                if (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonReaderException)
                {
                    Puts($"ERROR: {ex}");
                    return;
                }
                throw;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Configuration file missing or corrupt, creating default config file.");
            config = GetDefaultConfig();
        }
        
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void UpdateConfigValues()
        {
            ConfigData defaultConfig = GetDefaultConfig();
            if (config.Version < Version)
            {
                Puts("Config update detected! Updating config file...");
                if (config.Version < new VersionNumber(1, 1, 1))
                {
                    config.pickup.loseCondition = defaultConfig.pickup.loseCondition;
                    config.pickup.initialCondition = defaultConfig.pickup.initialCondition;
                    config.pickup.pickupPenalty = defaultConfig.pickup.pickupPenalty;
                    config.destroy.dropCondition = defaultConfig.destroy.dropCondition;
                }
                if (config.Version < new VersionNumber(1, 1, 10))
                {
                    config.options.useChatPrefix = defaultConfig.options.useChatPrefix;
                    config.options.chatPrefix = defaultConfig.options.chatPrefix;
                    config.craft.cost = defaultConfig.craft.cost;
                }
                if (config.Version < new VersionNumber(1, 1, 12))
                {
                    config.deploy.addVending = defaultConfig.deploy.addVending;
                    config.craft.cost = defaultConfig.craft.cost;
                }
                if (config.Version < new VersionNumber(1, 1, 13))
                {
                    config.options.showTips = defaultConfig.options.showTips;
                    config.craft.cost = defaultConfig.craft.cost;
                }
                if (config.Version < new VersionNumber(1, 1, 18))
                {
                    config.deploy.blockVendingDamage = defaultConfig.deploy.blockVendingDamage;
                }
                if (config.Version < new VersionNumber(1, 2, 0))
                {
                    config.deploy.vendingAuth = defaultConfig.deploy.vendingAuth;
                    config.deploy.dropVendingLoot = defaultConfig.deploy.dropVendingLoot;
                }
                if (config.Version < new VersionNumber(1, 2, 3))
                {
                    storedData = new StoredData();
                    SaveData();
                }

                Puts("Config update completed!");
            }

            config.Version = Version;
            SaveConfig();
        }

        #endregion Config
    }
}