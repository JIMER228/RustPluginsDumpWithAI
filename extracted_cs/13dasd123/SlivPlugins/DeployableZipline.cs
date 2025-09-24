// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static BaseEntity;

namespace Oxide.Plugins
{
    [Info("Deployable Zipline", "BlackLightning", "1.5.2")]
    public class DeployableZipline : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private Plugin Economics, NoEscape, ServerRewards;

        private const string DespawnConfigName = "DespawnConfig";

        private const string PermissionProtect = "deployablezipline.protect";
        private const string PermissionBuyTool = "deployablezipline.buy.tool";
        private const string PermissionBuyCable = "deployablezipline.buy.cable";

        private const string TeslaCoilPrefab = "assets/prefabs/deployable/playerioents/teslacoil/teslacoil.deployed.prefab";
        private const string LaunchPointPrefab = "assets/prefabs/building/powerline.zipline/ziplinelaunchpoint.prefab";

        private const string RemoverToolRefundItemName = "ZiplineCable";

        private const int AllowIgnoreLayers = 1 << (int)Rust.Layer.Deployed
            | 1 << (int)Rust.Layer.Tree;

        private const int MountCheckMask = 1 << (int)Rust.Layer.World
            | 1 << (int)Rust.Layer.Terrain
            | 1 << (int)Rust.Layer.Construction
            | 1 << (int)Rust.Layer.Prevent_Movement;

        private const float LowerSpearOffset = 0.37f;
        private const float UpperSpearOffset = 2.2f;

        private static readonly object False = false;
        private static readonly Vector3 TeslaCoilCableLocalOffset = new Vector3(0, 0.5f, 0);
        private static readonly Vector3 SpearCableLocalOffset = new Vector3(0, 2.6f, 0);

        private static readonly Vector3[] ExtraDismountPositions = { new Vector3(0, -1, 0) };

        private delegate void CalculateLinePointsDelegate(ZiplineLaunchPoint launchPoint, List<Vector3> targets, ref List<Vector3> points);

        private static readonly CalculateLinePointsDelegate ZLP_CalculateLinePointsDelegate = (CalculateLinePointsDelegate)Delegate.CreateDelegate(typeof(CalculateLinePointsDelegate),
            typeof(ZiplineLaunchPoint).GetMethod("CalculateZiplinePoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

        private static readonly FieldInfo ZLP_LinePointsField = typeof(ZiplineLaunchPoint).GetField(
            "linePoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private ItemDefinition _hammerItemDefinition;
        private ItemDefinition _toolgunItemDefinition;
        private ItemDefinition _spearItemDefinition;
        private ItemDefinition _laserSightItemDefinition;
        private ProtectionProperties _immortalProtection;
        private Configuration _config;
        private SavedData _data;
        private ZiplineManager _ziplineManager;
        private ToolSessionManager _toolSessionManager;
        private EconomicsPaymentProvider _economicsPaymentProvider;
        private ServerRewardsPaymentProvider _serverRewardsPaymentProvider;
        private readonly PreferencesManager _preferencesManager = new PreferencesManager();
        private readonly CooldownManager _toolCooldownManager = new CooldownManager();
        private readonly RemovableEntityInfo _removableEntityInfo = new RemovableEntityInfo();

        public DeployableZipline()
        {
            _economicsPaymentProvider = new EconomicsPaymentProvider(this);
            _serverRewardsPaymentProvider = new ServerRewardsPaymentProvider(this);
        }

        #endregion

        #region Hooks

        private void Init()
        {
            _config.Init(this);
            _data = SavedData.Load();

            _ziplineManager = new ZiplineManager(this);
            _toolSessionManager = new ToolSessionManager(this, _ziplineManager, _toolCooldownManager, _config, _data);

            permission.RegisterPermission(PermissionProtect, this);
            permission.RegisterPermission(PermissionBuyTool, this);
            permission.RegisterPermission(PermissionBuyCable, this);

            if (_config.CableItem?.ItemId == 0 || _config.CableItem?.ItemSkinId == 0)
            {
                Unsubscribe(nameof(CanBeRecycled));
            }
        }

        private void OnServerInitialized()
        {
            
        }
        private void OnServerInitialized(bool initial)
        {
            _hammerItemDefinition = ItemManager.FindItemDefinition("hammer");
            _toolgunItemDefinition = ItemManager.FindItemDefinition("toolgun");
            _spearItemDefinition = ItemManager.FindItemDefinition("speargun.spear");
            _laserSightItemDefinition = ItemManager.FindItemDefinition("weapon.mod.lasersight");

            _immortalProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
            _immortalProtection.name = $"{nameof(DeployableZipline)}Protection";
            _immortalProtection.Add(1);

            _ziplineManager.OnServerInitialized(initial);

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsSleeping())
                    continue;

                OnActiveItemChanged(player, null, player.GetActiveItem());
            }
        }

        private void OnNewSave()
        {
            _data.Reset();
        }

        private void OnServerSave()
        {
            _data.SaveIfNeeded();
        }

        private void Unload()
        {
            _data.SaveIfNeeded();
            _ziplineManager.Unload();

            UnityEngine.Object.Destroy(_immortalProtection);
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == DespawnConfigName)
            {
                _ziplineManager.CancelItemDespawn();
            }
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (!_config.ToolItem.ItemMatches(oldItem)
                && _config.ToolItem.ItemMatches(newItem))
            {
                var profile = _config.GetToolProfile(permission, player.UserIDString);
                if (profile == null)
                {
                    ChatMessage(player, LangEntry.SelectErrorNoPermission);
                    return;
                }

                _toolSessionManager.StartPlayerSession(player, profile);

                var extraInfo = profile.AllowBidirectional
                    ? (_preferencesManager.IsBidirectionalEnabled(player)
                        ? $" {GetMessage(player.UserIDString, LangEntry.SelectInfoBidirectionalEnabled)}"
                        : $" {GetMessage(player.UserIDString, LangEntry.SelectInfoBidirectionalDisabled)}")
                    : string.Empty;

                ChatMessage(player, LangEntry.SelectInfo, extraInfo);
            }
        }

        private object CanCombineDroppedItem(DroppedItem itemEntity, DroppedItem otherItemEntity)
        {
            var item = itemEntity.item;
            var otherItem = otherItemEntity.item;

            if (item.info == _spearItemDefinition)
            {
                if (_ziplineManager.HasEntity(itemEntity) || _ziplineManager.HasEntity(otherItemEntity))
                    return False;

                return null;
            }

            var cableItem = _config.CableItem;
            if (cableItem.ItemId != 0 && cableItem.ItemSkinId != 0)
            {
                if (item.info.itemid != cableItem.ItemId
                    || otherItem.info.itemid != cableItem.ItemId)
                    return null;

                if ((item.skin == cableItem.ItemSkinId) != (otherItem.skin == cableItem.ItemSkinId))
                    return False;

                return null;
            }

            return null;
        }

