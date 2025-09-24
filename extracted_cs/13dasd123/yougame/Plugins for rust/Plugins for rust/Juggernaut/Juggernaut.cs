// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Джаггернаут", "k1lly0u", "0.1.9", ResourceId = 0)]
    internal class Juggernaut : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;
        [PluginReference] Plugin LustyMap;
        [PluginReference] Plugin EventManager;
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;

        static Juggernaut ins;

        StoredData storedData;
        private DynamicConfigFile data;

        private BasePlayer juggernaut = null;
        private SphereEntity finalSphere = null;
        private Vector3 endPos = Vector3.zero;

        private Timer nextMatch;
        private Timer startMatch;
        private Timer eventTimer;
        private Timer broadcastTimer;
        private Timer openMessage;
        private Timer uiTimer;
        private double eventEnd;
        private double nextTrigger;

        private string juggernautIcon;

        private bool isOpen;
        private bool hasStarted;
        private bool initialized;
        private bool hasDestinations;

        private List<ulong> optedIn = new List<ulong>();
        private Dictionary<ulong, Destinations> destinationCreator = new Dictionary<ulong, Destinations>();

        private const string sphereEnt = "assets/prefabs/visualization/sphere.prefab";
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("juggernaut.canbe", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("juggernaut_data");
        }
        void OnServerInitialized()
        {
            ins = this;
            LoadVariables();
            LoadData();

            if (storedData.destinations.Count > 0)
            {
                hasDestinations = true;
                StartEventTimer();
            }
            else PrintWarning("Назначений нет! Не удалось запустить ивент");

            initialized = true;
        }
        void Unload()
        {
            if (eventTimer != null)
                eventTimer.Destroy();
            if (nextMatch != null)
                nextMatch.Destroy();
            if (startMatch != null)
                startMatch.Destroy();
            if (openMessage != null)
                openMessage.Destroy();
            if (uiTimer != null)
                uiTimer.Destroy();
            if (broadcastTimer != null)
                broadcastTimer.Destroy();

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
            CuiHelper.DestroyUi(juggernaut, Compass);

            if (juggernaut != null && hasStarted)
            {
                UnlockInventory(juggernaut);
                juggernaut.inventory.Strip();
                DestroyEvent();
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => OnPlayerInit(player));
                return;
            }
            if (storedData.unrestoredPlayers.ContainsKey(player.userID))
                TryRestorePlayer(player);
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (optedIn.Contains(player.userID))
                optedIn.Remove(player.userID);
            if (player == juggernaut && hasStarted)
            {                
                UnlockInventory(player);
                player.inventory.Strip();
                DestroyEvent();
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            var victim = entity.ToPlayer();
            var attacker = info.InitiatorPlayer;
            if (victim != null && victim == juggernaut)
            {
                if (info.Initiator != null && info.Initiator.ShortPrefabName.Equals("beartrap") && configData.JuggernautSettings.DisableBeartrapDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (info.WeaponPrefab != null && info.WeaponPrefab.ShortPrefabName.Equals("landmine") && configData.JuggernautSettings.DisableLandmineDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (info.damageTypes.GetMajorityDamageType() == DamageType.Fall && configData.JuggernautSettings.DisableFallDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (attacker != null)
                {
                    if (IsFriend(victim.userID, attacker.userID))
                    {
                        info.damageTypes.ScaleAll(0);
                        SendReply(attacker, msg("<color=white>Вы не можете навредить своим друзьям, когда они являются Джаггернаутом!</color>", attacker.UserIDString));
                        return;
                    }
                    if (IsClanmate(victim.userID, attacker.userID))
                    {
                        info.damageTypes.ScaleAll(0);
                        SendReply(attacker, msg("<color=white>Вы не можете навредить своим соклановцем, когда они являются Джаггернаутом!</color>", attacker.UserIDString));
                        return;
                    }                    
                }                
                info.damageTypes.ScaleAll(configData.JuggernautSettings.DefenseDamageModifier);
                return;
            }
            
            if (attacker == null) return;
            if (attacker == juggernaut)
            {
                if (!configData.JuggernautSettings.CanDamageStructures && (entity is BuildingBlock || entity is SimpleBuildingBlock))
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }

                info.damageTypes.ScaleAll(configData.JuggernautSettings.AttackDamageModifier);
            }
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized || entity == null) return;
            var player = entity as BasePlayer;
            if (player != null)
            {
                TryRestorePlayer(player);
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            var victim = entity.ToPlayer();
            if (victim == null) return;
            if (victim == juggernaut)
            {
                if (eventTimer != null)
                    eventTimer.Destroy();
                if (uiTimer != null)
                    uiTimer.Destroy();
                if (broadcastTimer != null)
                    broadcastTimer.Destroy();

                foreach (var player in BasePlayer.activePlayerList)
                    CuiHelper.DestroyUi(player, Main);
                CuiHelper.DestroyUi(juggernaut, Compass);

                if (!configData.Prizes.UseJuggernautInventory)
                    victim.inventory.Strip();

                var attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    if (victim == info.InitiatorPlayer)
                    {
                        victim.inventory.Strip();
                        PrintToChat(msg("<color=white>Джаггернаут совершил самоубийство! Ивент завершён и награда удалена</color>"));
                    }
                    else if (!IsFriend(victim.userID, attacker.userID) && !IsClanmate(victim.userID, attacker.userID))
                    {
                        PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Джаггернаут был убить, теперь можно забрать выигрыш с него!</color>"));
                        if (configData.Prizes.MoneyRewardAmount > 0)
                        {
                            if (configData.Prizes.UseEconomics)
                                Economics?.Call("Deposit", attacker.userID, (double)configData.Prizes.MoneyRewardAmount);
                            if (configData.Prizes.UseServerRewards)
                                ServerRewards?.Call("AddPoints", attacker.userID, configData.Prizes.MoneyRewardAmount);
                        }
                    }
                }
                else PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Джаггернаут был убить, теперь можно забрать выигрыш с него!</color>"));

                UnlockInventory(victim);
                juggernaut = null;
                UnityEngine.Object.Destroy(finalSphere.GetComponent<FinalSphere>());
            }
        }
        object CanNetworkTo(BaseEntity entity, BasePlayer target)
        {
            if (finalSphere != null && entity == finalSphere && juggernaut != null)
            {
                if (target != juggernaut)
                    return false;
            }
            return null;
        }       
        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (!hasStarted || arg.Connection == null || arg.Connection.player == null || arg.cmd.Name != "kill") return null;
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return null;
            if (player == juggernaut)
            {
                SendReply(player, msg("<color=white>Вы не можете совершить самоубийство, пока вы являетесь Джаггернаутом!</color>", player.UserIDString));
                return true;
            }      
            return null;
        }
        #endregion

        #region Functions
        void LockInventory(BasePlayer player)
        {
            if (!player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, true);
            if (!player.inventory.containerBelt.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, true);
            if (!player.inventory.containerWear.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, true);
        }
        void UnlockInventory(BasePlayer player)
        {
            if (player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, false);
            if (player.inventory.containerBelt.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, false);
            if (player.inventory.containerWear.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, false);
        }
        void StartEventTimer()
        {
            optedIn.Clear();
            UnsubscribeHooks();
            nextMatch = timer.Once(configData.EventTimers.TimeBetweenEvents, CheckConditions);
            nextTrigger = GrabCurrentTime() + configData.EventTimers.TimeBetweenEvents;
        }   
        void CheckConditions()
        {
            if (BasePlayer.activePlayerList.Count >= configData.EventConditions.MinimumPlayersToOpen)
                OpenEvent();
        }  
        void OpenEvent()
        {
            if (string.IsNullOrEmpty(juggernautIcon) && configData.UISettings.ShowUITimer && !string.IsNullOrEmpty(configData.UISettings.IconUrl))
                Add(configData.UISettings.IconUrl);

            isOpen = true;
            startMatch = timer.Once(configData.EventTimers.TimeBeforeSelection, StartEvent);
            openMessage = timer.Repeat(45, 0, () => PrintToChat(msg("<color=orange>[Джаггернаут] </color><color=white>: Ивент Джаггернаут открыт! Введите </color><color=#ffd479>/jug join</color><color=white> чтобы записаться на ивент, и возможно стать Джаггернаутом</color>")));
            nextTrigger = configData.EventTimers.TimeBeforeSelection + GrabCurrentTime();
            PrintToChat(msg("<color=orange>[Джаггернаут] </color><color=white>: Ивент Джаггернаут открыт! Введите </color><color=#ffd479>/jug join</color><color=white> чтобы записаться на ивент, и возможно стать Джаггернаутом</color>"));
        }
        void StartEvent()
        {
            isOpen = false;
            if (openMessage != null) openMessage.Destroy();

            if (optedIn.Count == 0)
            {
                PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Ни один игрок не записался на ивент. Ивент отменен!</color>"));
                StartEventTimer();
                return;
            }
            var minEntrants = (int)Math.Round(BasePlayer.activePlayerList.Count * configData.EventConditions.PercentageEntrantsToStart, 0);
            if (optedIn.Count < minEntrants)
            {
                PrintToChat(string.Format(msg("notEnoughEntrants"), $"{configData.EventConditions.PercentageEntrantsToStart * 100}%"));
                StartEventTimer();
                return;
            }

            SubscribeHooks();

            ulong playerId = optedIn.GetRandom();
            var player = BasePlayer.activePlayerList.FirstOrDefault(x => x.userID == playerId);
            if (player == null) return;
            var destinations = storedData.destinations.GetRandom();

            endPos = new Vector3(destinations.x2, destinations.y2, destinations.z2);

            StorePlayer(player);
            player.inventory.Strip();

            MovePosition(player, new Vector3(destinations.x1, destinations.y1, destinations.z1));
            PrepareJuggernaut(player);
            CreateSphere();

            eventEnd = configData.EventTimers.TimeToCompleteJourney + GrabCurrentTime();
            nextTrigger = configData.EventTimers.TimeToCompleteJourney + GrabCurrentTime();

            hasStarted = true;
        }
        void PrepareJuggernaut(BasePlayer player)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => PrepareJuggernaut(player));
                return;
            }
            juggernaut = player;

            if (configData.JuggernautSettings.StartWithFullMetabolism)
            {
                player.metabolism.Reset();
                player.health = player.MaxHealth();
                player.metabolism.hydration.value = player.metabolism.hydration.max;
                player.metabolism.calories.value = player.metabolism.calories.max;
                player.metabolism.SendChangesToClient();
            }
            GiveJuggernautGear();
            PrintToChat(string.Format(msg("<color=orange>[Джаггернаут] </color><color=white>: Джаггернаут приближается к </color><color=#ffd479>{0}</color><color=white>! Найдите и убейте его до того, как он доберется до места назначения!</color>"), $"X:{Math.Round(endPos.x, 1)}, Z:{Math.Round(endPos.z, 1)}"));
            SendReply(player, string.Format(msg("<color=orange>[Джаггернаут]</color><color=white> : Вы должны дойти до </color><color=#ffd479>{0}</color><color=white> и войти в белую сферу, чтобы выиграть ивент!</color>", player.UserIDString), $"X:{Math.Round(endPos.x, 1)}, Z:{Math.Round(endPos.z, 1)}"));

            eventTimer = timer.In(configData.EventTimers.TimeToCompleteJourney, EventCancel);
            if (configData.UISettings.ShowUITimer)
                RefreshAllUI();
            
            if (configData.LustyMapIntegration.ShowDestinationIcon)
            {
                if (configData.LustyMapIntegration.DestinationAmount > 1 && storedData.destinations.Count >= configData.LustyMapIntegration.DestinationAmount)
                {
                    var destinations = new List<Destinations>(storedData.destinations);
                    var randomNumber = UnityEngine.Random.Range(1, configData.LustyMapIntegration.DestinationAmount);
                    for (int i = 0; i < configData.LustyMapIntegration.DestinationAmount; i++)
                    {
                        if (i == randomNumber)
                        {
                            AddMapMarker(endPos.x, endPos.z, $"{msg("possibleDest")} {i + 1}");
                        }
                        else
                        {
                            var destination = destinations.GetRandom();
                            destinations.Remove(destination);
                            AddMapMarker(destination.x2, destination.z2, $"{msg("possibleDest")} {i + 1}");
                        }                        
                    }
                }
                else AddMapMarker(endPos.x, endPos.z, msg("destination"));
            }  
            if (configData.GameOptions.BroadcastJuggernautPositionEvery > 0)
            {
                broadcastTimer = timer.Repeat(configData.GameOptions.BroadcastJuggernautPositionEvery, 0, () =>
                {
                    if (juggernaut != null)
                    {
                        if (configData.GameOptions.BroadcastToLustyMap)
                        {
                            AddMapMarker(juggernaut.transform.position.x, juggernaut.transform.position.z, msg("juggernaut"));
                            timer.In(configData.GameOptions.SecondsToDisplayOnLM, () => RemoveMapMarker(msg("juggernaut")));
                        }
                        else PrintToChat(string.Format(msg("<color=white>Джаггернаута видели в последний раз на координатах</color> <color=#ffd479>X:{0}, Z:{1}</color>"), Math.Round(juggernaut.transform.position.x, 1), Math.Round(juggernaut.transform.position.z, 1)));
                    }
                });
            }          
        }  
        void EventCancel()
        {           
            PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Ивент окончен, потому что Джаггернаут не успел добраться до точки назначения</color>"));
            TryRestorePlayer(juggernaut);
            DestroyEvent();
        }
        void EventWin()
        {            
            PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Джаггернаут добрался до точки назначения и выиграл этот ивент!"));

            if (configData.Prizes.UseJuggernautInventory)
            {
                var remainingItems = new List<EventInvItem>();
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerBelt));
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerMain));
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerWear));

                if (!storedData.winnerRewards.ContainsKey(juggernaut.userID))
                    storedData.winnerRewards.Add(juggernaut.userID, remainingItems);
                else storedData.winnerRewards[juggernaut.userID] = remainingItems;
            }
            if (configData.Prizes.UseServerRewards || configData.Prizes.UseEconomics)
            {
                if (!storedData.winnerMoney.ContainsKey(juggernaut.userID))
                    storedData.winnerMoney.Add(juggernaut.userID, new List<Points>());
                storedData.winnerMoney[juggernaut.userID].Add(new Points
                {
                    isRp = configData.Prizes.UseServerRewards,
                    amount = configData.Prizes.MoneyRewardAmount
                });
            }

            TryRestorePlayer(juggernaut);
            DestroyEvent();
        }  
        void DestroyEvent()
        {
            hasStarted = false;

            if (eventTimer != null)
                eventTimer.Destroy();
            if (uiTimer != null)
                uiTimer.Destroy();
            if (broadcastTimer != null)
                broadcastTimer.Destroy();

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
            CuiHelper.DestroyUi(juggernaut, Compass);

            juggernaut = null;
            UnityEngine.Object.Destroy(finalSphere.GetComponent<FinalSphere>());

            SaveData();

            if (configData.GameOptions.BroadcastJuggernautPositionEvery > 0 && configData.GameOptions.BroadcastToLustyMap)
                RemoveMapMarker(msg("juggernaut"));
            if (configData.LustyMapIntegration.ShowDestinationIcon)
            {
                if (configData.LustyMapIntegration.DestinationAmount > 1)
                {                    
                    for (int i = 0; i < configData.LustyMapIntegration.DestinationAmount; i++)
                    {
                        RemoveMapMarker($"{msg("possibleDest")} {i + 1}");                        
                    }
                }
                else RemoveMapMarker(msg("destination"));
            }
            StartEventTimer();
        }
        void TryRestorePlayer(BasePlayer player)
        {
            player.inventory.Strip();
            UnlockInventory(player);
            PlayerData data;
            if (storedData.unrestoredPlayers.TryGetValue(player.userID, out data))
            {
                MovePosition(player, new Vector3(data.x, data.y, data.z));
                RestorePlayer(player, data);                
            }
        }
        void GiveJuggernautGear()
        {
            foreach (var entry in configData.JuggernautSettings.Inventory)
            {
                Item item = ItemManager.CreateByName(entry.Shortname, entry.Amount, entry.SkinID);
                item.MoveToContainer(entry.Container == "wear" ? juggernaut.inventory.containerWear : entry.Container == "belt" ? juggernaut.inventory.containerBelt : juggernaut.inventory.containerMain);
            }
            LockInventory(juggernaut);
        }
        void UnsubscribeHooks()
        {
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnEntityDeath));
            Unsubscribe(nameof(CanNetworkTo));
        }
        void SubscribeHooks()
        {
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnEntityDeath));
            Subscribe(nameof(CanNetworkTo));
        }
        double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region Teleportation Management  
        private void MovePosition(BasePlayer player, Vector3 destination)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }
        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        #endregion

        #region Player Saving and Restoration
        private void StorePlayer(BasePlayer player)
        {

            PlayerData data = new PlayerData
            {
                inventory = new Dictionary<string, List<EventInvItem>>
                    {
                        {"belt", GetItems(player.inventory.containerBelt).ToList() },
                        {"main", GetItems(player.inventory.containerMain).ToList() },
                        {"wear", GetItems(player.inventory.containerWear).ToList() }
                    },
                health = player.Health(),
                calories = player.metabolism.calories.value,
                hydration = player.metabolism.hydration.value,
                x = player.transform.position.x,
                y = player.transform.position.y,
                z = player.transform.position.z
            };
            if (!storedData.unrestoredPlayers.ContainsKey(player.userID))
                storedData.unrestoredPlayers.Add(player.userID, data);
            else storedData.unrestoredPlayers[player.userID] = data;
            SaveData();         
        }
        private IEnumerable<EventInvItem> GetItems(ItemContainer container)
        {
            return container.itemList.Select(item => new EventInvItem
            {
                itemid = item.info.itemid,
                amount = item.amount,
                ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                skin = item.skin,
                condition = item.condition,
                instanceData = item.instanceData ?? null,
                contents = item.contents?.itemList.Select(item1 => new EventInvItem
                {
                    itemid = item1.info.itemid,
                    amount = item1.amount,
                    condition = item1.condition
                }).ToArray()
            });
        }
        private void RestorePlayer(BasePlayer player, PlayerData data)
        {
            if (player == null) return;
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => RestorePlayer(player, data));
                return;
            }
            if (data == null) return;
            RestorePlayerStats(player, data.health, data.hydration, data.calories);
            RestoreItems(player, "wear", data.inventory["wear"]);
            RestoreItems(player, "belt", data.inventory["belt"]);
            RestoreItems(player, "main", data.inventory["main"]);
            if (storedData.unrestoredPlayers.ContainsKey(player.userID))
            {
                storedData.unrestoredPlayers.Remove(player.userID);
                SaveData();
            }
        }
        private void RestorePlayerStats(BasePlayer player, float health, float hydration, float calories)
        {
            player.metabolism.Reset();
            player.health = health;
            player.metabolism.calories.value = calories;
            player.metabolism.hydration.value = hydration;
            player.metabolism.bleeding.value = 0;
            player.metabolism.SendChangesToClient();
        }
        private void SendPlayerHome(BasePlayer player, Vector3 position) => MovePosition(player, position); 
        private void RestoreItems(BasePlayer player, string targetContainer, List<EventInvItem> items)
        {
            ItemContainer container = targetContainer == "belt" ? player.inventory.containerBelt : targetContainer == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

            for (int i = 0; i < container.capacity; i++)
            {
                var existingItem = container.GetSlot(i);
                if (existingItem != null)
                {
                    existingItem.RemoveFromContainer();
                    existingItem.Remove(0f);
                }
                if (items.Count > i)
                {
                    var itemData = items[i];
                    var item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                    item.condition = itemData.condition;
                    if (itemData.instanceData != null)
                        item.instanceData = itemData.instanceData;

                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (!string.IsNullOrEmpty(itemData.ammotype))
                            weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                        weapon.primaryMagazine.contents = itemData.ammo;
                    }
                    if (itemData.contents != null)
                    {
                        foreach (var contentData in itemData.contents)
                        {
                            var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                            if (newContent != null)
                            {
                                newContent.condition = contentData.condition;
                                newContent.MoveToContainer(item.contents);
                            }
                        }
                    }
                    item.position = i;
                    item.SetParent(container);
                }
            }           
        }
        #endregion

        #region Sphere Creation
        private void CreateSphere()
        {
            finalSphere = (SphereEntity)GameManager.server.CreateEntity(sphereEnt, endPos, new Quaternion(), true);
            finalSphere.currentRadius = 5;
            finalSphere.lerpSpeed = 0;
            finalSphere.enableSaving = false;
            finalSphere.Spawn();

            finalSphere.gameObject.AddComponent<FinalSphere>();
        }
        //[ChatCommand("jugs")]
        //void cmdjugs(BasePlayer player, string command, string[] args)
        //{
        //    OpenEvent();
        //    optedIn.Add(player.userID);
        //    StartEvent();
        //}
        class FinalSphere : MonoBehaviour
        {
            public BaseEntity entity;
            void Awake()
            {
                entity = GetComponent<BaseEntity>();                
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = $"Juggernaut Final Zone";
                enabled = false;

                var collider = entity.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.5f;
            }
            void OnDestroy()
            {
                if (!entity.IsDestroyed)
                    entity.Kill();               
            }
            void OnTriggerEnter(Collider obj)
            {
                if (obj.gameObject.layer == (int)Layer.Player_Server)
                {
                    var player = obj?.GetComponentInParent<BasePlayer>();
                    if (player != null)
                    {
                        if (player == ins.juggernaut)
                        {
                            ins.EventWin();
                        }
                    }
                }
            }             
        }
        #endregion

        #region Hooks
        #region Internal Checks
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends) return false;
            return (bool)Friends?.Call("IsFriend", playerID, friendID);
        }
        private bool IsPlaying(BasePlayer player)
        {
            if (!EventManager) return false;
            return (bool)EventManager.Call("isPlaying", player);
        }
        private void AddMapMarker(float x, float z, string markerName) => LustyMap?.Call("AddMarker", x, z, markerName, configData.UISettings.IconUrl);
        private void RemoveMapMarker(string markerName) => LustyMap?.Call("RemoveMarker", markerName);
        #endregion

        #region External Checks
        object CanTrade(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("Вы не можете обмениваться, пока вы являетесь Джаггернаутом!", player.UserIDString);
            return null;
        }
        object canRemove(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("Вы не можете пользоваться ремувом, пока вы являетесь Джаггернаутом!", player.UserIDString);
            return null;
        }
        object CanTeleport(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("Вы не можете телепортироваться, пока вы являетесь Джаггернаутом!", player.UserIDString);
            return null;
        }
        object canShop(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("Вы не можете зайти в магазин, пока вы являетесь Джаггернаутом!", player.UserIDString);
            return null;
        }
        #endregion
        #endregion

        #region UI
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return NewElement;
            }
            static public void AddImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax, float fadeOut = 0f)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Creation
        private const string Main = "JuggernautMain";
        private const string Compass = "JuggernautCompass";
        private void CreateTimerUI(BasePlayer player)
        {
            var MainCont = UI.CreateElementContainer(Main, UI.Color(configData.UISettings.UIBackgroundColor, configData.UISettings.UIOpacity), $"{configData.UISettings.TimerPosition.XPosition} {configData.UISettings.TimerPosition.YPosition}", $"{configData.UISettings.TimerPosition.XPosition + configData.UISettings.TimerPosition.XDimension} {configData.UISettings.TimerPosition.YPosition + configData.UISettings.TimerPosition.YDimension}");

            if (!string.IsNullOrEmpty(juggernautIcon))
                UI.AddImage(ref MainCont, Main, juggernautIcon, "0.01 0.05", "0.425 0.95");
            UI.CreateLabel(ref MainCont, Main, "", GetFormatTime(eventEnd), 15, "0.5 0", "1 1", TextAnchor.MiddleLeft);

            CuiHelper.DestroyUi(player, Main);
            CuiHelper.AddUi(player, MainCont);
        }
        private string GetFormatTime(double timeLeft)
        {
            var time = timeLeft - GrabCurrentTime();
            double minutes = Math.Floor((double)(time / 60));
            time -= (int)(minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, time);
        }
        private void RefreshAllUI()
        {
            uiTimer = timer.Repeat(1, (int)(eventEnd - GrabCurrentTime()) - 1, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                    CreateTimerUI(player);
                if (configData.UISettings.ShowCoordsToJuggernaut)
                    ShowCompass();
            });
        }
        private void ShowCompass()
        {
            var MainCont = UI.CreateElementContainer(Compass, UI.Color(configData.UISettings.UIBackgroundColor, configData.UISettings.UIOpacity), $"{configData.UISettings.CompassPosition.XPosition} {configData.UISettings.CompassPosition.YPosition}", $"{configData.UISettings.CompassPosition.XPosition + configData.UISettings.CompassPosition.XDimension} {configData.UISettings.CompassPosition.YPosition + configData.UISettings.CompassPosition.YDimension}");
            UI.CreateLabel(ref MainCont, Compass, "", $"{msg("target", juggernaut.UserIDString)} X: {Math.Round(endPos.x, 1)}, Z: {Math.Round(endPos.z, 1)}", 10, "0.05 0.5", "0.95 0.95", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref MainCont, Compass, "", $"{msg("current", juggernaut.UserIDString)} X: {Math.Round(juggernaut.transform.position.x, 1)}, Z: {Math.Round(juggernaut.transform.position.z, 1)}", 10, "0.05 0.05", "0.95 0.5", TextAnchor.MiddleLeft);
            CuiHelper.DestroyUi(juggernaut, Compass);
            CuiHelper.AddUi(juggernaut, MainCont);
        }
        #endregion

        #region Imagery
        private MemoryStream stream = new MemoryStream();
        private WWW info;
        public void Add(string url)
        {
            info = new WWW(url);
            TryDownloadImage();
        }
        void TryDownloadImage()
        {
            if (!info.isDone)
            {
                timer.In(1, TryDownloadImage);
                return;
            }
            if (!string.IsNullOrEmpty(info.error))
            {
                PrintError(string.Format("Failed to load the Juggernaut icon! Error: {0}", info.error));
                return;
            }
            else
            {
                stream.Position = 0;
                stream.SetLength(0);
                stream.Write(info.bytes, 0, info.bytes.Length);
                juggernautIcon = FileStorage.server.Store(stream, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                stream = null;
            }
        }
        #endregion

        #region Commands
        [ChatCommand("jug")]
        void cmdJuggernaut(BasePlayer player, string command, string[] args)
        {            
            if (args.Length == 0)
            {
                SendReply(player, $"<size=22><color=orange>{Title}</color></size>");
                SendReply(player, msg("<color=#ffd479>/jug info</color><color=white> - Показывать время, оставшееся до: начало ивента, окончание ивента в зависимости от текущего состояния ивента</color>", player.UserIDString));
                SendReply(player, msg("<color=#ffd479>/jug join</color><color=white> - Позволяет игроку записаться на жеребьевку, чтобы попробовать стать Джаггернаутом</color>", player.UserIDString));
                SendReply(player, msg("<color=#ffd479>/jug leave</color><color=white> - Удаляет игрока из жеребьевки, на становление Джаггернаутом</color>", player.UserIDString));
                SendReply(player, msg("<color=#ffd479>/jug claim</color><color=white> - Используется Джаггернаутом, выигравшим ивент (гарантировать наличие пустых ячеек в инвентаре при подаче заявки!)</color>", player.UserIDString));
                SendReply(player, msg("<color=#ffd479>/jug restore</color><color=white> - Используется для восстановления Джаггернаута после ивента, только если что-то случится с автоматизацией</color>", player.UserIDString));
                if (player.IsAdmin)
                {
                    SendReply(player, msg("<color=#ffd479>/jug open</color><color=white> - Принудительное открытие ивента</color>", player.UserIDString));
                    SendReply(player, msg("<color=#ffd479>/jug start</color><color=white> - Принудительное начало ивента</color>", player.UserIDString));
                    SendReply(player, msg("<color=#ffd479>/jug cancel</color><color=white> - Принудительное отключение ивента</color>", player.UserIDString));
                    SendReply(player, msg("<color=#ffd479>/jug destination</color><color=white> - Используется для создания адресатов для Джаггернаута</color>", player.UserIDString));
                }
                return;
            }
            switch (args[0].ToLower())
            {
                case "info":
                    string time = GetFormatTime(nextTrigger);
                    if (hasStarted)
                        SendReply(player, string.Format(msg($"<color=white>Ивент закончится через : </color><color=#ffd479>{time}</color>", player.UserIDString), time));
                    else if (isOpen)
                        SendReply(player, string.Format(msg($"<color=white>Ивент начнется через : </color><color=#ffd479>{time}</color>", player.UserIDString), time));
                    else SendReply(player, string.Format(msg($"<color=white>Следующий ивент начнется через : </color><color=#ffd479>{time}</color>", player.UserIDString), time));                    
                    return;
                case "join":
                    if (!permission.UserHasPermission(player.UserIDString, "juggernaut.canbe"))
                    {
                        SendReply(player, msg("noPerms", player.UserIDString));
                        return;
                    }
                    if (!isOpen)
                    {
                        SendReply(player, msg("<color=white>На данный момент нет открытого ивента</color>", player.UserIDString));
                        return;
                    }
                    if (IsPlaying(player))
                    {
                        SendReply(player, msg("<color=white>Вы не можете присоединиться к ивенту Джаггернаут, во время игры на Event Manager</color>", player.UserIDString));
                        return;
                    }
                    if (!optedIn.Contains(player.userID))
                    {
                        optedIn.Add(player.userID);
                        SendReply(player, msg("<color=white>Вы записались на ивент, чтобы быть Джаггернаутом. Вы можете выйти в любое время, набрав </color><color=#ffd479>/jug leave</color>", player.UserIDString));
                        PrintToChat(string.Format(msg("<color=#ffd479>{0}</color> <color=white>зарегистрировался на ивент Джаггернаут!</color> <color=#ffd479>({1} игроков зарегестрировано)</color>"), player.displayName, optedIn.Count));
                    }
                    return;
                case "leave":
                    if (!isOpen)
                    {
                        SendReply(player, msg("<color=white>На данный момент нет открытого ивента</color>", player.UserIDString));
                        return;
                    }
                    if (optedIn.Contains(player.userID))
                        optedIn.Remove(player.userID);
                    SendReply(player, msg("<color=white>Вы удалили себя из жеребьёвки, чтобы стать Джаггернаутом</color>", player.UserIDString));
                    return;
                case "claim":
                    if (storedData.winnerRewards.ContainsKey(player.userID))
                    {
                        foreach(var prize in storedData.winnerRewards[player.userID])
                        {
                            Item item = ItemManager.CreateByItemID(prize.itemid, prize.amount, prize.skin);
                            item.condition = prize.condition;                            
                            if (prize.instanceData != null)
                                item.instanceData = prize.instanceData;

                            var weapon = item.GetHeldEntity() as BaseProjectile;
                            if (weapon != null)
                            {
                                if (!string.IsNullOrEmpty(prize.ammotype))
                                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(prize.ammotype);
                                weapon.primaryMagazine.contents = prize.ammo;
                            }
                            if (prize.contents != null)
                            {
                                foreach (var contentData in prize.contents)
                                {
                                    var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                                    if (newContent != null)
                                    {
                                        newContent.condition = contentData.condition;
                                        newContent.MoveToContainer(item.contents);
                                    }
                                }
                            }
                            player.GiveItem(item, BaseEntity.GiveItemReason.Generic);
                        }
                        SendReply(player, msg("<color=white>Вы выиграли приз</color>", player.UserIDString));
                        storedData.winnerRewards.Remove(player.userID);
                        SaveData();
                    }
                    else if (storedData.winnerMoney.ContainsKey(player.userID))
                    {
                        foreach(var prize in storedData.winnerMoney[player.userID])
                        {
                            if (prize.isRp)                            
                                ServerRewards?.Call("AddPoints", player.userID, prize.amount);
                            else Economics?.Call("Deposit", player.userID, (double)prize.amount);
                        }
                        SendReply(player, msg("<color=white>Вы выиграли приз</color>", player.UserIDString));
                        storedData.winnerMoney.Remove(player.userID);
                        SaveData();
                    }
                    else SendReply(player, msg("<color=white>У вас нет призов</color>", player.UserIDString));
                    return;
                case "restore":
                    if (player == juggernaut)
                    {
                        SendReply(player, msg("<color=white>Вы не можете восстановиться, будучи Джаггернаутом</color>", player.UserIDString));
                        return;
                    }
                    if (storedData.unrestoredPlayers.ContainsKey(player.userID))
                    {
                        TryRestorePlayer(player);
                        return;
                    }
                    else SendReply(player, msg("<color=white>У вас нет сохраненных данных восстановления</color>", player.UserIDString));
                    return;
                case "open":
                    if (player.IsAdmin)
                    {
                        if (isOpen)
                        {
                            SendReply(player, msg("<color=white>Ивент уже открыт</color>", player.UserIDString));
                            return;
                        }
                        if (!hasDestinations)
                        {
                            SendReply(player, msg("<color=white>Не настроены адресаты! Не удалось начать ивент</color>", player.UserIDString));
                            return;
                        }
                        if (hasStarted)
                        {
                            SendReply(player, msg("<color=white>Ивент уже начался</color>", player.UserIDString));
                            return;
                        }
                        if (nextMatch != null)
                            nextMatch.Destroy();
                        OpenEvent();
                    }
                    return;
                case "start":
                    if (player.IsAdmin)
                    {
                        if (!isOpen)
                        {
                            SendReply(player, msg("isntOpen", player.UserIDString));
                            return;
                        }
                        if (hasStarted)
                        {
                            SendReply(player, msg("<color=white>Ивент уже начался</color>", player.UserIDString));
                            return;
                        }                        
                        if (startMatch != null)
                            startMatch.Destroy();
                        if (openMessage != null)
                            openMessage.Destroy();
                        StartEvent();
                    }
                    return;
                case "cancel":
                    if (player.IsAdmin)
                    {
                        if (!isOpen && !hasStarted)
                        {
                            SendReply(player, msg("notStarted", player.UserIDString));
                            return;
                        }
                        if (isOpen)
                        {
                            if (startMatch != null)
                                startMatch.Destroy();
                            if (openMessage != null)
                                openMessage.Destroy();
                            isOpen = false;
                            PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Админ отменил ивент!</color>"));
                            StartEventTimer();
                            return;
                        }  
                        if (hasStarted)
                        {                            
                            PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Админ отменил ивент!</color>"));
                            TryRestorePlayer(juggernaut);
                            DestroyEvent();
                            return;
                        }                      
                    }
                    return;
                case "destination":
                    if (player.IsAdmin)
                    {
                        if (!destinationCreator.ContainsKey(player.userID))
                        {
                            destinationCreator.Add(player.userID, new Destinations { x1 = player.transform.position.x, y1 = player.transform.position.y, z1 = player.transform.position.z });
                            SendReply(player, msg("<color=white>Вы начали создавать новый пункт назначения, начиная с текущей позиции. Пройдите на позицию, которую вы хотите сделать конечной, и введите эту команду еще раз</color>",player.UserIDString));
                            return;
                        }
                        else
                        {
                            Destinations data = destinationCreator[player.userID];
                            data.x2 = player.transform.position.x;
                            data.y2 = player.transform.position.y;
                            data.z2 = player.transform.position.z;
                            storedData.destinations.Add(data);
                            destinationCreator.Remove(player.userID);
                            SaveData();
                            if (!hasDestinations)
                                hasDestinations = true;
                            SendReply(player, msg("<color=white>Вы успешно создали возможный пункт назначения</color>", player.UserIDString));
                        }
                    }
                    return;
                default:
                    break;
            }
        }
        [ConsoleCommand("jug")]
        void ccmdJuggernaut(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, $"- {Title}  v{Version}  - {Author} @ www.chaoscode.io -");
                SendReply(arg, "jug open - Force open the event");
                SendReply(arg, "jug start - Force start the event");
                SendReply(arg, "jug cancel - Cancel a pending or current event");
                SendReply(arg, "jug clearicon - Clears any Juggernaut related icons from the map");

                return;
            }
            switch (arg.Args[0].ToLower())
            {
                case "open":
                    if (isOpen)
                    {
                        SendReply(arg, msg("<color=white>Ивент уже открыт</color>"));
                        return;
                    }
                    if (!hasDestinations)
                    {
                        SendReply(arg, msg("<color=white>Не настроены адресаты! Не удалось начать ивент</color>"));
                        return;
                    }
                    if (hasStarted)
                    {
                        SendReply(arg, msg("<color=white>Ивент уже начался</color>"));
                        return;
                    }
                    if (nextMatch != null)
                        nextMatch.Destroy();
                    OpenEvent();

                    return;
                case "start":
                    if (!isOpen)
                    {
                        SendReply(arg, msg("isntOpen"));
                        return;
                    }
                    if (hasStarted)
                    {
                        SendReply(arg, msg("<color=white>Ивент уже начался</color>"));
                        return;
                    }
                    if (startMatch != null)
                        startMatch.Destroy();
                    if (openMessage != null)
                        openMessage.Destroy();
                    StartEvent();
                    return;
                case "cancel":
                    if (!isOpen && !hasStarted)
                    {
                        SendReply(arg, msg("notStarted"));
                        return;
                    }
                    if (isOpen)
                    {
                        if (startMatch != null)
                            startMatch.Destroy();
                        if (openMessage != null)
                            openMessage.Destroy();
                        isOpen = false;
                        PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Админ отменил ивент!</color>"));
                        StartEventTimer();
                        return;
                    }
                    if (hasStarted)
                    {
                        PrintToChat(msg("<color=orange>[Джаггернаут]</color><color=white> : Админ отменил ивент!</color>"));
                        TryRestorePlayer(juggernaut);
                        DestroyEvent();
                        return;
                    }
                    return;
                case "clearicon":
                    RemoveMapMarker(msg("juggernaut"));
                    RemoveMapMarker(msg("destination"));
                    for (int i = 0; i < 10; i++)                    
                        RemoveMapMarker($"{msg("possibleDest")} {i + 1}");                    
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class JuggernautSettings
        {
            public float DefenseDamageModifier { get; set; }
            public float AttackDamageModifier { get; set; }
            public bool CanDamageStructures { get; set; }
            public bool StartWithFullMetabolism { get; set; }
            public bool DisableLandmineDamage { get; set; }
            public bool DisableBeartrapDamage { get; set; }
            public bool DisableFallDamage { get; set; }
            public List<InventoryItem> Inventory { get; set; }
        }        
        class InventoryItem
        {
            public string Shortname { get; set; }
            public int Amount { get; set; }
            public ulong SkinID { get; set; }
            public string Container { get; set; }
        }
        class Prize
        {
            public bool UseJuggernautInventory { get; set; }
            public bool UseEconomics { get; set; }
            public bool UseServerRewards { get; set; }
            public int MoneyRewardAmount { get; set; }
        }
        class Timers
        {
            public int TimeToCompleteJourney { get; set; }
            public int TimeBetweenEvents { get; set; }
            public int TimeBeforeSelection { get; set; }
        }
        class EventConditions
        {
            public float PercentageEntrantsToStart { get; set; }
            public int MinimumPlayersToOpen { get; set; }
        }
        class LustyMapIntegration
        {            
            public bool ShowDestinationIcon { get; set; }
            public int DestinationAmount { get; set; }
        }
        class GameOptions
        {
            public int BroadcastJuggernautPositionEvery { get; set; }
            public bool BroadcastToLustyMap { get; set; }
            public int SecondsToDisplayOnLM { get; set; }
        }
        class UIOptions
        {
            public bool ShowCoordsToJuggernaut { get; set; }
            public bool ShowUITimer { get; set; }
            public string IconUrl { get; set; }
            public UIPosition TimerPosition { get; set; }
            public UIPosition CompassPosition { get; set; }
            public string UIBackgroundColor { get; set; }
            public float UIOpacity { get; set; }
        }
        class UIPosition
        {
            public float XPosition { get; set; }
            public float YPosition { get; set; }
            public float XDimension { get; set; }
            public float YDimension { get; set; }
        }
        class ConfigData
        {
            public JuggernautSettings JuggernautSettings { get; set; }            
            public Timers EventTimers { get; set; }
            public UIOptions UISettings { get; set; }
            public EventConditions EventConditions { get; set; }
            public LustyMapIntegration LustyMapIntegration { get; set; }
            public GameOptions GameOptions { get; set; }
            public Prize Prizes { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                EventConditions = new EventConditions
                {
                    MinimumPlayersToOpen = 10,
                    PercentageEntrantsToStart = 0.6f
                },
                EventTimers = new Timers
                {
                    TimeBeforeSelection = 120,
                    TimeBetweenEvents = 3600,
                    TimeToCompleteJourney = 900
                },
                GameOptions = new GameOptions
                {
                    BroadcastJuggernautPositionEvery = 0,
                    BroadcastToLustyMap = false,
                    SecondsToDisplayOnLM = 5
                },
                LustyMapIntegration = new LustyMapIntegration
                {
                    DestinationAmount = 1,
                    ShowDestinationIcon = true
                },
                JuggernautSettings = new JuggernautSettings
                {
                    AttackDamageModifier = 1.5f,
                    CanDamageStructures = true,
                    StartWithFullMetabolism = true,
                    DefenseDamageModifier = 0.5f,
                    DisableBeartrapDamage = false,
                    DisableLandmineDamage = false,
                    DisableFallDamage = false,
                    Inventory = new List<InventoryItem>
                    {
                        new InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.pants",
                            SkinID = 0,
                            Container = "wear"
                        },
                        new InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.jacket",
                            SkinID = 0,
                            Container = "wear"
                        },
                        new InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.helmet",
                            SkinID = 0,
                            Container = "wear"
                        },
                        new InventoryItem
                        {
                            Amount = 1,
                            Shortname = "lmg.m249",
                            SkinID = 0,
                            Container = "belt"
                        },
                        new InventoryItem
                        {
                            Amount = 500,
                            Shortname = "ammo.rifle.explosive",
                            SkinID = 0,
                            Container = "main"
                        },
                        new InventoryItem
                        {
                            Amount = 3,
                            Shortname = "grenade.f1",
                            SkinID = 0,
                            Container = "belt"
                        },
                        new InventoryItem
                        {
                            Amount = 3,
                            Shortname = "syringe.medical",
                            SkinID = 0,
                            Container = "belt"
                        }
                    }
                },
                Prizes = new Prize
                {
                    MoneyRewardAmount = 0,
                    UseEconomics = false,
                    UseJuggernautInventory = true,
                    UseServerRewards = false
                },
                UISettings = new UIOptions
                {
                    IconUrl = "http://www.chaoscode.io/oxide/Images/juggernaut_icon.png",
                    UIBackgroundColor = "#4C4C4C",
                    ShowCoordsToJuggernaut = true,
                    ShowUITimer = true,
                    CompassPosition = new UIPosition
                    {
                        XDimension = 0.11f,
                        XPosition = 0.725f,
                        YDimension = 0.05f,
                        YPosition = 0.026f,
                    },
                    TimerPosition = new UIPosition
                    {
                        XDimension = 0.07f,
                        XPosition = 0.65f,
                        YDimension = 0.05f,
                        YPosition = 0.026f,                        
                    },                    
                    UIOpacity = 0.7f
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        class PlayerData
        {
            public Dictionary<string, List<EventInvItem>> inventory = new Dictionary<string, List<EventInvItem>>();
            public float health, hydration, calories, x, y, z;
        }
        public class EventInvItem
        {
            public int itemid;
            public ulong skin;
            public int amount;
            public float condition;
            public int ammo;
            public string ammotype;
            public ProtoBuf.Item.InstanceData instanceData;
            public EventInvItem[] contents;
        }
        class Destinations
        {
            public float x1, y1, z1, x2, y2, z2;
        }
        class Points
        {
            public int amount;
            public bool isRp;
        }
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<ulong, PlayerData> unrestoredPlayers = new Dictionary<ulong, PlayerData>();
            public Dictionary<ulong, List<EventInvItem>> winnerRewards = new Dictionary<ulong, List<EventInvItem>>();
            public Dictionary<ulong, List<Points>> winnerMoney = new Dictionary<ulong, List<Points>>();
            public List<Destinations> destinations = new List<Destinations>();
        }
        #endregion

        #region Localization
        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"eventOpen", "<color=orange>[Джаггернаут] </color><color=white>: Ивент Джаггернаут открыт! Введите </color><color=#ffd479>/jug join</color><color=white> чтобы записаться на ивент, и возможно стать Джаггернаутом</color>" },
            {"playerChosen", "<color=orange>[Джаггернаут] </color><color=white>: Джаггернаут был выбран! Ивент начнется через </color><color=#ffd479>10</color><color=white> секунд</color>" },
            {"juggerChosen", "<color=orange>[Джаггернаут] </color><color=white>: Вы стали Джаггернаутом!</color>" },
            {"eventStart", "<color=orange>[Джаггернаут] </color><color=white>: Джаггернаут приближается к </color><color=#ffd479>{0}</color><color=white>! Найдите и убейте его до того, как он доберется до места назначения!</color>" },
            {"juggerStart", "<color=orange>[Джаггернаут]</color><color=white> : Вы должны дойти до </color><color=#ffd479>{0}</color><color=white> и войти в белую сферу, чтобы выиграть ивент!</color>" },
            {"juggerDead", "<color=orange>[Джаггернаут]</color><color=white> : Джаггернаут был убить, теперь можно забрать выигрыш с него!</color>" },
            {"juggerWin", "<color=orange>[Джаггернаут]</color><color=white> : Джаггернаут добрался до точки назначения и выиграл этот ивент!</color>" },
            {"juggerClaim", "<color=orange>[Джаггернаут]</color><color=white> : Вы выиграли этот ивент! Чтобы получить вознаграждения, введите </color><color=#ffd479>/juggernaught claim</color><color=white>, после того как вы освободите свой инвентарь</color>" },
            {"noEntrants", "<color=orange>[Джаггернаут]</color><color=white> : Ни один игрок не записался на ивент. Ивент отменен!</color>" },
            {"notEnoughEntrants", "<color=orange>[Джаггернаут]</color><color=white> : Недостаточно игроков, которые записались на ивент. Минимальный процент онлайн-игроков, необходимых для запуска, составляет <color=#ffd479>{0}</color></color>" },
            {"eventCancel", "<color=orange>[Джаггернаут]</color><color=white> : Ивент окончен, потому что Джаггернаут не успел добраться до точки назначения</color>" },
            {"adminCancel", "<color=orange>[Джаггернаут]</color><color=white> : Админ отменил ивент!</color>" },
            {"enteredDraw", "<color=white>Вы записались на ивент, чтобы быть Джаггернаутом. Вы можете выйти в любое время, набрав </color><color=#ffd479>/jug leave</color>" },
            {"removedDraw", "<color=white>Вы удалили себя из жеребьёвки, чтобы стать Джаггернаутом</color>" },
            {"noPrizes", "<color=white>У вас нет призов</color>" },
            {"claimSuccess", "<color=white>Вы выиграли приз</color>" },
            {"destination", "Пункт назначения Джаггернаута" },
            {"possibleDest", "Возможный пункт назначения" },
            {"positionBroadcast", "<color=white>Джаггернаута видели в последний раз на координатах</color> <color=#ffd479>X:{0}, Z:{1}</color>" },
            {"target", "<color=#ffd479>Цель : </color>" },
            {"current", "color=#ffd479>Текущий : </color>" },
            {"juggernaut", "Джаггернаут" },
            {"noPerms", "<color=white>You do not have permission to be the juggernaut</color>" },
            {"help0", "<color=#ffd479>/jug info</color><color=white> - Показывать время, оставшееся до: начало ивента, окончание ивента в зависимости от текущего состояния ивента</color>" },
            {"help1", "<color=#ffd479>/jug join</color><color=white> - Позволяет игроку записаться на жеребьевку, чтобы попробовать стать Джаггернаутом</color>" },
            {"help2", "<color=#ffd479>/jug leave</color><color=white> - Удаляет игрока из жеребьевки, на становление Джаггернаутом</color>"},
            {"help3", "<color=#ffd479>/jug claim</color><color=white> - Используется Джаггернаутом, выигравшим ивент (гарантировать наличие пустых ячеек в инвентаре при подаче заявки!)</color>"},
            {"help4", "<color=#ffd479>/jug restore</color><color=white> - Используется для восстановления Джаггернаута после ивента, только если что-то случится с автоматизацией</color>" },
            {"help5", "<color=#ffd479>/jug open</color><color=white> - Принудительное открытие ивента</color>"},
            {"help6", "<color=#ffd479>/jug start</color><color=white> - Принудительное начало ивента</color>"},
            {"help7", "<color=#ffd479>/jug cancel</color><color=white> - Принудительное отключение ивента</color>"},
            {"help8", "<color=#ffd479>/jug destination</color><color=white> - Используется для создания адресатов для Джаггернаута</color>"},
            {"notOpen", "<color=white>На данный момент нет открытого ивента</color>"},
            {"playingEM", "<color=white>Вы не можете присоединиться к ивенту Джаггернаут, во время игры на Event Manager</color>"},
            {"cantRestore", "<color=white>Вы не можете восстановиться, будучи Джаггернаутом</color>"},
            {"noRestoreData", "<color=white>У вас нет сохраненных данных восстановления</color>"},
            {"alreadyOpen", "<color=white>Ивент уже открыт</color>"},
            {"noDestinations", "<color=white>Не настроены адресаты! Не удалось начать ивент</color>"},
            {"alreadyStarted", "<color=white>Ивент уже начался</color>"},
            {"isntOpen", "<color=white>Вы должны открыть ивент перед его запуском</color>"},
            {"notStarted", "<color=white>На данный момент ивента нет</color>"},
            {"destCreate1", "<color=white>Вы начали создавать новый пункт назначения, начиная с текущей позиции. Пройдите на позицию, которую вы хотите сделать конечной, и введите эту команду еще раз</color>"},
            {"destCreate2", "<color=white>Вы успешно создали возможный пункт назначения</color>"},
            {"tryTrading", "Вы не можете обмениваться, пока вы являетесь Джаггернаутом!"},
            {"tryRemove", "Вы не можете пользоваться ремувом, пока вы являетесь Джаггернаутом!"},
            {"tryTP", "Вы не можете телепортироваться, будучи Джаггернаутом!"},
            {"tryShop", "Вы не можете зайти в магазин, будучи Джаггернаутом!"},
            {"ff1", "<color=white>Вы не можете навредить своим друзьям, когда они являются Джаггернаутом!</color>"},
            {"ff2", "<color=white>Вы не можете навредить своим соклановцем, когда они являются Джаггернаутом!</color>"},
            {"endsIn", "<color=white>Ивент закончится через : </color><color=#ffd479>{0}</color>" },
            {"startsIn", "<color=white>Ивент начнется через : </color><color=#ffd479>{0}</color>" },
            {"nextEvent", "<color=white>Следующий ивент начнется через : </color><color=#ffd479>{0}</color>" },
            {"noSuicide", "<color=white>Вы не можете совершить самоубийство, пока вы являетесь Джаггернаутом!</color>" },
            {"juggerSuicide", "<color=white>Джаггернаут совершил самоубийство! Ивент завершён и награда удалена</color>" },
            {"candidate", "<color=#ffd479>{0}</color> <color=white>зарегистрировался на ивент Джаггернаут!</color> <color=#ffd479>({1} игроков зарегестрировано)</color>" }
        };
        #endregion
    }
}