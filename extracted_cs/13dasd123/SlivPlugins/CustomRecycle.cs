// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomRecycle", "ThePitereq", "2.0.15")]
    public class CustomRecycle : RustPlugin
    {
        private static CustomRecycle _plugin;
        [PluginReference] private readonly Plugin ImageLibrary, Economics, ServerRewards, PopUpAPI, ShoppyStock;
        private readonly Dictionary<ulong, int> playerLimits = new Dictionary<ulong, int>();

        private void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnEntityKill));
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnEntityKill));
            _plugin = this;
            cmd.AddConsoleCommand("giverecycler", this, nameof(GiveRecyclerCommand));
            cmd.AddConsoleCommand("UI_CustomRecycle", this, nameof(CustomRecycleCommand));
            permission.RegisterPermission("customrecycle.give", this);
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
            if (config.recyclerLevels)
                LoadData();
            if (!config.betterAccuracy)
                PrintWarning("\nYou have disabled better accuracy option!\nYour custom recycles will have less accuracy in larger recycle amounts and chances for items will be checked only once at recycle start! (Not individual for every one item)\nIf you know what are you doing you can leave this option disabled, but if you want better accuracy, please enable this option!");
            AddImage("https://images.pvrust.eu/ui_icons/CustomRecycle/recycler_0.png", "UI_CustomRecycle_recycler_0", 0);
            foreach (var level in config.levels)
                foreach (var item in level.requiredItems)
                    if (item.url != "")
                        AddImage(item.url, item.shortname, item.skin);
            foreach (var perm in config.recyclerPermissions)
                permission.RegisterPermission(perm.Key, this);
            foreach (var recycler in BaseNetworkable.serverEntities.OfType<Recycler>())
            {
                OnEntitySpawned(recycler);
                if (config.requirePermission)
                {
                    playerLimits.TryAdd(recycler.OwnerID, 0);
                    playerLimits[recycler.OwnerID]++;
                }
                timer.Once(5, () =>
                {
                    if (recycler == null) return;
                    if (config.recyclerLevels)
                    {
                        CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                        component.recyclePercentage = config.levels[data.recyclerLevels[recycler.net.ID.Value]].stackPercentage;
                        if (recycler.IsOn())
                        {
                            recycler.StopRecycling();
                            component.StopRecycling();
                            NextTick(() => component.StartRecycling());
                        }
                    }
                    else if (recycler.IsOn())
                    {
                        recycler.StopRecycling();
                        NextTick(() => recycler.StartRecycling());
                    }
                });
            }
        }

        private void Unload()
        {
            if (config.recyclerLevels)
            {
                foreach (var recycler in BaseNetworkable.serverEntities.OfType<Recycler>())
                {
                    CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                    if (recycler.IsOn())
                    {
                        component.StopRecycling();
                        NextTick(() => recycler.StartRecycling());
                    }
                    UnityEngine.Object.Destroy(component);
                }
                SaveData();
            }
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, "CustomRecycle_MainUI");
        }

        private void OnEntitySpawned(Recycler recycler)
        {
            CustomRecycle recycleComp = recycler.gameObject.GetComponent<CustomRecycle>();
            if (recycleComp == null)
                recycler.gameObject.AddComponent<CustomRecycler>();
            if (config.recyclerLevels)
                data.recyclerLevels.TryAdd(recycler.net.ID.Value, 0);
            recycler.inventory.canAcceptItem = (item, i) => CheckItem(item, i, recycler);
            recycler.SendNetworkUpdate();
        }

        private void OnEntityKill(Recycler recycler)
        {
            if (config.requirePermission && playerLimits.ContainsKey(recycler.OwnerID) && playerLimits[recycler.OwnerID] > 0)
                playerLimits[recycler.OwnerID]--;
            if (config.recyclerLevels)
            {
                CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                UnityEngine.Object.Destroy(component);
                data.recyclerLevels.Remove(recycler.net.ID.Value);
            }
            foreach (var item in recycler.inventory.itemList.ToList())
                item.Drop(recycler.transform.position + new Vector3(0, 1, 0), Vector3.zero);
        }

        private object OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (config.recyclerLevels)
            {
                CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                component.recyclePercentage = config.levels[data.recyclerLevels[recycler.net.ID.Value]].stackPercentage;
                if (recycler.IsOn())
                    component.StopRecycling();
                else if (recycler.HasRecyclable())
                    component.StartRecycling(player);
                return false;
            }
            else
            {
                if (recycler.IsOn()) return null;
                float recyclerSpeed = GetRecyclerSpeed(player);
                recycler.CancelInvoke(nameof(recycler.RecycleThink));
                NextTick(() => recycler.InvokeRepeating(recycler.RecycleThink, recyclerSpeed, recyclerSpeed));
                return null;
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            RepairBench repairBench = go.ToBaseEntity() as RepairBench;
            if (repairBench == null || repairBench.skinID != 2795785961) return;
            int maxRecyclers = 0;
            if (config.requirePermission)
            {
                maxRecyclers = GetHighestPermission(repairBench.OwnerID);
                playerLimits.TryAdd(repairBench.OwnerID, 0);
            }
            ulong owner = repairBench.OwnerID;
            ulong skin = repairBench.skinID;
            Vector3 pos = repairBench.transform.position;
            BasePlayer player = plan.GetOwnerPlayer();
            Item cachedItem = plan.GetItem();
            NextTick(() =>
            {
                Tugboat tugboat = null;
                repairBench.Kill();
                if (config.onlyOnFloor || config.placeOnTugBoat)
                {
                    RaycastHit hitinfo;
                    bool valid = false;
                    if (config.onlyOnFloor)
                    {
                        if (Physics.Raycast(pos + new Vector3(0, 1, 0), Vector3.down, out hitinfo, 2.5f, LayerMask.GetMask("Construction")) && hitinfo.GetEntity().IsValid())
                        {
                            BaseEntity entity = hitinfo.GetEntity();
                            if (entity.GetComponent<BuildingBlock>() != null)
                                valid = true;
                        }
                    }
                    if (!valid && config.placeOnTugBoat)
                    {
                        List<Tugboat> tugBoats = new List<Tugboat>();
                        Vis.Entities(pos, 5f, tugBoats);
                        if (tugBoats.Count > 0)
                        {
                            tugboat = tugBoats[0];
                            valid = true;
                        }
                    }
                    if (!valid && config.onlyOnFloor)
                    {
                        Item recyclerItem = ItemManager.CreateByName("box.repair.bench", 1, 2795785961);
                        recyclerItem.name = config.recyclerName;
                        if (player != null)
                        {
                            if (!recyclerItem.MoveToContainer(player.inventory.containerBelt))
                                recyclerItem.MoveToContainer(player.inventory.containerMain);
                            SendReply(player, Lang("OnlyOnFloor", player.UserIDString));
                        }
                        return;
                    }
                }
                if (config.requirePermission && playerLimits[owner] >= maxRecyclers)
                {
                    Item recyclerItem = ItemManager.CreateByName("box.repair.bench", 1, 2795785961);
                    recyclerItem.name = config.recyclerName;
                    if (player != null)
                    {
                        if (!recyclerItem.MoveToContainer(player.inventory.containerBelt))
                            recyclerItem.MoveToContainer(player.inventory.containerMain);
                        if (maxRecyclers != 0)
                            SendReply(player, Lang("TooManyRecyclers", player.UserIDString));
                        else
                            SendReply(player, Lang("CannotPlaceRecyclers", player.UserIDString));
                    }
                    return;
                }
                if (config.requirePermission)
                    playerLimits[owner]++;
                Recycler recycler = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", pos, repairBench.transform.rotation) as Recycler;
                recycler.skinID = skin;
                recycler.OwnerID = owner;
                if (tugboat != null)
                    recycler.SetParent(tugboat, true);
                recycler.Spawn();
                Interface.CallHook("OnRecyclerPlaced", player, recycler);
                if (config.recyclerLevels)
                {
                    data.recyclerLevels.TryAdd(recycler.net.ID.Value, 0);
                    if (config.saveLevels)
                    {
                        string[] splitName = cachedItem.name.Split('[');
                        if (splitName.Length > 1)
                        {
                            int level = Convert.ToInt32(splitName[1].Replace("Lv. ", "").Split(']')[0]);
                            data.recyclerLevels[recycler.net.ID.Value] = level - 1;
                        }
                    }
                }
            });
        }

        private object OnItemRecycle(Item item, Recycler recycler)
        {
            if (item == null) return null;
            RecycleConfig configValue = null;
            if ((config.customOnlyInPlaced && recycler.OwnerID != 0) || !config.customOnlyInPlaced)
            {
                if (item.skin != 0 && config.recycles.ContainsKey(item.skin.ToString()))
                    configValue = config.recycles[item.skin.ToString()];
                else if (config.recycles.ContainsKey(item.info.shortname))
                    configValue = config.recycles[item.info.shortname];
            }
            if (configValue == null)
            {
                if (item.info.Blueprint != null) return null;
                if (config.recyclerLevels)
                {
                    CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                    component.StopRecycling();
                }
                else
                    recycler.StopRecycling();
                return false;
            }
            int oneRecycleAmount = Mathf.CeilToInt(item.info.stackable * 0.1f);
            if (config.recyclerLevels)
                oneRecycleAmount = Mathf.CeilToInt(item.info.stackable * config.levels[data.recyclerLevels[recycler.net.ID.Value]].stackPercentage);
            int amount = item.amount < oneRecycleAmount ? item.amount : oneRecycleAmount;
            if (item.info.Blueprint == null || !configValue.defaultOutput)
            {
                if (item.amount > oneRecycleAmount)
                {
                    item.amount -= oneRecycleAmount;
                    item.MarkDirty();
                }
                else
                {
                    item.GetHeldEntity()?.Kill();
                    item.DoRemove();
                }
            }
            foreach (var additionalItem in configValue.outputItems)
            {
                float realChance = config.recyclerLevels ? additionalItem.chance * config.levels[data.recyclerLevels[recycler.net.ID.Value]].chanceMultiplier : additionalItem.chance;
                int chance;
                if (realChance >= 100)
                    chance = 100;
                else
                    chance = Core.Random.Range(0, 101);
                bool giveItems = false;
                int itemAmount = 0;
                if (!config.betterAccuracy && chance > realChance) continue;
                else if (!config.betterAccuracy && chance <= realChance)
                {
                    itemAmount = Core.Random.Range(additionalItem.minAmount * amount, additionalItem.maxAmount * amount + 1);
                    giveItems = true;
                }
                else if (config.betterAccuracy)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        if (realChance >= 100)
                            chance = 100;
                        else
                            chance = Core.Random.Range(0, 101);
                        if (chance <= realChance)
                        {
                            itemAmount += Core.Random.Range(additionalItem.minAmount, additionalItem.maxAmount + 1);
                            giveItems = true;
                        }
                    }
                }
                if (giveItems)
                {
                    Item outputItem = ItemManager.CreateByName(additionalItem.shortname, itemAmount, additionalItem.skin);
                    if (additionalItem.name != string.Empty)
                        outputItem.name = additionalItem.name;
                    if (!recycler.MoveItemToOutput(outputItem))
                    {
                        outputItem.Drop(recycler.transform.position + new Vector3(0, 2, 0), recycler.GetInheritedDropVelocity() + recycler.transform.forward * 2f);
                        if (recycler.IsOn())
                        {
                            if (config.recyclerLevels)
                            {
                                CustomRecycler component = recycler.GetComponent<CustomRecycler>();
                                component.StopRecycling();
                            }
                            else
                                recycler.StopRecycling();
                        }
                    }
                }
            }
            if (item.info.Blueprint != null && configValue.defaultOutput)
                return null;
            else
                return false;
        }

        private bool CanBeRecycled(Item item)
        {
            if (item == null) return false;
            return true;
        }

        private bool CanRecycle(Recycler recycler, Item item)
        {
            if (item == null) return false;
            if ((config.customOnlyInPlaced && recycler.OwnerID != 0) || !config.customOnlyInPlaced)
                if (config.recycles.ContainsKey(item.info.shortname) || config.recycles.ContainsKey(item.skin.ToString())) return true;
            if (item.info.Blueprint != null) return true;
            return false;
        }

        private bool CheckItem(Item item, int slot, Recycler recycler)
        {
            if (slot > 5) return true;
            if ((config.customOnlyInPlaced && recycler.OwnerID != 0) || !config.customOnlyInPlaced)
                if (config.recycles.ContainsKey(item.info.shortname) || config.recycles.ContainsKey(item.skin.ToString())) return true;
            if (item != null)
            {
                if (config.disabledRecipes.Contains(item.info.shortname)) return false;
                if (item.info.Blueprint != null) return true;
            }
            return false;
        }

        private void OnLootEntity(BasePlayer player, Recycler recycler)
        {
            if (!config.recyclerLevelsNoOwner && recycler.OwnerID == 0) return;
            OpenRecyclerUI(player, recycler);
        }

        private void OnLootEntityEnd(BasePlayer player, Recycler recycler) => CuiHelper.DestroyUi(player, "CustomRecycle_MainUI");

        private int GetHighestPermission(ulong userid)
        {
            int max = 0;
            foreach (var permissionKey in config.amountPermissions)
                if (permission.UserHasPermission(userid.ToString(), permissionKey.Key))
                    max = permissionKey.Value;
            return max;
        }

        private float GetRecyclerSpeed(BasePlayer player = null)
        {
            if (player == null) return config.recyclerSpeed;
            else
            {
                float highestSpeed = config.recyclerSpeed;
                foreach (var perm in config.recyclerPermissions)
                    if (permission.UserHasPermission(player.UserIDString, perm.Key) && perm.Value < highestSpeed)
                        highestSpeed = perm.Value;
                return highestSpeed;
            }
        }

        private void GiveRecyclerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "customrecycle.give")) return;
            ulong userId;
            if (arg.Args == null && player == null)
                SendReply(arg, "Usage: giverecycler <userId> - Gives recycler to described person.");
            else if (arg.Args == null && player != null)
            {
                Item recycler = ItemManager.CreateByName("box.repair.bench", 1, 2795785961);
                recycler.name = config.recyclerName;
                if (!recycler.MoveToContainer(player.inventory.containerBelt))
                    if (!recycler.MoveToContainer(player.inventory.containerMain))
                        recycler.Drop(player.eyes.transform.position, Vector3.zero);
            }
            else if (arg.Args != null && UInt64.TryParse(arg.Args[0], out userId))
            {
                BasePlayer target = BasePlayer.FindByID(userId);
                if (target == null)
                {
                    SendReply(arg, $"Player with ID {userId} is offline. Canceling redeem...");
                    return;
                }
                Item recycler = ItemManager.CreateByName("box.repair.bench", 1, 2795785961);
                recycler.name = config.recyclerName;
                if (!recycler.MoveToContainer(target.inventory.containerBelt))
                    if (!recycler.MoveToContainer(target.inventory.containerMain))
                        recycler.Drop(target.eyes.transform.position, Vector3.zero);
            }
            else
                SendReply(arg, "Usage: giverecycler <userId> - Gives recycler to described person.");
        }

        private void CustomRecycleCommand(ConsoleSystem.Arg arg)
        {
            if (config.recyclerLevels && arg.Args[0].Contains("upgrade"))
            {
                uint netId;
                if (!uint.TryParse(arg.Args[1], out netId)) return;
                BasePlayer player = arg.Player();
                if (arg.Args[2].Contains("items"))
                {
                    if (TakeResources(player, config.levels[data.recyclerLevels[netId]].requiredItems))
                        UpgradeRecycler(player, netId);
                    else
                        FailedUpgrade(player, "items");
                }
                else
                {
                    int price = config.levels[data.recyclerLevels[netId]].currencyCost;
                    bool success = false;
                    switch (config.moneyPlugin)
                    {
                        case 1:
                            if (Economics != null && Economics.Call<double>("Balance", player.userID) >= price)
                            {
                                Economics.Call<bool>("Withdraw", player.userID, (double)price);
                                success = true;
                            }
                            break;
                        case 2:
                            if (ServerRewards != null && ServerRewards.Call<int>("CheckPoints", player.userID) >= price)
                            {
                                ServerRewards.Call<bool>("TakePoints", player.userID, price);
                                success = true;
                            }
                            break;
                        case 3:
                            if (ShoppyStock != null && ShoppyStock.Call<int>("GetCurrencyAmount", config.currency, player.userID) >= price)
                            {
                                ShoppyStock.Call<bool>("TakeCurrency", config.currency, player.userID, price);
                                success = true;
                            }
                            break;
                        default:
                            break;
                    }
                    if (success)
                        UpgradeRecycler(player, netId);
                    else
                        FailedUpgrade(player, "currency");
                }
            }
            else if (arg.Args[0].Contains("pickup"))
            {
                uint netId;
                if (!uint.TryParse(arg.Args[1], out netId)) return;
                BasePlayer player = arg.Player();
                Recycler recycler = BaseNetworkable.serverEntities.Find(new NetworkableId(netId)) as Recycler;
                if (recycler == null) return;
                CuiHelper.DestroyUi(player, "CustomRecycle_MainUI");
                string name = config.recyclerName;
                if (config.saveLevels)
                    name += $" [Lv. {data.recyclerLevels[recycler.net.ID.Value] + 1}]";
                recycler.Kill();
                Item recyclerItem = ItemManager.CreateByName("box.repair.bench", 1, 2795785961);
                recyclerItem.name = name;
                if (!recyclerItem.MoveToContainer(player.inventory.containerBelt))
                    recyclerItem.MoveToContainer(player.inventory.containerMain);
                Effect.server.Run("assets/prefabs/weapons/arms/effects/pickup_item.prefab", player.transform.position);
                player.Command("note.inv 803222026 1 Recycler");
            }
        }

        private void UpgradeRecycler(BasePlayer player, uint netId)
        {
            data.recyclerLevels[netId]++;
            Recycler recycler = BaseNetworkable.serverEntities.Find(new NetworkableId(netId)) as Recycler;
            if (recycler != null)
            {
                Effect.server.Run("assets/prefabs/deployable/quarry/effects/mining-quarry-deploy.prefab", recycler.transform.position);
                Effect.server.Run("assets/bundled/prefabs/fx/build/promote_toptier.prefab", recycler.transform.position);
                Interface.CallHook("OnRecyclerUpgraded", player, recycler, data.recyclerLevels[netId] + 1);
                PopUpAPI?.Call("API_ShowPopup", player, Lang("RecyclerUpgraded", player.UserIDString, data.recyclerLevels[netId] + 1));
                CuiHelper.DestroyUi(player, "CustomRecycle_MainUI");
                OpenRecyclerUI(player, recycler);
            }
            else
                data.recyclerLevels.Remove(netId);
        }

        private void FailedUpgrade(BasePlayer player, string reason)
        {
            Effect.server.Run("assets/prefabs/tools/pager/effects/beep.prefab", player.transform.position);
            if (reason == "items")
                PopUpAPI?.Call("API_ShowPopup", player, Lang("NotEnoughItems", player.UserIDString));
            else if (reason == "currency")
                PopUpAPI?.Call("API_ShowPopup", player, Lang("NotEnoughCurrency", player.UserIDString));
        }

        private static bool TakeResources(BasePlayer player, List<UpgradeItem> items)
        {
            foreach (var requiredItem in items)
            {
                bool haveRequired = false;
                int inventoryAmount = 0;
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.skin == requiredItem.skin && item.info.shortname == requiredItem.shortname)
                    {
                        inventoryAmount += item.amount;
                        if (inventoryAmount >= requiredItem.amount)
                        {
                            haveRequired = true;
                            break;
                        }
                    }
                }
                if (!haveRequired)
                    return false;
            }
            foreach (var requiredItem in items)
            {
                int takenItems = 0;
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.skin == requiredItem.skin && item.info.shortname == requiredItem.shortname)
                    {
                        if (takenItems < requiredItem.amount)
                        {
                            if (item.amount > requiredItem.amount - takenItems)
                            {
                                item.amount -= requiredItem.amount - takenItems;
                                item.MarkDirty();
                                break;
                            }
                            if (item.amount <= requiredItem.amount - takenItems)
                            {
                                takenItems += item.amount;
                                item.GetHeldEntity()?.Kill();
                                item.Remove();
                            }
                        }
                        else break;
                    }
                }
            }
            return true;
        }

        private class CustomRecycler : FacepunchBehaviour
        {
            private Recycler recycler;
            public float recyclePercentage = 0.1f;

            private void Awake()
            {
                recycler = GetComponent<Recycler>();
                if (recycler.OwnerID != 0)
                {
                    if (recycler.GetComponent<GroundWatch>() == null)
                        recycler.gameObject.AddComponent<GroundWatch>();
                    if (recycler.GetComponent<DestroyOnGroundMissing>() == null)
                        recycler.gameObject.AddComponent<DestroyOnGroundMissing>();
                }
            }

            public void StartRecycling(BasePlayer player = null)
            {
                if (recycler.IsOn()) return;
                float recyclerSpeed = _plugin.GetRecyclerSpeed(player);
                InvokeRepeating(new Action(RecycleThink), recyclerSpeed, recyclerSpeed);
                Effect.server.Run(recycler.startSound.resourcePath, recycler, 0U, Vector3.zero, Vector3.zero, null, false);
                recycler.SetFlag(BaseEntity.Flags.On, true, false, true);
                recycler.SendNetworkUpdateImmediate(false);
            }

            public void StopRecycling()
            {
                CancelInvoke(new Action(RecycleThink));
                if (!recycler.IsOn()) return;
                Effect.server.Run(recycler.stopSound.resourcePath, recycler, 0U, Vector3.zero, Vector3.zero, null, false);
                recycler.SetFlag(BaseEntity.Flags.On, false, false, true);
                recycler.SendNetworkUpdateImmediate(false);
            }

            private void RecycleThink()
            {
                bool flag = false;
                float num = recycler.recycleEfficiency;
                for (int i = 0; i < 6; i++)
                {
                    Item slot = recycler.inventory.GetSlot(i);
                    if (CanBeRecycled(slot))
                    {
                        if (Interface.CallHook("OnItemRecycle", slot, recycler) != null)
                        {
                            if (!recycler.HasRecyclable())
                            {
                                StopRecycling();
                                return;
                            }
                            return;
                        }
                        else
                        {
                            if (slot.hasCondition)
                            {
                                num = Mathf.Clamp01(num * Mathf.Clamp(slot.conditionNormalized * slot.maxConditionNormalized, 0.1f, 1f));
                            }
                            int num2 = 1;
                            if (slot.amount > 1)
                            {
                                num2 = Mathf.CeilToInt(Mathf.Min((float)slot.amount, slot.info.stackable * recyclePercentage));
                            }
                            if (slot.info.Blueprint.scrapFromRecycle > 0)
                            {
                                int num3 = slot.info.Blueprint.scrapFromRecycle * num2;
                                if (slot.info.stackable == 1 && slot.hasCondition)
                                {
                                    num3 = Mathf.CeilToInt((float)num3 * slot.conditionNormalized);
                                }
                                if (num3 >= 1)
                                {
                                    Item newItem = ItemManager.CreateByName("scrap", num3, 0UL);
                                    recycler.MoveItemToOutput(newItem);
                                }
                            }
                            if (!string.IsNullOrEmpty(slot.info.Blueprint.RecycleStat))
                            {
                                List<BasePlayer> list = Facepunch.Pool.GetList<BasePlayer>();
                                Vis.Entities<BasePlayer>(base.transform.position, 3f, list, 131072, QueryTriggerInteraction.Collide);
                                foreach (BasePlayer basePlayer in list)
                                {
                                    if (basePlayer.IsAlive() && !basePlayer.IsSleeping() && basePlayer.inventory.loot.entitySource == recycler)
                                    {
                                        basePlayer.stats.Add(slot.info.Blueprint.RecycleStat, num2, (Stats)5);
                                        basePlayer.stats.Save();
                                    }
                                }
                                Facepunch.Pool.FreeList<BasePlayer>(ref list);
                            }
                            slot.UseItem(num2);
                            using (List<ItemAmount>.Enumerator enumerator2 = slot.info.Blueprint.ingredients.GetEnumerator())
                            {
                                while (enumerator2.MoveNext())
                                {
                                    ItemAmount itemAmount = enumerator2.Current;
                                    if (!(itemAmount.itemDef.shortname == "scrap"))
                                    {
                                        float num4 = itemAmount.amount / (float)slot.info.Blueprint.amountToCreate;
                                        int num5 = 0;
                                        if (num4 <= 1f)
                                        {
                                            for (int j = 0; j < num2; j++)
                                            {
                                                if (UnityEngine.Random.Range(0f, 1f) <= num4 * num)
                                                {
                                                    num5++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            num5 = Mathf.CeilToInt(Mathf.Clamp(num4 * num * UnityEngine.Random.Range(1f, 1f), 0f, itemAmount.amount)) * num2;
                                        }
                                        if (num5 > 0)
                                        {
                                            int num6 = Mathf.CeilToInt((float)num5 / (float)itemAmount.itemDef.stackable);
                                            for (int k = 0; k < num6; k++)
                                            {
                                                int num7 = (num5 > itemAmount.itemDef.stackable) ? itemAmount.itemDef.stackable : num5;
                                                Item newItem2 = ItemManager.Create(itemAmount.itemDef, num7, 0UL);
                                                if (!recycler.MoveItemToOutput(newItem2))
                                                {
                                                    flag = true;
                                                }
                                                num5 -= num7;
                                                if (num5 <= 0)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                if (flag || !recycler.HasRecyclable())
                {
                    StopRecycling();
                    return;
                }
            }

            private bool CanBeRecycled(Item item)
            {
                object obj = Interface.CallHook("CanBeRecycled", item, recycler);
                if (obj is bool)
                {
                    return (bool)obj;
                }
                return item != null && item.info.Blueprint != null;
            }

            private void OnDestroy()
            {
                CancelInvoke();
                recycler = null;
            }
        }

        private void OpenRecyclerUI(BasePlayer player, Recycler recycler)
        {
            CuiElementContainer container = new CuiElementContainer();
            int cornerX = 192;
            int cornerY = 427;
            UI_AddAnchor(container, "CustomRecycle_MainUI", "Hud.Menu", "0.5 0", "0.5 0");
            if (recycler.OwnerID == player.userID && Vector3.Distance(player.transform.position, recycler.transform.position) < 5f)
                UI_AddPickupButton(container, "CustomRecycle_MainUI", "0.45 0.237 0.194 1", "0.941 0.486 0.302 1", "assets/icons/pickup.png", $"UI_CustomRecycle pickup {recycler.net.ID.Value}", Lang("Pickup", player.UserIDString), "0.5 0", "0.5 0", "502 401", "572 421");
            if (config.recyclerLevels)
            {
                UI_AddBlurPanel(container, "CustomRecycle_MainUI", "0 0 0 0.15", "0.5 0", "0.5 0", $"{cornerX} {cornerY}", $"{cornerX + 380} {cornerY + 170}");
                UI_AddPanel(container, "CustomRecycle_MainUI", "0.61 0.6 0.58 0.09", "0.5 0", "0.5 0", $"{cornerX} {cornerY}", $"{cornerX + 380} {cornerY + 170}");
                UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("Upgrades", player.UserIDString), TextAnchor.MiddleLeft, 20, "0.5 0", "0.5 0", $"193 595", $"{cornerX + 380} 628");
                int level = data.recyclerLevels[recycler.net.ID.Value];
                bool maxLevel = level >= config.levels.Count - 1;
                if (maxLevel)
                    UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("Now", player.UserIDString), TextAnchor.MiddleCenter, 15, "0.5 0", "0.5 0", $"{cornerX + 1} {cornerY + 170}", $"{cornerX + 380} {cornerY + 203}");
                else
                {
                    UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("Now", player.UserIDString), TextAnchor.MiddleCenter, 15, "0.5 0", "0.5 0", $"{cornerX + 194} {cornerY + 170}", $"{cornerX + 287} {cornerY + 198}");
                    UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("After", player.UserIDString), TextAnchor.MiddleCenter, 15, "0.5 0", "0.5 0", $"{cornerX + 287} {cornerY + 170}", $"{cornerX + 380} {cornerY + 198}");
                }
                UI_AddPanel(container, "CustomRecycle_MainUI", "0.61 0.6 0.58 0.05", "0.5 0", "0.5 0", $"{cornerX + 0} {cornerY + 90}", $"{cornerX + 380} {cornerY + 170}");
                UI_AddText(container, "CustomRecycle_MainUI", "0.75 0.71 0.67", Lang("UpgradeInfo", player.UserIDString), TextAnchor.MiddleRight, 13, "0.5 0", "0.5 0", $"{cornerX + 0} {cornerY + 90}", $"{cornerX + 193} {cornerY + 170}");
                if (maxLevel)
                {
                    string display = config.showCustomBonus ? $"{level + 1}\n{config.levels[level].stackPercentage * 100f}%\nx{config.levels[level].chanceMultiplier}" : $"{level + 1}\n{config.levels[level].stackPercentage * 100f}%";
                    UI_AddBoldTextOutline(container, "CustomRecycle_MainUI", "0.733 0.851 0.533", display, TextAnchor.MiddleCenter, 13, "0.5 0", "0.5 0", $"{cornerX + 194} {cornerY + 90}", $"{cornerX + 380} {cornerY + 170}");
                }
                else
                {
                    UI_AddIcon(container, "CustomRecycle_MainUI", "0.75 0.71 0.67", "assets/icons/chevron_right.png", "0.5 0", "0.5 0", $"{cornerX + 281} {cornerY + 124}", $"{cornerX + 293} {cornerY + 136}");
                    string display = config.showCustomBonus ? $"{level + 1}\n{config.levels[level].stackPercentage * 100f}%\nx{config.levels[level].chanceMultiplier}" : $"{level + 1}\n{config.levels[level].stackPercentage * 100f}%";
                    UI_AddBoldTextOutline(container, "CustomRecycle_MainUI", "0.941 0.486 0.302", display, TextAnchor.MiddleCenter, 13, "0.5 0", "0.5 0", $"{cornerX + 194} {cornerY + 90}", $"{cornerX + 287} {cornerY + 170}");
                    display = config.showCustomBonus ? $"{level + 2}\n{config.levels[level + 1].stackPercentage * 100f}%\nx{config.levels[level + 1].chanceMultiplier}" : $"{level + 2}\n{config.levels[level + 1].stackPercentage * 100f}%";
                    UI_AddBoldTextOutline(container, "CustomRecycle_MainUI", "0.733 0.851 0.533", display, TextAnchor.MiddleCenter, 13, "0.5 0", "0.5 0", $"{cornerX + 287} {cornerY + 90}", $"{cornerX + 380} {cornerY + 170}");
                }
                UI_AddImage(container, "CustomRecycle_MainUI", "UI_CustomRecycle_recycler_0", 0, "0.5 0", "0.5 0", $"{cornerX + 10} {cornerY + 100}", $"{cornerX + 70} {cornerY + 160}");
                if (maxLevel)
                    UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("LevelMaxed", player.UserIDString), TextAnchor.MiddleCenter, 20, "0.5 0", "0.5 0", $"{cornerX + 0} {cornerY + 0}", $"{cornerX + 380} {cornerY + 90}");
                else
                {
                    UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("RequiredItems", player.UserIDString), TextAnchor.MiddleCenter, 13, "0.5 0", "0.5 0", $"{cornerX + 0} {cornerY + 35}", $"{cornerX + 193} {cornerY + 90}");

                    int start = cornerX + 168;
                    RecyclerLevel configValue = config.levels[level];
                    switch (configValue.requiredItems.Count)
                    {
                        case 3:
                            start = cornerX + 218;
                            break;
                        case 2:
                            start = cornerX + 238;
                            break;
                        case 1:
                            start = cornerX + 263;
                            break;
                        default:
                            break;
                    }
                    for (int i = 0; i < configValue.requiredItems.Count; i++)
                    {
                        UI_AddItemImage(container, "CustomRecycle_MainUI", configValue.requiredItems[i].shortname, configValue.requiredItems[i].skin, "0.5 0", "0.5 0", $"{start + 2} 467", $"{start + 43} 508");
                        UI_AddText(container, "CustomRecycle_MainUI", "0.75 0.71 0.67", $"x{configValue.requiredItems[i].amount} ", TextAnchor.LowerRight, 12, "0.5 0", "0.5 0", $"{start} 465", $"{start + 45} 510");
                        start += 50;
                    }
                    UI_AddPanel(container, "CustomRecycle_MainUI", "0.61 0.6 0.58 0.05", "0.5 0", "0.5 0", $"{cornerX + 0} {cornerY + 6}", $"{cornerX + 380} {cornerY + 35}");
                    UI_AddIcon(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", "assets/icons/store.png", "0.5 0", "0.5 0", $"{cornerX + 7} {cornerY + 13}", $"{cornerX + 22} {cornerY + 28}");
                    int end = configValue.currencyCost > 0 ? 183 : 366;
                    int fontSize = configValue.currencyCost > 0 ? 14 : 17;
                    UI_AddButton(container, "CustomRecycle_MainUI", "0.733 0.851 0.533 1", Lang("PurchaseItems", player.UserIDString), TextAnchor.MiddleCenter, fontSize, "0.439 0.538 0.261 1", $"UI_CustomRecycle upgrade {recycler.net.ID.Value} items", "0.5 0", "0.5 0", $"{cornerX + 29} {cornerY + 6}", $"{cornerX + end} {cornerY + 35}");
                    if (configValue.currencyCost != 0)
                    {
                        UI_AddBoldText(container, "CustomRecycle_MainUI", "0.91 0.87 0.83", Lang("Or", player.UserIDString), TextAnchor.MiddleCenter, 13, "0.5 0", "0.5 0", $"{cornerX + 183} {cornerY + 6}", $"{cornerX + 212} {cornerY + 35}");
                        UI_AddButton(container, "CustomRecycle_MainUI", "0.733 0.851 0.533 1", Lang("PurchaseCurrency", player.UserIDString, configValue.currencyCost), TextAnchor.MiddleCenter, 14, "0.439 0.538 0.261 1", $"UI_CustomRecycle upgrade {recycler.net.ID.Value} currency", "0.5 0", "0.5 0", $"{cornerX + 212} {cornerY + 6}", $"{cornerX + 366} {cornerY + 35}");
                    }
                }
            }
            CuiHelper.DestroyUi(player, "CustomRecycle_MainUI");
            CuiHelper.AddUi(player, container);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurchaseItems"] = "PURCHASE FOR ITEMS",
                ["PurchaseCurrency"] = "PURCHASE FOR {0} RP",
                ["Or"] = "OR",
                ["Pickup"] = "PICKUP   ",
                ["Upgrades"] = "UPGRADES",
                ["Now"] = "NOW",
                ["After"] = "AFTER",
                ["UpgradeInfo"] = "Current Level:\nStack Per Recycle:\nCustom Recycle Chance Multiplier:",
                ["LevelMaxed"] = "LEVEL MAXED",
                ["RequiredItems"] = "REQUIRED ITEMS",
                ["NotEnoughItems"] = "You don't have required items to upgrade this recycler!",
                ["NotEnoughCurrency"] = "You don't have enough RP to upgrade this recycler!",
                ["RecyclerUpgraded"] = "You've successfully upgraded this recycler!",
                ["TooManyRecyclers"] = "You've placed too many recyclers!\nDestroy old ones first to place new one!",
                ["CannotPlaceRecyclers"] = "You don't have permission to place new recyclers!",
                ["OnlyOnFloor"] = "You can place recyclers only on floor or foundation!"
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

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
                    }
                }
            });
        }

        private static void UI_AddBlurPanel(CuiElementContainer container, string parentName, string color, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = color,
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }

        private static void UI_AddPanel(CuiElementContainer container, string parentName, string color, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
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
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }

        private static void UI_AddBoldText(CuiElementContainer container, string parentName, string color, string text, TextAnchor textAnchor, int fontSize, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = color,
                        Text = text,
                        Align = textAnchor,
                        FontSize = fontSize
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }

        private static void UI_AddBoldTextOutline(CuiElementContainer container, string parentName, string color, string text, TextAnchor textAnchor, int fontSize, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = color,
                        Text = text,
                        Align = textAnchor,
                        FontSize = fontSize
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    },
                    new CuiOutlineComponent
                    {
                        Color = "0 0 0 1",
                        Distance = "0.3 0.3",
                    }
                }
            });
        }

        private static void UI_AddText(CuiElementContainer container, string parentName, string color, string text, TextAnchor textAnchor, int fontSize, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = color,
                        Text = text,
                        Align = textAnchor,
                        FontSize = fontSize,
                        Font = "RobotoCondensed-Regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }

        private static void UI_AddButton(CuiElementContainer container, string parentName, string textColor, string text, TextAnchor textAnchor, int fontSize, string buttonColor, string command, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiButton
            {
                Text =
                {
                    Color = textColor,
                    Text = text,
                    Align = textAnchor,
                    FontSize = fontSize
                },
                Button =
                {
                    Color = buttonColor,
                    Material = "assets/content/ui/namefontmaterial.mat",
                    Command = command,
                },
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax,
                }
            }, parentName);
        }

        private static void UI_AddIcon(CuiElementContainer container, string parentName, string color, string path, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = color,
                        Sprite = path
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }



        private void UI_AddItemImage(CuiElementContainer container, string parentName, string shortname, ulong skin, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            if (skin != 0 && !config.overrideCustomSkins)
            {
                UI_AddImage(container, parentName, shortname, skin, anchorMin, anchorMax, offsetMin, offsetMax);
                return;
            }
            ItemDefinition itemDef = ItemManager.FindItemDefinition(shortname);
            if (itemDef == null)
            {
                UI_AddImage(container, parentName, shortname, skin, anchorMin, anchorMax, offsetMin, offsetMax);
                return;
            }
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiImageComponent
                    {
                        ItemId = itemDef.itemid,
                        SkinId = skin
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax
                    }
                }
            });
        }

        private void UI_AddImage(CuiElementContainer container, string parentName, string shortname, ulong skin, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Parent = parentName,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(shortname, skin)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax,
                    }
                }
            });
        }

        private static void UI_AddPickupButton(CuiElementContainer container, string parentName, string buttonColor, string color, string path, string command, string text, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiButton
            {
                Text =
                {
                    Color = color,
                    Text = text,
                    Align = TextAnchor.MiddleRight,
                    FontSize = 13
                },
                Button =
                {
                    Color = buttonColor,
                    Material = "assets/content/ui/namefontmaterial.mat",
                    Command = command,
                },
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax,
                }
            }, parentName, "CustomRecycle_PickupUI");
            UI_AddIcon(container, "CustomRecycle_PickupUI", color, path, "0 0", "0 0", "0 0", "20 20");
        }

        private void AddImage(string url, string shortname, ulong skin) => ImageLibrary?.CallHook("AddImage", url, shortname, skin);

        private string GetImage(string shortname, ulong skin) => ImageLibrary?.Call<string>("GetImage", shortname, skin);

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(config = new PluginConfig()
            {
                recyclerPermissions = new Dictionary<string, float>()
                {
                    { "customrecycle.vip", 4 },
                    { "customrecycle.admin", 1 }
                },
                amountPermissions = new Dictionary<string, int>()
                {
                    { "customrecycle.default", 1 },
                    { "customrecycle.vip", 3 },
                    { "customrecycle.admin", 1000 }
                },
                disabledRecipes = new List<string>()
                {
                    "axe.salvaged",
                    "box.wooden.large"
                },
                levels = new List<RecyclerLevel>()
                {
                    new RecyclerLevel
                    {
                        stackPercentage = 0.15f,
                        requiredItems = new List<UpgradeItem>()
                        {
                            new UpgradeItem()
                            {
                                shortname = "wood",
                                amount = 1000,
                            },
                            new UpgradeItem()
                            {
                                shortname = "stones",
                                amount = 1000,
                            }
                        }
                    },
                    new RecyclerLevel
                    {
                        stackPercentage = 0.2f,
                        requiredItems = new List<UpgradeItem>()
                        {
                            new UpgradeItem()
                            {
                                shortname = "wood",
                                amount = 3000,
                            },
                            new UpgradeItem()
                            {
                                shortname = "stones",
                                amount = 3000,
                            }
                        }
                    }
                },
                recycles = new Dictionary<string, RecycleConfig>()
                {
                    { "rifle.ak", new RecycleConfig() {
                        outputItems = new List<RecycleItem>()
                        {
                            new RecycleItem() { shortname = "techparts" },
                            new RecycleItem() { shortname = "scrap", minAmount = 30, maxAmount = 70 }
                        }
                    }},
                    { "metal.refined", new RecycleConfig() {
                        outputItems = new List<RecycleItem>()
                        {
                            new RecycleItem() { shortname = "metal.fragments", minAmount = 50, maxAmount = 100 }
                        }
                    }},
                    { "2483299228", new RecycleConfig() {
                        outputItems = new List<RecycleItem>()
                        {
                            new RecycleItem() { shortname = "coal", minAmount = 1, maxAmount = 1, skin = 2550800428, chance = 50, name = "Golden Jackhammer Body" },
                            new RecycleItem() { shortname = "coal", minAmount = 1, maxAmount = 1, skin = 2550800641, chance = 50, name = "Golden Jackhammer Drill" }
                        }
                    }}
                }
            }, true);
        }

        private class PluginConfig
        {
            [JsonProperty("Override Custom Skinned Items With Steam Icons (no URLs needed)")]
            public bool overrideCustomSkins = false;

            [JsonProperty("Recycler Speed (5 = Default)")]
            public float recyclerSpeed = 5;

            [JsonProperty("Recycler Speed Permissions")]
            public Dictionary<string, float> recyclerPermissions = new Dictionary<string, float>();

            [JsonProperty("Require Permission To Place")]
            public bool requirePermission = false;

            [JsonProperty("Placed Recycler Amount Permissions")]
            public Dictionary<string, int> amountPermissions = new Dictionary<string, int>();

            [JsonProperty("Recycler Item Name")]
            public string recyclerName = "Recycler";

            [JsonProperty("Allow Placing Only On Floor")]
            public bool onlyOnFloor = false;

            [JsonProperty("Allow Placing On Tug Boat")]
            public bool placeOnTugBoat = false;

            [JsonProperty("Enable Better Amount Accuracy (More Calculations)")]
            public bool betterAccuracy = true;

            [JsonProperty("Disabled Vanilla Recipes")]
            public List<string> disabledRecipes = new List<string>();

            [JsonProperty("Recycler Levels - Enable")]
            public bool recyclerLevels = true;

            [JsonProperty("Recycler Levels - Save Levels On Pickup In Name")]
            public bool saveLevels = false;

            [JsonProperty("Recycler Levels - Enable For No Owner")]
            public bool recyclerLevelsNoOwner = false;

            [JsonProperty("Recycler Levels - Money Plugin (0 - None, 1 - Economics, 2 - ServerRewards, 3 - ServerRewards)")]
            public int moneyPlugin = 0;

            [JsonProperty("Recycler Levels - Money Plugin Currency (If ShoppyStock Is Used)")]
            public string currency = "rp";

            [JsonProperty("Recycler Levels")]
            public List<RecyclerLevel> levels = new List<RecyclerLevel>();

            [JsonProperty("Custom Recyclables - Show Level Bonus")]
            public bool showCustomBonus = true;

            [JsonProperty("Custom Recyclables - Allow Only In Placed Recyclers")]
            public bool customOnlyInPlaced = false;

            [JsonProperty("Custom Recyclables (Shortname or SkinID)")]
            public Dictionary<string, RecycleConfig> recycles = new Dictionary<string, RecycleConfig>();

        }
        private class RecyclerLevel
        {
            [JsonProperty("Recycler Stack Percentage Per Tick")]
            public float stackPercentage = 0.1f;

            [JsonProperty("Custom Recycle Chance Multiplier")]
            public float chanceMultiplier = 1;

            [JsonProperty("Next Level Currency Cost (0 to disable)")]
            public int currencyCost = 1000;

            [JsonProperty("Required For Next Level")]
            public List<UpgradeItem> requiredItems = new List<UpgradeItem>();
        }

        private class UpgradeItem
        {
            [JsonProperty("Item Shortname")]
            public string shortname;

            [JsonProperty("Item Skin")]
            public ulong skin = 0;

            [JsonProperty("Item Amount")]
            public int amount = 1;

            [JsonProperty("Icon URL")]
            public string url = "";
        }

        private class RecycleConfig
        {
            [JsonProperty("Give Default Output")]
            public bool defaultOutput = true;

            [JsonProperty("Custom Output Items")]
            public List<RecycleItem> outputItems = new List<RecycleItem>();
        }

        private class RecycleItem
        {
            [JsonProperty("Item Shortname")]
            public string shortname;

            [JsonProperty("Item Chance (0-100)")]
            public float chance = 100;

            [JsonProperty("Minimum Item Amount")]
            public int minAmount = 1;

            [JsonProperty("Maximum Item Amount")]
            public int maxAmount = 2;

            [JsonProperty("Item Skin")]
            public ulong skin = 0;

            [JsonProperty("Item Display Name")]
            public string name = string.Empty;
        }

        private static PluginData data;

        public class PluginData
        {
            [JsonProperty("Recycler Levels")]
            public Dictionary<ulong, int> recyclerLevels = new Dictionary<ulong, int>();
        }

        private void LoadData()
        {
            data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(this.Name);
            timer.Every(Core.Random.Range(500, 700), SaveData);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(this.Name, data);
    }
}