        // Detect when the Spear is picked up.
        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item.info != _spearItemDefinition)
                return null;

            var entity = item.GetWorldEntity() as DroppedItem;
            if (entity == null)
                return null;

            var controller = _ziplineManager.GetController(entity);
            if (controller == null)
                return null;

            if (_config.PickupRestrictions.RequireToolToPickUp)
            {
                var activeItem = player.GetActiveItem();

                var activeItemDefinition = activeItem?.info;
                if (activeItemDefinition != _hammerItemDefinition
                    && activeItemDefinition != _toolgunItemDefinition
                    && !_config.ToolItem.ItemMatches(activeItem))
                {
                    ChatMessage(player, LangEntry.PickupErrorToolRequired);
                    return False;
                }
            }

            if (!VerifyCanPickupZiplinePoint(player, entity, controller))
                return False;

            GiveRefund(player, controller);

            entity.ClientRPC(null, "PickupSound");
            item.RemoveFromWorld();
            item.Remove();
			player.SignalBroadcast(Signal.Gesture, "pickup_item");

            return False;
        }

        // Detect when the Tesla Coil is picked up.
        private object CanPickupEntity(BasePlayer player, TeslaCoil entity)
        {
            var controller = _ziplineManager.GetController(entity);
            if (controller == null)
                return null;

            if (!VerifyCanPickupZiplinePoint(player, entity, controller))
                return False;

            GiveRefund(player, controller);

            controller.Kill();
            return False;
        }

        private object CanMountEntity(BasePlayer player, ZiplineMountable ziplineMountable)
        {
            var mountPosition = ziplineMountable.endPosition;
            var controller = _ziplineManager.GetControllerByLaunchPoint(mountPosition);
            if (controller == null)
                return null;

            var capsuleHeight = 1.2f;
            var capsuleRadius = 0.4f;
            var pointA = mountPosition - new Vector3(0, capsuleRadius, 0);
            var pointB = mountPosition - new Vector3(0, capsuleRadius + capsuleHeight, 0);

            if (Physics.CheckCapsule(pointA, pointB, capsuleRadius, MountCheckMask, QueryTriggerInteraction.Ignore))
            {
                ChatMessage(player, LangEntry.MountErrorNoSpace);
                return False;
            }

            return null;
        }

        private void OnEntityMounted(ZiplineMountable ziplineMountable, BasePlayer player)
        {
            var mountPosition = ziplineMountable.endPosition;
            bool isForwardLaunchPoint;
            var controller = _ziplineManager.GetControllerByLaunchPoint(mountPosition, out isForwardLaunchPoint);
            if (controller == null)
                return;

            // Raise the mount anchor so the player is where the mountable is (does not affect visuals).
            // This reduces the likelihood that they will technically clip into terrain violation.
            ziplineMountable.mountAnchor.localPosition = ziplineMountable.mountAnchor.localPosition.WithY(0);

            AddDismountPositions(ziplineMountable, ExtraDismountPositions);

            var ignoreColliders = ziplineMountable.ignoreColliders;
            if (ignoreColliders != null)
            {
                for (var i = ignoreColliders.Count - 1; i >= 0; i--)
                {
                    var collider = ignoreColliders[i];
                    if (((1 << collider.gameObject.layer) & AllowIgnoreLayers) == 0)
                    {
                        ignoreColliders.RemoveAt(i);
                    }
                }
            }

            var speedProfile = _config.GetSpeedProfile(permission, player.UserIDString);
            if (speedProfile != null)
            {
                Vector3 startPoint;
                Vector3 endPoint;

                if (isForwardLaunchPoint)
                {
                    startPoint = controller.Data.StartPoint.Position;
                    endPoint = controller.Data.EndPoint.Position;
                }
                else
                {
                    startPoint = controller.Data.EndPoint.Position;
                    endPoint = controller.Data.StartPoint.Position;
                }

                var directionalMultiplier = 1f;
                var angle = AngleBetween(startPoint, endPoint);
                if (angle > 0)
                {
                    var speedPenalty = speedProfile.UphillSpeedPenalty;
                    directionalMultiplier = Mathf.Max(speedPenalty.MinSpeedPercent, 100f - angle * speedPenalty.PercentDecreasePerAngle) / 100f;
                }
                else if (angle < 0)
                {
                    var speedBonus = speedProfile.DownhillSpeedBonus;
                    directionalMultiplier = Mathf.Min(speedBonus.MaxSpeedPercent, 100f - angle * speedBonus.PercentIncreasePerAngle) / 100f;
                }

                ziplineMountable.MoveSpeed = speedProfile.MoveSpeed * directionalMultiplier;
                ziplineMountable.ForwardAdditive = speedProfile.BonusMoveSpeed * directionalMultiplier;
                ziplineMountable.SpeedUpTime = speedProfile.AccelerationTimeSeconds;
            }
        }

        // Only subscribed while Cable Item is valid and has a skin.
        private object CanBeRecycled(Item item, Recycler recycler)
        {
            if (item != null
                && item.skin == _config.CableItem?.ItemSkinId
                && item.info.Blueprint != null)
                return False;

            return null;
        }

        // RemoverTool
        private Dictionary<string, object> OnRemovableEntityInfo(BaseEntity entity, BasePlayer player)
        {
            var controller = _ziplineManager.GetController(entity);
            if (controller == null)
                return null;

            _removableEntityInfo.SetDisplayName(GetMessage(player.UserIDString, LangEntry.ZiplineName));

            var refundAmount = controller.GetRefundAmount();
            if (refundAmount > 0)
            {
                var cableName = GetMessage(player.UserIDString, LangEntry.CableName);
                _removableEntityInfo.SetRefund(cableName, refundAmount);
            }
            else
            {
                _removableEntityInfo.SetNoRefund();
            }

            return _removableEntityInfo.GetDictionary();
        }

        // RemoverTool
        private void OnRemovableEntityGiveRefund(BaseEntity entity, BasePlayer player, string itemName, int amount)
        {
            if (itemName != RemoverToolRefundItemName)
                return;

            var controller = _ziplineManager.GetController(entity);
            if (controller == null)
                return;

            GiveRefund(player, controller);
        }

        // RemoverTool
        private object canRemove(BasePlayer player, BaseEntity entity)
        {
            var controller = _ziplineManager.GetController(entity);
            if (controller == null)
                return null;

            if (controller.IsBusy())
                return GetMessage(player.UserIDString, LangEntry.PickupErrorGeneric);

            if (controller.Data.Protected && !permission.UserHasPermission(player.UserIDString, PermissionProtect))
                return GetMessage(player.UserIDString, LangEntry.ErrorNoPermission);

            if (!_config.PickupRestrictions.AllowPickupWhileBuildingBlockedAtOtherEnd)
            {
                var otherEntity = controller.GetOtherEntity(entity);
                if (otherEntity == null || otherEntity.IsDestroyed)
                    return False;

                if (player.IsBuildingBlocked(otherEntity.WorldSpaceBounds()))
                {
                    return GetMessage(player.UserIDString, LangEntry.PickupErrorBuildingBlockedOtherEnd);
                }
            }

            return null;
        }

        #endregion

        #region Dependencies

        private bool IsRaidBlocked(BasePlayer player)
        {
            var result = NoEscape?.Call("IsRaidBlocked", player);
            return result is bool && (bool)result;
        }

        private bool IsCombatBlocked(BasePlayer player)
        {
            var result = NoEscape?.Call("IsCombatBlocked", player);
            return result is bool && (bool)result;
        }

        #region Payment Providers

        private interface IPaymentProvider
        {
            int GetBalance(BasePlayer player);
            bool TakeBalance(BasePlayer player, int amount);
        }

        private class EconomicsPaymentProvider : IPaymentProvider
        {
            private DeployableZipline _plugin;
            private Plugin _ownerPlugin => _plugin.Economics;

            public EconomicsPaymentProvider(DeployableZipline plugin)
            {
                _plugin = plugin;
            }

            public bool IsAvailable => _ownerPlugin != null;

            public int GetBalance(BasePlayer player)
            {
                return Convert.ToInt32(_ownerPlugin.Call("Balance", player.userID));
            }

            public bool TakeBalance(BasePlayer player, int amount)
            {
                var result = _ownerPlugin.Call("Withdraw", player.userID, Convert.ToDouble(amount));
                return result is bool && (bool)result;
            }
        }

        private class ServerRewardsPaymentProvider : IPaymentProvider
        {
            private DeployableZipline _plugin;
            private Plugin _ownerPlugin => _plugin.ServerRewards;

            public ServerRewardsPaymentProvider(DeployableZipline plugin)
            {
                _plugin = plugin;
            }

            public bool IsAvailable => _ownerPlugin != null;

            public int GetBalance(BasePlayer player)
            {
                return Convert.ToInt32(_ownerPlugin.Call("CheckPoints", player.userID));
            }

            public bool TakeBalance(BasePlayer player, int amount)
            {
                var result = _ownerPlugin.Call("TakePoints", player.userID, amount);
                return result is bool && (bool)result;
            }
        }

        private class ItemsPaymentProvider : IPaymentProvider
        {
            private ItemDefinition _itemDefinition;
            private ulong _skinId;
            private int _itemId;

            public ItemDefinition ItemDefinition
            {
                get
                {
                    if (_itemDefinition == null)
                    {
                        _itemDefinition = ItemManager.FindItemDefinition(_itemId);
                    }
                    return _itemDefinition;
                }
            }

            public ItemsPaymentProvider(int itemId, ulong skinId)
            {
                _itemId = itemId;
                _skinId = skinId;
            }

            public int GetBalance(BasePlayer player)
            {
                return SumPlayerItems(player, _itemId, _skinId);
            }

            public bool TakeBalance(BasePlayer player, int amount)
            {
                if (amount <= 0)
                    return true;

                string displayName;
                TakePlayerItems(player, _itemId, _skinId, amount, out displayName);
                ShowGiveNotice(player, _itemId, -amount, displayName);
                return true;
            }
        }

        #endregion

        #region Remover Tool Integration

        private class RemovableEntityInfo
        {
            private const string DisplayNameField = "DisplayName";
            private const string RefundField = "Refund";
            private const string AmountField = "Amount";

            private Dictionary<string, object> _removeInfo;
            private Dictionary<string, object> _refundInfo;

            public RemovableEntityInfo()
            {
                _refundInfo = new Dictionary<string, object>
                {
                    [AmountField] = 0,
                };

                _removeInfo = new Dictionary<string, object>
                {
                    ["Refund"] = new Dictionary<string, object>
                    {
                        [RemoverToolRefundItemName] = _refundInfo,
                    },
                };
            }

            public void SetDisplayName(string displayName)
            {
                _removeInfo[DisplayNameField] = displayName;
            }

            public void SetRefund(string displayName, int amount)
            {
                _refundInfo[DisplayNameField] = displayName;
                _refundInfo[AmountField] = amount;
            }

            public void SetNoRefund()
            {
                _refundInfo[AmountField] = 0;
            }

            public Dictionary<string, object> GetDictionary()
            {
                return _removeInfo;
            }
        }

        #endregion

        #endregion

        #region Commands

        [Command("zipline")]
        private void CommandZipline(IPlayer player, string cmd, string[] args)
        {
            if (player.IsServer)
                return;

            var basePlayer = player.Object as BasePlayer;

            var firstArg = args.FirstOrDefault()?.ToLower();
            switch (firstArg)
            {
                case "buy":
                {
                    var toolInfo = _config.ToolItem;
                    if (toolInfo.ItemDefinition == null)
                        return;

                    if (!VerifyHasPermission(player, PermissionBuyTool))
                        return;

                    var costInfo = toolInfo.PurchaseInfo.Cost;
                    var paymentProvider = costInfo.GetPaymentProvider(_economicsPaymentProvider, _serverRewardsPaymentProvider);
                    var currentBalance = paymentProvider.GetBalance(basePlayer);
                    var currencyName = GetCurrencyName(player.Id, paymentProvider);

                    if (currentBalance < costInfo.Amount)
                    {
                        ReplyToPlayer(player, LangEntry.PurchaseErrorInsufficientFunds, currentBalance, costInfo.Amount, currencyName);
                        return;
                    }

                    if (!paymentProvider.TakeBalance(basePlayer, costInfo.Amount))
                    {
                        ReplyToPlayer(player, LangEntry.PurchaseErrorPaymentFailed);
                        return;
                    }

                    var attachmentItemDefinitions = toolInfo.PurchaseInfo.AttachmentItemDefinitions;

                    var toolItem = toolInfo.CreateItem(1, "Zipline Tool");
                    if (toolItem.contents != null && attachmentItemDefinitions != null)
                    {
                        foreach (var itemDefinition in attachmentItemDefinitions)
                        {
                            toolItem.contents.AddItem(itemDefinition, 1);
                        }
                    }

                    if (attachmentItemDefinitions.Contains(_laserSightItemDefinition))
                    {
                        var heldEntity = toolItem.GetHeldEntity() as HeldEntity;
                        if (heldEntity != null)
                        {
                            heldEntity.SetLightsOn(true);
                        }
                    }

                    var toolName = GetMessage(player.Id, LangEntry.ToolName);
                    GiveItem(basePlayer, toolItem, toolName, GiveItemReason.Crafted);

                    var message = GetMessage(player.Id, LangEntry.PurchaseToolSuccess, toolName);
                    if (costInfo.Amount > 0)
                    {
                        message += $" {GetMessage(player.Id, LangEntry.PurchaseSuccessInfo, costInfo.Amount, currencyName)}";
                    }
                    player.Reply(message);

                    return;
                }

                case "buycable":
                {
                    var cableInfo = _config.CableItem;
                    if (cableInfo.ItemDefinition == null)
                        return;

                    if (!VerifyHasPermission(player, PermissionBuyCable))
                        return;

                    int buyAmount;
                    if (args.Length < 2 || !int.TryParse(args[1], out buyAmount))
                    {
                        ReplyToPlayer(player, LangEntry.PurchaseCableErrorSyntax, cmd);
                        return;
                    }

                    var costInfo = cableInfo.Cost;
                    var paymentProvider = costInfo.GetPaymentProvider(_economicsPaymentProvider, _serverRewardsPaymentProvider);
                    var currentBalance = paymentProvider.GetBalance(basePlayer);
                    var currencyName = GetCurrencyName(player.Id, paymentProvider);

                    var spendAmount = buyAmount * costInfo.Amount;
                    if (currentBalance < spendAmount)
                    {
                        ReplyToPlayer(player, LangEntry.PurchaseErrorInsufficientFunds, currentBalance, spendAmount, currencyName);
                        return;
                    }

                    if (!paymentProvider.TakeBalance(basePlayer, spendAmount))
                    {
                        ReplyToPlayer(player, LangEntry.PurchaseErrorPaymentFailed);
                        return;
                    }

                    var cableItem = cableInfo.CreateItem(buyAmount, "Zipline Cable");
                    var cableName = GetMessage(player.Id, LangEntry.CableName);
                    GiveItem(basePlayer, cableItem, cableName, GiveItemReason.Crafted);

                    var message = GetMessage(player.Id, LangEntry.PurchaseCableSuccessInfo, buyAmount, cableName);
                    if (spendAmount > 0)
                    {
                        message += $" {GetMessage(player.Id, LangEntry.PurchaseSuccessInfo, spendAmount, currencyName)}";
                    }
                    player.Reply(message);

                    return;
                }

                case "toggle":
                {
                    var profile = _config.GetToolProfile(permission, player.Id);
                    if (profile == null)
                    {
                        ReplyToPlayer(player, LangEntry.SelectErrorNoPermission);
                        return;
                    }

                    if (!profile.AllowBidirectional)
                    {
                        ReplyToPlayer(player, LangEntry.BidirectionalErrorDisallowed);
                        return;
                    }

                    var isBidirectional = !_preferencesManager.IsBidirectionalEnabled(basePlayer);
                    _preferencesManager.SetBidirectional(basePlayer, isBidirectional);

                    var langKey = isBidirectional
                        ? LangEntry.BidirectionalEnabled
                        : LangEntry.BidirectionalDisabled;

                    ReplyToPlayer(player, langKey);

                    return;
                }

                case "protect":
                {
                    if (!VerifyHasPermission(player, PermissionProtect))
                        return;

                    var protectionEnabled = !_preferencesManager.IsProtectionEnabled(basePlayer);
                    _preferencesManager.SetProtectionEnabled(basePlayer, protectionEnabled);

                    var langKey = protectionEnabled
                        ? LangEntry.ProtectionEnabled
                        : LangEntry.ProtectionDisabled;

                    ReplyToPlayer(player, langKey);

                    return;
                }

                default:
                {
                    // TODO: Print help commands
                    return;
                }
            }
        }

        #endregion

        #region Helper Methods

        private bool VerifyHasPermission(IPlayer player, string perm)
        {
            if (player.HasPermission(perm))
                return true;

            ReplyToPlayer(player, LangEntry.ErrorNoPermission);
            return false;
        }

        private bool VerifyCanPickupZiplinePoint(BasePlayer player, BaseEntity entity, ZiplineController controller)
        {
            if (controller.IsBusy())
            {
                ChatMessage(player, LangEntry.PickupErrorGeneric);
                return false;
            }

            if (controller.Data.Protected && !permission.UserHasPermission(player.UserIDString, PermissionProtect))
            {
                ChatMessage(player, LangEntry.ErrorNoPermission);
                SendEffect(player, _config.Effects.PickupFailed);
                return false;
            }

            if (player.IsBuildingBlocked() || player.IsBuildingBlocked(entity.WorldSpaceBounds()))
            {
                ChatMessage(player, LangEntry.ErrorBuildingBlocked);
                SendEffect(player, _config.Effects.PickupFailed);
                return false;
            }

            if (!_config.PickupRestrictions.AllowPickupWhileBuildingBlockedAtOtherEnd)
            {
                var otherEntity = controller.GetOtherEntity(entity);
                if (otherEntity == null || otherEntity.IsDestroyed)
                    return false;

                if (player.IsBuildingBlocked(otherEntity.WorldSpaceBounds()))
                {
                    ChatMessage(player, LangEntry.PickupErrorBuildingBlockedOtherEnd);
                    SendEffect(player, _config.Effects.PickupFailed);
                    return false;
                }
            }

            return true;
        }

        private void GiveRefund(BasePlayer player, ZiplineController controller)
        {
            var cableCost = controller.Profile.CableCost;
            if (cableCost == null || cableCost.CostPerMeter <= 0 || cableCost.RefundPerMeter <= 0)
                return;

            var refundAmount = controller.GetRefundAmount();
            if (refundAmount <= 0)
                return;

            var refundItem = _config.CableItem.CreateItem(refundAmount);
            if (refundItem == null)
                return;

            var itemName = GetMessage(player.UserIDString, LangEntry.CableName);
            GiveItem(player, refundItem, itemName, GiveItemReason.PickedUp);
        }

        private static void AddDismountPositions(ZiplineMountable ziplineMountable, Vector3[] dismountPositions)
        {
            var originalLength = ziplineMountable.dismountPositions.Length;
            Array.Resize(ref ziplineMountable.dismountPositions, originalLength + dismountPositions.Length);

            for (var i = 0; i < dismountPositions.Length; i++)
            {
                var transform = ziplineMountable.gameObject.CreateChild().transform;
                transform.localPosition = dismountPositions[i];
                ziplineMountable.dismountPositions[originalLength + i] = transform;
            }
        }

        private static float AngleBetween(Vector3 pointA, Vector3 pointB)
        {
            var directionalMultiplier = pointA.y < pointB.y ? 1 : -1;
            var yDistance = Mathf.Abs(pointA.y - pointB.y);
            var xZDistance = (pointA - pointB).MagnitudeXZ();
            return directionalMultiplier * Mathf.Atan2(yDistance, xZDistance) * Mathf.Rad2Deg;
        }

        private static bool IsBuildingBlocked(BasePlayer player, Vector3 position)
        {
            return player.IsBuildingBlocked(position, Quaternion.identity, default(Bounds));
        }

        private static bool UserHasPermission(Permission permission, UserData userData, string perm)
        {
            return userData.Perms.Contains(perm)
                || permission.GroupsHavePermission(userData.Groups, perm);
        }

        private static string FormatTime(float seconds)
        {
            return TimeSpan.FromSeconds(Math.Ceiling(seconds)).ToString("c");
        }

        private static BaseEntity FindEntity(ulong entityId)
        {
            return BaseNetworkable.serverEntities.Find(new NetworkableId(entityId)) as BaseEntity;
        }

        private static void ShowGiveNotice(BasePlayer player, int itemId, int amount, string name = null, GiveItemReason reason = GiveItemReason.Generic)
        {
            player.Command("note.inv", itemId, amount, name, (int)reason);
        }

        private static void GiveItem(BasePlayer player, Item item, string name, GiveItemReason reason = GiveItemReason.Generic)
        {
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerBelt.dropPosition, player.inventory.containerMain.dropVelocity);
            }

            ShowGiveNotice(player, item.info.itemid, item.amount, name, reason);
        }

        private static int SumContainerItems(ItemContainer container, int itemId, ulong skinId)
        {
            var sum = 0;

            foreach (var item in container.itemList)
            {
                if (item.info.itemid != itemId)
                    continue;

                if (skinId != 0 && item.skin != skinId)
                    continue;

                sum += item.amount;
            }

            return sum;
        }

        private static int SumPlayerItems(BasePlayer player, int itemId, ulong skinId)
        {
            return SumContainerItems(player.inventory.containerMain, itemId, skinId)
                + SumContainerItems(player.inventory.containerBelt, itemId, skinId);
        }

        private static int TakeContainerItems(ItemContainer container, int itemId, ulong skinId, int amountToTake, out string displayName)
        {
            displayName = null;

            if (amountToTake == 0)
                return 0;

            var amountTaken = 0;

            for (var i = container.itemList.Count - 1; i >= 0; i--)
            {
                var item = container.itemList[i];
                if (item.info.itemid != itemId)
                    continue;

                if (skinId != 0 && item.skin != skinId)
                    continue;

                displayName = item.name;

                var takeAmount = Math.Min(item.amount, amountToTake - amountTaken);
                item.amount -= takeAmount;
                amountTaken += takeAmount;

                if (item.amount <= 0)
                {
                    item.RemoveFromContainer();
                    item.Remove();
                }
                else
                {
                    item.MarkDirty();
                }

                if (amountTaken >= amountToTake)
                    break;
            }

            return amountTaken;
        }

        private static int TakePlayerItems(BasePlayer player, int itemId, ulong skinId, int amountToTake, out string displayName)
        {
            var amountTaken = TakeContainerItems(player.inventory.containerMain, itemId, skinId, amountToTake, out displayName);
            if (amountTaken >= amountToTake)
                return amountTaken;

            amountTaken += TakeContainerItems(player.inventory.containerBelt, itemId, skinId, amountToTake - amountTaken, out displayName);

            return amountTaken;
        }

        private static void RunEffect(string effectPrefab, Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(effectPrefab))
                return;

            Effect.server.Run(effectPrefab, position);
        }

        private static void SendEffect(BasePlayer player, string effectPrefab, Vector3 position, Vector3 normal)
        {
            if (string.IsNullOrWhiteSpace(effectPrefab))
                return;

            var effect = new Effect(effectPrefab, position, normal);
            EffectNetwork.Send(effect, player.net.connection);
        }

        private static void SendEffect(BasePlayer player, string effectPrefab)
        {
            if (string.IsNullOrWhiteSpace(effectPrefab))
                return;

            var effect = new Effect(effectPrefab, player, 0, Vector3.zero, Vector3.forward);
            EffectNetwork.Send(effect, player.net.connection);
        }

        private static bool IsFacingUpward(Quaternion rotation)
        {
            return Vector3.Dot(rotation * Vector3.up, Vector3.up) > 0.5f;
        }

        private static Vector3 GetZiplineWorldOffset(Quaternion pointRotation)
        {
            var isTall = IsFacingUpward(pointRotation);
            var localOffset = isTall ? SpearCableLocalOffset : TeslaCoilCableLocalOffset;
            return pointRotation * localOffset;
        }

        private static void DestroyGroundWatch(BaseEntity entity)
        {
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
        }

        private static ZiplineLaunchPoint CreateLaunchPoint(Vector3 startPosition, Quaternion rotation, Vector3 endPosition, ulong ownerId)
        {
            var launchPoint = GameManager.server.CreateEntity(LaunchPointPrefab, startPosition, rotation) as ZiplineLaunchPoint;
            if (launchPoint == null)
                return null;

            launchPoint.ziplineTargets.Add(endPosition);
            launchPoint.OwnerID = ownerId;
            launchPoint.EnableSaving(false);
            launchPoint.Spawn();

            if (ZLP_CalculateLinePointsDelegate != null && ZLP_LinePointsField != null)
            {
                List<Vector3> linePoints = null;
                ZLP_CalculateLinePointsDelegate.Invoke(launchPoint, launchPoint.ziplineTargets, ref linePoints);
                ZLP_LinePointsField.SetValue(launchPoint, linePoints);

                var lastPosition = linePoints[linePoints.Count - 1];
                var direction = (lastPosition - linePoints[linePoints.Count - 2]).normalized;
                linePoints.Add(lastPosition + direction * 1.5f);
            }

            return launchPoint;
        }

        private BaseEntity CreateZiplinePoint(ZiplinePointData pointData, ulong ownerId, bool isTall, bool isUpper = false)
        {
            if (isTall)
                return CreateSpearPoint(pointData, ownerId, isUpper);

            return CreateTeslaCoilPoint(pointData, ownerId);
        }

        private static void HideInputsAndOutputs(IOEntity ioEntity)
        {
            foreach (var input in ioEntity.inputs)
            {
                input.type = IOEntity.IOType.Generic;
            }

            foreach (var output in ioEntity.outputs)
            {
                output.type = IOEntity.IOType.Generic;
            }
        }

        private static bool TryRaycast(BasePlayer player, out RaycastHit hit, float maxDistance, int layerMask = Rust.Layers.Solid)
        {
            return Physics.Raycast(player.eyes.HeadRay(), out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        private static bool TryCapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask = Rust.Layers.Solid)
        {
            return Physics.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        private DroppedItem CreateSpearPoint(ZiplinePointData pointData, ulong ownerId, bool isUpper)
        {
            var rotation = pointData.Rotation * Quaternion.LookRotation(isUpper ? Vector3.up : Vector3.down);
            var position = pointData.Position + pointData.Rotation * new Vector3(0, isUpper ? UpperSpearOffset : LowerSpearOffset, 0);

            var item = ItemManager.Create(_spearItemDefinition);
            var spearEntity = item.CreateWorldObject(position, rotation) as DroppedItem;
            if (spearEntity == null)
            {
                item.Remove();
                return null;
            }

            spearEntity.OwnerID = ownerId;
            spearEntity.EnableSaving(false);
            spearEntity.CancelInvoke(spearEntity.IdleDestroy);
            spearEntity.syncPosition = false;
            if (spearEntity.PositionTickFixedTime)
            {
                spearEntity.CancelInvokeFixedTime(spearEntity.NetworkPositionTick);
            }
            else
            {
                spearEntity.CancelInvoke(spearEntity.NetworkPositionTick);
            }
            UnityEngine.Object.DestroyImmediate(spearEntity.GetComponent<Rigidbody>());
            UnityEngine.Object.DestroyImmediate(spearEntity.GetComponent<PhysicsEffects>());

            return spearEntity;
        }

        private TeslaCoil CreateTeslaCoilPoint(ZiplinePointData pointData, ulong ownerId)
        {
            var teslaCoil = GameManager.server.CreateEntity(TeslaCoilPrefab, pointData.Position, pointData.Rotation) as TeslaCoil;
            if (teslaCoil == null)
                return null;

            teslaCoil.baseProtection = _immortalProtection;

            DestroyGroundWatch(teslaCoil);
            HideInputsAndOutputs(teslaCoil);

            teslaCoil.OwnerID = ownerId;
            teslaCoil.EnableSaving(false);
            teslaCoil.Spawn();

            return teslaCoil;
        }

        #endregion

        #region Preferences Manager

        private class PlayerPreferences
        {
            public bool Bidirectional;
            public bool Protection;
        }

        private class PreferencesManager
        {
            private readonly Dictionary<ulong, PlayerPreferences> _playerPreferences = new Dictionary<ulong, PlayerPreferences>();

            public PlayerPreferences GetPlayerPreferences(BasePlayer player)
            {
                PlayerPreferences preferences;
                return _playerPreferences.TryGetValue(player.userID, out preferences)
                    ? preferences
                    : null;
            }

            public PlayerPreferences EnsurePreferences(BasePlayer player)
            {
                var preferences = GetPlayerPreferences(player);
                if (preferences == null)
                {
                    preferences = new PlayerPreferences();
                    _playerPreferences[player.userID] = preferences;
                }
                return preferences;
            }

            public bool IsBidirectionalEnabled(BasePlayer player)
            {
                return GetPlayerPreferences(player)?.Bidirectional ?? false;
            }

            public void SetBidirectional(BasePlayer player, bool value)
            {
                EnsurePreferences(player).Bidirectional = value;
            }

            public bool IsProtectionEnabled(BasePlayer player)
            {
                return GetPlayerPreferences(player)?.Protection ?? false;
            }

            public void SetProtectionEnabled(BasePlayer player, bool value)
            {
                EnsurePreferences(player).Protection = value;
            }
        }

        #endregion

        #region Tool Session Manager

        private class ToolSessionManager
        {
            private const float InputCooldownTime = 0.25f;
            private const int LOSCheckMask = 1 << (int)Rust.Layer.Default
                | 1 << (int)Rust.Layer.Deployed
                | 1 << (int)Rust.Layer.World
                | 1 << (int)Rust.Layer.Construction
                | 1 << (int)Rust.Layer.Terrain
                | 1 << (int)Rust.Layer.Vehicle_Large
                | 1 << (int)Rust.Layer.Tree;

            private class ToolSession
            {
                public BasePlayer Player { get; }
                public ToolProfile Profile { get; }
                public float LastInputTime;
                public ZiplinePointData StartPointData;
                public BaseEntity StartHostEntity;

                public ToolSession(BasePlayer player, ToolProfile profile)
                {
                    Player = player;
                    Profile = profile;
                    LastInputTime = UnityEngine.Time.time;
                }
            }

            private DeployableZipline _plugin;
            private ZiplineManager _ziplineManager;
            private CooldownManager _toolCooldownManager;
            private Configuration _config;
            private SavedData _data;
            private Timer _timer;
            private Action _handleTimerCached;

            private List<ToolSession> _sessions = new List<ToolSession>();

            private PreferencesManager _preferencesManager => _plugin._preferencesManager;

            public ToolSessionManager(DeployableZipline plugin,  ZiplineManager ziplineManager, CooldownManager toolCooldownManager, Configuration config, SavedData data)
            {
                _plugin = plugin;
                _ziplineManager = ziplineManager;
                _toolCooldownManager = toolCooldownManager;
                _config = config;
                _data = data;
                _handleTimerCached = HandleTimer;
            }

            public void StartPlayerSession(BasePlayer player, ToolProfile profile)
            {
                var session = new ToolSession(player, profile);
                _sessions.Add(session);

                if (_timer == null || _timer.Destroyed)
                {
                    _timer = _plugin.timer.Every(0, _handleTimerCached);
                }
            }

            private bool CanUseWeapon(BasePlayer player)
            {
                if (player == null || player.IsDestroyed || !player.IsConnected || player.IsDead())
                    return false;

                var activeItem = player.GetActiveItem();
                if (activeItem == null)
                    return false;

                return _config.ToolItem.ItemMatches(activeItem);
            }

            private bool VerifyLocationAcceptable(ToolSession session, PointRuleset ruleset, RaycastHit hit, bool isStart)
            {
                var player = session.Player;
                var collider = hit.collider;

                if (!ruleset.AllowWhileBuildingBlocked
                    && IsBuildingBlocked(player, hit.point))
                {
                    var langEntry = isStart
                        ? LangEntry.DeployStartErrorBuildingBlocked
                        : LangEntry.DeployEndBuildingBlocked;

                    _plugin.ChatMessage(player, langEntry);
                    return false;
                }

                if (ruleset.DisallowedTopologyMask != 0)
                {
                    var topology = TerrainMeta.TopologyMap.GetTopology(hit.point);
                    if (!ruleset.ShouldAllowTopology(topology))
                    {
                        var langEntry = isStart
                            ? LangEntry.DeployStartErrorLocationRestricted
                            : LangEntry.DeployEndErrorLocationRestricted;

                        _plugin.ChatMessage(player, langEntry);
                        return false;
                    }
                }

                if (ruleset.AllowedLayerMask != 0 && !ruleset.ShouldAllowLayer(collider.gameObject.layer))
                {
                    var langEntry = isStart
                        ? LangEntry.DeployStartErrorSurfaceRestricted
                        : LangEntry.DeployEndErrorSurfaceRestricted;

                    _plugin.ChatMessage(player, langEntry);
                    return false;
                }

                if (ruleset.MinHeightAboveTerrain > 0)
                {
                    var diff = hit.point.y - TerrainMeta.HeightMap.GetHeight(hit.point);
                    if (diff < ruleset.MinHeightAboveTerrain)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployErrorTooCloseToTerrain, diff, ruleset.MinHeightAboveTerrain);
                        return false;
                    }
                }

                if (ruleset.MaxDeployDistance > 0)
                {
                    var distance = Vector3.Distance(player.eyes.position, hit.point);
                    if (distance > ruleset.MaxDeployDistance)
                    {
                        var langEntry = isStart
                            ? LangEntry.DeployStartErrorMaxDistance
                            : LangEntry.DeployEndErrorMaxDistance;

                        _plugin.ChatMessage(player, langEntry, distance, ruleset.MaxDeployDistance);
                        return false;
                    }
                }

                var entity = collider.ToBaseEntity();
                if (entity != null && collider.GetComponentInParent<Rigidbody>() != null)
                {
                    var langKey = isStart
                        ? LangEntry.DeployStartErrorObjectRestricted
                        : LangEntry.DeployEndErrorObjectRestricted;

                    _plugin.ChatMessage(player, langKey);
                    return false;
                }

                return true;
            }

            private bool HasMinimumRequiredCable(ToolSession session, out int minRequiredAmount)
            {
                minRequiredAmount = 0;

                var cableCost = session.Profile.CableCost;
                if (cableCost == null)
                    return true;

                minRequiredAmount = session.Profile.GetMinCableRequired();
                if (minRequiredAmount < 0)
                    return true;

                var currentAmount = SumPlayerItems(session.Player, _config.CableItem.ItemId, _config.CableItem.ItemSkinId);
                return currentAmount >= minRequiredAmount;
            }

            private bool VerifySufficientSpace(ToolSession session, RaycastHit hit)
            {
                var player = session.Player;
                var startPointData = session.StartPointData;

                var startPosition = startPointData.Position;
                var startRotation = startPointData.Rotation;

                var hitRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                var offsetStartPosition = startPosition + GetZiplineWorldOffset(startRotation);
                var offsetEndPosition = hit.point + GetZiplineWorldOffset(hitRotation);

                var capsuleHeight = 1.5f;
                var capsuleRadius = 0.25f;

                var startPointTop = offsetStartPosition - new Vector3(0, capsuleRadius, 0);
                var startPointBottom = offsetStartPosition - new Vector3(0, capsuleRadius + capsuleHeight, 0);
                var endPointTop = offsetEndPosition - new Vector3(0, capsuleRadius, 0);

                var castVector = endPointTop - startPointTop;
                var castDistance = castVector.magnitude - 0.001f;
                var castDirection = castVector / castDistance;

                RaycastHit losHit;
                if (TryCapsuleCast(startPointTop, startPointBottom, capsuleRadius, castDirection, out losHit, castDistance, LOSCheckMask))
                {
                    _plugin.ChatMessage(player, LangEntry.DeployEndErrorPathObstructed);
                    SendEffect(player, _config.Effects.ToolFailed);
                    return false;
                }

                return true;
            }

            private void HandleToolUse(ToolSession session)
            {
                var now = UnityEngine.Time.time;
                if (session.LastInputTime + InputCooldownTime > now)
                    return;

                session.LastInputTime = now;

                var player = session.Player;
                var profile = session.Profile;

                RaycastHit hit;
                if (!TryRaycast(player, out hit, Math.Max(profile.MaxLength, 1000)))
                {
                    _plugin.ChatMessage(player, LangEntry.DeployErrorNoSurface);
                    SendEffect(player, _config.Effects.ToolFailed);
                    return;
                }

                SendEffect(player, _config.Effects.ToolImpact, hit.point, hit.normal);

                if (session.Profile.MaxDeployed > 0)
                {
                    var ziplineCount = _data.CountZiplines(player.userID);
                    if (ziplineCount >= session.Profile.MaxDeployed)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployStartErrorMaxZiplines, ziplineCount, session.Profile.MaxDeployed);
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }
                }

                if (!ReferenceEquals(session.StartHostEntity, null))
                {
                    var startPointInvalid =  session.StartHostEntity == null
                        || session.StartHostEntity.IsDestroyed
                        || (
                            !profile.StartPointRuleset.AllowWhileBuildingBlocked
                            && IsBuildingBlocked(player, session.StartPointData.Position)
                        );

                    if (startPointInvalid)
                    {
                        session.StartPointData = null;
                        session.StartHostEntity = null;
                    }
                }

                if (!session.Profile.AllowToolWhileBuilingBlocked && player.IsBuildingBlocked())
                {
                    _plugin.ChatMessage(player, LangEntry.ErrorBuildingBlocked);
                    SendEffect(player, _config.Effects.ToolFailed);
                    return;
                }

                if (!profile.NoEscapeSettings.AllowDeployWhileRaidBlocked
                    && _plugin.IsRaidBlocked(player))
                {
                    _plugin.ChatMessage(player, LangEntry.DeployEndErrorRaidBlocked);
                    SendEffect(player, _config.Effects.ToolFailed);
                    return;
                }

                if (session.StartPointData == null)
                {
                    int minRequiredCable;
                    if (!HasMinimumRequiredCable(session, out minRequiredCable))
                    {
                        var cableName = _plugin.GetMessage(player.UserIDString, LangEntry.CableName);
                        _plugin.ChatMessage(player, LangEntry.DeployStartErrorInsufficientCableMinimum, minRequiredCable, cableName);
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    var cooldownSeconds = _toolCooldownManager.GetSecondsRemaining(player.userID, profile.ToolCooldownSeconds);
                    if (cooldownSeconds > 0)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployStartErrorOnCooldown, FormatTime(cooldownSeconds));
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    if (!VerifyLocationAcceptable(session, profile.StartPointRuleset, hit, isStart: true))
                    {
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    session.StartHostEntity = hit.collider.ToBaseEntity();
                    session.StartPointData = new ZiplinePointData
                    {
                        Position = hit.point,
                        RotationAngles = Quaternion.FromToRotation(Vector3.up, hit.normal).eulerAngles,
                        EntityId = session.StartHostEntity?.net?.ID.Value ?? 0,
                    };

                    _plugin.ChatMessage(player, LangEntry.DeployStartSuccess);

                    RunEffect(_config.Effects.ToolUsed, player.transform.position);
                }
                else
                {
                    if (!VerifyLocationAcceptable(session, profile.EndPointRuleset, hit, isStart: false))
                    {
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    var startPointData = session.StartPointData;
                    var startPosition = startPointData.Position;

                    var distance = Vector3.Distance(startPosition, hit.point);

                    if (profile.MinLength > 0 || profile.MaxLength > 0)
                    {
                        if (distance < profile.MinLength)
                        {
                            _plugin.ChatMessage(player, LangEntry.DeployEndErrorMinLength, distance, profile.MinLength);
                            SendEffect(player, _config.Effects.ToolFailed);
                            return;
                        }

                        if (distance > profile.MaxLength)
                        {
                            _plugin.ChatMessage(player, LangEntry.DeployEndErrorMaxLength, distance, profile.MaxLength);
                            SendEffect(player, _config.Effects.ToolFailed);
                            return;
                        }
                    }

                    var heightDifference = hit.point.y - startPosition.y;
                    if (heightDifference > profile.MaxElevationIncrease)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployEndErrorMaxHeightDifference, heightDifference, profile.MaxElevationIncrease);
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    if (!profile.NoEscapeSettings.AllowDeployWhileCombatBlocked
                        && _plugin.IsCombatBlocked(player))
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployEndErrorCombatBlocked);
                        SendEffect(player, _config.Effects.ToolFailed);
                        return;
                    }

                    var yDistance = Mathf.Abs(startPosition.y - hit.point.y);
                    var xZDistance = (startPosition - hit.point).MagnitudeXZ();
                    var angle = Mathf.Atan2(yDistance, xZDistance) * Mathf.Rad2Deg;

                    if (hit.point.y <= startPosition.y)
                    {
                        if (angle > profile.MaxDeclineAngle)
                        {
                            _plugin.ChatMessage(player, LangEntry.DeployEndErrorMaxDeclineAngle, angle, profile.MaxDeclineAngle);
                            SendEffect(player, _config.Effects.ToolFailed);
                            return;
                        }
                    }
                    else
                    {
                        if (angle > profile.MaxInclineAngle)
                        {
                            _plugin.ChatMessage(player, LangEntry.DeployEndErrorMaxInclineAngle, angle, profile.MaxInclineAngle);
                            SendEffect(player, _config.Effects.ToolFailed);
                            return;
                        }
                    }

                    var hitRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                    if (!VerifySufficientSpace(session, hit))
                        return;

                    var cableCost = profile.CableCost;
                    if (cableCost != null && cableCost.CostPerMeter > 0)
                    {
                        var cableItemId = _config.CableItem.ItemDefinition.itemid;
                        var requiredCableAmount = Mathf.CeilToInt(cableCost.CostPerMeter * distance);
                        var currentCableAmount = SumPlayerItems(player, cableItemId, _config.CableItem.ItemSkinId);
                        var cableName = _plugin.GetMessage(player.UserIDString, LangEntry.CableName);

                        if (currentCableAmount < requiredCableAmount)
                        {
                            _plugin.ChatMessage(player, LangEntry.DeployEndErrorInsufficientCable, cableName, currentCableAmount, requiredCableAmount, distance);
                            SendEffect(player, _config.Effects.ToolFailed);
                            return;
                        }

                        string cableItemDisplayName;
                        var amountTaken = TakePlayerItems(player, cableItemId, _config.CableItem.ItemSkinId, requiredCableAmount, out cableItemDisplayName);

                        ShowGiveNotice(player, cableItemId, -amountTaken, name: cableName);
                    }

                    var endHostEntity = hit.collider.ToBaseEntity();
                    var ziplineData = new ZiplineData
                    {
                        StartPoint = startPointData,
                        EndPoint = new ZiplinePointData
                        {
                            Position = hit.point,
                            RotationAngles = hitRotation.eulerAngles,
                            EntityId = endHostEntity?.net?.ID.Value ?? 0,
                        },
                        DeployedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    };

                    if (profile.AllowBidirectional && _preferencesManager.IsBidirectionalEnabled(player))
                    {
                        ziplineData.Bidirectional = true;
                    }

                    if (_preferencesManager.IsProtectionEnabled(player)
                        && _plugin.permission.UserHasPermission(player.UserIDString, PermissionProtect))
                    {
                        ziplineData.Protected = true;
                    }

                    _data.AddZipline(player.userID, ziplineData);

                    _toolCooldownManager.RestartCooldown(player.userID);

                    new ZiplineController(_ziplineManager, profile, player.userID, ziplineData, session.StartHostEntity, endHostEntity);

                    if (profile.MaxDeployed > 0)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployEndSuccessWithLimit, _data.CountZiplines(player.userID), profile.MaxDeployed);
                    }
                    else
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployEndSuccess);
                    }

                    if (ziplineData.Protected)
                    {
                        _plugin.ChatMessage(player, LangEntry.DeployEndProtected);
                    }

                    RunEffect(_config.Effects.ToolUsed, player.transform.position);
                    RunEffect(_config.Effects.DeploySucceeded, startPointData.Position);
                    RunEffect(_config.Effects.DeploySucceeded, hit.point);

                    if (profile.ToolConditionLossPercent > 0)
                    {
                        var activeItem = player.GetActiveItem();
                        if (activeItem.hasCondition && profile.ToolConditionLossPercent < 100)
                        {
                            activeItem.LoseCondition(activeItem.info.condition.max * profile.ToolConditionLossPercent / 100);
                            if (activeItem.isBroken)
                            {
                                activeItem.Remove();
                            }
                        }
                        else
                        {
                            activeItem.Remove();
                        }
                    }

                    session.StartPointData = null;
                    session.StartHostEntity = null;
                }
            }

            private void HandleToolSwitchMode(ToolSession session)
            {
                var now = UnityEngine.Time.time;
                if (session.LastInputTime + InputCooldownTime > now)
                    return;

                session.LastInputTime = now;

                var player = session.Player;
                var profile = session.Profile;

                if (!profile.AllowBidirectional)
                {
                    _plugin.ChatMessage(player, LangEntry.BidirectionalErrorDisallowed);
                    return;
                }

                var isBidirectional = !_preferencesManager.IsBidirectionalEnabled(player);
                _preferencesManager.SetBidirectional(player, isBidirectional);

                var langKey = isBidirectional
                    ? LangEntry.BidirectionalEnabled
                    : LangEntry.BidirectionalDisabled;

                _plugin.ChatMessage(player, langKey);
            }

            private void HandleTimer()
            {
                for (var i = _sessions.Count - 1; i >= 0; i--)
                {
                    var session = _sessions[i];
                    var player = session.Player;

                    if (!CanUseWeapon(player))
                    {
                        _sessions.RemoveAt(i);
                    }

                    if (player.serverInput.WasJustPressed(BUTTON.FIRE_THIRD))
                    {
                        HandleToolSwitchMode(session);
                    }

                    if (player.serverInput.WasJustPressed(BUTTON.FIRE_PRIMARY))
                    {
                        HandleToolUse(session);
                    }
                }

                if (_sessions.Count == 0)
                {
                    _timer.Destroy();
                    _timer = null;
                }
            }
        }

        #endregion

        #region Cooldown Manager

        private class CooldownManager
        {
            private readonly Dictionary<ulong, float> _cooldownMap = new Dictionary<ulong, float>();

            public void RestartCooldown(ulong userId)
            {
                if (_cooldownMap.ContainsKey(userId))
                {
                    _cooldownMap[userId] = UnityEngine.Time.realtimeSinceStartup;
                }
                else
                {
                    _cooldownMap.Add(userId, UnityEngine.Time.realtimeSinceStartup);
                }
            }

            public float GetSecondsRemaining(ulong userId, float duration)
            {
                if (!_cooldownMap.ContainsKey(userId))
                    return 0;

                return _cooldownMap[userId] + duration - UnityEngine.Time.realtimeSinceStartup;
            }
        }

        #endregion

        #region Zipline Manager

        private class ZiplineManager
        {
            private const float MaxDecayCheckIntervalSeconds = 600;

            public DeployableZipline Plugin { get; }
            private List<ZiplineController> _distinctControllers = new List<ZiplineController>();
            private Dictionary<BaseEntity, ZiplineController> _ziplineByEntity = new Dictionary<BaseEntity, ZiplineController>();
            private bool _isUnloading;
            private float _decayCheckIntervalSeconds;

            public ZiplineManager(DeployableZipline plugin)
            {
                Plugin = plugin;
            }

            public ZiplineController GetController(BaseEntity entity)
            {
                ZiplineController controller;
                return _ziplineByEntity.TryGetValue(entity, out controller)
                    ? controller
                    : null;
            }

            public ZiplineController GetControllerByLaunchPoint(Vector3 position, out bool isForward)
            {
                foreach (var controller in _distinctControllers)
                {
                    if (controller.HasLaunchPointAtPosition(position, out isForward))
                    {
                        return controller;
                    }
                }

                isForward = false;
                return null;
            }

            public ZiplineController GetControllerByLaunchPoint(Vector3 position)
            {
                bool isForward;
                return GetControllerByLaunchPoint(position, out isForward);
            }

            public bool HasEntity(BaseEntity entity)
            {
                return _ziplineByEntity.ContainsKey(entity);
            }

            public void OnServerInitialized(bool initial)
            {
                foreach (var entry in Plugin._data.PlayerZiplines.ToList())
                {
                    var ownerId = entry.Key;
                    var profile = Plugin._config.GetToolProfile(Plugin.permission, ownerId.ToString());

                    if (profile == null)
                    {
                        var removedCount = Plugin._data.RemoveAllForOwner(ownerId);
                        Plugin.LogWarning($"Removed {removedCount} ziplines for user {ownerId} since they no longer have permission to any profiles.");
                        continue;
                    }

                    foreach (var ziplineData in entry.Value.ToList())
                    {
                        BaseEntity startHostEntity = null;
                        BaseEntity endHostEntity = null;

                        if (ziplineData.StartPoint.EntityId != 0)
                        {
                            startHostEntity = FindEntity(ziplineData.StartPoint.EntityId);
                            if (startHostEntity == null || startHostEntity.IsDestroyed)
                            {
                                Plugin._data.RemoveZipline(ownerId, ziplineData);
                                Plugin.LogWarning($"Removed zipline for user {ownerId} because an entity it was attached to is no longer present.");
                                continue;
                            }
                        }

                        if (ziplineData.EndPoint.EntityId != 0)
                        {
                            endHostEntity = FindEntity(ziplineData.EndPoint.EntityId);
                            if (endHostEntity == null || endHostEntity.IsDestroyed)
                            {
                                Plugin._data.RemoveZipline(ownerId, ziplineData);
                                Plugin.LogWarning($"Removed zipline for user {ownerId} because an entity it was attached to is no longer present.");
                                continue;
                            }
                        }

                        new ZiplineController(this, profile, ownerId, ziplineData, startHostEntity, endHostEntity);
                    }
                }

                var minDecaySeconds = Plugin._config.MinDecaySeconds;
                if (minDecaySeconds > 0)
                {
                    DecayTick();
                    _decayCheckIntervalSeconds = Math.Min(minDecaySeconds, MaxDecayCheckIntervalSeconds);
                    Plugin.timer.Every(_decayCheckIntervalSeconds, DecayTick);
                }

                if (initial && Plugin.plugins.Exists(DespawnConfigName))
                {
                    Plugin.NextTick(CancelItemDespawn);
                }
            }

            public void Unload()
            {
                _isUnloading = true;

                foreach (var zipline in _ziplineByEntity.Values.ToList())
                {
                    zipline.Kill();
                }
            }

            public void CancelItemDespawn()
            {
                foreach (var zipline in _distinctControllers)
                {
                    zipline.CancelItemDespawn();
                }
            }

            public void Register(BaseEntity entity, ZiplineController controller)
            {
                _ziplineByEntity[entity] = controller;
            }

            public void Register(ZiplineController controller)
            {
                _distinctControllers.Add(controller);
            }

            public void Unregister(BaseEntity entity)
            {
                _ziplineByEntity.Remove(entity);
            }

            public void Unregister(ZiplineController controller)
            {
                _distinctControllers.Remove(controller);

                if (!_isUnloading)
                {
                    Plugin._data.RemoveZipline(controller.OwnerId, controller.Data);
                }
            }

            private void DecayTick()
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (_distinctControllers.Count > 0)
                {
                    var controllerIndex = _distinctControllers.Count;
                    Plugin.timer.Repeat(0, _distinctControllers.Count, () =>
                    {
                        if (--controllerIndex < 0)
                            return;

                        controllerIndex = Math.Min(controllerIndex, _distinctControllers.Count - 1);
                        if (controllerIndex < 0)
                            return;

                        var controller = _distinctControllers[controllerIndex];
                        if (controller.IsDestroyed)
                            return;

                        if (controller.Data.Protected)
                            return;

                        var decaySettings = controller.Profile.DecaySettings;
                        if (decaySettings == null || !decaySettings.Enabled)
                            return;

                        var decaySeconds = decaySettings.DecaySeconds;
                        if (decaySeconds <= 0)
                            return;

                        var data = controller.Data;
                        if (controller.HasToolCupboard())
                        {
                            data.DecayPreventedTime = now;
                            return;
                        }

                        var lastSafeTime = Math.Max(data.DeployedTime, data.DecayPreventedTime);
                        if (lastSafeTime + decaySeconds > now)
                            return;

                        controller.Kill();
                    });
                }
            }
        }

        #endregion

        #region Zipline Controller

        private class ZiplineEntityComponent : FacepunchBehaviour
        {
            public static ZiplineEntityComponent AddToEntity(BaseEntity entity, ZiplineController controller)
            {
                var component = entity.gameObject.AddComponent<ZiplineEntityComponent>();
                component.Entity = entity;
                component._controller = controller;
                return component;
            }

            public BaseEntity Entity { get; private set; }

            private ZiplineController _controller;

            private void OnDestroy()
            {
                _controller.Kill();
            }
        }

        private class ZiplineLaunchPointComponent : FacepunchBehaviour
        {
            public static void AddToEntity(ZiplineLaunchPoint launchPoint, ZiplineController controller)
            {
                var component = launchPoint.gameObject.AddComponent<ZiplineLaunchPointComponent>();
                component._controller = controller;
            }

            private ZiplineController _controller;

            private void OnDestroy()
            {
                _controller.Kill();
            }
        }

        private class ZiplineController
        {
            public ulong OwnerId { get; }
            public ToolProfile Profile { get; }
            public ZiplineData Data { get; }
            public bool IsDestroyed { get; private set; }

            private ZiplineManager _manager;
            private List<ZiplineEntityComponent> StartPoints = new List<ZiplineEntityComponent>(2);
            private List<ZiplineEntityComponent> EndPoints = new List<ZiplineEntityComponent>(2);
            private ZiplineLaunchPoint LaunchPointForward;
            private ZiplineLaunchPoint LaunchPointBackward;

            public ZiplineController(ZiplineManager manager, ToolProfile profile, ulong ownerId, ZiplineData data, BaseEntity startHostEntity, BaseEntity endHostEntity)
            {
                OwnerId = ownerId;
                Profile = profile;
                Data = data;
                _manager = manager;

                var isStartTall = IsFacingUpward(data.StartPoint.Rotation);
                var isEndTall = IsFacingUpward(data.EndPoint.Rotation);

                if (startHostEntity != null)
                {
                    ZiplineEntityComponent.AddToEntity(startHostEntity, this);
                }

                if (endHostEntity != null)
                {
                    ZiplineEntityComponent.AddToEntity(endHostEntity, this);
                }

                manager.Register(this);

                var plugin = manager.Plugin;

                var startEntity = plugin.CreateZiplinePoint(data.StartPoint, ownerId, isTall: isStartTall);
                StartPoints.Add(ZiplineEntityComponent.AddToEntity(startEntity, this));
                manager.Register(startEntity, this);

                if (isStartTall)
                {
                    startEntity = plugin.CreateZiplinePoint(data.StartPoint, ownerId, isTall: isStartTall, isUpper: true);
                    StartPoints.Add(ZiplineEntityComponent.AddToEntity(startEntity, this));
                    manager.Register(startEntity, this);
                }

                var endEntity = plugin.CreateZiplinePoint(data.EndPoint, ownerId, isTall: isEndTall);
                EndPoints.Add(ZiplineEntityComponent.AddToEntity(endEntity, this));
                manager.Register(endEntity, this);

                if (isEndTall)
                {
                    endEntity = plugin.CreateZiplinePoint(data.EndPoint, ownerId, isTall: isEndTall, isUpper: true);
                    EndPoints.Add(ZiplineEntityComponent.AddToEntity(endEntity, this));
                    manager.Register(endEntity, this);
                }

                var startPosition = data.StartPoint.Position + data.StartPoint.Rotation * (isStartTall ? SpearCableLocalOffset : TeslaCoilCableLocalOffset);
                var endPosition = data.EndPoint.Position + data.EndPoint.Rotation * (isEndTall ? SpearCableLocalOffset : TeslaCoilCableLocalOffset);

                LaunchPointForward = CreateLaunchPoint(startPosition, Quaternion.LookRotation(endPosition - startPosition), endPosition, ownerId);
                ZiplineLaunchPointComponent.AddToEntity(LaunchPointForward, this);
                manager.Register(LaunchPointForward, this);

                if (Data.Bidirectional)
                {
                    LaunchPointBackward = CreateLaunchPoint(endPosition, Quaternion.LookRotation(startPosition - endPosition), startPosition, ownerId);
                    ZiplineLaunchPointComponent.AddToEntity(LaunchPointBackward, this);
                    manager.Register(LaunchPointBackward, this);
                }
            }

            public bool HasLaunchPointAtPosition(Vector3 position, out bool isForward)
            {
                if (LaunchPointForward.LineDeparturePoint.position == position)
                {
                    isForward = true;
                    return true;
                }

                isForward = false;
                return (object)LaunchPointBackward != null && LaunchPointBackward.LineDeparturePoint.position == position;
            }

            public bool IsBusy()
            {
                if (LaunchPointForward.IsBusy())
                    return true;

                return LaunchPointBackward?.IsBusy() ?? false;
            }

            public bool HasToolCupboard()
            {
                return StartPoints.FirstOrDefault()?.Entity.GetBuildingPrivilege() != null
                    || EndPoints.FirstOrDefault()?.Entity.GetBuildingPrivilege() != null;
            }

            public BaseEntity GetOtherEntity(BaseEntity entity)
            {
                if (IsStartPointEntity(entity))
                    return EndPoints.FirstOrDefault()?.Entity;

                if (IsEndPointEntity(entity))
                    return StartPoints.FirstOrDefault()?.Entity;

                return null;
            }

            public int GetRefundAmount()
            {
                var cableCost = Profile.CableCost;
                if (cableCost == null || cableCost.CostPerMeter <= 0 || cableCost.RefundPerMeter <= 0)
                    return 0;

                return Mathf.CeilToInt(GetDistance() * cableCost.RefundPerMeter);
            }

            public void CancelItemDespawn()
            {
                foreach (var startPoint in StartPoints)
                {
                    var droppedItem = startPoint.Entity as DroppedItem;
                    if ((object)droppedItem != null)
                    {
                        droppedItem.CancelInvoke(droppedItem.IdleDestroy);
                    }
                }

                foreach (var endPoint in EndPoints)
                {
                    var droppedItem = endPoint.Entity as DroppedItem;
                    if ((object)droppedItem != null)
                    {
                        droppedItem.CancelInvoke(droppedItem.IdleDestroy);
                    }
                }
            }

            public void Kill()
            {
                IsDestroyed = true;

                for (var i = StartPoints.Count - 1; i >= 0; i--)
                {
                    var point = StartPoints[i];
                    _manager.Unregister(point.Entity);

                    if (point.Entity != null && !point.Entity.IsDestroyed)
                    {
                        point.Entity.Kill();
                    }
                }

                for (var i = EndPoints.Count - 1; i >= 0; i--)
                {
                    var point = EndPoints[i];
                    _manager.Unregister(point.Entity);

                    if (point.Entity != null && !point.Entity.IsDestroyed)
                    {
                        point.Entity.Kill();
                    }
                }

                if ((object)LaunchPointForward != null)
                {
                    _manager.Unregister(LaunchPointForward);
                    if (LaunchPointForward != null && !LaunchPointForward.IsDestroyed)
                    {
                        LaunchPointForward.Kill();
                    }
                }

                if ((object)LaunchPointBackward != null)
                {
                    _manager.Unregister(LaunchPointBackward);
                    if (LaunchPointBackward != null && !LaunchPointBackward.IsDestroyed)
                    {
                        LaunchPointBackward.Kill();
                    }
                }

                _manager.Unregister(this);
            }

            private float GetDistance()
            {
                var startPoint = StartPoints.FirstOrDefault();
                var endPoint = EndPoints.FirstOrDefault();
                return Vector3.Distance(startPoint.transform.position, endPoint.transform.position);
            }

            private bool IsStartPointEntity(BaseEntity entity)
            {
                foreach (var component in StartPoints)
                {
                    if (component.Entity == entity)
                        return true;
                }

                return false;
            }

            private bool IsEndPointEntity(BaseEntity entity)
            {
                foreach (var component in EndPoints)
                {
                    if (component.Entity == entity)
                        return true;
                }

                return false;
            }
        }

        #endregion

        #region Data

        [JsonObject(MemberSerialization.OptIn)]
        private class ZiplinePointData
        {
            [JsonProperty("Position")]
            public Vector3 Position;

            [JsonProperty("RotationAngles")]
            public Vector3 RotationAngles;

            [JsonProperty("EntityId", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ulong EntityId;

            [JsonIgnore]
            public Quaternion Rotation => Quaternion.Euler(RotationAngles);
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class ZiplineData
        {
            [JsonIgnore]
            public long DecayPreventedTime;

            [JsonProperty("StartPoint")]
            public ZiplinePointData StartPoint;

            [JsonProperty("EndPoint")]
            public ZiplinePointData EndPoint;

            [JsonProperty("DeployedTime")]
            public long DeployedTime;

            [JsonProperty("Bidirectional", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Bidirectional;

            [JsonProperty("Protected", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Protected;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class SavedData
        {
            public static SavedData Load()
            {
                return Interface.Oxide.DataFileSystem.ReadObject<SavedData>(nameof(DeployableZipline)) ?? new SavedData();
            }

            [JsonProperty("PlayerZiplines")]
            public Dictionary<ulong, List<ZiplineData>> PlayerZiplines = new Dictionary<ulong, List<ZiplineData>>();

            [JsonIgnore]
            private bool _isDirty;

            public int CountZiplines(ulong ownerId)
            {
                return GetPlayerData(ownerId)?.Count ?? 0;
            }

            public void AddZipline(ulong ownerId, ZiplineData data)
            {
                var ziplines = EnsurePlayerData(ownerId);
                ziplines.Add(data);

                _isDirty = true;
            }

            public void RemoveZipline(ulong ownerId, ZiplineData data)
            {
                var ziplines = EnsurePlayerData(ownerId);
                if (ziplines.Remove(data))
                {
                    if (ziplines.Count == 0)
                    {
                        PlayerZiplines.Remove(ownerId);
                    }

                    _isDirty = true;
                }
            }

            public int RemoveAllForOwner(ulong ownerId)
            {
                var count = GetPlayerData(ownerId)?.Count ?? 0;

                if (PlayerZiplines.Remove(ownerId))
                {
                    _isDirty = true;
                }

                return count;
            }

            public void SaveIfNeeded()
            {
                if (_isDirty)
                {
                    Save();
                }
            }

            public void Reset()
            {
                PlayerZiplines.Clear();
                Save();
            }

            private void Save()
            {
                Interface.Oxide.DataFileSystem.WriteObject(nameof(DeployableZipline), this);
                _isDirty = false;
            }

            private List<ZiplineData> GetPlayerData(ulong ownerId)
            {
                List<ZiplineData> ziplines;
                return PlayerZiplines.TryGetValue(ownerId, out ziplines)
                    ? ziplines
                    : null;
            }

            private List<ZiplineData> EnsurePlayerData(ulong ownerId)
            {
                var ziplines = GetPlayerData(ownerId);
                if (ziplines == null)
                {
                    ziplines = new List<ZiplineData>();
                    PlayerZiplines[ownerId] = ziplines;
                }

                return ziplines;
            }
        }

        #endregion

        #region Configuration

        [JsonObject(MemberSerialization.OptIn)]
        private class PointRuleset
        {
            [JsonIgnore]
            public int AllowedLayerMask { get; private set; }

            [JsonIgnore]
            public int DisallowedTopologyMask { get; private set; }

            [JsonProperty("Allow while building blocked")]
            public bool AllowWhileBuildingBlocked;

            [JsonProperty("Min height above terrain")]
            public int MinHeightAboveTerrain;

            [JsonProperty("Max deploy distance")]
            public float MaxDeployDistance;

            [JsonProperty("Allowed layers")]
            public string[] AllowedLayers =
            {
                "Default",
                "World",
                "Construction",
                "Terrain",
                "Tree",
            };

            [JsonProperty("Disallowed topology")]
            public string[] DisallowedTopology = Array.Empty<string>();

            public void Init(DeployableZipline plugin)
            {
                if (AllowedLayers != null)
                {
                    foreach (var topologyName in AllowedLayers)
                    {
                        Rust.Layer enumValue;
                        if (Enum.TryParse(topologyName, ignoreCase: true, result: out enumValue))
                        {
                            AllowedLayerMask |= 1 << (int)enumValue;
                        }
                        else
                        {
                            plugin.LogError($"Invalid layer: {topologyName}");
                        }
                    }
                }

                if (DisallowedTopology != null)
                {
                    foreach (var topologyName in DisallowedTopology)
                    {
                        TerrainTopology.Enum enumValueMask;
                        if (Enum.TryParse(topologyName, ignoreCase: true, result: out enumValueMask))
                        {
                            DisallowedTopologyMask |= (int)enumValueMask;
                        }
                        else
                        {
                            plugin.LogError($"Invalid topology: {topologyName}");
                        }
                    }
                }
            }

            public bool ShouldAllowLayer(int layer)
            {
                return (AllowedLayerMask & 1 << layer) != 0;
            }

            public bool ShouldAllowTopology(int topologyMask)
            {
                return (DisallowedTopologyMask & topologyMask) == 0;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class CableCost
        {
            [JsonProperty("Cost per meter")]
            public float CostPerMeter;

            [JsonProperty("Refund amount per meter on pickup")]
            public float RefundPerMeter;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class DecaySettings
        {
            [JsonProperty("Enabled")]
            public bool Enabled;

            [JsonProperty("Decay minutes")]
            public int DecayMinutes;

            [JsonIgnore]
            public int DecaySeconds => DecayMinutes * 60;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class NoEscapeSettings
        {
            [JsonProperty("Allow tool while raid blocked")]
            public bool AllowDeployWhileRaidBlocked = true;

            [JsonProperty("Allow tool while combat blocked")]
            public bool AllowDeployWhileCombatBlocked = true;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private abstract class BasePermissionProfile
        {
            protected abstract string _permissionPrefix { get; }

            [JsonIgnore]
            private string _permission;

            [JsonIgnore]
            public string Permission
            {
                get
                {
                    if (_permission == null && PermissionSuffix != null)
                    {
                        _permission = $"{nameof(DeployableZipline)}.{_permissionPrefix}.{PermissionSuffix}".ToLower();
                    }

                    return _permission;
                }
            }

            [JsonProperty("Permission suffix", Order = -2)]
            public string PermissionSuffix;

            public virtual void Init(DeployableZipline plugin)
            {
                if (string.IsNullOrWhiteSpace(PermissionSuffix))
                    return;

                plugin.permission.RegisterPermission(Permission, plugin);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class ToolProfile : BasePermissionProfile
        {
            protected override string _permissionPrefix => "profile";

            [JsonProperty("Allow tool while building blocked")]
            public bool AllowToolWhileBuilingBlocked;

            [JsonProperty("Allow bidirectional ziplines")]
            public bool AllowBidirectional;

            [JsonProperty("Zipline min length")]
            public float MinLength;

            [JsonProperty("Zipline max length")]
            public float MaxLength;

            [JsonProperty("Zipline max decline angle")]
            public float MaxDeclineAngle;

            [JsonProperty("Zipline max incline angle")]
            public float MaxInclineAngle;

            [JsonProperty("Zipline max elevation increase")]
            public float MaxElevationIncrease;

            [JsonProperty("Tool condition loss percent per zipline deployed")]
            public float ToolConditionLossPercent;

            [JsonProperty("Tool cooldown seconds")]
            public float ToolCooldownSeconds;

            [JsonProperty("Max ziplines at once")]
            public int MaxDeployed;

            [JsonProperty("Cable cost")]
            public CableCost CableCost;

            [JsonProperty("Start point ruleset")]
            public PointRuleset StartPointRuleset = new PointRuleset();

            [JsonProperty("End point ruleset")]
            public PointRuleset EndPointRuleset = new PointRuleset();

            [JsonProperty("Zipline decay settings")]
            public DecaySettings DecaySettings = new DecaySettings
            {
                Enabled = true,
                DecayMinutes = 60,
            };

            [JsonProperty("No Escape integration")]
            public NoEscapeSettings NoEscapeSettings = new NoEscapeSettings();

            public override void Init(DeployableZipline plugin)
            {
                base.Init(plugin);
                StartPointRuleset.Init(plugin);
                EndPointRuleset.Init(plugin);
            }

            public int GetMinCableRequired()
            {
                if (CableCost == null)
                    return 0;

                return Mathf.CeilToInt(MinLength * CableCost.CostPerMeter);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class ItemInfo
        {
            [JsonIgnore]
            private ItemDefinition _itemDefinition;

            [JsonIgnore]
            public ItemDefinition ItemDefinition
            {
                get
                {
                    if (_itemDefinition == null)
                    {
                        _itemDefinition = ItemManager.FindItemDefinition(ItemShortName);
                    }

                    return _itemDefinition;
                }
            }

            [JsonIgnore]
            public int ItemId => ItemDefinition?.itemid ?? 0;

            [JsonProperty("Item short name", Order = -3)]
            public string ItemShortName;

            [JsonProperty("Item skin ID", Order = -2)]
            public ulong ItemSkinId;

            public Item CreateItem(int amount, string displayName = null)
            {
                if (ItemDefinition == null)
                    return null;

                var item = ItemManager.Create(ItemDefinition, amount, ItemSkinId);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    item.name = displayName;
                }

                return item;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class CostInfo : ItemInfo
        {
            [JsonIgnore]
            private ItemsPaymentProvider _itemsPaymentProvider;

            [JsonProperty("Amount")]
            public int Amount;

            [JsonProperty("Use Economics")]
            public bool UseEconomics;

            [JsonProperty("Use Server Rewards")]
            public bool UseServerRewards;

            public IPaymentProvider GetPaymentProvider(EconomicsPaymentProvider economicsPaymentProvider, ServerRewardsPaymentProvider serverRewardsPaymentProvider)
            {
                if (UseEconomics && economicsPaymentProvider.IsAvailable)
                    return economicsPaymentProvider;

                if (UseServerRewards && serverRewardsPaymentProvider.IsAvailable)
                    return serverRewardsPaymentProvider;

                if (_itemsPaymentProvider == null)
                {
                    if (ItemDefinition == null)
                        return null;

                    _itemsPaymentProvider = new ItemsPaymentProvider(ItemDefinition.itemid, ItemSkinId);
                }

                return _itemsPaymentProvider;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class ToolPurchaseInfo
        {
            [JsonIgnore]
            public ItemDefinition[] AttachmentItemDefinitions;

            [JsonProperty("Cost")]
            public CostInfo Cost = new CostInfo
            {
                ItemShortName = "scrap",
                ItemSkinId = 0,
                Amount = 100,
                UseEconomics = false,
                UseServerRewards = false,
            };

            [JsonProperty("Attachment item short names")]
            private string[] AttachmentItemShortNames =
            {
                "weapon.mod.holosight",
                "weapon.mod.lasersight",
            };

            public void Init(DeployableZipline plugin)
            {
                if (Cost.ItemDefinition == null)
                {
                    plugin.LogWarning($"Invalid tool price item short name: {Cost.ItemShortName}");
                }

                var itemDefinitions = new List<ItemDefinition>();

                foreach (var shortName in AttachmentItemShortNames)
                {
                    var itemDefinition = ItemManager.FindItemDefinition(shortName);
                    if (itemDefinition == null)
                    {
                        plugin.LogWarning($"Invalid attachment short name: {shortName}");
                        continue;
                    }

                    itemDefinitions.Add(itemDefinition);
                }

                AttachmentItemDefinitions = itemDefinitions.ToArray();
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class ToolInfo : ItemInfo
        {
            [JsonProperty("Purchase info")]
            public ToolPurchaseInfo PurchaseInfo = new ToolPurchaseInfo();

            public bool ItemMatches(Item item)
            {
                if (item == null)
                    return false;

                return item.info == ItemDefinition
                    && item.skin == ItemSkinId;
            }

            public void Init(DeployableZipline plugin)
            {
                if (ItemDefinition == null)
                {
                    plugin.LogWarning($"Invalid tool item short name: {ItemShortName}");
                }

                PurchaseInfo.Init(plugin);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class CableInfo : ItemInfo
        {
            [JsonProperty("Cost")]
            public CostInfo Cost = new CostInfo
            {
                ItemShortName = "scrap",
                ItemSkinId = 0,
                Amount = 1,
                UseEconomics = false,
                UseServerRewards = false,
            };

            public void Init(DeployableZipline plugin)
            {
                if (ItemDefinition == null)
                {
                    plugin.LogWarning($"Invalid cable item short name: {ItemShortName}");
                }

                if (Cost.ItemDefinition == null)
                {
                    plugin.LogWarning($"Invalid tool price item short name: {Cost.ItemShortName}");
                }
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class PickupRestrictions
        {
            [JsonProperty("Require hammer or zipline tool to pick up zipline")]
            public bool RequireToolToPickUp = true;

            [JsonProperty("Allow pickup while building blocked at other end")]
            public bool AllowPickupWhileBuildingBlockedAtOtherEnd = true;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class EffectsConfig
        {
            [JsonProperty("Tool impact (player only)")]
            public string ToolImpact = "assets/bundled/prefabs/fx/impacts/bullet/metal/metal1.prefab";

            [JsonProperty("Tool failed (player only)")]
            public string ToolFailed = "assets/bundled/prefabs/fx/build/repair_failed.prefab";

            [JsonProperty("Tool used")]
            public string ToolUsed = "assets/prefabs/weapons/crossbow/effects/attack.prefab";

            [JsonProperty("Deploy succeeded")]
            public string DeploySucceeded = "assets/prefabs/deployable/barricades/effects/barricade-metal-deploy.prefab";

            [JsonProperty("Pickup failed (player only)")]
            public string PickupFailed = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class DownhillSpeedBonus
        {
            [JsonProperty("Percent increase per angle degree")]
            public float PercentIncreasePerAngle;

            [JsonProperty("Max speed percent")]
            public float MaxSpeedPercent = 100;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class UphillSpeedPenalty
        {
            [JsonProperty("Percent decrease per angle degree")]
            public float PercentDecreasePerAngle;

            [JsonProperty("Min speed percent")]
            public float MinSpeedPercent = 100;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class SpeedProfile : BasePermissionProfile
        {
            protected override string _permissionPrefix => "speed";

            [JsonProperty("Move speed")]
            public float MoveSpeed = 12;

            [JsonProperty("Bonus move speed")]
            public float BonusMoveSpeed = 7.5f;

            [JsonProperty("Acceleration time (seconds)")]
            public float AccelerationTimeSeconds = 3;

            [JsonProperty("Downhill speed bonus")]
            public DownhillSpeedBonus DownhillSpeedBonus = new DownhillSpeedBonus();

            [JsonProperty("Uphill speed penalty")]
            public UphillSpeedPenalty UphillSpeedPenalty = new UphillSpeedPenalty();
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class Configuration : BaseConfiguration
        {
            [JsonProperty("Zipline Tool")]
            public ToolInfo ToolItem = new ToolInfo
            {
                ItemShortName = "crossbow",
                ItemSkinId = 2793006815,
            };

            [JsonProperty("Zipline Cable")]
            public CableInfo CableItem = new CableInfo
            {
                ItemShortName = "rope",
                ItemSkinId = 2793158547,
            };

            [JsonProperty("Pickup restrictions")]
            public PickupRestrictions PickupRestrictions = new PickupRestrictions();

            [JsonProperty("Effects")]
            public EffectsConfig Effects = new EffectsConfig();

            [JsonProperty("Zipline Tool permission profiles")]
            private ToolProfile[] ToolProfiles =
            {
                new ToolProfile
                {
                    PermissionSuffix = "balanced",
                    MinLength = 10,
                    MaxLength = 100,
                    MaxElevationIncrease = 10,
                    MaxDeclineAngle = 45,
                    MaxInclineAngle = 15,
                    ToolCooldownSeconds = 0,
                    ToolConditionLossPercent = 10,
                    MaxDeployed = 4,
                    AllowToolWhileBuilingBlocked = false,
                    CableCost = new CableCost
                    {
                        CostPerMeter = 1,
                        RefundPerMeter = 1,
                    },
                    StartPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = false,
                        MinHeightAboveTerrain = 0,
                        MaxDeployDistance = 10,
                        DisallowedTopology = new[]
                        {
                            "Monument"
                        },
                    },
                    EndPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = false,
                        MinHeightAboveTerrain = 0,
                        MaxDeployDistance = 100,
                        DisallowedTopology = new[]
                        {
                            "Monument"
                        },
                    },
                },
                new ToolProfile
                {
                    PermissionSuffix = "fun",
                    MinLength = 10,
                    MaxLength = 200,
                    MaxElevationIncrease = 100,
                    MaxDeclineAngle = 45,
                    MaxInclineAngle = 45,
                    ToolCooldownSeconds = 0,
                    ToolConditionLossPercent = 0,
                    MaxDeployed = 6,
                    AllowToolWhileBuilingBlocked = false,
                    CableCost = new CableCost
                    {
                        CostPerMeter = 1,
                        RefundPerMeter = 1,
                    },
                    StartPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = false,
                        MinHeightAboveTerrain = 0,
                        MaxDeployDistance = 50,
                    },
                    EndPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = false,
                        MinHeightAboveTerrain = 0,
                        MaxDeployDistance = 200,
                    },
                },
                new ToolProfile
                {
                    PermissionSuffix = "unrestricted",
                    MinLength = 0,
                    MaxLength = 10000,
                    MaxElevationIncrease = 10000,
                    MaxDeclineAngle = 90,
                    MaxInclineAngle = 90,
                    ToolCooldownSeconds = 0,
                    ToolConditionLossPercent = 0,
                    MaxDeployed = 0,
                    AllowToolWhileBuilingBlocked = true,
                    AllowBidirectional = true,
                    StartPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = true,
                        MaxDeployDistance = 0,
                    },
                    EndPointRuleset = new PointRuleset
                    {
                        AllowWhileBuildingBlocked = true,
                        MaxDeployDistance = 0,
                    },
                },
            };

            [JsonProperty("Permission profiles")]
            private ToolProfile[] DeprecatedPermissionProfiles
            {
                set
                {
                    ToolProfiles = value;
                }
            }

            [JsonProperty("Speed permission profiles")]
            private SpeedProfile[] SpeedProfiles =
            {
                new SpeedProfile
                {
                    PermissionSuffix = "slow",
                    MoveSpeed = 6,
                    BonusMoveSpeed = 3.75f,
                    AccelerationTimeSeconds = 3,
                    DownhillSpeedBonus = new DownhillSpeedBonus
                    {
                        PercentIncreasePerAngle = 1,
                        MaxSpeedPercent = 150,
                    },
                    UphillSpeedPenalty = new UphillSpeedPenalty
                    {
                        PercentDecreasePerAngle = 1,
                        MinSpeedPercent = 50,
                    },
                },
                new SpeedProfile
                {
                    PermissionSuffix = "balanced",
                    MoveSpeed = 12,
                    BonusMoveSpeed = 7.5f,
                    AccelerationTimeSeconds = 3,
                    DownhillSpeedBonus = new DownhillSpeedBonus
                    {
                        PercentIncreasePerAngle = 1,
                        MaxSpeedPercent = 150,
                    },
                    UphillSpeedPenalty = new UphillSpeedPenalty
                    {
                        PercentDecreasePerAngle = 1,
                        MinSpeedPercent = 50,
                    },
                },
                new SpeedProfile
                {
                    PermissionSuffix = "fast",
                    MoveSpeed = 18,
                    BonusMoveSpeed = 11.25f,
                    AccelerationTimeSeconds = 3,
                    DownhillSpeedBonus = new DownhillSpeedBonus
                    {
                        PercentIncreasePerAngle = 1,
                        MaxSpeedPercent = 150,
                    },
                    UphillSpeedPenalty = new UphillSpeedPenalty
                    {
                        PercentDecreasePerAngle = 1,
                        MinSpeedPercent = 50,
                    },
                },
                new SpeedProfile
                {
                    PermissionSuffix = "ridiculous",
                    MoveSpeed = 60,
                    BonusMoveSpeed = 37.5f,
                    AccelerationTimeSeconds = 0,
                },
                new SpeedProfile
                {
                    PermissionSuffix = "ludicrous",
                    MoveSpeed = 120,
                    BonusMoveSpeed = 75,
                    AccelerationTimeSeconds = 0,
                },
            };

            [JsonProperty("Zipline decay settings")]
            public DecaySettings DeprecatedDecaySettings;

            public bool ShouldSerializeDeprecatedDecaySettings() => false;

            [JsonIgnore]
            public float MinDecaySeconds
            {
                get
                {
                    var minDecaySeconds = float.MaxValue;

                    foreach (var toolProfile in ToolProfiles)
                    {
                        var decaySettings = toolProfile.DecaySettings;
                        if (decaySettings != null
                            && decaySettings.Enabled
                            && decaySettings.DecaySeconds > 0)
                        {
                            minDecaySeconds = Math.Min(minDecaySeconds, decaySettings.DecaySeconds);
                        }
                    }

                    return minDecaySeconds == float.MaxValue ? 0 : minDecaySeconds;
                }
            }

            private bool MaybeMigrateDecaySettings()
            {
                if (DeprecatedDecaySettings == null || ToolProfiles.Length == 0)
                    return false;

                foreach (var toolProfile in ToolProfiles)
                {
                    toolProfile.DecaySettings = DeprecatedDecaySettings;
                }

                return true;
            }

            public bool MaybeMigrate()
            {
                return MaybeMigrateDecaySettings();
            }

            public void Init(DeployableZipline plugin)
            {
                ToolItem.Init(plugin);

                foreach (var profile in ToolProfiles)
                {
                    profile.Init(plugin);
                }

                foreach (var profile in SpeedProfiles)
                {
                    profile.Init(plugin);
                }
            }

            public ToolProfile GetToolProfile(Permission permission, string playerId)
            {
                var userData = permission.GetUserData(playerId);

                for (var i = ToolProfiles.Length - 1; i >= 0; i--)
                {
                    var profile = ToolProfiles[i];
                    if (UserHasPermission(permission, userData, profile.Permission))
                        return profile;
                }

                return null;
            }

            public SpeedProfile GetSpeedProfile(Permission permission, string playerId)
            {
                var userData = permission.GetUserData(playerId);

                for (var i = SpeedProfiles.Length - 1; i >= 0; i--)
                {
                    var profile = SpeedProfiles[i];
                    if (UserHasPermission(permission, userData, profile.Permission))
                        return profile;
                }

                return null;
            }
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #region Configuration Helpers

        private class BaseConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(BaseConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigSection(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigSection(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigSection(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config) || _config.MaybeMigrate())
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_config, true);
        }

        #endregion

        #endregion

        #region Localization

        private class LangEntry
        {
            public static List<LangEntry> AllLangEntries = new List<LangEntry>();

            public static readonly LangEntry ZiplineName = new LangEntry("ZiplineName", "Zipline");
            public static readonly LangEntry ToolName = new LangEntry("ToolName", "Zipline Tool");
            public static readonly LangEntry CableName = new LangEntry("CableName", "Cable");
            public static readonly LangEntry CurrencyNameEconomics = new LangEntry("CurrencyName.Economics", "Coins");
            public static readonly LangEntry CurrencyNameServerRewards = new LangEntry("CurrencyName.ServerRewards", "RP");

            public static readonly LangEntry ErrorNoPermission = new LangEntry("Error.NoPermission", "You don't have permission to do that.");
            public static readonly LangEntry ErrorBuildingBlocked = new LangEntry("Error.BuildingBlocked", "You cannot do that while building blocked.");

            public static readonly LangEntry PurchaseErrorInsufficientFunds = new LangEntry("Purchase.Error.InsufficientFunds", "Insufficient funds. You have <color=#fe0>{0} {2}</color> but need <color=#0ff>{1} {2}</color>.");
            public static readonly LangEntry PurchaseErrorPaymentFailed = new LangEntry("Purchase.Error.PaymentFailed", "An unexpected error occurred when charging you for the Zipline Tool.");
            public static readonly LangEntry PurchaseToolSuccess = new LangEntry("Purchase.Tool.Success2", "The <color=#0ff>{0}</color> was added to your inventory.");
            public static readonly LangEntry PurchaseSuccessInfo = new LangEntry("Purchase.Success.Info", "You have been charged <color=#fe0>{0} {1}</color>.");
            public static readonly LangEntry PurchaseCableSuccessInfo = new LangEntry("PurchaseCable.Success.Info", "<color=#0ff>{0} {1}</color> was added to your inventory.");
            public static readonly LangEntry PurchaseCableErrorSyntax = new LangEntry("PurchaseCable.Error.Syntax", "Syntax: <color=#0ff>{0} buycable <amount></color>");

            public static readonly LangEntry SelectInfo = new LangEntry("Select.Info", "<color=#0ff>[Zipline Tool]</color>: Attack to set start point.{0}");
            public static readonly LangEntry SelectInfoBidirectionalEnabled = new LangEntry("Select.Info.BidirectionalEnabled", "Toggle bidirectional mode with <color=#0ff>zipline toggle</color> or by pressing <color=#0ff>MMB</color> (<color=#fe0>Enabled</color>).");
            public static readonly LangEntry SelectInfoBidirectionalDisabled = new LangEntry("Select.Info.BidirectionalDisabled", "Toggle bidirectional mode with <color=#0ff>zipline toggle</color> or by pressing <color=#0ff>MMB</color> (<color=#fe0>Disabled</color>).");
            public static readonly LangEntry SelectErrorNoPermission = new LangEntry("Select.Error.NoPermissionToTool", "You don't have permission to use the Zipline Tool.");

            public static readonly LangEntry BidirectionalErrorDisallowed = new LangEntry("Bidirectional.Error.Disallowed", "<color=#0ff>[Zipline Tool]</color>: Bidirectional mode is not allowed.");
            public static readonly LangEntry BidirectionalEnabled = new LangEntry("Bidirectional.Info.Enabled2", "<color=#0ff>[Zipline Tool]</color>: Bidirectional mode <color=#fe0>Enabled</color>.");
            public static readonly LangEntry BidirectionalDisabled = new LangEntry("Bidirectional.Info.Disabled2", "<color=#0ff>[Zipline Tool]</color>: Bidirectional mode <color=#fe0>Disabled</color>.");

            public static readonly LangEntry ProtectionEnabled = new LangEntry("Protection.Enabled", "<color=#0ff>[Zipline Tool]</color>: Protection <color=#fe0>Enabled</color>.");
            public static readonly LangEntry ProtectionDisabled = new LangEntry("Protection.Disabled",  "<color=#0ff>[Zipline Tool]</color>: Protection <color=#fe0>Disabled</color>.");

            public static readonly LangEntry PickupErrorBuildingBlockedOtherEnd = new LangEntry("Pickup.Error.BuildingBlockedOtherEnd", "You cannot pick up that zipline because you are building blocked at the other end.");
            public static readonly LangEntry PickupErrorGeneric = new LangEntry("Pickup.Error.Generic", "You cannot pick up that zipline right now.");
            public static readonly LangEntry PickupErrorToolRequired = new LangEntry("Pickup.Error.ToolRequired", "You must wield a Hammer or Zipline Tool to pick up a zipline.");

            public static readonly LangEntry DeployStartSuccess = new LangEntry("DeployStart.Success", "<color=#0ff>[Zipline Tool]</color>: Zipline started. Attack to set end point.");
            public static readonly LangEntry DeployStartErrorInsufficientCableMinimum = new LangEntry("DeployStart.Error.InsufficientCable.Minimum", "<color=#0ff>[Zipline Tool]</color>: You need at least <color=#0ff>{0}</color> {1} to create a zipline.");
            public static readonly LangEntry DeployStartErrorMaxZiplines = new LangEntry("DeployStart.Error.MaxZiplines", "<color=#0ff>[Zipline Tool]</color>: You already have <color=#fe0>{0}</color> out of <color=#0ff>{1}</color> max Ziplines.");
            public static readonly LangEntry DeployStartErrorOnCooldown = new LangEntry("DeployStart.Error.OnCooldown", "<color=#0ff>[Zipline Tool]</color>: Please wait <color=#fe0>{0}</color> and try again.");
            public static readonly LangEntry DeployStartErrorMaxDistance = new LangEntry("DeployStart.Error.MaxDistance2", "<color=#0ff>[Zipline Tool]</color>: Cannot start that far away (<color=#fe0>{0:f1}m</color> > <color=#0ff>{1}m</color>)");
            public static readonly LangEntry DeployStartErrorSurfaceRestricted = new LangEntry("DeployStart.Error.SurfaceRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot start a zipline on that surface.");
            public static readonly LangEntry DeployStartErrorLocationRestricted = new LangEntry("DeployStart.Error.LocationRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot start a zipline at that location.");
            public static readonly LangEntry DeployStartErrorObjectRestricted = new LangEntry("DeployStart.Error.ObjectRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot start a zipline on that object.");
            public static readonly LangEntry DeployStartErrorBuildingBlocked = new LangEntry("DeployStart.Error.BuildingBlocked", "<color=#0ff>[Zipline Tool]</color>: You cannot start a zipline at a location where you are building blocked.");

            public static readonly LangEntry DeployEndSuccess = new LangEntry("DeployEnd.Success", "<color=#0ff>[Zipline Tool]</color>: Zipline deployed.");
            public static readonly LangEntry DeployEndSuccessWithLimit = new LangEntry("DeployEnd.Success.WithLimit", "<color=#0ff>[Zipline Tool]</color>: Zipline deployed (<color=#fe0>{0}</color> out of max <color=#0ff>{1}</color>).");
            public static readonly LangEntry DeployEndProtected = new LangEntry("DeployEnd.Protected", "<color=#0ff>This zipline is protected from decay and unauthorized pickup.</color>");
            public static readonly LangEntry DeployEndErrorInsufficientCable = new LangEntry("Deploy.Error.InsufficentCable", "<color=#0ff>[Zipline Tool]</color>: Insufficient {0}. You have <color=#fe0>{1}</color> but need <color=#0ff>{2}</color> for length <color=#fe0>{3:f1}m</color>.");
            public static readonly LangEntry DeployEndErrorMaxDistance = new LangEntry("DeployEnd.Error.MaxEndDistance2", "<color=#0ff>[Zipline Tool]</color>: Cannot end that far away (<color=#fe0>{0:f1}m</color> > <color=#0ff>{1}m</color>)");
            public static readonly LangEntry DeployEndErrorMinLength = new LangEntry("DeployEnd.Error.MinLength", "<color=#0ff>[Zipline Tool]</color>: Zipline too short (<color=#fe0>{0:f1}m</color> < <color=#0ff>{1}m</color>).");
            public static readonly LangEntry DeployEndErrorMaxLength = new LangEntry("DeployEnd.Error.MaxLength", "<color=#0ff>[Zipline Tool]</color>: Zipline too long (<color=#fe0>{0:f1}m</color> > <color=#0ff>{1}m</color>).");
            public static readonly LangEntry DeployEndErrorMaxHeightDifference = new LangEntry("DeployEnd.Error.MaxHeightDifference", "<color=#0ff>[Zipline Tool]</color>: Too high relative to start point (<color=#fe0>{0:f1}</color> > <color=#0ff>{1:f1}</color>m).");
            public static readonly LangEntry DeployEndErrorMaxDeclineAngle = new LangEntry("DeployEnd.Error.MinDeclineAngle", "<color=#0ff>[Zipline Tool]</color>: Decline angle <color=#fe0>{0:f1}°</color> cannot exceed <color=#0ff>{1}°</color>.");
            public static readonly LangEntry DeployEndErrorMaxInclineAngle = new LangEntry("DeployEnd.Error.MaxInclineAngle", "<color=#0ff>[Zipline Tool]</color>: Incline angle <color=#fe0>{0:f1}°</color> cannot exceed <color=#0ff>{1}°</color>.");
            public static readonly LangEntry DeployEndErrorPathObstructed = new LangEntry("DeployEnd.Error.PathObstructed", "<color=#0ff>[Zipline Tool]</color>: Path obstructed.");
            public static readonly LangEntry DeployEndErrorRaidBlocked = new LangEntry("DeployEnd.Error.RaidBlocked", "<color=#0ff>[Zipline Tool]</color>: You cannot do that while raid blocked.");
            public static readonly LangEntry DeployEndErrorCombatBlocked = new LangEntry("DeployEnd.Error.CombatBlocked", "<color=#0ff>[Zipline Tool]</color>: You cannot do that while combat blocked.");
            public static readonly LangEntry DeployEndErrorSurfaceRestricted = new LangEntry("DeployEnd.Error.SurfaceRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot end a zipline on that surface.");
            public static readonly LangEntry DeployEndErrorLocationRestricted = new LangEntry("DeployEnd.Error.LocationRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot end a zipline at that location.");
            public static readonly LangEntry DeployEndErrorObjectRestricted = new LangEntry("DeployEnd.Error.ObjectRestricted", "<color=#0ff>[Zipline Tool]</color>: You cannot end a zipline on that object.");
            public static readonly LangEntry DeployEndBuildingBlocked = new LangEntry("DeployEnd.Error.BuildingBlocked", "<color=#0ff>[Zipline Tool]</color>: You cannot end a zipline at a location where you are building blocked.");

            public static readonly LangEntry DeployErrorNoSurface = new LangEntry("Deploy.Error.NoSurface", "<color=#0ff>[Zipline Tool]</color>: No surface found.");
            public static readonly LangEntry DeployErrorTooCloseToTerrain = new LangEntry("Deploy.Error.TooCloseToTerrain", "<color=#0ff>[Zipline Tool]</color>: Too close to terrain (<color=#fe0>{0:f1}m</color> < <color=#0ff>{1}m</color>).");

            public static readonly LangEntry MountErrorNoSpace = new LangEntry("Mount.Error.NoSpace", "Not enough space to mount zipline.");

            public string Name;
            public string English;

            public LangEntry(string name, string english)
            {
                Name = name;
                English = english;

                AllLangEntries.Add(this);
            }
        }

        private string GetCurrencyName(string playerId, IPaymentProvider paymentProvider)
        {
            var itemsPaymentProvider = paymentProvider as ItemsPaymentProvider;
            if (itemsPaymentProvider != null)
            {
                return itemsPaymentProvider.ItemDefinition?.displayName.english;
            }

            var economicsPaymentProvider = paymentProvider as EconomicsPaymentProvider;
            if (economicsPaymentProvider != null)
            {
                return GetMessage(playerId, LangEntry.CurrencyNameEconomics);
            }

            var serverRewardsPaymentProvider = paymentProvider as ServerRewardsPaymentProvider;
            if (serverRewardsPaymentProvider != null)
            {
                return GetMessage(playerId, LangEntry.CurrencyNameServerRewards);
            }

            return "?";
        }


        private string GetMessage(string playerId, LangEntry langEntry) =>
            lang.GetMessage(langEntry.Name, this, playerId);

        private string GetMessage(string playerId, LangEntry langEntry, object arg1) =>
            string.Format(GetMessage(playerId, langEntry), arg1);

        private string GetMessage(string playerId, LangEntry langEntry, object arg1, object arg2) =>
            string.Format(GetMessage(playerId, langEntry), arg1, arg2);

        private string GetMessage(string playerId, LangEntry langEntry, object arg1, object arg2, string arg3) =>
            string.Format(GetMessage(playerId, langEntry), arg1, arg2, arg3);

        private string GetMessage(string playerId, LangEntry langEntry, params object[] args) =>
            string.Format(GetMessage(playerId, langEntry), args);


        private void ReplyToPlayer(IPlayer player, LangEntry langEntry) =>
            player.Reply(GetMessage(player.Id, langEntry));

        private void ReplyToPlayer(IPlayer player, LangEntry langEntry, object arg1) =>
            player.Reply(GetMessage(player.Id, langEntry, arg1));

        private void ReplyToPlayer(IPlayer player, LangEntry langEntry, object arg1, object arg2) =>
            player.Reply(GetMessage(player.Id, langEntry, arg1, arg2));

        private void ReplyToPlayer(IPlayer player, LangEntry langEntry, object arg1, object arg2, object arg3) =>
            player.Reply(GetMessage(player.Id, langEntry, arg1, arg2, arg3));

        private void ReplyToPlayer(IPlayer player, LangEntry langEntry, params object[] args) =>
            player.Reply(GetMessage(player.Id, langEntry, args));


        private void ChatMessage(BasePlayer player, LangEntry langEntry) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry));

        private void ChatMessage(BasePlayer player, LangEntry langEntry, object arg1) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry, arg1));

        private void ChatMessage(BasePlayer player, LangEntry langEntry, object arg1, object arg2) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry, arg1, arg2));

        private void ChatMessage(BasePlayer player, LangEntry langEntry, object arg1, object arg2, object arg3) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry, arg1, arg2, arg3));

        private void ChatMessage(BasePlayer player, LangEntry langEntry, object arg1, object arg2, object arg3, object arg4) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry, arg1, arg2, arg3, arg4));

        private void ChatMessage(BasePlayer player, LangEntry langEntry, params object[] args) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry, args));


        protected override void LoadDefaultMessages()
        {
            var englishLangKeys = new Dictionary<string, string>();

            foreach (var langEntry in LangEntry.AllLangEntries)
            {
                englishLangKeys[langEntry.Name] = langEntry.English;
            }

            lang.RegisterMessages(englishLangKeys, this, "en");
        }

        #endregion
    }
}
