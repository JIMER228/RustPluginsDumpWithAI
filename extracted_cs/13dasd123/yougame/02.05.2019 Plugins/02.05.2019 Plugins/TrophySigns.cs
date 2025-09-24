// Reference: System.Drawing
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("TrophySigns", "k1lly0u", "0.1.07", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class TrophySigns : RustPlugin
    {
        #region Fields        
        StoredData storedData;
        private DynamicConfigFile data;
                
        private bool wipeDetected;

        private Hash<Signage, DroppedItem> droppedItems = new Hash<Signage, DroppedItem>();
        private ImageStorage imageStorage;

        static int layerPlcmnt;
        static TrophySigns ins;

        const string burlapSack = "assets/prefabs/misc/burlap sack/generic_world.prefab";
        const int skullId = 996293980;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("trophysigns.use", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("trophysigns_data");
        }

        private void OnServerInitialized()
        {
            ins = this;
            layerPlcmnt = LayerMask.GetMask("Construction", "Default", "Deployed", "World", "Terrain");

            imageStorage = new GameObject().AddComponent<ImageStorage>();

            LoadData();

            if (wipeDetected)
            {
                storedData = new StoredData();
                SaveData();
            }

            ServerMgr.Instance.StartCoroutine(FindRegisteredSignage(BaseNetworkable.serverEntities.Where(x => x is Signage).Cast<Signage>()));
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (entity == null || info == null)
            {
                ItemPlacement placement = entity.GetComponent<ItemPlacement>();
                if (placement != null)
                    placement.OnPlayerDeath();                
            }
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            Signage signage = networkable.GetComponent<Signage>();
            if (signage != null)
            {
                if (droppedItems.ContainsKey(signage))
                    droppedItems.Remove(signage);
            }
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave() => SaveData();

        private void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || newItem == null || !permission.UserHasPermission(player.UserIDString, "trophysigns.use"))
                return;

            if (newItem.info.itemid == skullId && !player.GetComponent<ItemPlacement>())
                player.gameObject.AddComponent<ItemPlacement>();
        }

        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item != null && player != null)
            {
                ItemPlacement placement = player.GetComponent<ItemPlacement>();
                if (placement != null)                
                    return false;                
                else
                {
                    DroppedItem droppedItem = item.GetWorldEntity()?.GetComponent<DroppedItem>();

                    if (droppedItem != null && droppedItem.item.info.itemid == skullId)
                    {
                        Signage signage = droppedItem.GetComponentInParent<Signage>();
                        if (signage != null && droppedItems.ContainsKey(signage))
                        {
                            if (!configData.Remove.RemoveSkulls)
                                return false;

                            if (configData.Remove.RequirePrivilege && !player.IsBuildingAuthed())
                            {
                                SendReply(player, msg("noAuth", player.userID));
                                return false;
                            }

                            droppedItems.Remove(signage);
                            if (signage.HasFlag(BaseEntity.Flags.Locked))
                                signage.SetFlag(BaseEntity.Flags.Locked, false);
                            UpdateSignImage(signage, "", true);
                        }
                    }
                }
            }
            return null;
        }

        private void Unload()
        {
            SaveData();

            ItemPlacement[] placementComps = UnityEngine.Object.FindObjectsOfType<ItemPlacement>();
            if (placementComps != null)
            {
                foreach (ItemPlacement placement in placementComps)
                    placement.CancelPlacement();
            }

            for (int i = droppedItems.Count - 1; i >= 0; i--)
            {
                DroppedItem droppedItem = droppedItems.ElementAt(i).Value;
                droppedItem.DestroyItem();
                droppedItem.Kill();
            }            
        }
        #endregion
      
        #region Functions  
        private IEnumerator FindRegisteredSignage(IEnumerable<Signage> signage)
        {
            for (int i = 0; i < signage.Count(); i++)
            {
                Signage sign = signage.ElementAt(i);

                if (sign == null || sign.IsDestroyed || !storedData.data.ContainsKey(sign.net.ID))
                    continue;

                StoredData.TrophyData data = storedData.data[sign.net.ID];
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));
                                
                InitializeSign(sign, data.displayName, new Vector3(data.position[0], data.position[1], data.position[2]), new Vector3(data.rotation[0], data.rotation[1], data.rotation[2]));
            }
        }

        private void InitializeSign(Signage sign, string name, Vector3 localPosition, Vector3 localRotation)
        {              
            BaseEntity[] children = sign.children.Where(x => x.GetComponent<DroppedItem>())?.ToArray() ?? null;

            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    sign.RemoveChild(children[i]);
                    children[i].Kill();
                }
            }
          
            DroppedItem droppedItem = SpawnDroppedItem(name, sign.transform.position);

            droppedItem.SetParent(sign);
            droppedItem.transform.localPosition = localPosition;
            droppedItem.transform.rotation = Quaternion.Euler(localRotation);

            UpdateSignImage(sign, name);

            droppedItems.Add(sign, droppedItem);
        }
                
        private BaseEntity CreateWorldObject(string name, Vector3 pos, bool canPickup)
        {
            Item item = ItemManager.CreateByItemID(skullId);
            item.name = name;

            BaseEntity worldEntity = GameManager.server.CreateEntity(burlapSack, pos);
            WorldItem worldItem = worldEntity as WorldItem;
            if (worldItem != null)
                worldItem.InitializeItem(item);

            worldItem.enableSaving = false;
            worldItem.allowPickup = canPickup;
            worldEntity.Spawn();
            item.SetWorldEntity(worldEntity);
            return worldEntity;
        }

        private DroppedItem SpawnDroppedItem(string name, Vector3 position, bool canPickup = true)
        {
            BaseEntity worldEntity = CreateWorldObject(name, position, canPickup);

            UnityEngine.Object.Destroy(worldEntity.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<EntityCollisionMessage>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<PhysicsEffects>());

            DroppedItem droppedItem = worldEntity.GetComponent<DroppedItem>();
            droppedItem.CancelInvoke(droppedItem.IdleDestroy);

            return droppedItem;
        }
        #endregion

        #region Image Storage
        private void UpdateSignImage(Signage signage, string text, bool hideText = false)
        {
            if (!configData.Sign.GenerateImage)
                return;
            
            imageStorage.AddQueueItem(new ImageStorage.QueueItem(text, signage, hideText));            
        }        

        private class ImageStorage : MonoBehaviour
        {
            private Queue<QueueItem> queue;
            private WWW www;
            private QueueItem queueItem;
            private bool isBusy;

            private string backgroundColor;
            private string textColor;
            private int textSize;

            private void Awake()
            {
                queue = new Queue<QueueItem>();
                backgroundColor = ins.configData.Sign.BackgroundColor;
                textColor = ins.configData.Sign.TextColor;
                textSize = ins.configData.Sign.TextSize;
            }

            private void OnDestroy()
            {
                www.Dispose();
                queue.Clear();
                Destroy(gameObject);
            }

            public void AddQueueItem(QueueItem queueItem)
            {
                queue.Enqueue(queueItem);
                if (!isBusy)
                    StartNextItem();
            }

            private void StartNextItem()
            {
                isBusy = true;

                queueItem = queue.Dequeue();

                if (queueItem.signage == null || queueItem.signage.IsDestroyed)
                {
                    isBusy = false;
                    StartNextItem();
                    return;
                }

                StartCoroutine(DownloadImage());
            }
            
            private IEnumerator DownloadImage()
            {
                ImageSize imageSize;
                if (!ImageSize.Sizes.TryGetValue(queueItem.signage.ShortPrefabName, out imageSize))
                {
                    print($"[ERROR] TrophySigns: No sign sizes set for {queueItem.signage.ShortPrefabName}, please report this in the plugin support thread");

                    isBusy = false;
                    if (queue.Count > 0)
                        StartNextItem();
                    yield break;
                }

                float scale = (float)imageSize.Width / (float)600;
                int targetFontSize = (int)(textSize * scale);

                string imageUrl = $"http://placeholdit.imgix.net/~text?bg={backgroundColor}&txtclr={(queueItem.hideText ? backgroundColor : textColor)}&txtsize={targetFontSize}&txt={queueItem.text}&w={imageSize.Width}&h={imageSize.Height}";

                www = new WWW(imageUrl);

                yield return new WaitWhile(() => !www.isDone);

                byte[] imageBytes = www.texture.EncodeToPNG();

                if (imageSize.Width != imageSize.ImageWidth || imageSize.Height != imageSize.ImageHeight)
                    imageBytes = ResizeImage(imageBytes, imageSize);

                queueItem.signage.textureID = FileStorage.server.Store(imageBytes, FileStorage.Type.png, queueItem.signage.net.ID, 0U);
                queueItem.signage.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                if (!queueItem.signage.HasFlag(BaseEntity.Flags.Locked))
                    queueItem.signage.SetFlag(BaseEntity.Flags.Locked, true);

                isBusy = false;
                www.Dispose();

                if (queue.Count > 0)
                    StartNextItem();
            }

            private byte[] ResizeImage(byte[] bytes, ImageSize imageSize)
            {
                byte[] resizedImageBytes;
                using (MemoryStream originalBytesStream = new MemoryStream(), resizedBytesStream = new MemoryStream())
                {
                    originalBytesStream.Write(bytes, 0, bytes.Length);
                    Bitmap image = new Bitmap(originalBytesStream);

                    Bitmap resizedImage = new Bitmap(imageSize.Width, imageSize.Height);

                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
                    {
                        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        graphics.DrawImage(image, new Rectangle(0, 0, imageSize.ImageWidth, imageSize.ImageHeight));
                    }

                    resizedImage.Save(resizedBytesStream, ImageFormat.Png);
                    resizedImageBytes = resizedBytesStream.ToArray();
                }
                return resizedImageBytes;
            }

            public class QueueItem
            {
                public string text;
                public bool hideText;
                public Signage signage;

                public QueueItem() { }
                public QueueItem(string text, Signage signage, bool hideText)
                {
                    this.text = text;
                    this.signage = signage;
                    this.hideText = hideText;
                }
            }                       

            private class ImageSize
            {
                public int Width { get; }
                public int Height { get; }
                public int ImageWidth { get; }
                public int ImageHeight { get; }

                public ImageSize(int width, int height) : this(width, height, width, height) { }
                public ImageSize(int width, int height, int imageWidth, int imageHeight)
                {
                    Width = width;
                    Height = height;
                    ImageWidth = imageWidth;
                    ImageHeight = imageHeight;
                }

                public static Dictionary<string, ImageSize> Sizes = new Dictionary<string, ImageSize>
                {
                    ["sign.pictureframe.landscape"] = new ImageSize(256, 128),
                    ["sign.pictureframe.tall"] = new ImageSize(128, 512),
                    ["sign.pictureframe.portrait"] = new ImageSize(128, 256),
                    ["sign.pictureframe.xxl"] = new ImageSize(1024, 512),
                    ["sign.pictureframe.xl"] = new ImageSize(512, 512),
                    ["sign.small.wood"] = new ImageSize(128, 64),
                    ["sign.huge.wood"] = new ImageSize(512, 128),
                    ["sign.medium.wood"] = new ImageSize(256, 128),
                    ["sign.large.wood"] = new ImageSize(256, 128),
                    ["sign.hanging.banner.large"] = new ImageSize(64, 256),
                    ["sign.pole.banner.large"] = new ImageSize(64, 256),
                    ["sign.post.single"] = new ImageSize(128, 64),
                    ["sign.post.double"] = new ImageSize(256, 256),
                    ["sign.post.town"] = new ImageSize(256, 128),
                    ["sign.post.town.roof"] = new ImageSize(256, 128),
                    ["sign.hanging"] = new ImageSize(128, 256),
                    ["sign.hanging.ornate"] = new ImageSize(256, 128),
                    ["spinner.wheel.deployed"] = new ImageSize(512, 512, 285, 285),
                };
            }
        }
        #endregion

        #region Component
        class ItemPlacement : MonoBehaviour
        {
            private BasePlayer player;
            private DroppedItem droppedItem;
            private Signage hitEntity = null;
            private bool isValidPlacement;

            private float placementDistance = 3f;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;

                SpawnDroppedItem(player.GetActiveItem()?.name);
                player.ChatMessage(ins.msg("placeHelp1", player.userID));
            }

            private void FixedUpdate()
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != skullId)
                    CancelPlacement();

                isValidPlacement = false;

                InputState input = player.serverInput;
                Vector3 eyePosition = player.transform.position + (Vector3.up * 1.8f);

                RaycastHit hit;
                if (Physics.Raycast(new Ray(player.transform.position + (Vector3.up * 1.7f), Quaternion.Euler(input.current.aimAngles) * Vector3.forward), out hit, placementDistance, layerPlcmnt))
                {
                    droppedItem.transform.position = hit.point + (-droppedItem.transform.up * 0.1f);
                    droppedItem.transform.rotation = Quaternion.LookRotation(eyePosition - droppedItem.transform.position, Vector3.up) * Quaternion.Euler(270, 0, 0);

                    isValidPlacement = hitEntity = hit.GetEntity()?.GetComponent<Signage>();
                }
                else
                {
                    droppedItem.transform.position = new Ray(eyePosition, Quaternion.Euler(input.current.aimAngles) * Vector3.forward).GetPoint(2);
                    droppedItem.transform.rotation = Quaternion.LookRotation(eyePosition - droppedItem.transform.position, Vector3.up) * Quaternion.Euler(270, 0, 0);
                }

                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    if (!isValidPlacement)
                    {
                        player.ChatMessage(ins.msg("placeHelp2", player.userID));
                        return;
                    }
                    else
                    {
                        if (ins.droppedItems.ContainsKey(hitEntity))
                            player.ChatMessage(ins.msg("placeHelp5", player.userID));
                        else PlaceSkull(activeItem);
                    }
                }
                else if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                    CancelPlacement();
            }

            private void SpawnDroppedItem(string name)
            {
                droppedItem = ins.SpawnDroppedItem(name, player.transform.position, false);
                enabled = true;
            }

            public void CancelPlacement()
            {
                enabled = false;
                droppedItem.DestroyItem();
                droppedItem.Kill();
                player.ChatMessage(ins.msg("placeHelp3", player.userID));
                Destroy(this);
            }

            private void PlaceSkull(Item activeItem)
            {
                player.ChatMessage(ins.msg("placeHelp4", player.userID));
                activeItem.MarkDirty();
                activeItem.RemoveFromContainer();                

                ins.InitializeSign(hitEntity, droppedItem.item.name, hitEntity.transform.InverseTransformPoint(droppedItem.transform.position), droppedItem.transform.eulerAngles);

                droppedItem.DestroyItem();
                droppedItem.Kill();

                Destroy(this);
            }

            public void OnPlayerDeath()
            {
                CancelPlacement();
                Destroy(this);
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Removal Options")]
            public RemoveOptions Remove { get; set; }
            [JsonProperty(PropertyName = "Sign Options")]
            public SignOptions Sign { get; set; }
            public class SignOptions
            {
                [JsonProperty(PropertyName = "Auto-generate sign image using skull owner")]
                public bool GenerateImage { get; set; }
                [JsonProperty(PropertyName = "Image background color (hex without the #)")]
                public string BackgroundColor { get; set; }
                [JsonProperty(PropertyName = "Text color (hex without the #)")]
                public string TextColor { get; set; }
                [JsonProperty(PropertyName = "Text size")]
                public int TextSize { get; set; }
            }
            public class RemoveOptions
            {
                [JsonProperty(PropertyName = "Allow skulls to be removed")]
                public bool RemoveSkulls { get; set; }
                [JsonProperty(PropertyName = "Require building privilege to remove skulls")]
                public bool RequirePrivilege { get; set; }

            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Remove = new ConfigData.RemoveOptions
                {
                    RemoveSkulls = true,
                    RequirePrivilege = true
                },
                Sign = new ConfigData.SignOptions
                {
                    BackgroundColor = "282828",
                    GenerateImage = true,
                    TextColor = "cccccc",
                    TextSize = 60
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 05))
                configData.Sign.TextSize = baseConfig.Sign.TextSize;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.data = new Dictionary<uint, StoredData.TrophyData>();

            foreach (var entry in droppedItems)
            {
                if (entry.Key == null || entry.Key.IsDestroyed || entry.Value == null || entry.Value.IsDestroyed)
                    continue;
                storedData.data.Add(entry.Key.net.ID, new StoredData.TrophyData(entry.Value));
            }

            data.WriteObject(storedData);
        }

        private void LoadData()
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

        private class StoredData
        {
            public Dictionary<uint, TrophyData> data = new Dictionary<uint, TrophyData>();

            public class TrophyData
            {
                public float[] position;
                public float[] rotation;
                public string displayName;

                public TrophyData() { }

                public TrophyData(DroppedItem item)
                {
                    position = new float[] { item.transform.localPosition.x, item.transform.localPosition.y, item.transform.localPosition.z };
                    rotation = new float[] { item.transform.eulerAngles.x, item.transform.eulerAngles.y, item.transform.eulerAngles.z };
                    displayName = item.item.name;
                }
            }
        }
        #endregion

        #region Localization
        string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {            
            ["placeHelp1"] = "<color=#939393>Use the <color=#ce422b>fire</color> button to place this skull on any sign</color>",
            ["placeHelp2"] = "<color=#939393>Skulls can only be placed on signs</color>",
            ["placeHelp3"] = "<color=#ce422b>Skull placement cancelled!</color>",
            ["placeHelp4"] = "<color=#ce422b>Skull placed!</color>",
            ["placeHelp5"] = "<color=#939393>This sign already has a skull on it!</color>",
            ["noAuth"] = "<color=#939393>You need building auth to remove skulls</color>"
        };
        #endregion
    }
}
