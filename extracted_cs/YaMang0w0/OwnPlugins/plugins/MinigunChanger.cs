using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Minigun Changer", "YaMang -w-", "1.0.0")]
    [Description("Minigun Changer")]
    public class MinigunChanger : RustPlugin
    {
        #region Fields
        private static MinigunChanger Instance { get; set; }
        [PluginReference] Plugin ImageLibrary;
        #endregion
        #region Hook

        void OnServerInitialized(bool initial)
        {
            Instance = this;
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player.Connection == null || player.IsNpc) return;
            
            var comp = player.GetComponent<MinigunChangers>();

            if (newItem != null)
            {
                
                if (newItem.info.shortname != "minigun") return;
                //if (player.IsAdmin)
                //{
                //    if (newItem.contents == null)
                //    {
                //        newItem.contents = new ItemContainer();
                //        newItem.contents.ServerInitialize(null, 1);
                //        newItem.contents.GiveUID();
                //        //newItem.contents.parent = newItem;
                //        newItem.contents.AddItem(ItemManager.FindItemDefinition(-132516482), 1);
                //        //Item newContent = ItemManager.CreateByItemID(-132516482);
                //        //newContent.position = 1;
                //        //if (newContent != null)
                //        //{
                //        //    newContent.MoveToContainer(newItem.contents);
                //        //}
                //        PrintToChat("null");
                //    }
                //    else
                //    {
                //        PrintToChat("xnull");
                //        newItem.contents = null;
                //    }
                    
                //}
                var minigun = newItem.GetHeldEntity() as SpinUpWeapon;
                minigun.spinUpTime = _config.heatingTime;
                
                if (!comp)
                {
                    player.gameObject.AddComponent<MinigunChangers>();
                }
                    
            }

            if (oldItem != null)
            {
                if (oldItem.info.shortname == "minigun")
                {
                    if (comp)
                        UnityEngine.Object.DestroyImmediate(comp);
                }
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var comp = player.GetComponent<MinigunChangers>();
                if (comp)
                    UnityEngine.Object.DestroyImmediate(comp);
                CuiHelper.DestroyUi(player, "SA_Main");
            }
        }


        #endregion

        #region Commands
        [ConsoleCommand("minigunsa")]
        private void MinigunCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if(arg.Args.Length == 0) return;

            if (arg.Args[0] == "close")
            {
                CuiHelper.DestroyUi(player, "SA_Main");
                return;
            }


            var active = player.GetActiveItem();
            if (active == null)
            {
                PrintToChat(player, "들고 있는 무기가 없습니다.");
                return;
            }
            if(active.info.shortname != "minigun")
            {
                PrintToChat(player, $"{active.name ?? active.info.displayName.english} 는 미니건이 아닙니다.");
                return;
            }

            var weapon = active.GetHeldEntity() as BaseProjectile;

            if (weapon != null)
            {
                switch (arg.Args[0])
                {

                    case "ammo1":
                        if(weapon.primaryMagazine.contents != 0)
                        {
                            var ammo1 = ItemManager.CreateByItemID(weapon.primaryMagazine.ammoType.itemid, weapon.primaryMagazine.contents);
                            player.GiveItem(ammo1);
                            weapon.primaryMagazine.contents = 0;
                        }

                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(-1211166256);
                        PrintToChat(player, "총알이 소총탄 으로 변경 되었습니다.");
                        break;

                    case "ammo2":
                        if (weapon.primaryMagazine.contents != 0)
                        {
                            var ammo1 = ItemManager.CreateByItemID(weapon.primaryMagazine.ammoType.itemid, weapon.primaryMagazine.contents);
                            player.GiveItem(ammo1);
                            weapon.primaryMagazine.contents = 0;
                        }
                        
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(1712070256);
                        PrintToChat(player, "총알이 고속탄 으로 변경 되었습니다.");
                        break;

                    case "ammo3":
                        if (weapon.primaryMagazine.contents != 0)
                        {
                            var ammo1 = ItemManager.CreateByItemID(weapon.primaryMagazine.ammoType.itemid, weapon.primaryMagazine.contents);
                            player.GiveItem(ammo1);
                            weapon.primaryMagazine.contents = 0;
                        }

                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(-1321651331);
                        PrintToChat(player, "총알이 폭발탄 으로 변경 되었습니다.");
                        break;

                    case "ammo4":
                        if (weapon.primaryMagazine.contents != 0)
                        {
                            var ammo1 = ItemManager.CreateByItemID(weapon.primaryMagazine.ammoType.itemid, weapon.primaryMagazine.contents);
                            player.GiveItem(ammo1);
                            weapon.primaryMagazine.contents = 0;
                        }

                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(605467368);
                        
                        PrintToChat(player, "총알이 소이탄 으로 변경 되었습니다.");
                        break;
                }

                if(true) //Use Reload
                    weapon.TryReloadMagazine(player.inventory);

                weapon.SendNetworkUpdate();
                weapon.SendChildrenNetworkUpdateImmediate();
                CuiHelper.DestroyUi(player, "SA_Main");
            }
            
        }
        #endregion

        #region ImageLibrary
        private string TryForImage(string shortname, ulong skin = 0)
        {
            if (!ImageLibrary) return "https://i.imgur.com/yxESUQJ.png";
            if (shortname.Contains("http") || shortname.Contains("www")) return shortname;
            return GetImage(shortname, skin);
        }
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname.ToLower(), skin);
        private string GetImage(string shortname, ulong skin = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", shortname.ToLower(), skin, returnUrl);
        #endregion

        #region Config        
        private ConfigData _config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Commands")] public List<string> Commands { get; set; }
            [JsonProperty(PropertyName = "Heating Timer")] public float heatingTime { get; set; }
            [JsonProperty(PropertyName = "Switch Ammo Timer")] public float switchTime { get; set; }
            [JsonProperty(PropertyName = "Reload Timer")] public float reloadTimer { get; set; }
            [JsonProperty(PropertyName = "Without WorkBench Reload")] public bool withoutWorkbenchReload { get; set; }
            [JsonProperty(PropertyName = "Without WorkBench Switch")] public bool withoutWorkbenchSwitch { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                heatingTime = 0.3f,
                reloadTimer = 2f,
                switchTime = 2f,
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");
            _config.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Helper
        private static string HexToColor(string HEX, float Alpha = 100)
        {
            if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

            var str = HEX.Trim('#');
            if (str.Length != 6) throw new Exception(HEX);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double)r / 255} {(double)g / 255} {(double)b / 255} {Alpha / 100f}";
        }

        private CuiElementContainer CreateOutLine(string parent, int size = 1)
        {
            return new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"0 -{size}"},
                        Image = {Color = "1 1 1 1"}
                    },
                    parent
                },
                {
                    new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMax = $"0 {size}"},
                        Image = {Color = "1 1 1 1" }
                    },
                    parent
                },
                {
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"-{size} -{size}",
                            OffsetMax = $"0 {size}"
                        },
                        Image = {Color = "1 1 1 1" }
                    },
                    parent
                },
                {
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = $"{size} {size}"
                        },
                        Image = {Color = "1 1 1 1" }
                    },
                    parent
                }
            };
        }
        #endregion

        #region Mono
        private class MinigunChangers : MonoBehaviour
        {
            private BasePlayer player;
            private InputState input;
            private bool isReloading;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                input = player.serverInput;
                isReloading = false;
                Instance.PrintToChat(player, "미니건 활성화됨");
            }

            private void FixedUpdate()
            {
                if (!isMinigun())
                {
                    DestroyComponent();
                    return;
                }
                if (input.IsDown(BUTTON.RELOAD))
                {
                    
                    if (!isReloading)
                    {
                        isReloading = true;
                        input.Clear();
                        //UI
                        if(player.currentCraftLevel == 0 || player.currentCraftLevel == 1)
                        {
                            Instance.PrintToChat(player, "미니건 재장전은 2레벨 제작대 앞에서 해야합니다.\nR키를 누르면 탄종을 선택할 수 있고 \n인벤토리에서 reload 를 눌러야 합니다 ");
                            return;
                        }
                        if(player.currentCraftLevel == 2 || player.currentCraftLevel == 3)
                            SA_Main(player);

                        isReloading = false;
                    }
                }
                else
                {
                    isReloading = false;
                }
            }

            private void OnDestroy()
            {
                Instance.PrintToChat(player, "미니건 비활성화됨");
            }
            public void DestroyComponent()
            {
                DestroyImmediate(this);
            }

            private bool isMinigun()
            {
                var active = player.GetActiveItem();
                if (active == null) return false;

                if (active.info.shortname == "minigun") return true;
                return false;
            }
            #region UI
            private void SA_Main(BasePlayer player)
            {
                var items = player.inventory.AllItems();

                var ammo1 = 0;
                var ammo2 = 0;
                var ammo3 = 0;
                var ammo4 = 0;

                foreach (var item in items)
                {
                    if (item.info.shortname == "ammo.rifle") ammo1 = ammo1 + item.amount;
                    if (item.info.shortname == "ammo.rifle.hv") ammo2 = ammo2 + item.amount;
                    if (item.info.shortname == "ammo.rifle.explosive") ammo3 = ammo3 + item.amount;
                    if (item.info.shortname == "ammo.rifle.incendiary") ammo4 = ammo4 + item.amount;
                }


                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Name = "SA_Main",
                    Parent = "Overlay",
                    DestroyUi = "SA_Main",
                    Components =
                {
                    new CuiNeedsCursorComponent(),
                    new CuiImageComponent{ Color = HexToColor("000000", 40), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    new CuiRectTransformComponent{ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-225 -49.992", OffsetMax = "225 50.008" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = "minigunsa close" },
                    Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-225 -50", OffsetMax = "225 50" }
                }, "SA_Main", "SA_CloseBtn");

                container.Add(new CuiButton
                {
                    Button = { Color = HexToColor("332727"), Command = "minigunsa ammo1" },
                    Text = { Text = $"<b>소총탄\n({ammo1})</b>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-215 -45.655", OffsetMax = "-115 45.655" }
                }, "SA_Main", "SA_RifleBtn");

                container.Add(new CuiElement
                {
                    Name = "SA_RifleBtnI",
                    Parent = "SA_RifleBtn",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = Instance.TryForImage("ammo.rifle") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -4.345", OffsetMax = "25 45.655" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = HexToColor("332727"), Command = "minigunsa ammo2" },
                    Text = { Text = $"<b>고속탄\n({ammo2})</b>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-104 -45.655", OffsetMax = "-4 45.655" }
                }, "SA_Main", "SA_HvBtn");

                container.Add(new CuiElement
                {
                    Name = "SA_HvBtnI",
                    Parent = "SA_HvBtn",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = Instance.TryForImage("ammo.rifle.hv") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -4.345", OffsetMax = "25 45.655" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = HexToColor("332727"), Command = "minigunsa ammo3" },
                    Text = { Text = $"<b>폭발탄\n({ammo3})</b>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "9 -45.655", OffsetMax = "109 45.655" }
                }, "SA_Main", "SA_ExposiveBtn");

                container.Add(new CuiElement
                {
                    Name = "SA_ExposiveBtnI",
                    Parent = "SA_ExposiveBtn",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = Instance.TryForImage("ammo.rifle.explosive") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -4.345", OffsetMax = "25 45.655" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = HexToColor("332727"), Command = "minigunsa ammo4" },
                    Text = { Text = $"<b>소이탄\n({ammo4})</b>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "117 -45.655", OffsetMax = "217 45.655" }
                }, "SA_Main", "SA_IncBtn");

                container.Add(new CuiElement
                {
                    Name = "SA_IncBtnI",
                    Parent = "SA_IncBtn",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = Instance.TryForImage("ammo.rifle.incendiary") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -4.345", OffsetMax = "25 45.655" }
                }
                });


                CuiHelper.AddUi(player, container);
            }
            #endregion
        }
        #endregion
    }
}