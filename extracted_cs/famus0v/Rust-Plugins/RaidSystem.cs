using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RaidSystem", "Kira", "1.0.0")]
    [Description(" ")]
    public class RaidSystem : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin GuildSystem, ImageLibrary;
        private static RaidSystem _;
        private StoredData _database = new StoredData();
        private const string UIMain = "UI.RaidSystem";
        private const int UpCostGates = 10000;
        private const int UpCostTurret = 10000;
        private const int MimimumBalance = 20000;

        private Guild OCG = new Guild
        {
            Name = "OCGBirch",
            RaidPosition = new Vector3(1283.2f, 0f, -1416.6f),
            GateModifies = new List<GateModify>
            {
                new GateModify
                {
                    GateLevel = 1,
                    GateType = GateType.Metal,
                    MaxLevel = 6,
                    Health = 500,
                    Position = new Vector3(-1943.2f, 5.1f, 1409.5f),
                    Rotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.02f)
                }
            },
            TurretModifies = new List<TurretModify>
            {
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 6,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1954.6f, 18.7f, 1492.8f),
                    Rotation = new Quaternion(0.0f, 0.2f, 0.0f, -1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1949.7f, 5.3f, 1503.9f),
                    Rotation = new Quaternion(0.0f, 0.3f, 0.0f, 1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1927.2f, 5.3f, 1484.8f),
                    Rotation = new Quaternion(0.0f, 0.3f, 0.0f, 1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1929.0f, 5.1f, 1463.4f),
                    Rotation = new Quaternion(0.0f, 0.9f, 0.0f, 0.4f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1926.8f, 18f, 1414.4f),
                    Rotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.2f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1958.0f, 15.3f, 1460.2f),
                    Rotation = new Quaternion(0.0f, 0.7f, 0.0f, 0.7f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1910.3f, 5.3f, 1493.6f),
                    Rotation = new Quaternion(0.0f, 0.5f, 0.0f, -0.9f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1949.8f, 5.3f, 1423.1f),
                    Rotation = new Quaternion(0.0f, 0.9f, 0.0f, 0.3f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1914.3f, 15.6f, 1470.0f),
                    Rotation = new Quaternion(0.0f, 0.3f, 0.0f, -1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(-1914.7f, 15.6f, 1466.8f),
                    Rotation = new Quaternion(0.0f, 1.0f, 0.0f, -0.2f)
                }
            }
        };

        private Guild WSARMY = new Guild
        {
            Name = "WSARMY",
            RaidPosition = new Vector3(1283.2f, 0f, -1416.6f),
            GateModifies = new List<GateModify>
            {
                new GateModify
                {
                    GateLevel = 1,
                    MaxLevel = 6,
                    GateType = GateType.Metal,
                    Health = 500,
                    Position = new Vector3(1237.319f, 5.662f, -1359.408f),
                    Rotation = new Quaternion(0.0f, 0.9f, 0.0f, 0.3f)
                }
            },
            TurretModifies = new List<TurretModify>
            {
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1272.1f, 16.3f, -1374.7f),
                    Rotation = new Quaternion(0.0f, 0.9f, 0.0f, 0.3f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1265.8f, 16.0f, -1374.8f),
                    Rotation = new Quaternion(0.0f, 0.6f, 0.0f, -0.8f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1330.9f, 21.5f, -1400.9f),
                    Rotation = new Quaternion(0.0f, 0.8f, 0.0f, -0.7f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1251.2f, 5.8f, -1364.8f),
                    Rotation = new Quaternion(0.0f, 0.6f, 0.0f, -0.8f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1223.3f, 5.8f, -1380.6f),
                    Rotation = new Quaternion(0.0f, 0.2f, 0.0f, 1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1303.5f, 5.7f, -1374.1f),
                    Rotation = new Quaternion(0.0f, 0.7f, 0.0f, -0.7f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1338.4f, 26.5f, -1412.6f),
                    Rotation = new Quaternion(0.0f, 0.8f, 0.0f, 0.6f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1265.3f, 5.8f, -1402.8f),
                    Rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Distant,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.l96"
                    },
                    Position = new Vector3(1211.3f, 40.4f, -1417.54f),
                    Rotation = new Quaternion(0.0f, 0.5f, 0.0f, -0.8f)
                },
                new TurretModify
                {
                    Health = 500,
                    TurretLevel = 1,
                    TurretType = TurretType.Near,
                    MaxLevel = 4,
                    Weapon = new TurretWeapon
                    {
                        CurrentWeapon = "rifle.ak"
                    },
                    Position = new Vector3(1319.9f, 5.8f, -1417.1f),
                    Rotation = new Quaternion(0.0f, 0.1f, 0.0f, -1.0f)
                }
            }
        };

        #endregion

        #region [Classes]

        public class RaidSensor : MonoBehaviour
        {
            public SphereCollider Sphere;
            public Guild GuildData;
            public BaseEntity Parent;
            public List<ulong> Players = new List<ulong>();

            public void Awake()
            {
                var o = gameObject;
                o.layer = (int) Layer.Reserved1;
                Parent = GetComponent<BaseEntity>();
            }

            public void Start()
            {
                GuildData.RaidSensor = this;
                Invoke(nameof(Timer), 300f);
            }

            public void Timer()
            {
                if (Players.Count != 0)
                {
                    Invoke(nameof(Timer), 10f);
                    return;
                }

                StartCoroutine(UnloadGates(GuildData));
                StartCoroutine(UnloadTurrets(GuildData));
                StartCoroutine(LoadTurrets(GuildData));
                StartCoroutine(LoadGates(GuildData));
                Destroy(Parent);
                Destroy(this);
            }
        }

        public class GateSensor : MonoBehaviour
        {
            public Door Gate;
            public SphereCollider sphere;

            public void Awake()
            {
                Gate = GetComponent<Door>();
                var o = gameObject;
                o.layer = (int) Layer.Reserved1;
            }

            public void Start()
            {
                Invoke(nameof(SpawnSphere), 1f);
            }

            public void OnTriggerEnter(Collider other)
            {
                var ent = other.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = (BasePlayer) ent;
                if (!(bool) _.GuildSystem.Call("GetGuildPlayer", player.userID, Gate.skinID)) return;
                Gate.SetOpen(true);
                Invoke(nameof(SetClose), 5f);
            }

            public void OnTriggerExit(Collider other)
            {
                var ent = other.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = (BasePlayer) ent;
                if (!(bool) _.GuildSystem.Call("GetGuildPlayer", player.userID, Gate.skinID)) return;
                Invoke(nameof(SetClose), 1f);
            }

            public void SetClose()
            {
                Gate.SetOpen(false);
            }

            public void SpawnSphere()
            {
                sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 10f;
                sphere.transform.position = Gate.transform.position;
            }
        }

        public class TurretCore : MonoBehaviour
        {
            public AutoTurret Turret;
            public Guild GuildData;

            public void Awake()
            {
                Turret = GetComponent<AutoTurret>();
                InvokeRepeating(nameof(ReloadAmmo), 300f, 300f);
            }

            public void ReloadAmmo()
            {
                if (Turret.GetTotalAmmo() > 0) return;
                ItemManager.CreateByName("ammo.rifle", 300).MoveToContainer(Turret.inventory, 1);
                Turret.Reload();
            }
        }

        // public class TurretRespawn : MonoBehaviour
        // {
        //     public Guild GuildData;
        //     public TurretModify Turret;
        //
        //     public void Start()
        //     {
        //         if (Turret == null)
        //         {
        //             _.PrintToChat("Turret is NULL");
        //             Destroy(this);
        //             return;
        //         }
        //
        //         Invoke(nameof(Respawn), 10f);
        //     }
        //
        //     public void Respawn()
        //     {
        //         var ent = (AutoTurret) GameManager.server.CreateEntity(
        //             "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Turret.Position, Turret.Rotation);
        //         ent.Spawn();
        //         Destroy(ent.GetComponent<DestroyOnGroundMissing>());
        //         Destroy(ent.GetComponent<GroundWatch>());
        //         ent.InitializeHealth(Turret.Health * Turret.TurretLevel, 1000000);
        //         ItemManager.CreateByName(Turret.Weapon.CurrentWeapon).MoveToContainer(ent.inventory, 0);
        //         foreach (var player in (List<ulong>) _.GuildSystem.Call("GetGuildPlayers", GuildData.Name))
        //             ent.authorizedPlayers.Add(new PlayerNameID {userid = player, username = "GUILD PLAYER"});
        //         ent.UpdateAttachedWeapon();
        //         ent.SetOnline();
        //         ent.SendNetworkUpdateImmediate();
        //
        //         Turret.Turret = ent;
        //         Turret.Core = ent.gameObject.AddComponent<TurretCore>();
        //         Destroy(this);
        //     }
        // }

        public class Guild
        {
            public string Name;
            public int Balance;
            public RaidSensor RaidSensor;
            public Vector3 RaidPosition;
            public List<GateModify> GateModifies = new List<GateModify>();
            public List<TurretModify> TurretModifies = new List<TurretModify>();
        }

        public enum TurretType
        {
            Near,
            Average,
            Distant
        }

        public Dictionary<TurretType, List<string>> Weapons = new Dictionary<TurretType, List<string>>
        {
            [TurretType.Near] = new List<string>
            {
                "rifle.ak"
            },
            [TurretType.Average] = new List<string>
            {
                "rifle.ak"
            },
            [TurretType.Distant] = new List<string>
            {
                "rifle.lr300"
            }
        };

        public class TurretWeapon
        {
            public string CurrentWeapon;
        }

        public enum GateType
        {
            Wood,
            Metal
        }

        public class TurretModify
        {
            public AutoTurret Turret;
            public int TurretLevel;
            public float Health = 500;
            public int MaxLevel = 3;
            public TurretCore Core;
            public TurretWeapon Weapon = new TurretWeapon();
            public TurretType TurretType;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public class GateModify
        {
            public Door Gate;
            public GateSensor Sensor;
            public int GateLevel;
            public float Health = 2000;
            public int MaxLevel = 6;
            public GateType GateType;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        #endregion

        #region [DrawUI]

        private void DrawUI_Main(BasePlayer player, string guild)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = UIMain
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
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.HeadingTurret",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = "ТУРЕЛИ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2192708 0.4351852",
                        AnchorMax = "0.4890625 0.6287037"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.HeadingGates",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = "ВОРОТА"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5109339 0.4351852",
                        AnchorMax = "0.780725 0.6287037"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"raidsystem.upgradeturrets {guild}",
                    Color = "0 0 0 0",
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    FontSize = 14,
                    Text = "УПРАВЛЕНИЕ"
                },
                RectTransform =
                {
                    AnchorMin = "0.2197916 0.3722221",
                    AnchorMax = "0.4895833 0.4212963"
                }
            }, UIMain);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"raidsystem.upgradegates {guild}",
                    Color = "0 0 0 0",
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    FontSize = 14,
                    Text = "УПРАВЛЕНИЕ"
                },
                RectTransform =
                {
                    AnchorMin = "0.5109339 0.3722221",
                    AnchorMax = "0.780725 0.4212963"
                }
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_TurretList(BasePlayer player, Guild guild)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Background2")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = UIMain
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
            }, UIMain);

            var x = 0;
            var pos = 0.5f - (guild.TurretModifies.Count * 0.09f +
                              (guild.TurretModifies.Count - 1) * 0.005f) / 2;
            foreach (var turret in guild.TurretModifies.Take(10))
            {
                var parent = $"{UIMain}.TurretBG.{x}";

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.TurretBG")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.4009259",
                            AnchorMax = $"{pos + 0.085f} 0.5990741"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Num",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 20,
                            Color = "0.39 0.40 0.44 1.00",
                            Text = $"{x + 1}\n<size=11>Стоимость : {UpCostTurret}</size>"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.275701",
                            AnchorMax = "1 1"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"raidsystemup.turret {x} {guild.Name}",
                        Color = "0 0 0 0",
                        Close = UIMain
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 0.8",
                        FontSize = 16,
                        Text = "UPGRADE"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.21"
                    }
                }, parent);

                pos += 0.09f;
                x++;
            }

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_GateList(BasePlayer player, Guild guild)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Background2")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = UIMain
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
            }, UIMain);

            var x = 0;
            var pos = 0.5f - (guild.GateModifies.Count * 0.09f +
                              (guild.GateModifies.Count - 1) * 0.005f) / 2;
            foreach (var gate in guild.GateModifies.Take(10))
            {
                var parent = $"{UIMain}.GateBG.{x}";

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.TurretBG")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.4009259",
                            AnchorMax = $"{pos + 0.085f} 0.5990741"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Num",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Color = "0.39 0.40 0.44 1.00",
                            Text = $"{x + 1}\n<size=11>Стоимость : {UpCostTurret}</size>"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.275701",
                            AnchorMax = "1 1"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"raidsystemup.gates {x} {guild.Name}",
                        Color = "0 0 0 0",
                        Close = UIMain
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 0.8",
                        FontSize = 16,
                        Text = "UPGRADE"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.21"
                    }
                }, parent);

                pos += 0.09f;
                x++;
            }

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            PrintWarning(UnityEngine.Object.FindObjectsOfType<AutoTurret>().Count().ToString());
            foreach (var d in UnityEngine.Object.FindObjectsOfType<AutoTurret>())
            {
                UnityEngine.Object.Destroy(d);
            }

            LoadData();
            ImageLibrary.Call("AddImage", "https://i.imgur.com/u7COy0e.png", $"{UIMain}.Background");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/9FEXZhy.png", $"{UIMain}.Background2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vGqoJpP.png", $"{UIMain}.TurretBG");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/FDRq7ym.png", $"{UIMain}.TurretMBG");
            _ = this;
            RecoverTurret(OCG);
            RecoverTurret(WSARMY);
            RecoverGate(OCG);
            RecoverGate(WSARMY);
            NextFrame(() => { ServerMgr.Instance.StartCoroutine(LoadTurrets(OCG)); });
            NextFrame(() => { ServerMgr.Instance.StartCoroutine(LoadGates(OCG)); });
            NextFrame(() => { ServerMgr.Instance.StartCoroutine(LoadTurrets(WSARMY)); });
            NextFrame(() => { ServerMgr.Instance.StartCoroutine(LoadGates(WSARMY)); });
            SpawnBot(new Vector3(1306.4f, 5.8f, -1380.5f), "Управляющий WSARMY", 231);
            SpawnBot(new Vector3(-1934.4f, 5.3f, 1486.6f), "Управляющий 'ОПГ' Берёза", 142);
        }

        private List<BaseEntity> bots = new List<BaseEntity>();
        private List<BaseEntity> players = new List<BaseEntity>();

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            switch (entity.skinID)
            {
                case 3001:
                {
                    ServerMgr.Instance.gameObject.AddComponent<RaidSensor>().GuildData = OCG;
                    var gate1 = OCG.GateModifies[0];
                    gate1.GateLevel = 1;
                    gate1.GateType = GateType.Wood;
                    break;
                }
                case 3002:
                {
                    ServerMgr.Instance.gameObject.AddComponent<RaidSensor>().GuildData = WSARMY;
                    var gate2 = WSARMY.GateModifies[0];
                    gate2.GateLevel = 1;
                    gate2.GateType = GateType.Wood;
                    break;
                }
            }

            if (entity == null) return;
            if (entity.gameObject.GetComponent<AutoTurret>() == null) return;
            var turret = (AutoTurret) entity;
            switch (turret._name)
            {
                case "OCGBirch":
                    var turret1 = OCG.TurretModifies.Find(x => x.Position == turret.GetNetworkPosition());
                    turret1.TurretLevel = 0;
                    break;
                case "WSARMY":
                    var turret2 = WSARMY.TurretModifies.Find(x => x.Position == turret.GetNetworkPosition());
                    turret2.TurretLevel = 0;
                    break;
            }
        }
       
        // ReSharper disable once UnusedMember.Local
        private object OnEntityTakeDamage(BasePlayer entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (players.Contains(entity)) return false;
            return null;
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (bots.Contains(container))
            {
                string guild = null;
                switch (player.Team.teamName)
                {
                    case "OCGBirch":
                        guild = "OCGBirch";
                        break;
                    case "WSARMY":
                        guild = "WSARMY";
                        break;
                }

                PrintToChat("1");
                DrawUI_Main(player, guild);
                return false;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            if (OCG.RaidSensor != null)
                UnityEngine.Object.Destroy(OCG.RaidSensor);
            if (WSARMY.RaidSensor != null)
                UnityEngine.Object.Destroy(WSARMY.RaidSensor);
            ServerMgr.Instance.StartCoroutine(UnloadTurrets(WSARMY));
            ServerMgr.Instance.StartCoroutine(UnloadGates(WSARMY));
            ServerMgr.Instance.StartCoroutine(UnloadTurrets(OCG));
            ServerMgr.Instance.StartCoroutine(UnloadGates(OCG));

            TurretSave(OCG);
            GateSave(OCG);
            TurretSave(WSARMY);
            GateSave(WSARMY);
            SaveData();
            foreach (var bot in bots) bot.Kill();
            foreach (var bot in players) bot.Kill();
        }

        private void RecoverTurret(Guild guild)
        {
            var num = 0;
            StoredData.GuildSave db = null;
            switch (guild.Name)
            {
                case "OCGBirch":
                    db = _database.OCG;
                    break;
                case "WSARMY":
                    db = _database.WSARMY;
                    break;
            }

            foreach (var turret in guild.TurretModifies)
            {
                turret.TurretLevel = db.TurretSaves[num].TurretLevel;
                turret.Weapon.CurrentWeapon = db.TurretSaves[num].Weapon;
                num++;
            }
        }

        private void RecoverGate(Guild guild)
        {
            StoredData.GuildSave db = null;
            switch (guild.Name)
            {
                case "OCGBirch":
                    db = _database.OCG;
                    break;
                case "WSARMY":
                    db = _database.WSARMY;
                    break;
            }

            guild.GateModifies[0].GateLevel = db.GateSaves[0].GateLevel;
            guild.GateModifies[0].GateType = db.GateSaves[0].Type;
        }

        private void TurretSave(Guild guild)
        {
            var num = 0;
            StoredData.GuildSave db = null;
            switch (guild.Name)
            {
                case "OCGBirch":
                    db = _database.OCG;
                    break;
                case "WSARMY":
                    db = _database.WSARMY;
                    break;
            }

            foreach (var turret in guild.TurretModifies)
            {
                db.TurretSaves[num].TurretLevel = turret.TurretLevel;
                db.TurretSaves[num].Weapon = turret.Weapon.CurrentWeapon;
                num++;
            }
        }

        private void GateSave(Guild guild)
        {
            var num = 0;
            StoredData.GuildSave db = null;
            switch (guild.Name)
            {
                case "OCGBirch":
                    db = _database.OCG;
                    break;
                case "WSARMY":
                    db = _database.WSARMY;
                    break;
            }

            foreach (var gate in guild.GateModifies)
            {
                db.GateSaves[num].GateLevel = gate.GateLevel;
                db.GateSaves[num].Type = gate.GateType;
                num++;
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (!(entity is BasePlayer)) return null;
            if (!BasePlayer.activePlayerList.Contains(entity.ToPlayer())) return false;
            var player = entity.ToPlayer();
            if (turret._name == player.Team.teamName) return false;
            return null;
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("raidsystem.upgradeturrets")]
        private void TurretUpList(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            DrawUI_TurretList(player, WSARMY);
        }

        [ConsoleCommand("raidsystem.upgradegates")]
        private void GatesUpList(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            Guild guild = null;
            switch (player.Team.teamName)
            {
                case "OCGBirch":
                    guild = OCG;
                    break;
                case "WSARMY":
                    guild = WSARMY;
                    break;
            }

            DrawUI_GateList(player, guild);
        }

        [ConsoleCommand("raidsystemup.turret")]
        private void TurretManage(ConsoleSystem.Arg args)
        {
            Guild db = null;
            switch (args.Args[1])
            {
                case "OCGBirch":
                    db = OCG;
                    break;
                case "WSARMY":
                    db = WSARMY;
                    break;
            }

            if ((db.Balance - MimimumBalance) < UpCostTurret) return;
            var turret = db.TurretModifies[args.Args[0].ToInt()];
            if (turret.MaxLevel >= turret.TurretLevel + 1) turret.TurretLevel++;
            ServerMgr.Instance.StartCoroutine(UnloadTurrets(db));
            NextFrame(() => ServerMgr.Instance.StartCoroutine(LoadTurrets(db)));
        }

        [ConsoleCommand("raidsystemup.gates")]
        private void GatesManage(ConsoleSystem.Arg args)
        {
            Guild db = null;
            switch (args.Args[1])
            {
                case "OCGBirch":
                    db = OCG;
                    break;
                case "WSARMY":
                    db = WSARMY;
                    break;
            }

            if ((db.Balance - MimimumBalance) < UpCostGates) return;
            var gate = db.GateModifies[args.Args[0].ToInt()];
            if (gate.MaxLevel >= gate.GateLevel + 1) gate.GateLevel++;
            ServerMgr.Instance.StartCoroutine(UnloadGates(db));
            NextFrame(() => ServerMgr.Instance.StartCoroutine(LoadGates(db)));
        }

        #endregion

        #region [DataBase]

        private class StoredData
        {
            public GuildSave OCG = new GuildSave();
            public GuildSave WSARMY = new GuildSave();

            public class GuildSave
            {
                public Dictionary<int, GateSave> GateSaves = new Dictionary<int, GateSave>();
                public Dictionary<int, TurretSave> TurretSaves = new Dictionary<int, TurretSave>();
            }

            public class TurretSave
            {
                public int TurretLevel;
                public TurretType Type = TurretType.Near;
                public string Weapon;
            }

            public class GateSave
            {
                public int GateLevel;
                public GateType Type = GateType.Wood;
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _database);

        private void LoadData()
        {
            try
            {
                _database = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _database = new StoredData();
            }
        }

        #endregion

        #region [Helpers]

        private void SpawnBot(Vector3 pos, string name, float rot)
        {
            var sphere = GameManager.server.CreateEntity("assets/prefabs/deployable/quarry/fuelstorage.prefab", pos);
            sphere.Spawn();
            var _ent = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab") as BasePlayer;
            if (_ent == null) return;
            sphere.transform.rotation = Quaternion.Euler(0, rot, 0);
            _ent.Spawn();
            _ent.SetParent(sphere);
            _ent.enableSaving = false;
            _ent.displayName = name;
            sphere.SendNetworkUpdateImmediate();
            ItemManager.CreateByName("hoodie").MoveToContainer(_ent.inventory.containerWear);
            ItemManager.CreateByName("pants").MoveToContainer(_ent.inventory.containerWear);
            ItemManager.CreateByName("shoes.boots").MoveToContainer(_ent.inventory.containerWear);
            ItemManager.CreateByName("hat.cap").MoveToContainer(_ent.inventory.containerWear);

            bots.Add(sphere);
            players.Add(_ent);
            _ent.SendNetworkUpdateImmediate(true);
        }

        private static IEnumerator LoadGates(Guild guild)
        {
            yield return new WaitForSeconds(1f);
            var num = 0;
            foreach (var gate in guild.GateModifies)
            {
                Door ent = null;
                switch (gate.GateType)
                {
                    case GateType.Wood:
                        ent = (Door) GameManager.server.CreateEntity(
                            "assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab");
                        break;
                    case GateType.Metal:
                        ent = (Door) GameManager.server.CreateEntity(
                            "assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab");
                        break;
                    default:
                        _.PrintWarning("Gate is NULL");
                        break;
                }

                switch (guild.Name)
                {
                    case "OCGBirch":
                        ent.skinID = 3001;
                        break;
                    case "WSARMY":
                        ent.skinID = 3002;
                        break;
                    default:
                        _.PrintWarning("Gate name is NULL");
                        break;
                }

                ent.transform.SetPositionAndRotation(gate.Position, gate.Rotation);
                ent.Spawn();
                UnityEngine.Object.Destroy(ent.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(ent.GetComponent<GroundWatch>());
                ent.InitializeHealth(gate.Health * gate.GateLevel, 10000);
                ent.SendNetworkUpdateImmediate();
                gate.Sensor = ent.gameObject.AddComponent<GateSensor>();
                gate.Gate = ent;
                num++;
            }

            yield return 0;
        }

        private static IEnumerator LoadTurrets(Guild guild)
        {
            yield return new WaitForSeconds(1f);
            var num = 0;
            foreach (var turret in guild.TurretModifies)
            {
                if (turret.TurretLevel == 0) continue;
                var ent = (AutoTurret) GameManager.server.CreateEntity(
                    "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", turret.Position, turret.Rotation);
                ent.Spawn();
                UnityEngine.Object.Destroy(ent.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(ent.GetComponent<GroundWatch>());
                switch (turret.TurretType)
                {
                    case TurretType.Near:
                        ItemManager.CreateByName("rifle.m39").MoveToContainer(ent.inventory, 0);
                        break;
                    case TurretType.Average:
                        ItemManager.CreateByName("rifle.ak").MoveToContainer(ent.inventory, 0);
                        break;
                    case TurretType.Distant:
                        ItemManager.CreateByName("rifle.l96").MoveToContainer(ent.inventory, 0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                ent.UpdateAttachedWeapon();
                ent._name = guild.Name;
                ent.SetOnline();
                ent.SendNetworkUpdateImmediate();
                _.NextFrame(() =>
                {
                    ent.InitializeHealth(turret.TurretLevel * turret.Health, 10000);
                    ent.SendNetworkUpdateImmediate();
                });

                turret.Turret = ent;
                turret.Core = ent.gameObject.AddComponent<TurretCore>();
                num++;
            }

            yield return 0;
        }

        private static IEnumerator UnloadTurrets(Guild guild)
        {
            foreach (var turret in guild.TurretModifies.Where(turret => turret.Turret != null)) turret.Turret.Kill();
            yield return 0;
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        private static IEnumerator UnloadGates(Guild guild)
        {
            foreach (var gate in guild.GateModifies.Where(gate => gate.Gate != null)) gate.Gate.Kill();
            yield return 0;
        }

        #endregion
    }
}