// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

namespace Oxide.Plugins
{
    [Info("TPWipeBlock", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.5")]
    class TPWipeBlock : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary, Duel, ArenaTournament;
        private static ConfigData config;
        private string CONF_IgnorePermission = "block.ignore";
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Начало отрисовки столбцов(требуется для центрирования)")]
            public float StartUI = 0.188f;
            [JsonProperty(PropertyName = "Информация плагина")]
            public string Info = "Ахуенный плагин скилов всем советую, а кто не купит, тот гомосек";
            [JsonProperty(PropertyName = "Блокировка предметов")]
            public Dictionary<int, List<string>> items;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                items = new Dictionary<int, List<string>>
                {
                    [7200] = new List<string>()
                    {
                       "crossbow",
                       "shotgun.waterpipe",
                       "flamethrower",
                       "bucket.helmet",
                       "pistol.revolver",
                       "riot.helmet"
                    },
                    [14400] = new List<string>()
                    {
                        "pistol.python",
                        "pistol.semiauto",
                        "shotgun.double",
                        "coffeecan.helmet",
                        "pistol.m92",
                        "roadsign.jacket"
                    },
                    [21600] = new List<string>()
                    {
                        "rifle.semiauto",
                        "shotgun.pump",
                        "smg.2",
                        "smg.mp5",
                        "smg.thompson",
                        "shotgun.spas12"
                    },
                    [36000] = new List<string>()
                    {
                        "rifle.m39",
                        "metal.facemask",
                        "rifle.bolt",
                        "grenade.f1",
                        "hmlmg",
                        "metal.plate.torso"
                    },
                    [64800] = new List<string>()
                    {
                        "heavy.plate.helmet",
                        "heavy.plate.jacket",
                        "heavy.plate.pants",
                        "rifle.ak.ice",
                        "metal.plate.torso.icevest",
                        "metal.facemask.icemask"
                    },
                    [86400] = new List<string>()
                    {
                        "rifle.ak",
                        "rifle.lr300",
                        "rifle.l96",
                        "grenade.beancan",
                        "explosive.satchel",
                        "ammo.rifle.explosive"
                    },
                    [1008000] = new List<string>()
                    {
                        "lmg.m249",
                        "rocket.launcher",
                        "explosive.timed",
                        "rifle.ak.diver",
                        "multiplegrenadelauncher",
                        "homingmissile.launcher"
                    },
                }
            };
        }


        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        object CanMoveItem(Item item, PlayerInventory inventory, ItemContainerId itemContainer, int num, int num2)
        {
            if (inventory == null || item == null) return null;

            var player = inventory.GetComponent<BasePlayer>();
            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission)) return null;

            var container = inventory.FindContainer(itemContainer);
            if (container == null || container.entityOwner == null) return null;

            if ((container.entityOwner is AutoTurret || container.entityOwner is MLRS))
            {
                var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?)null;
                if (isBlocked == false)
                {
                    MessBlockUi(player, item.info.shortname);
                    timer.Once(0.8f, () =>
                    {
                        CuiHelper.DestroyUi(player, Layer);
                    });
                    return true;
                }
            }

            return null;
        }

        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container == null || item == null || container.entityOwner == null) return null;

            if (container.entityOwner is AutoTurret || container.entityOwner is MLRS)
            {
                var player = item.GetOwnerPlayer();
                if (player == null) return null;
                if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission)) return null;

                var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?)null;
                if (isBlocked == false)
                {
                    MessBlockUi(player, item.info.shortname);
                    timer.Once(0.8f, () =>
                    {
                        CuiHelper.DestroyUi(player, Layer);
                    });
                    return ItemContainer.CanAcceptResult.CannotAcceptRightNow;
                }
            }

            return null;
        }

        private object CanWearItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (!player.userID.IsSteamId())
            {
                return null;
            }
            if (playerOnDuel(player)) return null;

            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;

            var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?)null;
            if (isBlocked == false)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;

                MessBlockUi(player, item.info.shortname);
                timer.Once(0.8f, () =>
                {
                    CuiHelper.DestroyUi(player, Layer);
                });
            }
            return isBlocked;
        }

        private object CanEquipItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null) return null;
            if (playerOnDuel(player)) return null;

            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;

            var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?)null;
            if (isBlocked == false)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;
                MessBlockUi(player, item.info.shortname);
                timer.Once(3.8f, () =>
                {
                    CuiHelper.DestroyUi(player, Layer);
                });
            }
            return isBlocked;
        }

        object OnWeaponReload(BaseProjectile projectile, BasePlayer player)
        {
            if (!player.userID.IsSteamId())
            {
                return null;
            }
            if (playerOnDuel(player)) return null;

            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;

            if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                return null;

            var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?)null;
            if (isBlocked == false)
            {
                MessBlockUi(player, projectile.primaryMagazine.ammoType.shortname);
                timer.Once(2f, () =>
                {
                    CuiHelper.DestroyUi(player, Layer);
                });

                return isBlocked;
            }

            return null;
        }

        object OnMagazineReload(BaseProjectile projectile, IAmmoContainer desiredAmount, BasePlayer player)
        {
            if (!player.userID.IsSteamId())
            {
                return null;
            }
            if (playerOnDuel(player)) return null;

            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;

            NextTick(() =>
            {
                var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?)null;
                if (isBlocked == false)
                {
                    projectile.primaryMagazine.contents = 0;
                    projectile.SendNetworkUpdate();
                    player.SendNetworkUpdate();
                    MessBlockUi(player, projectile.primaryMagazine.ammoType.shortname);
                    timer.Once(2f, () =>
                    {
                        CuiHelper.DestroyUi(player, Layer);
                    });
                }
            });

            return null;
        }

        private bool playerOnDuel(BasePlayer player)
        {
            if (plugins.Find("ArenaTournament") && (bool)plugins.Find("ArenaTournament").Call("IsOnTournament", (ulong)player.userID)) return true;
            if (plugins.Find("Duel") && (bool)plugins.Find("Duel").Call("IsPlayerOnActiveDuel", player)) return true;
            if (plugins.Find("OneVSOne") && (bool)plugins.Find("OneVSOne").Call("IsEventPlayer", player)) return true;
            return false;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(CONF_IgnorePermission, this);
            foreach (var check in config.items.SelectMany(p => p.Value))
                ImageLibrary.Call("AddImage", $"https://rustexplore.com/images/130/{check}.png", check);
        }

        private void MessBlockUi(BasePlayer player, string shortname)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#534E489E") },
                RectTransform =
                     {AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "-120 -25", OffsetMax = "120 50"},
                CursorEnabled = false,
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BlockItem",
                Components =
                 {
                     new CuiImageComponent {Color = "1 1 1 0.1"},
                     new CuiRectTransformComponent { AnchorMin = "0.01586128 0.05839238", AnchorMax = "0.2891653 0.9208925" }
                 }
            });

            container.Add(new CuiElement
            {
                Parent = Layer + ".BlockItem",
                Components =
                 {
                     new CuiRawImageComponent
                     {
                         Png = (string) ImageLibrary.Call("GetImage", $"{shortname}")
                     },
                     new CuiRectTransformComponent
                         {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                 }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                 {
                     new CuiTextComponent()
                     {
                         Color = "1 1 1 1",
                         Text = "Предмет заблокирован, для получения дополнительной информации пишите /block",
                         FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"
                     },
                     new CuiRectTransformComponent {AnchorMin = "0.3204 0.0833925", AnchorMax = "0.9802345 0.9458925"},
                 }
            });

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("mb.block.open")]
        private void cmdCaseOpen(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;

            BlockUi(arg.Player());
        }

        private const string Layer = "lay";
        void BlockUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.93" },
                Image = { Color = "0 0 0 0" },
            }, "ui.MenuBase.bg" + ".main.div", Layer + ".Main");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.932 195", OffsetMax = "66.788 250" },
                Text = { Text = $"БЛОКИРОВКА", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".Main");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Name = "BlockItems",
                Components =
                {
                    new CuiScrollViewComponent
                    {
                        Horizontal = false,
                        Vertical = true,
                        MovementType = UnityEngine.UI.ScrollRect.MovementType.Unrestricted,
                        Elasticity = 0,
                        Inertia = true,
                        DecelerationRate = 0.24f,
                        ScrollSensitivity = 20,
                        ContentTransform = new()
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "17 -410", OffsetMax = "610 -25"
                    },
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                }
            });

            float currentY = 0f, titleHeight = 32f, itemWidth = 60.5f, itemHeight = 59f, gap = 6f;
            int columns = 9, p = 0;

            foreach (var check in config.items)
            {
                p++;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"0 {-currentY - titleHeight}", OffsetMax = $"725 {-currentY}" },
                    Image = { Color = "1 1 1 0" }
                }, "BlockItems", "Title");

                var text = IsBlocked(check.Value.ElementAt(0)) > 0 ? $"{FormatShortTime(TimeSpan.FromSeconds(IsBlocked(check.Value.ElementAt(0))))}" : "разблокированно";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.01 0", AnchorMax = "0.98 1", OffsetMax = "0 0" },
                    Text = { Text = $"<b>{p} этап</b> - {text}", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf" },
                }, "Title");


                currentY += titleHeight + gap;
                List<string> items = check.Value;

                int totalRows = Mathf.CeilToInt(items.Count / (float)columns);

                for (int i = 0; i < items.Count; i++)
                {
                    int row = i / columns;
                    int col = i % columns;

                    float x = 0 + col * (itemWidth + gap);
                    float y = currentY + row * (itemHeight + gap);

                    var item = items[i];

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{x} {-y - itemHeight}", OffsetMax = $"{x + itemWidth} {-y}" },
                        Image = { Color = "1 1 1 0.3" }
                    }, "BlockItems", "Items");

                    var color = IsBlocked(item) > 0 ? "1 1 1 0.2" : "1 1 1 1";
                    var itemImage = ItemManager.FindItemDefinition(item).itemid;
                    container.Add(new CuiElement
                    {
                        Parent = "Items",
                        Components =
                        {
                            new CuiImageComponent {ItemId = itemImage, Color = color, FadeIn = 1f },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "15 15", OffsetMax = "-15 -15" }
                        }
                    });
                }
                currentY += totalRows * (itemHeight + gap) + gap + 20f;
            }

            foreach (var el in container)
            {
                foreach (var comp in el.Components)
                {
                    if (comp is CuiScrollViewComponent scroll)
                    {
                        float scrollHeight = -currentY + 7.5f;
                        scroll.ContentTransform.OffsetMin = $"0 {scrollHeight}";
                        scroll.ContentTransform.OffsetMax = $"0 0";
                        scroll.ContentTransform.AnchorMin = "0 1";
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("blockdesc")]
        void DescUI(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, Layer + ".Main" + ".Description");
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = Layer + ".Main" + ".Description",
                Parent = Layer + ".Main",
                Components = {
                    new CuiRectTransformComponent { AnchorMin = $"0.58 0.6", AnchorMax = $"0.8 0.8" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = "0.9 1" },
                Text = { Text = $"Описание блокировки", Color = "1 1 1 0.65", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, Layer + ".Main" + ".Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.05 0", AnchorMax = "1 0.7" },
                Text = { Text = $"{config.Info}", Color = "1 1 1 0.65", FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, Layer + ".Main" + ".Description");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.9 0.82", AnchorMax = "0.98 0.98" },
                Button = { Close = Layer + ".Main" + ".Description", Color = "1 1 1 0" },
                Text = { Text = "" }
            }, Layer + ".Main" + ".Description");

            CuiHelper.AddUi(player, container);
        }

        private double IsBlocked(string shortName)
        {
            if (!config.items.SelectMany(p => p.Value).Contains(shortName))
                return 0;
            var blockTime = config.items.FirstOrDefault(p => p.Value.Contains(shortName)).Key;
            var lefTime = (UnBlockTime(blockTime)) - CurrentTime();
            return lefTime > 0 ? lefTime : 0;
        }

        private bool BlockTimeGui(string shortName)
        {
            var blockTime = config.items.FirstOrDefault(p => p.Value.Contains(shortName)).Key;
            var lefTime = (UnBlockTime(blockTime)) - CurrentTime();
            if (lefTime > 0)
            {
                return true;
            }

            return false;
        }
        private double UnBlockTime(int amount) => SaveRestore.SaveCreatedTime.ToUniversalTime().Subtract(epoch).TotalSeconds + amount;
        private double IsBlocked(ItemDefinition itemDefinition) => IsBlocked(itemDefinition.shortname);
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result = $"{time.Days.ToString("00")}д ";
            if (time.Hours != 0)
                result += $"{time.Hours.ToString("00")}ч ";
            if (time.Minutes != 0)
                result += $"{time.Minutes.ToString("00")}с";
            return result;
        }

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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
    }
}