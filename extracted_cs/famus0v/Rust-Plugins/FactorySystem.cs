using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FactorySystem", "Kira", "1.0.3")]
    [Description("Система заводов")]
    public class FactorySystem : RustPlugin
    {
        #region [Vars]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;

#pragma warning restore CS0649

        private StoredData _dataBase = new StoredData();
        private static FactorySystem _;
        private const string PREFAB_SWITCH = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";

        private const string PREFAB_LASER =
            "assets/prefabs/deployable/playerioents/detectors/laserdetector/laserdetector.prefab";

        private const string UIMain = "UI_FactorySystem";

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "FactorySystem - Настройка")]
            public FactoryCFG FactoryConfig = new FactoryCFG();

            public class FactoryCFG
            {
                [JsonProperty(PropertyName = "Список заводов")]
                public Dictionary<ulong, FactorySettings> FactorySettingsMap;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                FactoryConfig = new ConfigData.FactoryCFG
                {
                    FactorySettingsMap = new Dictionary<ulong, FactorySettings>
                    {
                        [1] = new FactorySettings
                        {
                            NameRU = "Плавильня в золото",
                            NameENG = "Плавильня в золото",
                            Shortname = "box.wooden.large",
                            Power = true,
                            PowerConsum = 20,
                            Tick = 1,
                            Capacity = 30,
                            SwitchPosition = new Vector3(0, -0.5f, 0.5f),
                            SwitchRotation = new Vector3(0, 0, 0),
                            LaserPosition = new Vector3(-0.9f, 0.4f, 0),
                            LaserRotation = new Vector3(90, 0, 0),
                            ProducedItems = new Dictionary<int, ProducedItem>
                            {
                                [0] = new ProducedItem
                                {
                                    Shortname = "rifle.ak",
                                    ProducedMin = 1,
                                    ProducedMax = 1,
                                    SkinID = 0,
                                    IsSide = false,
                                    ConsumedItems = new List<ConsumedItem>
                                    {
                                        new ConsumedItem
                                        {
                                            Shortname = "wood",
                                            SkinID = 0,
                                            ConsumMin = 1,
                                            ConsumMax = 2
                                        }
                                    },
                                    SideItems = new List<ProducedItem>
                                    {
                                        new ProducedItem
                                        {
                                            Shortname = "stones",
                                            SkinID = 0,
                                            ProducedMin = 1,
                                            ProducedMax = 10,
                                            IsSide = true
                                        }
                                    },
                                    Stack = 1000
                                },
                                [1] = new ProducedItem
                                {
                                    Shortname = "rifle.lr300",
                                    ProducedMin = 1,
                                    ProducedMax = 1,
                                    SkinID = 0,
                                    IsSide = false,
                                    ConsumedItems = new List<ConsumedItem>
                                    {
                                        new ConsumedItem
                                        {
                                            Shortname = "stones",
                                            SkinID = 0,
                                            ConsumMin = 1,
                                            ConsumMax = 2
                                        }
                                    },
                                    SideItems = new List<ProducedItem>
                                    {
                                        new ProducedItem
                                        {
                                            Shortname = "stones",
                                            SkinID = 1,
                                            ProducedMin = 1,
                                            ProducedMax = 10,
                                            IsSide = true,
                                            Stack = 1000
                                        }
                                    },
                                    Stack = 1000
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["AVAILABLE_CRAFT"] = "ДОСТУПНЫЕ КРАФТЫ",
                ["REQUIRED_ITEMS"] = "НЕОБХОДИМЫЕ ПРЕДМЕТЫ",
                ["RECEIVED_ITEMS"] = "ПОЛУЧАЕМЫЕ ПРЕДМЕТЫ",
                ["POWER"] = "ПИТАНИЕ",
                ["TICK"] = "ВРЕМЯ ПРИГОТОВЛЕНИЯ {0}"
            };

            var en = new Dictionary<string, string>
            {
                ["AVAILABLE_CRAFT"] = "AVAILABLE CRAFTS",
                ["REQUIRED_ITEMS"] = "REQUIRED ITEMS",
                ["RECEIVED_ITEMS"] = "RECEIVED ITEMS",
                ["POWER"] = "POWER",
                ["TICK"] = "TIME FOR PREPARING {0}"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Dictionaries]

        private Dictionary<uint, FactoryCore> FactoryCores =
            new Dictionary<uint, FactoryCore>();

        #endregion 

        #region [Classes]

        public class ConsumedItem
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string Shortname;

            [JsonProperty(PropertyName = "Минимальное требуемое кол-во")]
            public int ConsumMin;

            [JsonProperty(PropertyName = "Максимальное требуемое кол-во")]
            public int ConsumMax;

            [JsonProperty(PropertyName = "SkinID")]
            public ulong SkinID;
        }

        public class ProducedItem
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string Shortname;

            [JsonProperty(PropertyName = "Минимальное производимое кол-во")]
            public int ProducedMin;

            [JsonProperty(PropertyName = "Максимальное производимое кол-во")]
            public int ProducedMax;

            [JsonProperty(PropertyName = "SkinID")]
            public ulong SkinID;

            [JsonProperty(PropertyName = "ID")] public int ID;

            [JsonProperty(PropertyName = "Стак предмета")]
            public int Stack;

            [JsonProperty(PropertyName = "Это побочный предмет ? (Если да, то не отображается в общем списке крафтов)")]
            public bool IsSide;

            [JsonProperty(PropertyName = "Входные предметы")]
            public List<ConsumedItem> ConsumedItems = new List<ConsumedItem>();

            [JsonProperty(PropertyName = "Выходные предметы")]
            public List<ProducedItem> SideItems = new List<ProducedItem>();
        }

        public class FactorySettings
        {
            [JsonProperty(PropertyName = "Название RU")]
            public string NameRU;

            [JsonProperty(PropertyName = "Название ENG")]
            public string NameENG;

            [JsonProperty(PropertyName = "Shortname")]
            public string Shortname;

            [JsonProperty(PropertyName = "Требуется питание ?")]
            public bool Power;

            [JsonProperty(PropertyName = "Сколько требуется питания ?")]
            public int PowerConsum;

            [JsonProperty(PropertyName = "Количество слотов")]
            public int Capacity = 30;

            [JsonProperty(PropertyName = "Позиция переключателя")]
            public Vector3 SwitchPosition;

            [JsonProperty(PropertyName = "Ротация переключателя")]
            public Vector3 SwitchRotation;

            [JsonProperty(PropertyName = "Позиция порта питания")]
            public Vector3 LaserPosition;

            [JsonProperty(PropertyName = "Ротация порта питания")]
            public Vector3 LaserRotation;

            [JsonProperty(PropertyName = "Тикрейт")]
            public int Tick;

            [JsonProperty(PropertyName = "Производимые предметы")]
            public Dictionary<int, ProducedItem> ProducedItems = new Dictionary<int, ProducedItem>();
        }
 
        #endregion 
 
        #region [MonoBehaviours]

        private class FactoryCore : MonoBehaviour
        {
            private BaseEntity Chest;
            public uint ID;
            public bool IsEnable;
            public List<BasePlayer> Players;
            public StorageContainer Container;
            public FactorySettings Settings;
            public LaserDetector Laser;
            public ElectricSwitch Switch;
            public ProducedItem CurrentCraft;
            private StoredData.FactorySave ExportSave;

            #region [Initialization]

            private void Awake()
            {
                Chest = GetComponent<BaseEntity>();
                Container = Chest.GetComponent<StorageContainer>();
                Players = new List<BasePlayer>();
                ID = Chest.net.ID;
                Settings = _._config.FactoryConfig.FactorySettingsMap[Chest.skinID];
                Switch = Chest.GetComponentInChildren<ElectricSwitch>();
                Laser = Chest.GetComponentInChildren<LaserDetector>();
            }

            private void Start()
            {
                Container.inventory.SetLocked(false);
                if (Settings == null) return;
                if (CurrentCraft == null) CurrentCraft = Settings.ProducedItems[0];
                SpawnSwitch();
                if (Settings.Power) SpawnLaser();
                Container.inventory.capacity = Settings.Capacity;
                if (!_._dataBase.FactorySaves.ContainsKey(ID))
                    _._dataBase.FactorySaves.Add(ID, new StoredData.FactorySave());
                Switch.SetSwitch(!IsEnable);
                Invoke(nameof(OnOff), 1f);
                InvokeRepeating(nameof(UpdateUI), 1f,1f);
            }

            #endregion

            #region [Helpers]

            private void DrawUI_Processing(BasePlayer player)
            {
                var ui = new CuiElementContainer();

                int xc = 0, xr = 0, xrec = 0;
                foreach (var craft in Settings.ProducedItems)
                {
                    var parent = $"{UIMain}.AvailableC.{xc}";
                    CuiHelper.DestroyUi(player, parent);
                    if (craft.Value.IsSide) continue;
                    ui.Add(new CuiElement
                    {
                        Name = parent,
                        Parent = UIMain,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0.13 0.13 0.15 0.5"
                            },
                            new CuiRectTransformComponent 
                            {
                                AnchorMin = $"{0 + (xc * 0.1f)} 0.7584253",
                                AnchorMax = $"{0.08779627 + (xc * 0.1f)} 0.8946651"
                            },
                            new CuiOutlineComponent
                            {
                                Color = craft.Key == CurrentCraft.ID
                                    ? "0.94 0.39 0.12 0.3"
                                    : "0 0 0 0",
                                Distance = craft.Key == CurrentCraft.ID
                                    ? "1 1"
                                    : "0 0"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Parent = parent,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", craft.Value.Shortname)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"factory.setcraft {ID} {craft.Value.SkinID} {craft.Value.Shortname} {craft.Key}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Text = " "
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, parent);

                    xc++;
                }

                foreach (var consum in CurrentCraft.ConsumedItems)
                {
                    var parent = $"{UIMain}.AvailableCon.{xr}";
                    CuiHelper.DestroyUi(player, $"{UIMain}.AvailableCon.{xr}");
                    ui.Add(new CuiElement
                    {
                        Name = parent,
                        Parent = UIMain,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0.13 0.13 0.15 0.5"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0 + (xr * 0.1f)} 0.5013528",
                                AnchorMax = $"{0.08779627 + (xr * 0.1f)} 0.6375913"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Parent = parent,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", consum.Shortname)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleRight,
                            Color = "1 1 1 0.7",
                            FontSize = 8,
                            Text = $"{consum.ConsumMin}-{consum.ConsumMax}x"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.3"
                        }
                    }, parent);

                    xr++;
                }

                var list = new List<ProducedItem>();
                list.AddRange(CurrentCraft.SideItems);
                list.Add(CurrentCraft);
                foreach (var craft in list)
                {
                    var parent = $"{UIMain}.AvailableP.{xrec}";
                    CuiHelper.DestroyUi(player, $"{UIMain}.AvailableP.{xrec}");
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIMain}.AvailableP.{xrec}",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0.13 0.13 0.15 0.5"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0 + (xrec * 0.1f)} 0.245222",
                                AnchorMax = $"{0.08779627 + (xrec * 0.1f)} 0.381462"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Parent = $"{UIMain}.AvailableP.{xrec}",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", craft.Shortname)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleRight,
                            Color = "1 1 1 0.7",
                            FontSize = 8,
                            Text = $"{craft.ProducedMin}-{craft.ProducedMax}x"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.3"
                        }
                    }, parent);

                    xrec++;
                }

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = "1 1 1 0.7",
                        FontSize = 8,
                        Text = _.lang.GetMessage("TICK", _, player.UserIDString)
                            .Replace("{0}", Settings.Tick.ToString())
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4820016 0",
                        AnchorMax = "1 0.1089918"
                    }
                }, UIMain, $"{UIMain}.Timer");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = "0.91 0.87 0.83 0.8",
                        FontSize = 15,
                        Text = _.lang.GetLanguage(player.UserIDString) == "ru" ? Settings.NameRU : Settings.NameENG
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1198911",
                        AnchorMax = "1 0.2152589"
                    }
                }, UIMain, $"{UIMain}.Name");

                if (Settings.Power)
                {
                    var power = Laser.currentEnergy >= Settings.PowerConsum
                        ? "<color=green>ON</color>"
                        : "<color=red>OFF</color>";

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 0.7",
                            FontSize = 14,
                            Text = $"{_.lang.GetMessage("POWER", _, player.UserIDString)} : {power}"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.5522384 0.891009",
                            AnchorMax = "1 1"
                        }
                    }, UIMain, $"{UIMain}.Power");
                }

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"craft.chestinfo {Chest.skinID}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        FontSize = 14,
                        Color = "0.94 0.39 0.12 1",
                        Text = "INFO"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5522384 0.891009",
                        AnchorMax = "1 1"
                    }
                }, UIMain, $"{UIMain}.Info");

                CuiHelper.DestroyUi(player, $"{UIMain}.Timer");
                CuiHelper.DestroyUi(player, $"{UIMain}.Info");
                CuiHelper.DestroyUi(player, $"{UIMain}.Name");
                CuiHelper.DestroyUi(player, $"{UIMain}.Power");
                CuiHelper.AddUi(player, ui);
            }

            public void UpdateUI()
            {
                foreach (var player in Players) DrawUI_Processing(player);
            }

            public void OnOff()
            {
                IsEnable = !IsEnable;
                CancelInvoke(nameof(Tick));
                if (IsEnable) InvokeRepeating(nameof(Tick), Settings.Tick, Settings.Tick);
                else CancelInvoke(nameof(Tick));
            }

            public void SwitchCurrentCraft(ulong Skin, string shortname, int key)
            {
                var find = Settings.ProducedItems.First(s =>
                    s.Value.SkinID == Skin & s.Value.Shortname == shortname & s.Key == key);
                if (find.Value == null) return;
                CurrentCraft = find.Value;
            }

            private void SpawnLaser()
            {
                if (Laser != null) return;
                Laser = (LaserDetector) GameManager.server.CreateEntity(PREFAB_LASER);
                RemoveColliderProtection(Laser);
                Laser.transform.position = Chest.GetNetworkPosition();
                Laser.SetParent(Chest);
                Laser.Spawn();
                var transform1 = Laser.transform;
                transform1.localEulerAngles = Settings.LaserRotation;
                transform1.localPosition = Settings.LaserPosition;
                Laser.SendNetworkUpdateImmediate();
                Laser.pickup.enabled = false;
            }

            public void SpawnSwitch()
            {
                if (Switch != null) return;
                Switch = (ElectricSwitch) GameManager.server.CreateEntity(PREFAB_SWITCH, Chest.GetNetworkPosition());
                RemoveColliderProtection(Switch);
                var switchTransform = Switch.transform;
                Switch.SetParent(Chest);
                Switch.Spawn();
                switchTransform.localEulerAngles = Settings.SwitchRotation;
                switchTransform.localPosition = Settings.SwitchPosition;
                Switch.SendNetworkUpdateImmediate();
                Switch.pickup.enabled = false;
            }

            public void Save()
            {
                Container.inventory.SetLocked(true);
                ExportSave = new StoredData.FactorySave();
                ExportSave.ItemSaves.Clear();
                ExportSave.Enable = !Switch.IsOn();
                Container.inventory.Clear();
                ExportSave.Position = Chest.GetNetworkPosition();
                ExportSave.Rotation = Chest.transform.eulerAngles;
                ExportSave.Shortname = Chest.PrefabName;
                ExportSave.SkinID = Chest.skinID;
                ExportSave.ID = ID;
                ExportSave.CurrentCraft = CurrentCraft;
                foreach (var item in Container.inventory.itemList)
                {
                    ExportSave.ItemSaves.Add(new StoredData.ItemSave
                    {
                        Shortname = item.info.shortname,
                        SkinID = item.skin,
                        Amount = item.amount
                    });
                }

                if (_._dataBase.FactorySaves.ContainsKey(ID)) _._dataBase.FactorySaves[ID] = ExportSave;
                else _._dataBase.FactorySaves.Add(ID, ExportSave);


                _.SaveData();
            }

            #endregion

            #region [Processing]

            private void Tick()
            {
                if (Settings.Power)
                    if (Laser.currentEnergy < Settings.PowerConsum)
                        return;
                if (Container.inventory.capacity == Container.inventory.itemList.Count)
                {
                    OnOff();
                    return;
                }

                var consumitems = CurrentCraft.ConsumedItems.Select(consumed =>
                    ItemManager.CreateByName(consumed.Shortname,
                        Core.Random.Range(consumed.ConsumMin, consumed.ConsumMax) <= 0
                            ? 1
                            : Core.Random.Range(consumed.ConsumMin, consumed.ConsumMax), consumed.SkinID)).ToList();

                var list = new List<ProducedItem>();
                list.AddRange(CurrentCraft.SideItems);
                list.Add(CurrentCraft);
                var produceditems = (from producedItem in list
                    let random = Core.Random.Range(producedItem.ProducedMin, producedItem.ProducedMax)
                    select ItemManager.Create(ItemManager.FindItemDefinition(producedItem.Shortname),
                        random <= 0 ? 1 : random, producedItem.SkinID)).ToList();

                var IsOk = consumitems.Count(i => GetAvalibleItem(Container, i.info.shortname, i.amount, i.skin));
                if (IsOk != consumitems.Count) return;
                {
                    foreach (var i in consumitems)
                        Container.inventory.Take(null, i.info.itemid, i.amount < 1 ? 1 : i.amount);
                    foreach (var p in produceditems) p.MoveToContainer(Container.inventory);
                }

                Sort();
            }

            private void Sort()
            {
                if (Container == null || Container.inventory.itemList.Count < 1) return;
                var a = Container.inventory.itemList;
                a.Sort((x, y) => x.info.itemid.CompareTo(y.info.itemid) + x.skin.CompareTo(y.skin));
                a = a.OrderBy(b => b.info.category).ToList();
                while (Container.inventory.itemList.Count > 0) Container.inventory.itemList[0].RemoveFromContainer();

                foreach (var c in a) c.MoveToContainer(Container.inventory);
            }

            #endregion

            #region [Hooks]

            public void Destroyed()
            {
                _._dataBase.FactorySaves.Remove(ID);
            }

            #endregion
        }

        #region [DrawUI]

        private static void DrawUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = UIMain,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1.000521 1.09537",
                            OffsetMin = "-447 -276",
                            OffsetMax = "-68 -100"
                        }
                    }
                }
            };

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.7",
                    FontSize = 14,
                    Text = _.lang.GetMessage("AVAILABLE_CRAFT", _, player.UserIDString),
                    FadeIn = 1f
                },
                RectTransform =
                {
                    AnchorMin = "0 0.8910081",
                    AnchorMax = "0.9815627 0.9999999"
                }
            }, UIMain);

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.7",
                    FontSize = 14,
                    Text = _.lang.GetMessage("REQUIRED_ITEMS", _, player.UserIDString),
                    FadeIn = 1f
                },
                RectTransform =
                {
                    AnchorMin = "0 0.6398116",
                    AnchorMax = "0.9815627 0.7488034"
                }
            }, UIMain);

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.7",
                    FontSize = 14,
                    Text = _.lang.GetMessage("RECEIVED_ITEMS", _, player.UserIDString),
                    FadeIn = 1f
                },
                RectTransform =
                {
                    AnchorMin = "0 0.3841961",
                    AnchorMax = "0.9815627 0.4931879"
                }
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
            ServerMgr.Instance.StartCoroutine(RecoveryFactory());
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.gameObject.GetComponent<BaseEntity>() == null) return;
            var ent = entity.gameObject.GetComponent<BaseEntity>();
            if (!(ent is StorageContainer)) return;
            if (ent.skinID == 0) return;
            var component = ent.gameObject.GetComponent<FactoryCore>();
            if (component == null) return;
            component.Destroyed();
        }

        // ReSharper disable once UnusedMember.Local 
        private void Unload()
        {
            foreach (var core in FactoryCores)
            {
                if (core.Value == null) continue;
                core.Value.Save();
                UnityEngine.Object.Destroy(core.Value);
            }

            SaveData();
        }

        // ReSharper disable once UnusedMember.Local 
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!(entity is StorageContainer)) return;
            if (entity.skinID == 0) return;
            var component = entity.gameObject.GetComponent<FactoryCore>();
            if (component == null) return;
            component.Players.Add(player);
            DrawUI_Main(player);
            component.UpdateUI();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (!(entity is StorageContainer)) return;
            if (entity.skinID == 0) return;
            var component = entity.gameObject.GetComponent<FactoryCore>();
            if (component == null) return;
            component.Players.Remove(player);
            CuiHelper.DestroyUi(player, UIMain);
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnSwitchToggle(ElectricSwitch electricSwitch, BasePlayer player)
        {
            if (!(electricSwitch.GetParentEntity() is StorageContainer)) return null;
            var chest = electricSwitch.GetParentEntity();
            if (chest == null) return null;
            var component = chest.gameObject.GetComponent<FactoryCore>();
            if (component == null) return null;
            component.OnOff();
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;
            var player = plan.GetOwnerPlayer();
            if (player == null) return;
            var obj = go.ToBaseEntity();
            if (obj == null) return;
            if (obj.skinID == 0) return;
            if (!_config.FactoryConfig.FactorySettingsMap.ContainsKey(obj.skinID)) return;
            var id = obj.net.ID;
            var component = obj.gameObject.AddComponent<FactoryCore>();
            if (!FactoryCores.ContainsKey(id)) FactoryCores.Add(id, component);
        }

        #endregion
 
        #region [Commands]

        [ConsoleCommand("factory.give")]
        // ReSharper disable once UnusedMember.Local
        private void GiveFactory(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            if (!args.HasArgs(3))
            {
                PrintWarning("factory.give SkinID SteamID/Name Amount");
                return;
            }

            var player = BasePlayer.Find(args.Args[1]);
            if (player == null)
            {
                PrintWarning("Игрок не найден");
                return;
            }

            var id = Convert.ToUInt64(args.Args[0]);
            var factory = _config.FactoryConfig.FactorySettingsMap[id];
            var item = ItemManager.CreateByName(factory.Shortname, Convert.ToInt32(args.Args[2]), id);
            item.name = lang.GetLanguage(player.UserIDString) == "ru" ? factory.NameRU : factory.NameENG;
            player.GiveItem(item);
        }

        [ConsoleCommand("factory.setcraft")]
        // ReSharper disable once UnusedMember.Local
        private void SetCraftFactory(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var ID = Convert.ToUInt32(args.Args[0]);
            var Skin = Convert.ToUInt64(args.Args[1]);
            var Shortname = args.Args[2];
            var Key = args.Args[3].ToInt();
            var component = FactoryCores[ID];
            if (component == null) return;
            component.SwitchCurrentCraft(Skin, Shortname, Key);
        }

        [ConsoleCommand("factory.clear")]
        // ReSharper disable once UnusedMember.Local
        private void ClearDB(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            _dataBase.FactorySaves.Clear();
            SaveData();
        }

        #endregion

        #region [DataBase]

        public class StoredData
        {
            public Dictionary<uint, FactorySave> FactorySaves = new Dictionary<uint, FactorySave>();

            public class FactorySave
            {
                public string Shortname;
                public ulong SkinID;
                public Vector3 Position;
                public Vector3 Rotation;
                public uint ID;
                public bool Enable;
                public ProducedItem CurrentCraft;
                public List<ItemSave> ItemSaves = new List<ItemSave>();
            }

            public class ItemSave
            {
                public string Shortname;
                public ulong SkinID;
                public int Amount;
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers]

        private IEnumerator RecoveryFactory()
        {
            foreach (var factory in _dataBase.FactorySaves)
            {
                var factoryobj = BaseNetworkable.serverEntities.entityList[factory.Value.ID];
                factoryobj.name = factory.Value.Shortname;
                var container = factoryobj.gameObject.GetComponent<StorageContainer>();
                foreach (var item in factory.Value.ItemSaves.Select(itemSave =>
                             ItemManager.CreateByName(itemSave.Shortname, itemSave.Amount, itemSave.SkinID)))
                    item.MoveToContainer(container.inventory);

                var component = factoryobj.gameObject.AddComponent<FactoryCore>();
                component.CurrentCraft = factory.Value.CurrentCraft;
                component.ID = factory.Value.ID;
                component.IsEnable = factory.Value.Enable;
                component.Settings = _config.FactoryConfig.FactorySettingsMap[factory.Value.SkinID];
                if (!FactoryCores.ContainsKey(factory.Key)) FactoryCores.Add(factory.Key, component);
            }

            yield return 0;
        }

        private static void RemoveColliderProtection(BaseEntity ent)
        {
            foreach (var meshCollider in ent.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);

            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
        }

        private static bool GetAvalibleItem(StorageContainer container, string shortname, int count, ulong skinid)
        {
            var item = container.inventory.FindItemsByItemName(shortname);
            if (item == null) return false;
            if (item.skin != skinid) return false;
            return item.amount >= count;
        }

        #endregion
    }
}