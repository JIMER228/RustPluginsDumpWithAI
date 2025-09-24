using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using ProtoBuf;
using Rust;
using UnityEngine;

namespace Oxide.Plugins;

[Info("Chest Stacks", "supreme", "1.3.9")]
[Description("Allows players to stack chests")]
public class ChestStacks : RustPlugin
{
    #region Class Fields
        
    private static ChestStacks _pluginInstance;
    private PluginConfig _pluginConfig;
    private PluginData _pluginData;
        
    private readonly Hash<ulong, ChestStacking> _cachedComponents = new Hash<ulong, ChestStacking>();
    private readonly HashSet<ulong> _cachedPickedBoxes = new HashSet<ulong>();
        
    private const string UsePermission = "cheststacks.use";
    private const string LargeBoxEffect = "assets/prefabs/deployable/large wood storage/effects/large-wood-box-deploy.prefab";
    private const string LargeBoxPrefab = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";
    private const string LargeBoxShortname = "box.wooden.large";
    private const string SmallBoxEffect = "assets/prefabs/deployable/woodenbox/effects/wooden-box-deploy.prefab";
    private const string SmallBoxPrefab = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";
    private const string SmallBoxDeployedEntity = "woodbox_deployed";
    private const string SmallBoxShortname = "box.wooden";
    private const int BoxLayer = Layers.Mask.Deployed;
    private readonly Vector3 _smallBoxOffset = new Vector3(0f, 0.57f);
    private readonly Vector3 _smallBoxSphereOffset = new Vector3(0f, 0.3f);
    private readonly Vector3 _largeBoxOffset = new Vector3(0f, 0.8f);
    private readonly Vector3 _largeBoxSphereOffset = new Vector3(0f, 0.6f);
    private readonly object _returnObject = true;

    private enum ChestType : byte
    {
        None = 0,
        SmallBox = 1,
        LargeBox = 2
    }

    #endregion

    #region Hooks
        
    private void Init()
    {
        _pluginInstance = this;
        LoadData();
            
        HashSet<string> perms = new HashSet<string>{UsePermission};
        foreach (string perm in _pluginConfig.ChestStacksAmount.Keys)
        {
            perms.Add(perm);
        }

        foreach (string perm in perms)
        {
            permission.RegisterPermission(perm, this);
        }
    }

    private void OnServerInitialized()
    {
        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
            OnPlayerConnected(player);
        }
            
        foreach (ulong chestId in _pluginData.StoredBoxes.Keys)
        {
            BoxStorage foundChest = BaseNetworkable.serverEntities.Find(new NetworkableId(chestId)) as BoxStorage;
            if (!foundChest)
            {
                continue;
            }
                
            DestroyGroundWatch(foundChest);
        }
            
