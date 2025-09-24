// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("CraftSystem", "Sparkless, куплено на RustPlugin.ru", "0.0.3")]
    public class CraftingSystem : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private ConfigData _config;

        public class CheckItems
        {
            [JsonProperty("Shortname предмета")] 
            public string ShortName;
            [JsonProperty("Кол-во предмета")] 
            public int Amount;
        }
        
        class ConfigData
        {
            [JsonProperty("Можно ли устанавливать переработчик на землю?")]
            public bool Ground = false;
            [JsonProperty("Включить/Выключить подбор переработчика")]
            public bool Available = true;
            [JsonProperty("Запрещать ли подбор переработчика в билде?")]
            public bool Privelege = true;
            [JsonProperty("Можно ли дамажить переработчик?")]
            public bool Damage = true;
            [JsonProperty("Сколько хп будет у переработчика?")]
            public float Health = 500f;
            
            [JsonProperty("Ресурсы для крафта коптера(макс 6)")]
            public List<CheckItems> CraftCopter { get; set; }

            [JsonProperty("Ресурсы для крафта переработчика(макс 6)")]
            public List<CheckItems> CraftRec { get; set; }

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();
                newConfig.CraftCopter = new List<CheckItems>
                {
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    }
                };
                newConfig.CraftRec = new List<CheckItems>
                {
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new CheckItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    }
                };
                return newConfig;
            }
        }
        protected override void LoadDefaultConfig() => _config = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                if (_config?.CraftCopter == null) LoadDefaultConfig();
                if (_config?.CraftRec == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        void OnServerInitialized()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<Recycler>();
            foreach (var r in allobjects)
            {
                if (r.OwnerID != 0 && r.gameObject.GetComponent<RecyclerEntity>() == null)
                    r.gameObject.AddComponent<RecyclerEntity>();
                RecyclerSetting(r, true);
            }
            ImageLibrary.Call("AddImage", "https://i.imgur.com/AptcbtT.png", "CopterImage");
            ImageLibrary.Call("AddImage", "https://imgur.com/QEUXtZJ.png", "RecyclerImage");
        }
        object CanStackItem(Item item, Item anotherItem) {
            if (item.info.itemid == -1861522751 && item.skin != anotherItem.skin) return false;
            if (item.info.itemid ==  833533164 && item.skin != anotherItem.skin) return false;
            return null;
        }
        object OnItemSplit(Item item, int split_Amount) {
            if (item.info.itemid == -1861522751 && item.skin == 1663370375) 
            {
                Item byItemId = ItemManager.CreateByItemID(item.info.itemid, 1, item.skin);
                item.amount -= split_Amount;
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
                item.MarkDirty();
                return byItemId;
            }
            if (item.info.itemid == 833533164 && item.skin == 1321253094) 
            {
                Item byItemId = ItemManager.CreateByItemID(item.info.itemid, 1, item.skin);
                item.amount -= split_Amount;
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
                item.MarkDirty();
                return byItemId;
            }
            return null;
        }
        object CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem) {
            if (drItem.item.info.itemid == -1861522751 && drItem.item.info.itemid == anotherDrItem.item.info.itemid && drItem.item.skin != anotherDrItem.item.skin) return false;
            if (drItem.item.info.itemid == -833533164 && drItem.item.info.itemid == anotherDrItem.item.info.itemid && drItem.item.skin != anotherDrItem.item.skin) return false;
            return null;
        }
        void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<RecyclerEntity>();
            foreach (var key in objects)
            {
                GameObject.Destroy(key);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        private class RecyclerEntity : MonoBehaviour
        {
            private DestroyOnGroundMissing desGround;
            private GroundWatch groundWatch;
            public ulong OwnerID;

            void Awake()
            {
                OwnerID = GetComponent<BaseEntity>().OwnerID;
                desGround = GetComponent<DestroyOnGroundMissing>();
                if (!desGround) gameObject.AddComponent<DestroyOnGroundMissing>();
                groundWatch = GetComponent<GroundWatch>();
                if (!groundWatch) gameObject.AddComponent<GroundWatch>();
            }
            private void OnDestroy()
            {
                Destroy(this);
            }
        }
        
        bool Check(BaseEntity entity) 
        {
            GroundWatch component = entity.gameObject.GetComponent < GroundWatch > ();
            List < Collider > list = Facepunch.Pool.GetList < Collider > ();
            Vis.Colliders < Collider > (entity.transform.TransformPoint(component.groundPosition), component.radius, list, component.layers, QueryTriggerInteraction.Collide);
            foreach(Collider collider in list) {
                if (!(collider.transform.root == entity.gameObject.transform.root)) {
                    BaseEntity baseEntity = collider.gameObject.ToBaseEntity();
                    if ((!(bool)(baseEntity) || !baseEntity.IsDestroyed && !baseEntity.isClient) && baseEntity is BuildingBlock) 
                    {
                        Facepunch.Pool.FreeList < Collider > (ref list);
                        return true;
                    }
                }
            }
            Facepunch.Pool.FreeList < Collider > (ref list);
            return false;
        }
        
        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            var entity = obj.GetComponent<BaseEntity>();
            var ePos = entity.transform.position;
            RaycastHit rHit;
            if (entity != null && entity.ShortPrefabName == "researchtable_deployed" && entity.skinID == 1321253094)
            {
                if (!_config.Ground)
                {
                    if (!Check(entity))
                    {
                        GiveRecycler(player);
                        SendReply(player, $"Запрещно устанавливать на землю!");
                        entity.Kill();
                        return;
                    }
                }
                Recycler recycler = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", entity.transform.position, entity.transform.rotation) as Recycler;
                recycler.OwnerID = player.userID;
                recycler.Spawn();
                NextTick(() =>
                {
                    entity.Kill();
                });
                recycler.gameObject.AddComponent<RecyclerEntity>();
                RecyclerSetting(recycler);
            }
            if (entity != null && entity.ShortPrefabName == "box.wooden.large" && entity.skinID == 1663370375)
            {
                MiniCopter mini = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab",entity.transform.position, entity.transform.rotation) as MiniCopter;
                mini.Spawn();
                NextTick(() =>
                {
                    entity.Kill();
                });
            }
        }
        void RecyclerSetting(BaseCombatEntity recycler, bool init = false)
        {
            if (_config.Damage)
            {
                var clone = GameManager.server
                    .FindPrefab("assets/prefabs/deployable/research table/researchtable_deployed.prefab")
                    .GetComponent<BaseCombatEntity>();
                recycler._maxHealth = _config.Health;
                if (!init)
                {
                    recycler.health = recycler.MaxHealth();
                }
                if (_config.Damage)
                {
                    recycler.baseProtection = clone.baseProtection;   
                }
            }
        }
        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || info.HitEntity == null) return;
            RecyclerEntity recyclerentity = info.HitEntity.GetComponent<RecyclerEntity>();
            if (recyclerentity != null && recyclerentity.OwnerID != 0)
            {
                if (!_config.Available)
                {
                    SendReply(player, "Подбор переработчика запрещен!");
                    return;
                }
                if (_config.Privelege && !player.IsBuildingAuthed())
                {
                    SendReply (player, "Вам нужно право на строительство чтобы подобрать переработчик");
                    return;
                }
                if (GiveRecycler(player))
                {
                    SendReply(player, "Вы успешно подобрали переработчик!");
                }
                else
                {
                    SendReply(player, "У вас недостаточно места в инвентаре!");
                }
                info.HitEntity.Kill();   
            }
        }
        bool GiveRecycler(BasePlayer player) 
        {
            var item = ItemManager.CreateByName("research.table", 1, 1321253094);
            item.name = "Переработчик";
            if (!player.inventory.GiveItem(item)) {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }
        
        [ConsoleCommand("recycler.add")]
        void GiveRecyclers(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendError(arg, "[Ошибка] У вас нет доступа к этой команде!");
                return;
            }
            if (!arg.HasArgs())
            {
                PrintError(":\n[Ошибка] Введите recycler.add steamid/nickname\n[Пример] recyler.add Имя\n[Пример] recyler.add 76561198311240000");
                return;
            }
		    
            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            if (player == null)
            {
                PrintError($"[Ошибка] Не удается найти игрока {arg.Args[0]}");
                return;
            }
            GiveRecycler(player);
        }

        private string Layer = "Ui_Osnova";

        [ChatCommand("craft")]
        void craftopen(BasePlayer player)
        {
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#202020C2"), Material = "assets/content/ui/uibackgroundblur.mat"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiElement // osnova copter
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color =  "0.4739 0.4739 0.4739 0.7170817"},
                    new CuiRectTransformComponent {AnchorMin = "0.04322915 0.1268517", AnchorMax = "0.4234375 0.8907406"}
                }
            });
            container.Add(new CuiElement // osnova rec
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color =  "0.4739 0.4739 0.4739 0.7170817"},
                    new CuiRectTransformComponent {AnchorMin = "0.5713508 0.1268517", AnchorMax = "0.9515555 0.8907406"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.4313726", FadeIn = 0.25f, Text = "КРАФТ КОПТЕРА", FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.04374999 0.8546296", AnchorMax = "0.4223959 0.8898149" },
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1531249 0.5592594", AnchorMax = "0.3130209 0.8453705" },
                Button = { Color = "0.4666667 0.4666667 0.4666667 0.7137255" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, ".Images");
 
            container.Add(new CuiElement
            {
                Parent = ".Images",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage",  "CopterImage") },
                    new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.4313726", FadeIn = 0.25f, Text = "РЕСУРСЫ ДЛЯ КРАФТА", FontSize = 20, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.04427084 0.5203704", AnchorMax = "0.4223959 0.5537038" },
                }
            });
            for (int i = 0; i < _config.CraftCopter.Count || i < 6; i++)
            {
                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.05156252 + i * 0.060 - Math.Floor((double) i / 6) * 6 * 0.060} {0.4046296 - Math.Floor((double) i / 6) * 0.18}",
                            AnchorMax =
                                $"{0.1135417 + i * 0.060 - Math.Floor((double) i / 6) * 6 * 0.060} {0.5166669 - Math.Floor((double) i / 6) * 0.18}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "1 1 1 0.01", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Command = $""
                        },
                        Text =
                        {
                            Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                        }
                    }, Layer, Layer + $".{i}");
                
                var resi = _config.CraftCopter[i];
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{i}",
                    Name = Layer+ $".{i}.Img",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", resi.ShortName)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{i}",
                    Name = Layer + $".{i}.Txt",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x{resi.Amount}", Align = TextAnchor.LowerRight,
                            Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1536459 0.150925", AnchorMax = "0.3135417 0.2129639" },
                Button = { Close = Layer, Command = "craftcopter", Color = "0.4666667 0.4666667 0.4666667 0.7137255", FadeIn = 0.1f},
                Text = { Text = "СКРАФТИТЬ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05260417 0.2222222", AnchorMax = "0.4151042 0.3944445" },
                Button = { Color = HexToCuiColor("#FFFFFF00"), FadeIn = 0.1f},
                Text = { Text = "Чтобы скрафтить коптер, нужно найти все необходимые компоненты и положить в инвентарь.Зайти в меню крафта и нажать на кнопку скрафтить.После этого он появится у вас в инвентаре.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.4313726", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.4313726", FadeIn = 0.25f, Text = "КРАФТ ПЕРЕРАБОТЧИКА", FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.5723922 0.8546296", AnchorMax = "0.9510352 0.8898149" },
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.681765 0.5592594", AnchorMax = "0.8416607 0.8453705" },
                Button = { Color = "0.4666667 0.4666667 0.4666667 0.7137255" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, ".Imagess");
 
            container.Add(new CuiElement
            {
                Parent = ".Imagess",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage",  "RecyclerImage") },
                    new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.4313726", FadeIn = 0.25f, Text = "РЕСУРСЫ ДЛЯ КРАФТА", FontSize = 20, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.5729135 0.5203704", AnchorMax = "0.9510352 0.5537038" },
                }
            });
            for (int x = 0; x < _config.CraftRec.Count || x < 6; x++)
            {
                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.580205 + x * 0.060 - Math.Floor((double) x / 6) * 6 * 0.060} {0.4046296 - Math.Floor((double) x / 6) * 0.18}",
                            AnchorMax =
                                $"{0.6421813 + x * 0.060 - Math.Floor((double) x / 6) * 6 * 0.060} {0.5166669 - Math.Floor((double) x / 6) * 0.18}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "1 1 1 0.01", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Command = $""
                        },
                        Text =
                        {
                            Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                        }
                    }, Layer, Layer + $".{x}");
                var resis = _config.CraftRec[x];
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{x}",
                    Name = Layer+ $".{x}.Img",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", resis.ShortName)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{x}",
                    Name = Layer + $".{x}.Txt",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x{resis.Amount}", Align = TextAnchor.LowerRight,
                            Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6822866 0.150925", AnchorMax = "0.8421808 0.2129639" },
                Button = { Close = Layer, Command = "craftrecycler", Color = "0.4666667 0.4666667 0.4666667 0.7137255", FadeIn = 0.1f},
                Text = { Text = "СКРАФТИТЬ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5812466 0.2222222", AnchorMax = "0.9437433 0.3944445" },
                Button = { Color = HexToCuiColor("#FFFFFF00"), FadeIn = 0.1f},
                Text = { Text = "Чтобы скрафтить переработчик, нужно найти все необходимые компоненты и положить в инвентарь.Зайти в меню крафта и нажать на кнопку скрафтить.После этого он появится у вас в инвентаре.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.4313726", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("craftcopter")]
        void craftcopter(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            
            foreach (var ct in _config.CraftCopter.Select((i, t) => new {A = i, B = t}))
            {
                int HaveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(ct.A.ShortName).itemid);

                if (HaveCount < ct.A.Amount)
                {
                    SendReply(player, $"Вам не хватает: {ItemManager.FindItemDefinition(ct.A.ShortName).displayName.english}: {ct.A.Amount - HaveCount}");
                    return;
                }
            }

            foreach (var ct in _config.CraftCopter.Select((i, t) => new {A = i, B = t}))
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(ct.A.ShortName).itemid, ct.A.Amount);
            }
            GiveCopter(player);
            SendReply(player, "<color=#7f35ff>Вы успешно скрафтили коптер!</color>");
        }
        bool GiveCopter(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1663370375UL);
            item.name = "Коптер";
            if (!player.inventory.GiveItem(item)) {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }
        [ConsoleCommand("craftrecycler")]
        void craftrecycler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            
            foreach (var ct in _config.CraftRec.Select((i, t) => new {A = i, B = t}))
            {
                int haveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(ct.A.ShortName).itemid);

                if (haveCount < ct.A.Amount)
                {
                    SendReply(player, $"Вам не хватает: {ItemManager.FindItemDefinition(ct.A.ShortName).displayName.english}: {ct.A.Amount - haveCount} шт");
                    return;
                }
            }
            foreach (var ct in _config.CraftRec.Select((i, t) => new {A = i, B = t}))
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(ct.A.ShortName).itemid, ct.A.Amount);
            }
            GiveRecycler(player);
            SendReply(player,$"<color=#7f35ff>Вы успешно скрафтили переработчик</color>");
        }
        #region Helper
        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        #endregion
    }
}