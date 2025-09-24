using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealCupboardProtection", "Kira", "1.0.0")]
    [Description("Система модификации шкафа")]
    public class ZealCupboardProtection : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] private Plugin ImageLibrary; 
        public static ZealCupboardProtection _;
        public StoredData DataBase = new StoredData();

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary?.Call("GetImage", shortname, skin);

        #endregion
 
        #region [Classes] / [Классы] 

        public class CupboardUpdate : MonoBehaviour
        {
            public BasePlayer Player;
            public bool IsOn = false;
            public StorageContainer Cupboard;
            public Item Shield;

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                Cupboard = GetComponent<StorageContainer>();
                Shield = GetComponent<Item>();
                InvokeRepeating(nameof(ControllerUpdate), 0.1f, 5f);
            }

            public bool _IsOn()
            {
                foreach (var item in Cupboard.inventory.itemList)
                {
                    if (item.skin == ShieldUID)
                    {
                        IsOn = true;
                    }
                    else
                    {
                        IsOn = false;
                    }
                }

                return IsOn;
            }

            public void UpdateUI()
            {
                _IsOn();
                DrawUI_CupboardPanel();
            }

            public void DestroyUI()
            {
                CuiHelper.DestroyUi(Player, UIP_Cupboard);
            }

            public void ControllerUpdate()
            {
                UpdateUI();
            }

            public void DrawUI_CupboardPanel()
            {
                CuiElementContainer Gui = new CuiElementContainer();

                string UIP_Cupboard = "UIP_Cupboard";
                CuiHelper.DestroyUi(Player, UIP_Cupboard);
                CuiHelper.DestroyUi(Player, "TumblerNoEscape");

                Gui.Add(new CuiElement
                {
                    Name = UIP_Cupboard,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(IsOn ? "#61B06AFF" : "#B06161FF"),
                            Material = Sharp
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0",
                            AnchorMax = "0.5 0",
                            OffsetMin = "193 491",
                            OffsetMax = "573 556"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = $"{UIP_Cupboard}_UIElement1",
                    Parent = UIP_Cupboard,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(IsOn ? "#61B06AFF" : "#B06161FF"),
                            Material = Blur,
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.824147 0.02051282",
                            AnchorMax = "0.997375 0.9794872"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "1 1"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = $"{UIP_Cupboard}_UIElement2",
                    Parent = UIP_Cupboard,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(IsOn ? "#5A9461FF" : "#935050FF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.117 0.5846",
                            AnchorMax = "0.729 0.5948"
                        }
                    }
                });

                if (IsOn)
                { 
                    Gui.Add(new CuiElement
                    {
                        Name = $"{UIP_Cupboard}_Shield",
                        Parent = UIP_Cupboard,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 1",
                                Png = (string) _.ImageLibrary.Call("GetImage", "ICShield")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.827 0.025",
                                AnchorMax = "1.02 0.979"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat(IsOn ? "#5A9461FF" : "#8B3A3AFF"),
                                Distance = "0.9 0.9"
                            }
                        }
                    });
                }
                else
                {
                    Gui.Add(new CuiElement
                    {
                        Name = $"{UIP_Cupboard}_Shield",
                        Parent = UIP_Cupboard,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 1",
                                Sprite = "assets/icons/warning.png"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.8731409 0.2666667",
                                AnchorMax = "0.9501313 0.7179487"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat(IsOn ? "#5A9461FF" : "#8B3A3AFF"),
                                Distance = "0.9 0.9"
                            }
                        }
                    });
                }

                Gui.Add(new CuiElement
                {
                    Name = $"{UIP_Cupboard}_Status",
                    Parent = UIP_Cupboard,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 15,
                            Text = IsOn ? "<b>ЗАЩИТА АКТИВНА</b>" : "<b>ЗАЩИТА НЕ АКТИВНА</b>",
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.6102569",
                            AnchorMax = "0.87 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat(IsOn ? "#5A9461FF" : "#8B3A3AFF"),
                            Distance = "0.9 0.9"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = $"{UIP_Cupboard}_ShieldHealth",
                    Parent = UIP_Cupboard,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 11, 
                            Text = IsOn
                                ? $"<b>ПРОЧНОСТЬ ЗАЩИТЫ</b>\n{Cupboard._health}/{Cupboard._maxHealth} HP"
                                : "<b>ПРОЧНОСТЬ ЗАЩИТЫ</b>\n0/100 HP",
                            Font = "robotocondensed-regular.ttf"
                        }, 
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.87 0.6000001"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat(IsOn ? "#5A9461FF" : "#8B3A3AFF"),
                            Distance = "0.9 0.9"
                        }
                    }
                });

                CuiHelper.AddUi(Player, Gui);
            }
        }

        #endregion  
 
        #region [Dictionary/Vars] / [Словари/Переменные]

        private static string Sharp = "assets/content/ui/ui.background.tile.psd";
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";
        private static string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private static string regular = "robotocondensed-regular.ttf";

        private static ulong ShieldUID = 1789973209;

        private static string UIP_Cupboard = "UIP_Cupboard";
        private static string UIP_RepairBench = "UIP_RepairBench";

        List<BuildingPrivlidge> ActiveCupboards = new List<BuildingPrivlidge>();

        #endregion

        #region [DrawUI] / [Показ UI]  

        #endregion 

        #region [Hooks] / [Крюки]

        void OnServerInitialized()
        {
            _ = this;
            LoadData();
            FindCupboards();
            ImageLibrary.Call("AddImage", "https://i.imgur.com/y4OfzEZ.png", "ICShield");
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if ((entity is BuildingBlock))
            {
                var block = entity as BuildingBlock;
                var player = info.InitiatorPlayer.ToPlayer();
                StorageContainer Cupboard = null;
                bool ItemContains = false;
                foreach (var Privelege in GetBuilding(block))
                {
                    Cupboard = Privelege;
                }

                if (DataBase.Cupboards.Contains(Cupboard.buildingID))
                {
                    info.damageTypes.Set(DamageType.Explosion, 1);
                }
            }

            return null;
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!(entity is BuildingPrivlidge)) return;
            StorageContainer Cupboard = entity as BuildingPrivlidge;
            InitializeCupboard(player, Cupboard);
        }
 
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (!(entity is BuildingPrivlidge)) return;
            DestroyCupboard(player);
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ConsoleCommand("test")]
        private void DrawUI(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
        }

        [ConsoleCommand("test2")]
        private void DrawUId(ConsoleSystem.Arg args)
        {
            args.Player().gameObject.AddComponent<CupboardUpdate>();
        }

        [ConsoleCommand("shield")]
        private void GiveShield(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            Item Shield = ItemManager.CreateByName("coal", 1, 1789973209);
            SendReply(args.Player(), Shield.info.category.ToString());
            Shield._condition = 999;
            Shield.maxCondition = 1000;
            Shield.name = $"Генератор щита [{Shield._condition} %]";
            SendReply(args.Player(), Shield.condition.ToString());
            args.Player().GiveItem(Shield, BaseEntity.GiveItemReason.Generic);
        }

        [ConsoleCommand("close.test")]
        private void DestroyUI(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), UIP_Cupboard);
            BasePlayer player;
            
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        public class StoredData
        {
            public List<uint> Cupboards = new List<uint>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private void FindCupboards()
        {
            foreach (BuildingPrivlidge cupboard in GetBuildingPrivlidges())
            {
                if (!DataBase.Cupboards.Contains(cupboard.buildingID))
                {
                    DataBase.Cupboards.Add(cupboard.buildingID);
                    PrintWarning($"Cupboard : {cupboard.buildingID}");
                    SaveData();
                }
            }
        }

        private void LoadCupboards()
        {
            foreach (var cupboard_id in DataBase.Cupboards)
            {
                foreach (var cupboard in BuildingManager.server.GetBuilding(cupboard_id).buildingPrivileges)
                {
                    ActiveCupboards.Add(cupboard);
                }
            }
        }

        private void GetShield()
        {
            foreach (var cupboard in ActiveCupboards)
            {
                foreach (var item in cupboard.inventory.itemList)
                {
                    if (item.skin == ShieldUID)
                    {
                    }
                }
            }
        }

        static List<BuildingPrivlidge> GetBuildingPrivlidges()
        {
            List<BuildingPrivlidge> list = new List<BuildingPrivlidge>();
            foreach (var kv in (ListDictionary<uint, BuildingManager.Building>) typeof(BuildingManager)
                .GetField("buildingDictionary",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(BuildingManager.server)) list.AddRange(kv.Value.buildingPrivileges);
            return list;
        }

        public void InitializeCupboard(BasePlayer player, StorageContainer Cupboard)
        {
            if (player.gameObject.GetComponent<CupboardUpdate>() != null) return;
            player.gameObject.AddComponent<CupboardUpdate>();
            player.gameObject.GetComponent<CupboardUpdate>().Cupboard = Cupboard;
        }

        public void DestroyCupboard(BasePlayer player)
        {
            UnityEngine.Object.Destroy(player.gameObject.GetComponent<CupboardUpdate>());

            CuiHelper.DestroyUi(player, UIP_Cupboard);
            CuiHelper.DestroyUi(player, "TumblerNoEscape");
        }

        static ListHashSet<BuildingPrivlidge> GetBuilding(BuildingBlock buildingBlock) =>
            BuildingManager.server.GetBuilding(buildingBlock.buildingID).buildingPrivileges;

        private static string HexToRustFormat(string hex)
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
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}