        SaveData();
    }

    private void Unload()
    {
        List<ChestStacking> chestStackingComponent = Pool.GetList<ChestStacking>();
        chestStackingComponent.AddRange(_cachedComponents.Values);
        
        for (int i = 0; i < chestStackingComponent.Count; i++)
        {
            chestStackingComponent[i].Destroy();
        }

        SaveData();
        _pluginInstance = null;
        Pool.FreeList(ref chestStackingComponent);
    }
        
    private void OnPlayerConnected(BasePlayer player)
    {
        ChestStacking chestStacking = _cachedComponents[player.userID];
        if (chestStacking)
        {
            return;
        }
            
        player.gameObject.AddComponent<ChestStacking>();
    }
        
    private void OnPlayerDisconnected(BasePlayer player)
    {
        ChestStacking chestStacking = _cachedComponents[player.userID];
        if (!chestStacking)
        {
            return;
        }
            
        chestStacking.Destroy();
    }

    private object OnEntityGroundMissing(BoxStorage box)
    {
        if (!box)
        {
            return null;
        }
            
        return _pluginData.StoredBoxes.ContainsKey(box.net.ID.Value) ? _returnObject : null;
    }

    private object OnEntityKill(BoxStorage box)
    {
        if (!box)
        {
            return null;
        }

        Vector3 boxPosition = box.transform.position;
        if (_pluginData.StoredBoxes.ContainsKey(box.net.ID.Value) && box.health > 0 && HasGround(boxPosition) && 
            !_cachedPickedBoxes.Contains(box.net.ID.Value))
        {
            return _returnObject;
        }

        if (_cachedPickedBoxes.Contains(box.net.ID.Value))
        {
            _cachedPickedBoxes.Remove(box.net.ID.Value);
        }

        BoxStorage[] foundBoxes = OverlapSphere<BoxStorage>(boxPosition, 2f, BoxLayer);
        if (foundBoxes.Length > 0)
        {
            for (int i = 0; i < foundBoxes.Length; i++)
            {
                BoxStorage foundBox = foundBoxes[i];
                if (!_pluginData.StoredBoxes.ContainsKey(foundBox.net.ID.Value))
                {
                    continue;
                }

                NextFrame(() => CheckGround(foundBox));
            }
        }

        if (!_pluginData.StoredBoxes.ContainsKey(box.net.ID.Value))
        {
            return null;
        }

        HandleUnStacking(box);
        return null;
    }
        
    private void CanPickupEntity(BasePlayer player, BoxStorage box)
    {
        if (!_pluginData.StoredBoxes.ContainsKey(box.net.ID.Value))
        {
            return;
        }
            
        _cachedPickedBoxes.Add(box.net.ID.Value);
    }

    #endregion

    #region Remover Tool Hooks

    private object canRemove(BasePlayer player, BoxStorage box)
    {
        return _pluginData.StoredBoxes[box.net.ID.Value] != null ? _returnObject : null;
    }

    #endregion
        
    #region Helper Methods

    private void DestroyGroundWatch(BaseEntity entity)
    {
        DestroyOnGroundMissing missing = entity.GetComponent<DestroyOnGroundMissing>();
        if (missing)
        {
            UnityEngine.Object.Destroy(missing);
        }
            
        GroundWatch watch = entity.GetComponent<GroundWatch>();
        if (watch)
        {
            UnityEngine.Object.Destroy(watch);
        }
    }

    private void CheckGround(BoxStorage box)
    {
        if (!box || Physics.Raycast(box.transform.position, Vector3.down, 0.5f, BoxLayer))
        {
            return;
        }
        
        box.DropItems();
        box.Kill();
    }

    private bool HasGround(Vector3 boxPosition)
    {
        return Physics.Raycast(boxPosition, Vector3.down, 0.5f, BoxLayer);
    }
        
    private bool HasCeiling(Vector3 boxPosition, ChestType chestType)
    {
        return chestType switch
        {
            ChestType.SmallBox => Physics.Raycast(boxPosition + _smallBoxSphereOffset, Vector3.up, 0.5f),
            ChestType.LargeBox => Physics.Raycast(boxPosition + _largeBoxSphereOffset, Vector3.up, 0.9f),
            _ => false
        };
    }

    private T[] OverlapSphere<T>(Vector3 pos, float radius, int layer)
    {
        return Physics.OverlapSphere(pos, radius, layer).Select(c => c.ToBaseEntity()).OfType<T>().ToArray();
    }

    private ulong GetBottomBoxId(ulong netId)
    {
        return _pluginData.StoredBoxes[netId]?.BottomBoxId == null ? 0UL : _pluginData.StoredBoxes[netId].BottomBoxId;
    }

    private int GetBoxes(ulong netId)
    {
        return _pluginData.StoredBoxes[netId]?.Boxes == null ? 0 : _pluginData.StoredBoxes[netId].Boxes;
    }
        
    private void HandleUnStacking(BoxStorage box)
    {
        ulong bottomBoxId = GetBottomBoxId(box.net.ID.Value);
        if (bottomBoxId == 0)
        {
            return;
        }
            
        int boxes = GetBoxes(bottomBoxId);
        _pluginData.StoredBoxes[bottomBoxId].Boxes = --boxes;
    }
        
    private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        
    private int GetPermissionValue(BasePlayer player, Hash<string, int> permissions, int defaultValue)
    {
        foreach (KeyValuePair<string, int> perm in permissions.OrderByDescending(p => p.Value))
        {
            if (HasPermission(player, perm.Key))
            {
                return perm.Value;
            }
        }

        return defaultValue;
    }
        
    private Tugboat GetTugboat(BasePlayer player)
    {
        return player.GetParentEntity() as Tugboat;
    }

    #endregion

    #region Chest Stacking Handler
        
    public class ChestStacking : FacepunchBehaviour
    {
        private BasePlayer Player { get; set; }
        private float NextTime { get; set; }
            
        private void Awake()
        {
            Player = GetComponent<BasePlayer>();
            _pluginInstance._cachedComponents[Player.userID] = this;
        }

        private void Update()
        {
            if (!Player || !_pluginInstance.permission.UserHasPermission(Player.UserIDString, UsePermission))
            {
                return;
            }

            if (!Player.serverInput.WasJustPressed(BUTTON.FIRE_SECONDARY))
            {
                return;
            }
                
            if (NextTime > Time.time)
            {
                return;
            }
                    
            NextTime = Time.time + 0.5f;
                    
            Item activeItem = Player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != SmallBoxShortname && activeItem.info.shortname != LargeBoxShortname)
            {
                return;
            }
                    
            if (_pluginInstance._pluginConfig.BlacklistedSkins.Contains(activeItem.skin))
            {
                return;
            }
                        
            BoxStorage box = GetBox(Player);
            if (!box)
            {
                return;
            }
                    
            ulong bottomBoxId = _pluginInstance.GetBottomBoxId(box.net.ID.Value);
            int boxes = _pluginInstance.GetBoxes(bottomBoxId);
            int allowedBoxesAmount = _pluginInstance.GetPermissionValue(Player, _pluginInstance._pluginConfig.ChestStacksAmount, 2);
            if (boxes >= allowedBoxesAmount)
            {
                Player.ChatMessage(_pluginInstance.Lang(LangKeys.MaxStackAmount, null, allowedBoxesAmount));
                return;
            }

            BuildingPrivlidge tc = Player.GetBuildingPrivilege();
            switch (box.ShortPrefabName)
            {
                case SmallBoxDeployedEntity:
                {
                    StackChest(box, box.transform, activeItem, ChestType.SmallBox, tc);
                    break;
                }
                case LargeBoxShortname:
                {
                    StackChest(box, box.transform, activeItem, ChestType.LargeBox, tc);
                    break;
                }
            }
        }

        private void StackChest(BoxStorage box, Transform boxTransform, Item activeItem, ChestType chestType, BuildingPrivlidge tc)
        {
            Tugboat tugboat = _pluginInstance.GetTugboat(Player);
            if (_pluginInstance._pluginConfig.StackChestsInBuildingPrivileged && !Player.IsBuildingAuthed() && !tugboat)
            {
                Player.ChatMessage(_pluginInstance.Lang(LangKeys.BuildingBlock));
                return;
            }

            Vector3 boxPosition = boxTransform.position;
            if (_pluginInstance.HasCeiling(boxPosition, chestType))
            {
                Player.ChatMessage(_pluginInstance.Lang(LangKeys.CeilingBlock));
                return;
            }
                
            switch (chestType)
            {
                case ChestType.SmallBox:
                {
                    if (activeItem.info.shortname != SmallBoxShortname)
                    {
                        Player.ChatMessage(_pluginInstance.Lang(LangKeys.OnlyStackSameType));
                        return;
                    }
                        
                    BoxStorage smallBox = GameManager.server.CreateEntity(SmallBoxPrefab, boxPosition + _pluginInstance._smallBoxOffset, boxTransform.rotation) as BoxStorage;
                    if (!smallBox)
                    {
                        return;
                    }
                        
                    smallBox.Spawn();
                    smallBox.OwnerID = Player.userID;
                    smallBox.skinID = activeItem.skin;
                    if (tc)
                    {
                        smallBox.AttachToBuilding(tc.buildingID);
                    }
                        
                    Interface.CallHook("OnEntityBuilt", Player.GetHeldEntity(), smallBox.transform.gameObject);
                    _pluginInstance.DestroyGroundWatch(smallBox);
                    if (tugboat)
                    {
                        smallBox.SetParent(tugboat, true);
                    }
                    
                    Effect.server.Run(SmallBoxEffect, boxPosition);
                    smallBox.SendNetworkUpdateImmediate();
                    HandleStacking(box, smallBox);
                    break;
                }
                case ChestType.LargeBox:
                {
                    if (activeItem.info.shortname != LargeBoxShortname)
                    {
                        Player.ChatMessage(_pluginInstance.Lang(LangKeys.OnlyStackSameType));
                        return;
                    }
                        
                    BoxStorage largeBox = GameManager.server.CreateEntity(LargeBoxPrefab, 
                        boxPosition + _pluginInstance._largeBoxOffset, boxTransform.rotation) as BoxStorage; 
                    if (!largeBox) 
                    { 
                        return; 
                    }
                        
                    largeBox.Spawn(); 
                    largeBox.OwnerID = Player.userID; 
                    largeBox.skinID = activeItem.skin;
                    if (tc)
                    {
                        largeBox.AttachToBuilding(tc.buildingID); 
                    }
                        
                    Interface.CallHook("OnEntityBuilt", Player.GetHeldEntity(), largeBox.transform.gameObject); 
                    _pluginInstance.DestroyGroundWatch(largeBox); 
                    if (tugboat)
                    {
                        largeBox.SetParent(tugboat, true);
                    }
                    Effect.server.Run(LargeBoxEffect, boxPosition);
                    largeBox.SendNetworkUpdateImmediate();
                    HandleStacking(box, largeBox);
                    break;
                }
            }
                
            activeItem.UseItem();
            _pluginInstance.SaveData();
        }

        private void HandleStacking(BoxStorage lastBox, BoxStorage newBox)
        {
            ulong bottomBoxId = _pluginInstance.GetBottomBoxId(lastBox.net.ID.Value);
            if (bottomBoxId == 0)
            {
                _pluginInstance._pluginData.StoredBoxes[newBox.net.ID.Value] = new BoxData
                {
                    BottomBoxId = newBox.net.ID.Value,
                    Boxes = 2
                };
            }
            else
            {
                int boxes = _pluginInstance.GetBoxes(bottomBoxId);
                _pluginInstance._pluginData.StoredBoxes[newBox.net.ID.Value] = new BoxData
                {
                    BottomBoxId = bottomBoxId,
                };

                _pluginInstance._pluginData.StoredBoxes[bottomBoxId].Boxes = ++boxes;
            }
        }
            
        private BoxStorage GetBox(BasePlayer player)
        {
            if (!Physics.Raycast(player.eyes.HeadRay(), out RaycastHit raycast, 3f, BoxLayer))
            {
                return null;
            }
            
            return raycast.GetEntity() as BoxStorage;
        }

        public void Destroy()
        {
            _pluginInstance._cachedComponents.Remove(Player.userID);
            DestroyImmediate(this);
        }
    }

    #endregion

    #region Configuration
        
    private class PluginConfig
    {

        [DefaultValue(true)]
        [JsonProperty(PropertyName = "Only stack chests in Building Privileged zones")]
        public bool StackChestsInBuildingPrivileged { get; set; }
            
        [JsonProperty(PropertyName = "Blacklisted Skins")]
        public HashSet<ulong> BlacklistedSkins { get; set; }
            
        [JsonProperty(PropertyName = "Permissions & their amount of stacked chests allowed")]
        public Hash<string, int> ChestStacksAmount { get; set; }
    }

    protected override void LoadDefaultConfig()
    {
        PrintWarning("Loading Default Config");
    }

    protected override void LoadConfig()
    {
        base.LoadConfig();
        Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
        _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
        Config.WriteObject(_pluginConfig);
    }

    private PluginConfig AdditionalConfig(PluginConfig pluginConfig)
    {
        pluginConfig.BlacklistedSkins = pluginConfig.BlacklistedSkins ?? new HashSet<ulong>
        {
            2618923347
        };

        pluginConfig.ChestStacksAmount = pluginConfig.ChestStacksAmount ?? new Hash<string, int>
        {
            ["cheststacks.use"] = 3,
            ["cheststacks.vip"] = 4
        };
            
        return pluginConfig;
    }

    #endregion
        
    #region Data

    private void SaveData()
    {
        if (_pluginData == null)
        {
            return;
        }
            
        ProtoStorage.Save(_pluginData, Name);
    }

    private void LoadData()
    {
        _pluginData = ProtoStorage.Load<PluginData>(Name) ?? new PluginData();
    }

    [ProtoContract]
    private class PluginData
    {
        [ProtoMember(1)]
        public Hash<ulong, BoxData> StoredBoxes { get; set; } = new Hash<ulong, BoxData>();
    }

    [ProtoContract]
    private class BoxData
    {
        [ProtoMember(1)]
        public ulong BottomBoxId { get; set; }
        [ProtoMember(2)]
        public int Boxes { get; set; }
    }
        
    #endregion
        
    #region Language
        
    private class LangKeys
    {
        public const string MaxStackAmount = nameof(MaxStackAmount);
        public const string OnlyStackSameType = nameof(OnlyStackSameType);
        public const string CeilingBlock = nameof(CeilingBlock);
        public const string BuildingBlock = nameof(BuildingBlock);
    }
        
    protected override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            [LangKeys.MaxStackAmount] = "You are trying to stack more than {0} chests!",
            [LangKeys.OnlyStackSameType] = "You can only stack the same type of chests!",
            [LangKeys.CeilingBlock] = "A ceiling is blocking you from stacking this chest!",
            [LangKeys.BuildingBlock] = "You need to be Building Privileged in order to stack chests!"

        }, this);
    }
        
    private string Lang(string key, BasePlayer player = null)
    {
        return lang.GetMessage(key, this, player?.UserIDString);
    }
        
    private string Lang(string key, BasePlayer player = null, params object[] args)
    {
        try
        {
            return string.Format(Lang(key, player), args);
        }
        catch (Exception ex)
        {
            PrintError($"Lang Key '{key}' threw exception:\n{ex}");
            throw;
        }
    }
        
    #endregion
}