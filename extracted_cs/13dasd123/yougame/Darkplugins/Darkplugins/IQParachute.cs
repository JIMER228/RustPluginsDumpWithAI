// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using UnityEngine;
using static SamSite;
using VLB;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using Oxide.Game.Rust.Cui;
using Oxide.Core;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("IQParachute", "https://discord.gg/dNGbxafuJn", "1.2.6")]
    [Description("A real paratrooper damn")]
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
    // Обновление 1.2.6
    /// - Добавлена мультиязычность в конфигурации
    /// - Исправлен NRE в GetLang
    /// - Исправлена проблема с блокировкой инвентаря если парашют открылся неуспешно
    /// - Добавлена возможность открыть парашют спрыгнув с любой высоты (настраивается в конфигурации)
    /// 
    class IQParachute : RustPlugin
    {

        
                [ConsoleCommand("chute.give")]
        private void Console_ChuteAdd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player != null) return;

            if (arg.Args == null || arg.Args.Length < 1)
            {
                if (player == null) Puts("Wrong syntax! Usage: chute.give <SteamID:Name:IP>");
                else player.ConsoleMessage("Wrong syntax! Usage: chute.give <SteamID:Name:IP>");
                return;
            }

            String playerid = arg.Args[0];
            BasePlayer basePlayer = BasePlayer.Find(playerid);
            if (basePlayer == null)
            {
                if (player == null) Puts("Error! Player not found!");
                else player.ConsoleMessage("Error! Player not found!");
                return;
            }

            basePlayer.GiveItem(_config.ChuteSettings.Chute.ToItem());

            if (player == null) Puts("Success! Item added to inventory!");
            else player.ConsoleMessage("Success! Item added to inventory!");
        }
        private Boolean IsPVE() => TruePVE != null || NextGenPVE != null || Imperium != null;
        private static MonumentInfo BanditTown;
        public void InitializeNPC(Vector3 pos, Quaternion rot)
        {
            InvisibleVendingMachine vending = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab", pos, rot) as InvisibleVendingMachine;
            vending.Spawn();
            vending.OwnerID = 435322777;
            vending.skinID = 435322777;
            vending.shopName = _config.ShopChuteSetting.ShopName;
            vending.CancelInvoke(vending.InstallFromVendingOrders);
            vending.ClearSellOrders();
            NPCPlayer npc = GameManager.server.CreateEntity("assets/prefabs/npc/bandit/shopkeepers/boat_shopkeeper.prefab", pos, rot) as NPCPlayer;
            if (npc == null)
            {
                Interface.Oxide.LogError($"Initializing NPCShopKeeper failed! NPC Component == null #3");
                return;
            }
            npc.SetFlag(BaseEntity.Flags.Busy, true);

            npc.userID = _config.ShopChuteSetting.NPCShopSetting.userID;
            npc.OwnerID = 435322777;
            npc.name = _config.ShopChuteSetting.NPCShopSetting.Name;
            npc.displayName = npc.name; 
            npc.Spawn();
            npc.eyes.rotation = Quaternion.LookRotation(vending.transform.rotation * Vector3.forward, Vector3.up);
            npc.OverrideViewAngles(vending.transform.eulerAngles);
            npc.ServerRotation = npc.eyes.rotation;
            npc.SendNetworkUpdate();

            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(_config.ChuteSettings.Chute.ShortName);
            if (itemDefinition == null)
            {
                PrintError($"ItemDefinition from BuyItem ShortName {_config.ChuteSettings.Chute.ShortName} not found.");
                return;
            }
            ItemDefinition x = ItemManager.FindItemDefinition(_config.ShopChuteSetting.VendorShopSetting.Shortname);
            if (x == null)
            {
                PrintError($"ItemDefinition from PayItem ShortName {_config.ShopChuteSetting.VendorShopSetting.Shortname} not found.");
                return;
            }
            AddItemForSale(vending, itemDefinition.itemid, 1, x.itemid, _config.ShopChuteSetting.VendorShopSetting.Amount, vending.GetBPState(false, false), 300, _config.ChuteSettings.Chute.SkinId);
            MonumentEntities.Add(npc);
            MonumentEntities.Add(vending);

            if (_config.ShopChuteSetting.NPCShopSetting.Wear.Count > 0)
            {
                npc.inventory.Strip();
                for (int i = 0; i < _config.ShopChuteSetting.NPCShopSetting.Wear.Count; i++)
                    ItemManager.Create(ItemManager.FindItemDefinition(_config.ShopChuteSetting.NPCShopSetting.Wear[i].ShortName), 1, _config.ShopChuteSetting.NPCShopSetting.Wear[i].SkinId).MoveToContainer(npc.inventory.containerWear);
            }
        }

        private void ClearEnt(MonumentInfo monument,Vector3 Pos)
        {
            List<BaseEntity> obj = new List<BaseEntity>();
            Int32 Mask = LayerMask.GetMask("AI", "Player (Server)", "Construction", "Deployable", "Deployed", "Ragdoll", "Transparent");
            Vis.Entities(Pos, 1000f, obj, Mask);

            foreach (BaseEntity item in obj.Where(x => x.OwnerID == 435322777 || x.skinID == 435322777))
            {
                if (item == null)
                    continue;
                item.Kill(BaseNetworkable.DestroyMode.None);
            }
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PARACHUTE_DONT_OPEN"] = "Oh shit, the parachute is faulty! He couldn't open up!!!",
                ["PARACHUTE_OPEN_INFO"] = "OPEN PARACHUTE - PRESS <color=#68d3f9>[R]</color>",


            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PARACHUTE_DONT_OPEN"] = "Оу черт, парашют неисрпавен! Он не смог раскрыться!!!",
                ["PARACHUTE_OPEN_INFO"] = "ОТКРЫТЬ ПАРАШЮТ - НАЖМИТЕ <color=#68d3f9>[R]</color>",

            }, this, "ru");
        }
        
        
        
        public class TriggerJump : FacepunchBehaviour//
        {
            private BaseEntity entity;
            private Single Radius;
            private List<BasePlayer> PlayerTriggers = new List<BasePlayer>();
            private void Awake()
            {
                gameObject.layer = (Int32)Rust.Layer.Reserved1;
                gameObject.name = "TRIGGER_JUMPING";
            }
            public void Init(BaseEntity entity,Single Radius)
            {
                this.entity = entity;
                this.Radius = Radius;
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
            }
            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;

                if (!PlayerTriggers.Contains(player))
                    PlayerTriggers.Add(player);
            }
            private void OnTriggerExit(Collider other)
            {       
                BasePlayer player = other.GetComponentInParent<BasePlayer>();

                if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;
                if (!PlayerTriggers.Contains(player)) return;
                if (player.isMounted) return;
                if (Vector3.Distance(player.transform.position, entity.transform.position) < 10f) return;

                _.OnEntityDismounted(null, player);
                PlayerTriggers.Remove(player);
            }
            private void OnDestroy()
            {
                UnityEngine.GameObject.Destroy(this.gameObject);
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = Radius;
                }
            }
        }
        private void SpawnPreset(MonumentInfo monument, List<Point> PointList)
        {
            ClearEnt(monument, monument.transform.TransformPoint(PointList[0].Pos));

            timer.Once(5f, () =>
            {
                foreach (Point point in PointList)
                {
                    Vector3 Position = monument.transform.TransformPoint(point.Pos);
                    Quaternion Rotation = GetMonumentRotation(point.Y, monument, point.X);

                    switch (point.Name)
                    {
                        case "NPC.POINT":
                            {
                                InitializeNPC(Position, Rotation);
                                break;
                            }
                        case "TABLES.POINT":
                            {
                                SpawnTables(Position, Rotation, point.LinkImage);
                                break;
                            }
                    }
                }
            });
        }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        
                private class SAMTargetComponent : FacepunchBehaviour, ISamSiteTarget
        {
            public static List<SAMTargetComponent> SAMTargetComponents = new List<SAMTargetComponent>();
            public static void AddComp(BaseCombatEntity ENT) =>
                ENT.GetOrAddComponent<SAMTargetComponent>();

            public static void RemoveComp(BaseCombatEntity ENT)
            {
                SAMTargetComponent samComponent = ENT.GetComponent<SAMTargetComponent>();
                if (samComponent != null)
                    DestroyImmediate(samComponent);
            }

            private GameObject _child;
            private void Awake()
            {
                SAMTargetComponents.Add(this);
                baseEntity = GetComponent<BaseEntity>();
                if (baseEntity is BaseHelicopter)
                {
                    _child = baseEntity.gameObject.CreateChild();
                    _child.gameObject.layer = (int)Rust.Layer.Vehicle_World;
                    _child.AddComponent<SphereCollider>();
                }
            }
            public BaseEntity baseEntity;

            private void OnDestroy()
            {
                if (_child != null)
                    DestroyImmediate(_child);
                if (SAMTargetComponents.Contains(this))
                    SAMTargetComponents.Remove(this);

            }
            public bool IsValidSAMTarget() => true;

            public Vector3 Position => baseEntity.transform.position;

            public SamTargetType SAMTargetType => SamSite.targetTypeVehicle;

            public bool isClient => false;

            public bool IsValidSAMTarget(bool isStaticSamSite) => true;

            public Vector3 CenterPoint() => baseEntity.CenterPoint();

            public Vector3 GetWorldVelocity() => baseEntity.GetWorldVelocity();
            public bool IsVisible(Vector3 position, float distance) => baseEntity.IsVisible(position, distance);

        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ChuteSettings = new PluginConfig.Chutes
                {
                    UseChuteAllOpen = false,
                    UseAutoOpen = true,
                    UseSamSite = true,
                    Chute = new CustomItem("Parachute", "sunglasses", 2663677167),
                    Turned = new PluginConfig.Chutes.TurnedFuncion
                    {
                        EffectSetting = new PluginConfig.Chutes.TurnedFuncion.Effect
                        {
                            UseEffect = true,
                            PathEffect = "assets/bundled/prefabs/fx/smoke_signal_full.prefab",
                        },
                        OpenChutes = new PluginConfig.Chutes.TurnedFuncion.OpenChute
                        {
                            UseRareOpen = true,
                            RareOpen = 80,
                        }
                    },
                    FlyControlling = new PluginConfig.Chutes.FlyController
                    {
                        maxDropSpeed = -10f,
                        upForce = 7f,
                        forwardStrength = 4f,
                        backwardStrength = 4f,
                        rotationStrength = 0.4f,
                        forwardResistance = 0.3f,
                        rotationResistance = 0.5f,
                        glidedevider = 3.0f,
                        decendmultiplyer = 1.5f,
                        angularModifier = 30f,
                    },
                },
                ShopChuteSetting = new PluginConfig.ShopChute
                {
                    ShopName = "SKYDIVING SHOP",
                    TurnedShop = new PluginConfig.ShopChute.TurnedShopSpawn
                    {
                        UseAirfield = true,
                        UseBanditTown = true,
                        UseCompound = true,
                    },
                    VendorShopSetting = new PluginConfig.ShopChute.VendorShop
                    {
                        Shortname = "scrap",
                        Amount = 100,
                        SkinID = 0,
                    },
                    NPCShopSetting = new PluginConfig.ShopChute.NPCShop
                    {
                        Name = "SKYDIVER",
                        userID = 1321,
                        Wear = new List<PluginConfig.ShopChute.NPCShop.ItemsNpc>
                       {
                           new PluginConfig.ShopChute.NPCShop.ItemsNpc
                           {
                               ShortName = "shoes.boots",
                               SkinId = 1526996873,
                           },
                           new PluginConfig.ShopChute.NPCShop.ItemsNpc
                           {
                               ShortName = "pants",
                               SkinId = 963524525,
                           },
                           new PluginConfig.ShopChute.NPCShop.ItemsNpc
                           {
                               ShortName = "tshirt",
                               SkinId = 860007266,
                           },
                           new PluginConfig.ShopChute.NPCShop.ItemsNpc
                           {
                               ShortName = "metal.facemask",
                               SkinId = 1720716475,
                           },
                           new PluginConfig.ShopChute.NPCShop.ItemsNpc
                           {
                               ShortName = "metal.plate.torso",
                               SkinId = 1720718368,
                           }
                       }
                    },
                },
                ReferenceSetting = new PluginConfig.ReferencePlugin
                {
                    IQChatSetting = new PluginConfig.ReferencePlugin.IQChatReference
                    {
                        CustomAvatar = "",
                        CustomPrefix = "[IQParachute]",
                        UIAlertUse = false,
                    }
                }
            };
        }
        class FlyFinder : FacepunchBehaviour
        {
            BasePlayer player;
            public FlyFinder()
            {
                enabled = false;
            }

            public void SetPlayer(BasePlayer player)
            {
                this.player = player;
                enabled = true;

                if (_config.ChuteSettings.UseAutoOpen)
                    InvokeRepeating(CheckPreOpening, 0f, 1f);
            }
            private void CheckPreOpening()
            {
                if (_.JumpingPlayer.Contains(player))
                    if (Physics.Raycast(new Ray(player.transform.position, Vector3.down), 50f, LAND_LAYERS) && !player.IsOnGround())
                    {
                        _.OpenChute(player);
                        CancelInvoke(CheckPreOpening);
                    }
            }
            public void OnDestroy() => KillParent();

            public void KillParent()
            {
                enabled = false;
                UnityEngine.GameObject.Destroy(this);
            }
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            if (sb == null)
                sb = new StringBuilder();

            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        private const Int32 LAND_LAYERS = 1 << 4 | 1 << 8 | 1 << 16 | 1 << 21 | 1 << 23;
        void OnServerInitialized()
        {
            foreach(BasePlayer p in BasePlayer.activePlayerList.Where(x => x.inventory.containerWear.IsLocked()))
                p.inventory.containerWear.SetLocked(false);

            _ = this;

            BanditTown = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("bandit_town"));
            Compound = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("compound"));
            Airfield = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("airfield"));

            if (!_config.ChuteSettings.UseChuteAllOpen)
            {
                Unsubscribe("CanWearItem");
                Unsubscribe("OnItemAddedToContainer");
                Unsubscribe("OnItemDropped");
                Unsubscribe("CanAcceptItem");
            }

            if (!IsInitializeNPCShop())
            {
                Unsubscribe("OnVendingTransaction");
                Unsubscribe("OnEntityTakeDamage");
                Unsubscribe("CanBradleyApcTarget");
                Unsubscribe("OnNpcTarget");
            }
            else
            {
                if (IsCompound())
                    SpawnPreset(Compound, PointsList["COMPOUND"]);
                if (IsBanitTown())
                    SpawnPreset(BanditTown, PointsList["BANDIT_TOWN"]);
                if (IsAirfield())
                    SpawnPreset(Airfield, PointsList["AIRFIELD"]);
            }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            if(IsPVE())
                Unsubscribe("OnEntityTakeDamage");

            if (!_config.ChuteSettings.UseSamSite)
            {
                Unsubscribe("OnSamSiteTargetScan");
                Unsubscribe("CanSamSiteShoot");
            }
        }

        public struct CustomItem
        {
            public CustomItem(String displayName, String shortName, UInt64 skinId)
            {
                DisplayName = displayName;
                ShortName = shortName;
                SkinId = skinId;
            }

            public String DisplayName;
            public String ShortName;
            public UInt64 SkinId;

            public Item ToItem(int amount = 1)
            {
                Item item = ItemManager.CreateByName(ShortName, amount, SkinId);
                if (item != null && String.IsNullOrEmpty(DisplayName) == false)
                    item.name = DisplayName;

                return item;
            }

            public bool CompareTo(Item item)
            {
                return this.SkinId == item?.skin && this.ShortName == item?.info.shortname;
            }
        }
        object OnNpcTarget(BaseAnimalNPC animal, BasePlayer target)
        {
            if (animal == null || target == null) return null;
            if (target.OwnerID == 435322777)
                return false;
            return null;
        }
        
                private Dictionary<BasePlayer, FlyFinder> FlyFinderList = new Dictionary<BasePlayer, FlyFinder>();
        private void OnSamSiteTargetScan(SamSite samSite, List<ISamSiteTarget> targetList)
        {
            if (SAMTargetComponent.SAMTargetComponents.Count == 0)
                return;

            foreach (var SAMtargetcomp in SAMTargetComponent.SAMTargetComponents)
                targetList.Add(SAMtargetcomp);
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                _config = GetDefaultConfig();
            }
        }

        
        
        private List<ChuteConroller> ChuteConrollerList = new List<ChuteConroller>();
        void OnServerShutdown() => Unload();

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }
        public void AddItemForSale(VendingMachine vending, int itemID, int amountToSell, int currencyID, int currencyPerTransaction, byte bpState, int maxByStack, ulong SkinID)
        {
            vending.AddSellOrder(itemID, amountToSell, currencyID, currencyPerTransaction, bpState);
            vending.transactionActive = true;
            if (bpState == 1 || bpState == 3)
            {
                for (int i = 0; i < maxByStack; i++)
                {
                    global::Item item = ItemManager.CreateByItemID(vending.blueprintBaseDef.itemid, 1, _config.ShopChuteSetting.VendorShopSetting.SkinID);
                    item.blueprintTarget = itemID;
                    vending.inventory.Insert(item);
                }
            }
            else
            {
                vending.inventory.AddItem(ItemManager.FindItemDefinition(itemID), amountToSell * maxByStack, SkinID);
            }
            vending.transactionActive = false;
            vending.RefreshSellOrderStockLevel(null);
        }
        private static MonumentInfo Compound;
        private Boolean IsBanitTown() => (Boolean)(_config.ShopChuteSetting.TurnedShop.UseBanditTown && BanditTown != null);

        
        private static IEnumerator DownloadImage(string url, Signage sign, string name)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            yield return www.SendWebRequest();
            if (_ == null)
                yield break;
            if (www.isNetworkError || www.isHttpError)
            {
                _.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, name));
                www.Dispose();

                yield break;
            }

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(www.downloadHandler.data);
            if (texture != null)
            {
                byte[] bytes = texture.EncodeToPNG();

                var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();

                var size = Math.Max(sign.paintableSources.Length, 1);
                if (sign.textureIDs == null || sign.textureIDs.Length != size)
                {
                    Array.Resize(ref sign.textureIDs, size);
                }
                if (sign.textureIDs[0] > 0)
                {
                    FileStorage.server.RemoveExact(sign.textureIDs[0], FileStorage.Type.png, sign.net.ID, 0U);
                }
                uint idinstorage = FileStorage.server.Store(bytes, FileStorage.Type.png, sign.net.ID);
                sign.textureIDs[0] = idinstorage;
                sign.SendNetworkUpdate();

                UnityEngine.Object.DestroyImmediate(texture);
            }

            www.Dispose();
            yield break;
        }
        class ChuteConroller : FacepunchBehaviour
        {
            Rigidbody rigidbodyBP;
            BaseEntity backpack;

            Rigidbody rigidbody;
            BaseEntity worldItem;
            public BaseEntity chair;
            BaseEntity parachute;
            BasePlayer player;
           
            public ChuteConroller()
            {
                worldItem = GetComponent<BaseEntity>();
                parachute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(), new Quaternion(), true);
                parachute.enableSaving = false;
                parachute.transform.localPosition = new Vector3(0f, -7f, 0f);
                parachute?.Spawn();

                if (_config.ChuteSettings.Turned.EffectSetting.UseEffect)
                    Effect.server.Run(_config.ChuteSettings.Turned.EffectSetting.PathEffect, parachute, 0, Vector3.zero, Vector3.zero, null, true);

                String chairprefab = "assets/prefabs/vehicle/seats/standingdriver.prefab";
                chair = GameManager.server.CreateEntity(chairprefab, new Vector3(), new Quaternion(), true);
                chair.enableSaving = false;
                chair.Spawn();
                chair.skinID = 99999995;
                chair.transform.localPosition = new Vector3(0.0f, -1.2f, 0.3f);
                BaseMountable HasMount = chair.GetComponent<BaseMountable>();
                HasMount.dismountPositions = new[] { parachute.transform };
                chair.GetComponent<MeshCollider>().convex = true;
                chair.SetParent(parachute, 0, false, false);
                chair.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                chair.UpdateNetworkGroup();
                if (_config.ChuteSettings.UseSamSite)
                    SAMTargetComponent.AddComp(chair as BaseCombatEntity);

                parachute.SetParent(worldItem, 0, false, false);

                rigidbody = worldItem.GetComponent<Rigidbody>();
                rigidbody.isKinematic = false;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
                enabled = false;
            }

            private void SetBackpack()
            {
                backpack = GameManager.server.CreateEntity("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position + new Vector3(0f, 1f, 0f), new Quaternion());
                rigidbodyBP = backpack.GetComponent<Rigidbody>();
                rigidbodyBP.useGravity = false;
                rigidbodyBP.isKinematic = true;
                rigidbodyBP.drag = 0;
                rigidbodyBP.interpolation = RigidbodyInterpolation.Extrapolate;

                backpack.Spawn();
                backpack.SetFlag(BaseEntity.Flags.Locked, true);
                backpack.SetFlag(BaseEntity.Flags.Busy, true);
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
                backpack.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                backpack.UpdateNetworkGroup();

                backpack.transform.localPosition = new Vector3(-0.05f, 0.06f, 0f);
                backpack.transform.localRotation = new Quaternion(-3f, 0f, 3f, 0f);

                backpack.SetParent(player, "spine2");
            }
            public void SetPlayer(BasePlayer player)
            {
                this.player = player;
                CheckPlayer();
                chair.GetComponent<BaseMountable>().MountPlayer(player);
                SetBackpack();
                enabled = true;
            }

            public void CheckPlayer()
            {
                if (chair == null)
                {
                    OnDestroy();
                    return;
                }
                if (Physics.Raycast(new Ray(chair.transform.position, Vector3.down), 1.5f, LAND_LAYERS))
                {
                    OnDestroy();
                    return;
                }

                if (!player.IsAlive() || player.IsSleeping())
                {
                    player.EnsureDismounted();
                    OnDestroy();
                    return;
                }

                foreach (Collider col in Physics.OverlapSphere(chair.transform.position, 2.0f, LAND_LAYERS))
                {
                    BaseEntity baseEntity = col.gameObject.ToBaseEntity();

                    if (baseEntity != null && (baseEntity == chair || baseEntity == player.GetComponent<BaseEntity>()))
                        continue;
                    else
                    {
                        OnDestroy();
                        return;
                    }
                }
                if (TerrainMeta.HeightMap.GetHeight(chair.transform.position) >= chair.transform.position.y)
                {
                    Vector3 newPos = chair.transform.position;
                    newPos.y = TerrainMeta.HeightMap.GetHeight(chair.transform.position);
                    OnDestroy();
                    player.Teleport(newPos);
                    return;
                }
            }
            public void OnDestroy() => KillParent();

            public void KillParent()
            {
                enabled = false;

                if (chair != null && chair.GetComponent<BaseMountable>().IsMounted())
                    chair.GetComponent<BaseMountable>().DismountPlayer(player, false);
                if (player != null)
                {
                    if (_.JumpingPlayer.Contains(player))
                        _.JumpingPlayer.Remove(player);

                    if (player.isMounted)
                        player.DismountObject();
                }

                if (!chair.IsDestroyed)
                {
                    if (_config.ChuteSettings.UseSamSite)
                        SAMTargetComponent.RemoveComp(chair as BaseCombatEntity);

                    chair.Kill();
                }
                if (!parachute.IsDestroyed)
                    parachute.Kill();
                if (!backpack.IsDestroyed)
                    backpack.Kill();
                if (!worldItem.IsDestroyed)
                    worldItem.Kill();

                Item Chute = _.GetChute(player);
                if (Chute != null)
                {
                    Chute.Remove();
                    Chute.GetRootContainer().SetLocked(false);
                    player.inventory.SendUpdatedInventory(PlayerInventory.Type.Wear, player.inventory.containerWear);
                }

                UnityEngine.GameObject.Destroy(this.gameObject);
            }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            private void FixedUpdate()
            {
                CheckPlayer();
                
                if (player.serverInput.IsDown(BUTTON.SPRINT))
                    rigidbody.AddForce(Vector3.up * ((_config.ChuteSettings.FlyControlling.maxDropSpeed / _config.ChuteSettings.FlyControlling.glidedevider) - rigidbody.velocity.y), ForceMode.Impulse);
              
                if (player.serverInput.IsDown(BUTTON.DUCK) && !player.serverInput.IsDown(BUTTON.SPRINT))
                    rigidbody.AddForce(Vector3.up * ((_config.ChuteSettings.FlyControlling.maxDropSpeed * _config.ChuteSettings.FlyControlling.decendmultiplyer) - rigidbody.velocity.y), ForceMode.Impulse);

                if (rigidbody.velocity.y < _config.ChuteSettings.FlyControlling.maxDropSpeed)
                    rigidbody.AddForce(Vector3.up * (_config.ChuteSettings.FlyControlling.maxDropSpeed - rigidbody.velocity.y), ForceMode.Impulse);

                rigidbody.AddForce(Vector3.up * _config.ChuteSettings.FlyControlling.upForce, ForceMode.Acceleration);

                if (rigidbody.velocity.x < 0f || rigidbody.velocity.x > 0f || rigidbody.velocity.z < 0f || rigidbody.velocity.z > 0f)
                    rigidbody.AddForce(new Vector3(-rigidbody.velocity.x, 0f, -rigidbody.velocity.z) * _config.ChuteSettings.FlyControlling.forwardResistance, ForceMode.Acceleration);

                if (rigidbody.angularVelocity.y > 0f || rigidbody.angularVelocity.y > 0f)
                    rigidbody.AddTorque(new Vector3(0f, -rigidbody.angularVelocity.y, 0f) * _config.ChuteSettings.FlyControlling.rotationResistance, ForceMode.Acceleration);

                if (player.serverInput.IsDown(BUTTON.FORWARD))
                    rigidbody.AddForce(rigidbody.transform.forward * _config.ChuteSettings.FlyControlling.forwardStrength, ForceMode.Acceleration);

                if (player.serverInput.IsDown(BUTTON.BACKWARD))
                    rigidbody.AddForce(-rigidbody.transform.forward * _config.ChuteSettings.FlyControlling.backwardStrength, ForceMode.Acceleration);

                if (player.serverInput.IsDown(BUTTON.RIGHT))
                    rigidbody.AddTorque(Vector3.up * _config.ChuteSettings.FlyControlling.rotationStrength, ForceMode.Acceleration);

                if (player.serverInput.IsDown(BUTTON.LEFT))
                    rigidbody.AddTorque(Vector3.up * - _config.ChuteSettings.FlyControlling.rotationStrength, ForceMode.Acceleration);

                if (rigidbody.angularVelocity.y > 0f || rigidbody.angularVelocity.y < 0f)
                    worldItem.transform.rotation = Quaternion.Euler(worldItem.transform.rotation.eulerAngles.x, worldItem.transform.rotation.eulerAngles.y, -rigidbody.angularVelocity.y * _config.ChuteSettings.FlyControlling.angularModifier);
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        
                private object CanBradleyApcTarget(BradleyAPC apc, BaseEntity entity)
        {
            if (apc == null || entity == null) return null;

            foreach (var Targets in apc.targetList.Where(ent => ent.entity != null && ent.entity.OwnerID == 435322777))
                    NextTick(() => { apc.targetList.Remove(Targets); });

            return null;
        }
        [PluginReference] Plugin IQChat, TruePVE, NextGenPVE, Imperium, Convoy;

        private static LayerMask layerMask;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        
                private List<BasePlayer> JumpingPlayer = new List<BasePlayer>();
        private void DestroyOBJ(UnityEngine.Object obj) => UnityEngine.Object.DestroyImmediate(obj);

        object OnVendingTransaction(VendingMachine machine, BasePlayer player, int sellOrderId, int numberOfTransactions)
        {
            if (machine.OwnerID == 435322777)
            {
                ProtoBuf.VendingMachine.SellOrder sellOrder = machine.sellOrders.sellOrders[sellOrderId];

                Item item = machine.inventory.itemList.Where(x => x.info.itemid == sellOrder.itemToSellID).First();
                Item sell = player.inventory.FindItemIDs(sellOrder.currencyID).First();


                List<global::Item> list = Facepunch.Pool.GetList<global::Item>();

                List<global::Item> list2 = player.inventory.FindItemIDs(sellOrder.currencyID);
                if (sellOrder.currencyIsBP)
                {
                    list2 = (from x in player.inventory.FindItemIDs(machine.blueprintBaseDef.itemid)
                             where x.blueprintTarget == sellOrder.currencyID
                             select x).ToList<global::Item>();
                }
                list2 = (from x in list2
                         where !x.hasCondition || (x.conditionNormalized >= 0.5f && x.maxConditionNormalized > 0.5f)
                         select x).ToList<global::Item>();
                if (list2.Count == 0)
                {
                    Facepunch.Pool.FreeList<global::Item>(ref list);
                    return false;
                }
                int num3 = list2.Sum((global::Item x) => x.amount);
                int num4 = sellOrder.currencyAmountPerItem * numberOfTransactions;
                if (num3 < num4)
                {
                    Facepunch.Pool.FreeList<global::Item>(ref list);
                    return false;
                }
                machine.transactionActive = true;
                int num5 = 0;
                foreach (global::Item itemss in list2)
                {
                    int num6 = Mathf.Min(num4 - num5, itemss.amount);
                    global::Item item2;
                    if (itemss.amount <= num6)
                    {
                        item2 = itemss;
                    }
                    else
                    {
                        item2 = itemss.SplitItem(num6);
                    }
                    machine.TakeCurrencyItem(item2);
                    num5 += num6;
                    if (num5 >= num4)
                    {
                        break;
                    }
                }

                Item itemToGive = ItemManager.CreateByName(item.info.shortname, sellOrder.itemToSellAmount * numberOfTransactions, _config.ChuteSettings.Chute.SkinId);
                itemToGive.name = _config.ChuteSettings.Chute.DisplayName;
                if (itemToGive.info.shortname == "smallwaterbottle")
                {
                    var item1 = ItemManager.CreateByPartialName("water", 50);
                    item1.MoveToContainer(itemToGive.contents);
                }
                player.GiveItem(itemToGive, BaseEntity.GiveItemReason.PickedUp);
                Facepunch.Pool.FreeList<global::Item>(ref list);
                machine.UpdateEmptyFlag();
                machine.transactionActive = false;
                return false;
            }
            return null;
        }
        
        
                void Init()
        {
            layerMask = (1 << 29);
            layerMask |= (1 << 18);
            layerMask = ~layerMask;
        }
        private static IQParachute _;

        private void CanSamSiteShoot(SamSite samSite)
        {
            if (samSite == null) return;
            BaseEntity target = null;
            foreach (var SAMtargetcomp in SAMTargetComponent.SAMTargetComponents)
            {
                if (target == null || Vector3.Distance(SAMtargetcomp.baseEntity.transform.position, samSite.transform.position) > 50f || Vector3.Distance(SAMtargetcomp.baseEntity.transform.position, samSite.transform.position) < Vector3.Distance(target.transform.position, samSite.transform.position))
                    target = SAMtargetcomp.baseEntity;
            }

            if (target == null)
                return;
            
            if (target is BaseVehicleSeat && target.skinID == 99999995)
            {
                Vector3 targetVelocity = (target.transform.position.normalized * 20f) * 1.25f;
                Vector3 estimatedPoint = PredictedPos(target, samSite, targetVelocity) - new Vector3(0f, 30f, 0f);
                samSite.currentAimDir = (estimatedPoint - samSite.eyePoint.transform.position).normalized;
                return;
            }
        }
        private void OnEntitySpawned(HotAirBalloon entity)
        {
            if (entity == null) return;
            TriggerJump Trigger = entity.gameObject.AddComponent<TriggerJump>();
            Trigger.Init(entity, 10f);
        }
        private List<BaseEntity> MonumentEntities = new List<BaseEntity>();

        private object OnSamSiteTarget(SamSite entity, SAMTargetComponent target)
        {
            if (target == null || entity == null) return null;

            BaseVehicleSeat chair = target.baseEntity as BaseVehicleSeat;
            if (chair == null)
                return null;

            BasePlayer player = chair.GetMounted();
            if (player == null)
                return null;

            if (player.userID == entity.OwnerID)
                return false;

            return null;
        }

        private Dictionary<String, List<Point>> PointsList = new Dictionary<String, List<Point>>
        {
            ["COMPOUND"] = new List<Point>
            {
                new Point
                {
                    Name = "NPC.POINT",
                    Y = 40f,
                    X = 0f,
                    Pos = new Vector3(-4.3f, 0.3f, -1.4f),
                    LinkImage = String.Empty,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = 0f,
                    X = 0f,
                    Pos = new Vector3(-8f, 2.5f, -1.8f),
                    LinkImage = SignImgLeft,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = 0,
                    X = 0f,
                    Pos = new Vector3(3.7f, 0.6f, 16.2f),
                    LinkImage = SignImgRight,
                },
            },
            ["BANDIT_TOWN"] = new List<Point>
            {
                new Point
                {
                    Name = "NPC.POINT",
                    Y = -65f,
                    X = 0f,
                    Pos = new Vector3(-54f, 2f, 28f),
                    LinkImage = String.Empty,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = -142f,
                    X = 50f,
                    Pos = new Vector3(-52.45f, 2f, 32f),
                    LinkImage = SignImgLeft,
                },
            },
            ["AIRFIELD"] = new List<Point>
            {
                new Point
                {
                    Name = "NPC.POINT",
                    Y = 20f,
                    X = 0f,
                    Pos = new Vector3(-22.1f, 1.8f, -75.4f),
                    LinkImage = String.Empty,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = 0f,
                    X = 0f,
                    Pos = new Vector3(-24.5f, 2.8f, -75.8f),
                    LinkImage = SignImgLeft,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = -30f,
                    X = 30f,
                    Pos = new Vector3(-60.3f, 2.1f, -74.5f),
                    LinkImage = SignImgLeft,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = 180f,
                    X = 13f,
                    Pos = new Vector3(-158f, 0.22f, -76.8f),
                    LinkImage = SignImgRight,
                },
                new Point
                {
                    Name = "TABLES.POINT",
                    Y = 10f,
                    X = 20f,
                    Pos = new Vector3(45.8f, 0.25f, -64.9f),
                    LinkImage = SignImgRight,
                },
            },
        };
        public void SendChat(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (_config.ReferenceSetting.IQChatSetting.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, _config.ReferenceSetting.IQChatSetting.CustomPrefix, _config.ReferenceSetting.IQChatSetting.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        private const String SignImgLeft = "https://i.imgur.com/xcflG7e.png";
        
                private void SpawnTables(Vector3 Position, Quaternion Rotation, String Image)
        {
            String Prefab = "assets/prefabs/deployable/signs/sign.large.wood.prefab";
            Signage Tables = GameManager.server.CreateEntity(Prefab, Position, Rotation) as Signage;
            Tables.Spawn();
            UnityEngine.Object.DestroyImmediate(Tables.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(Tables.GetComponent<GroundWatch>());
            Tables.OwnerID = 435322777;
            ServerMgr.Instance.StartCoroutine(DownloadImage(Image, Tables, "sign.wooden.large"));
            Tables.SetFlag(BaseEntity.Flags.Busy, true);
            Tables.SetFlag(BaseEntity.Flags.Locked, true);
            MonumentEntities.Add(Tables);
        }

        Quaternion GetMonumentRotation(float y, MonumentInfo monument, Single x)
        {
            Quaternion monumentQT = Quaternion.Euler(monument.transform.rotation.eulerAngles.x - x, monument.transform.rotation.eulerAngles.y - y, 0f);
            return monumentQT;
        }

        private Vector3 PredictedPos(BaseEntity target, SamSite samSite, Vector3 targetVelocity)
        {
            Vector3 targetpos = target.transform.TransformPoint(target.transform.GetBounds().center);
            Vector3 displacement = targetpos - samSite.eyePoint.transform.position;
            float projectileSpeed = samSite.projectileTest.Get().GetComponent<ServerProjectile>().speed;
            float targetMoveAngle = Vector3.Angle(-displacement, targetVelocity) * Mathf.Deg2Rad;
            if (targetVelocity.magnitude == 0 || targetVelocity.magnitude > projectileSpeed && Mathf.Sin(targetMoveAngle) / projectileSpeed > Mathf.Cos(targetMoveAngle) / targetVelocity.magnitude)
            {
                return targetpos;
            }
            float shootAngle = Mathf.Asin(Mathf.Sin(targetMoveAngle) * targetVelocity.magnitude / projectileSpeed);
            return targetpos + targetVelocity * displacement.magnitude / Mathf.Sin(Mathf.PI - targetMoveAngle - shootAngle) * Mathf.Sin(shootAngle) / targetVelocity.magnitude;
        }
        
                
        object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            if (inventory == null || item == null) return null;
            BasePlayer player = inventory.gameObject.ToBaseEntity() as BasePlayer;

            if (player != null && item.skin == _config.ChuteSettings.Chute.SkinId && item.info.shortname == _config.ChuteSettings.Chute.ShortName)
            {
                NextTick(() =>
                {
                    if (GetChute(player) == null) return;
                    //addcomponents
                    FlyDetected Detected = player.gameObject.AddComponent<FlyDetected>();
                    Detected.SetPlayer(player);
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
                    if (!FlyDetectedList.ContainsKey(player))
                        FlyDetectedList.Add(player, Detected);
                    else FlyDetectedList[player] = Detected;
                });
            }

            return null;
        }
        object OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (info == null || entity == null)
                return null;
            if (entity.OwnerID == 435322777) return false;
            return null;
        }
        class FlyDetected : FacepunchBehaviour
        {
            BasePlayer player;
            public FlyDetected()
            {
                enabled = false;
            }

            public void SetPlayer(BasePlayer player)
            {
                this.player = player;
                enabled = true;
                InvokeRepeating(CheckFly, 0f, 1f);
            }
            private void CheckFly()
            {
                if (!player.IsOnGround() && !player.isMounted)
                    if (!Physics.CheckSphere(player.transform.position, 2f, LAND_LAYERS))
                    {
                        _.OnEntityDismounted(null, player);
                        return;
                    }
            }

            public void OnDestroy() => KillParent();

            public void KillParent()
            {
                CancelInvoke(CheckFly);
                enabled = false;
                UnityEngine.GameObject.Destroy(this);
            }
        }
        
        
                private void ShowInfo(BasePlayer player)
        {
            if (player == null) return;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            CuiHelper.DestroyUi(player, "UI_LABEL");
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                FadeOut = 0.3f,
                Name = "UI_LABEL",
                Parent = "Overlay",
                Components = {
                    new CuiTextComponent { FadeIn = 0.3f, Text = GetLang("PARACHUTE_OPEN_INFO", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-244.2060 89.65697", OffsetMax = "227.806 123.390" } 
                }
            });

            CuiHelper.AddUi(player, container);

            player.Invoke(() =>
            {
                CuiHelper.DestroyUi(player, "UI_LABEL");
            }, 3f);
        }

        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container == null || item == null) return null;
            BasePlayer player = container.playerOwner;
            if (player == null) return null;
            if (player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return null;

            if (item.skin == _config.ChuteSettings.Chute.SkinId && item.info.shortname == _config.ChuteSettings.Chute.ShortName)
            {

               // NextTick(() => { 
                if (GetChute(player) == null)
                {
                    if (FlyDetectedList.ContainsKey(player))
                        if (FlyDetectedList[player] != null)
                            if (FlyDetectedList[player] != null)
                                DestroyOBJ(FlyDetectedList[player]);
                            
                    ///removecomponents
                }
                //});
            }

            return null;
        }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        private class Point
        {
            public Vector3 Pos;
            public String LinkImage;
            public Single X;
            public Single Y;
            public String Name;
        }
        private static MonumentInfo Airfield;
        void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (entity is BaseBoat || entity is RidableHorse) return;
            Item Chute = _.GetChute(player);
            if (Chute == null) return;

            if (Physics.Raycast(new Ray(player.transform.position, Vector3.down), 3f, LAND_LAYERS)) return;

            if (!JumpingPlayer.Contains(player))
                JumpingPlayer.Add(player);

            player.Invoke(() => PlayerInput(player), 0.01f);
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            if (_config.ChuteSettings.UseAutoOpen)
            {
                FlyFinder Finder = player.gameObject.AddComponent<FlyFinder>();
                Finder.SetPlayer(player);

                if (!FlyFinderList.ContainsKey(player))
                    FlyFinderList.Add(player, Finder);
            }

            ShowInfo(player);
        }

        private object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (type != AntiHackType.FlyHack) return null;
            if (player == null) return null;
            if (JumpingPlayer.Contains(player)) return true;
            return null;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https:jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }
            NextTick(SaveConfig);
            ValidateConfig();
        }
        private void OnEntitySpawned(ScrapTransportHelicopter entity)
        {
            if (entity == null) return;
            TriggerJump Trigger = entity.gameObject.AddComponent<TriggerJump>();
            Trigger.Init(entity, 20f);
        }    


        public void OpenChute(BasePlayer player)
        {
            Item Chute = _.GetChute(player);
            if (Chute == null) return;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            if (_config.ChuteSettings.UseChuteAllOpen)
                if (FlyDetectedList.ContainsKey(player))
                {
                    DestroyOBJ(FlyDetectedList[player]);
                    FlyDetectedList.Remove(player);
                }

            if (!_config.ChuteSettings.Turned.OpenChutes.IsOpenedChute())
            {
                Chute.GetRootContainer().SetLocked(false);
                Chute.Remove();
                player.inventory.SendUpdatedInventory(PlayerInventory.Type.Wear, player.inventory.containerWear);

                if (JumpingPlayer.Contains(player))
                    JumpingPlayer.Remove(player);

                if (_config.ChuteSettings.UseAutoOpen)
                    if (FlyFinderList.ContainsKey(player))
                    {
                        DestroyOBJ(FlyFinderList[player]);
                        FlyFinderList.Remove(player);
                    }

                SendChat(GetLang("PARACHUTE_DONT_OPEN", player.UserIDString), player);
                return;
            }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            Chute.GetRootContainer().SetLocked(true);
            BaseEntity worldItem = GameManager.server.CreateEntity("assets/prefabs/misc/burlap sack/generic_world.prefab", player.transform.position + new Vector3(0f, 7f, 0f), Quaternion.Euler(new Vector3(0f, player.GetNetworkRotation().eulerAngles.y, 0f)), true);
            worldItem.enableSaving = false;
            worldItem.Spawn();

            ChuteConroller chuteConroller = worldItem.gameObject.AddComponent<ChuteConroller>();
            chuteConroller.SetPlayer(player);
            ChuteConrollerList.Add(chuteConroller);
            return;
        }
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        void Unload()
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList.Where(x => x.inventory.containerWear.IsLocked()))
                p.inventory.containerWear.SetLocked(false);

            foreach (ChuteConroller controllerChute in ChuteConrollerList.Where(x => x != null))
            {
                if (_config.ChuteSettings.UseSamSite)
                    SAMTargetComponent.RemoveComp(controllerChute.chair as BaseCombatEntity);

                DestroyOBJ(controllerChute);
            }      
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            foreach (FlyFinder Finder in FlyFinderList.Values.Where(x => x != null))
                DestroyOBJ(Finder);

            if (_config.ChuteSettings.UseChuteAllOpen)
                foreach (FlyDetected Detected in FlyDetectedList.Values.Where(x => x != null))
                    DestroyOBJ(Detected);

            foreach (BaseEntity ent in MonumentEntities.Where(x => !x.IsDestroyed))
                ent.Kill();

            MonumentEntities.Clear();
            FlyFinderList.Clear();
            ChuteConrollerList.Clear();
            _config = null;
            sb = null;
            BanditTown = null;
            Compound = null;
            Airfield = null;
            _ = null;
        }
        
        
        private Dictionary<BasePlayer, FlyDetected> FlyDetectedList = new Dictionary<BasePlayer, FlyDetected>();
        
        
                private Item GetChute(BasePlayer player)
        {
            Item FindChute = player.inventory.containerWear.FindItemsByItemName(_config.ChuteSettings.Chute.ShortName);
            if (FindChute == null) return null;
            if (FindChute.skin != _config.ChuteSettings.Chute.SkinId) return null;
            return FindChute;
        }

        
                private class PluginConfig
        {
            [JsonProperty(LanguageEn ?"Setting up the parachute" : "Настройка парашюта")]
            public Chutes ChuteSettings;
            [JsonProperty(LanguageEn ?"Setting up an NPC Merchant" : "Настройка NPC торговца")]
            public ShopChute ShopChuteSetting;
            [JsonProperty(LanguageEn ?"Configuring supporting plugins" : "Настройка поддерживающих плагинов")]
            public ReferencePlugin ReferenceSetting;
            internal class ReferencePlugin
            {
                [JsonProperty(LanguageEn ?"Settings IQChat" : "Настройка IQChat")]
                public IQChatReference IQChatSetting;
                internal class IQChatReference
                {
                    [JsonProperty(LanguageEn ?"IQ Chat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix = "[IQParachute]";
                    [JsonProperty(LanguageEn ?"IQChat : Custom avatar in the chat (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar = "0";
                    [JsonProperty(LanguageEn ?"IQChat : Use UI notifications" : "IQChat : Использовать UI уведомления")]
                    public Boolean UIAlertUse = false;
                }
            }
            internal class ShopChute
            {
                [JsonProperty(LanguageEn ?"Store names on the map" : "Названия магазина на карте")]
                public String ShopName;
                [JsonProperty(LanguageEn ?"Setting up an NPC who Sells a Parachute" : "Настройка NPC, который продает парашют")]
                public NPCShop NPCShopSetting;
                [JsonProperty(LanguageEn ?"Setting up a Parachute sale" : "Настройка продажи парашюта")]
                public VendorShop VendorShopSetting;
                [JsonProperty(LanguageEn ?"Setting up NPC Merchants" : "Настройка NPC торговцев")]
                public TurnedShopSpawn TurnedShop;
                internal class TurnedShopSpawn
                {
                    [JsonProperty(LanguageEn ?"Use an NPC merchant in a compound" : "Использовать NPC-торговца в мирном городе")]
                    public Boolean UseCompound;
                    [JsonProperty(LanguageEn ?"Use an NPC merchant in a bandit camp" : "Использовать NPC-торговца в городе бандитов")]
                    public Boolean UseBanditTown;
                    [JsonProperty(LanguageEn ?"Use an NPC merchant at the airfield" : "Использовать NPC-торговца на аэропорту")]
                    public Boolean UseAirfield;
                }
                internal class VendorShop
                {
                    [JsonProperty(LanguageEn ?"Payment item Shortname" : "Shortname платежного предмета")]
                    public String Shortname;
                    [JsonProperty(LanguageEn ? "SkinID of the payment item(0 is the default)" : "SkinID платежного предмета(0 - по умолчанию)")]
                    public UInt64 SkinID;
                    [JsonProperty(LanguageEn ?"The cost of a parachute" : "Стоимость парашюта")]
                    public Int32 Amount;
                }
                internal class NPCShop
                {
                    [JsonProperty(LanguageEn ?"DisplayName NPC" : "Имя NPC")]
                    public String Name;
                    [JsonProperty(LanguageEn ?"ID NPC (His appearance depends on his ID)" : "ID NPC (От его ид зависит его внешность)")]
                    public UInt64 userID;
                    [JsonProperty(LanguageEn ?"Clothes NPC" : "Одежда NPC")]
                    public List<ItemsNpc> Wear = new List<ItemsNpc>();

                    public class ItemsNpc
                    {
                        [JsonProperty("ShortName")]
                        public String ShortName;
                        [JsonProperty("SkinId")]
                        public UInt64 SkinId;
                    }
                }
            }
            internal class Chutes
            {
                [JsonProperty(LanguageEn ? "Open the parachute during any jump from a height, otherwise only from transport" : "Открывать парашют при любом прыжке с высоты, иначе только с транспорта")]
                public Boolean UseChuteAllOpen = false;
                [JsonProperty(LanguageEn ?"Automatically open the parachute before landing" : "Автоматически раскрывать парашют перед приземлением")]
                public Boolean UseAutoOpen = false;
                [JsonProperty(LanguageEn ?"Whether to allow an air defense attack on a parachutist" : "Разрешить ли атаку ПВО по парашютисту")]
                public Boolean UseSamSite;
                [JsonProperty(LanguageEn ?"Setting up an item" : "Настройка предмета")]
                public CustomItem Chute;
                [JsonProperty(LanguageEn ?"Additional configuration" : "Дополнительная настройка")]
                public TurnedFuncion Turned;
                internal class TurnedFuncion
                {
                    [JsonProperty(LanguageEn ?"Setting up the effect" : "Настройка эффекта")]
                    public Effect EffectSetting;
                    [JsonProperty(LanguageEn ?"Setting up the opening of the parachute" : "Настройка открытия парашюта")]
                    public OpenChute OpenChutes;
                    internal class Effect
                    {
                        [JsonProperty(LanguageEn ?"Enable parachute effect support" : "Включить поддержку эффекта на парашюте")]
                        public Boolean UseEffect;
                        [JsonProperty(LanguageEn ?"Effect" : "Эффект")]
                        public String PathEffect;
                    }
                    internal class OpenChute
                    {
                        [JsonProperty(LanguageEn ?"Enable the chance of opening the parachute (true - yes/false - no)" : "Включить шанс открытия парашюта (true - да/false - нет)")]
                        public Boolean UseRareOpen;
                        [JsonProperty(LanguageEn ?"Chance of opening a parachute" : "Шанс открытия парашюта")]
                        public Int32 RareOpen;
                        public Boolean IsOpenedChute()
                        {
                            if (UseRareOpen)
                                return Core.Random.Range(0, 100) >= (100 - RareOpen);
                            else return true;
                        }
                    }
                }
                [JsonProperty(LanguageEn ?"Flight Setup" : "Настройка полета")]
                public FlyController FlyControlling;
                public class FlyController
                {
                    [JsonProperty(LanguageEn ?"Maximum falling speed" : "Максимальная скорость падения")]
                    public Single maxDropSpeed = -5f;
                    [JsonProperty(LanguageEn ?"Opposition to gravity" : "Противостояние гравитации")]
                    public Single upForce = 7f;
                    [JsonProperty(LanguageEn ?"Acceleration force (When pressing W)" : "Сила ускорения (При нажатии W)")]
                    public Single forwardStrength = 3f;
                    [JsonProperty(LanguageEn ?"Acceleration force (When pressing S)" : "Сила ускорения (При нажатии S)")]
                    public Single backwardStrength = 3f;
                    [JsonProperty(LanguageEn ?"Rotation acceleration force (When pressing A/D)" : "Сила ускорения вращения (При нажатии A/D)")]
                    public Single rotationStrength = 0.3f;
                    [JsonProperty(LanguageEn ?"Resistance (Parachute Deceleration)" : "Сопротивление (Замедления парашюта)")]
                    public Single forwardResistance = 0.3f;
                    [JsonProperty(LanguageEn ?"Resistance to rotation (Reduces the rotation speed when the player does not press the A/D buttons)" : "Сопротивление вращению (Уменьшает скорость вращения когда игрок не нажимает кнопки A/D)")]
                    public Single rotationResistance = 0.5f;
                    [JsonProperty(LanguageEn ?"Sliding(Gives sliding to the parachute when the SHIFT key is pressed)" : "Скольжение (Придает скольжения парашюту при нажатой клавише SHIFT)")]
                    public Single glidedevider = 3.0f;
                    [JsonProperty(LanguageEn ?"Acceleration (Gives acceleration to the parachute when the CTRL key is pressed)" : "Ускорение (Придает ускорения парашюту при нажатой клавише CTRL)")]
                    public Single decendmultiplyer = 1.5f;
                    [JsonProperty(LanguageEn ?"The maximum angle of inclination of the parachute when turning A/ D" : "Максимальный угол наклона парашюта при поворотах A/D")]
                    public Single angularModifier = 15f;
                }
            }
        }
      
        object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null) return null;
            if (entity.skinID == 99999995) return false;
            return null;
        }
        
        
        
        private static StringBuilder sb = new StringBuilder();
        private IEnumerable<BasePlayer> FindMyBot()
        {
            return BasePlayer.allPlayerList.Where(x => x.userID == _config.ShopChuteSetting.NPCShopSetting.userID);
        }
        void OnPlayerDeath(BasePlayer Wanted, HitInfo info)
        {
            if (Wanted == null || info == null || !Wanted.userID.IsSteamId() || Wanted.userID < 2147483647) return;
            Item Chute = GetChute(Wanted);
            if (Chute == null) return;
            if (!JumpingPlayer.Contains(Wanted)) return;
            Chute.GetRootContainer().SetLocked(false);
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item == null || container == null) return;
            BasePlayer player = container.playerOwner;
            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return;

            if (item.skin == _config.ChuteSettings.Chute.SkinId && item.info.shortname == _config.ChuteSettings.Chute.ShortName)
                if (GetChute(player) == null)
                {
                    if (FlyDetectedList.ContainsKey(player))
                        if (FlyDetectedList[player] != null)
                            DestroyOBJ(FlyDetectedList[player]);
                    ///removecomponents
                }

        }
        private Boolean IsInitializeNPCShop()
        {
            Int32 CountSpawn = 0;
            if (IsCompound())
                CountSpawn++;
            if (IsBanitTown())
                CountSpawn++;
            if (IsAirfield())
                CountSpawn++;

            return CountSpawn != 0;
        }

        private static PluginConfig _config;
        private const String SignImgRight = "https://i.imgur.com/kG3OmZJ.png";
        private void PlayerInput(BasePlayer player)
        {
            InputState inputState = player.serverInput;
            if (JumpingPlayer.Contains(player))
            {
                if (!player.isMounted)
                {
                    if(Physics.Raycast(new Ray(player.transform.position, Vector3.down), 3f, LAND_LAYERS))
                    {
                        if (JumpingPlayer.Contains(player))
                            JumpingPlayer.Remove(player);
                        return;
                    }
                    if (inputState.WasJustPressed(BUTTON.RELOAD))
                    {
                        OpenChute(player);
                        return;
                    }
                    else player.Invoke(() => PlayerInput(player), 0.01f);
                }
            }
        }

                private const Boolean LanguageEn = false;

        private Boolean IsCompound() => (Boolean)(_config.ShopChuteSetting.TurnedShop.UseCompound && Compound != null);
        private Boolean IsAirfield() => (Boolean)(_config.ShopChuteSetting.TurnedShop.UseAirfield && Airfield != null);

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null || entity == null) return;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return;

            if (item.skin == _config.ChuteSettings.Chute.SkinId && item.info.shortname == _config.ChuteSettings.Chute.ShortName)
            {
                NextTick(() =>
                {
                    if (GetChute(player) == null)
                    {
                        if (FlyDetectedList.ContainsKey(player))
                            if (FlyDetectedList[player] != null)
                                DestroyOBJ(FlyDetectedList[player]);
                        ///removecomponents
                    }
                });
                return;
            }


        }
            }
}
