// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AdminPanel", "tofurahie", "1.0.7")]
    internal class AdminPanel : RustPlugin
    {
        #region Static

        private string perm = "adminpanel.use";
        private const string Layer = "UI_AdminPanel";

        private List<string> playerTypes = new List<string>
        {
            "ONLINE", "ALL", "OFFLINE"
        };

        private List<string> spawnEntityTypes = new List<string>
        {
            "ANIMALS", "CRATES", "TREES","VEHICLES"
        };
        
        List<char> Letters = new List<char> {'ö','ä','ß','ü','0','9','8','7','6','5','4','3','2','1','☼', 's', 't', 'r', 'e', 'т', 'ы', 'в', 'о', 'ч', 'х', 'а', 'р', 'u', 'c', 'h', 'a', 'n', 'z', 'o', '^', 'm', 'l', 'b', 'i', 'p', 'w', 'f', 'k', 'y', 'v', '$', '+', 'x', '®', 'd', '#', 'г', 'ш', 'к', '.', 'я', 'у', 'с', 'ь', 'ц', 'и', 'б', 'е', 'л', 'й', '_', 'м', 'п', 'н', 'g', 'q', ']', 'j', '[', '{', '}', '_', '!', '@', '#', '$', '%', '&', '?', '-', '+', '=', '~', ' ', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'а', 'б', 'в', 'г', 'д', 'е', 'ё', 'ж', 'з', 'и', 'й', 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ь', 'ы', 'ъ', 'э', 'ю', 'я' };

        private Dictionary<string, string> animals = new Dictionary<string, string>();
        private Dictionary<string, string> trees = new Dictionary<string, string>();
        private Dictionary<string, string> crates = new Dictionary<string, string>();
        private Dictionary<string, string> vehicles = new Dictionary<string, string>
        {
            ["MODULAR_CAR_2"] = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
            ["MODULAR_CAR_3"] = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
            ["MODULAR_CAR_4"] = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
            ["MINICOPTER"] = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
            ["SCRAPCOPTER"] = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
            ["MLRS"] = "assets/content/vehicles/mlrs/mlrs.entity.prefab",
            ["ROWBOAT"] = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
            ["RHIB"] = "assets/content/vehicles/boats/rhib/rhib.prefab",
            ["SUBMARINESOLO"] = "assets/content/vehicles/submarine/submarinesolo.entity.prefab",
            ["SUBMARINEDUO"] = "assets/content/vehicles/submarine/submarineduo.entity.prefab",
            ["SNOWMOBILE"] = "assets/content/vehicles/snowmobiles/snowmobile.prefab"
        };

        #region Image

        [PluginReference] private Plugin ImageLibrary;
        private int ILCheck = 0;

        #endregion

        #endregion

        #region OxideHooks

        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                if (ILCheck == 3)
                {
                    PrintError("ImageLibrary not found!Unloading");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }

                timer.In(1, () =>
                {
                    ILCheck++;
                    OnServerInitialized();
                });
                return;
            }

            if (!permission.PermissionExists(perm)) permission.RegisterPermission(perm, this);
            
            animals.Add("ridableHorse", "assets/rust.ai/nextai/testridablehorse.prefab");
            
            foreach (var check in BaseNetworkable.serverEntities.OfType<BaseAnimalNPC>())
            {
                if (animals.ContainsKey(check.ShortPrefabName)) continue;
                animals.Add(check.ShortPrefabName, check.PrefabName);
            }

            foreach (var check in BaseNetworkable.serverEntities.OfType<LootContainer>())
            {
                if (crates.ContainsKey(check.ShortPrefabName)) continue;
                crates.Add(check.ShortPrefabName, check.PrefabName);
            }

            foreach (var check in BaseNetworkable.serverEntities.OfType<TreeEntity>())
            {
                if (trees.ContainsKey(check.ShortPrefabName)) continue;
                trees.Add(check.ShortPrefabName, check.PrefabName);
            }
        }

        private void OnEntitySpawned(BaseAnimalNPC animal)
        {
            if (animal == null || animals.ContainsKey(animal.ShortPrefabName)) return;
            animals.Add(animal.ShortPrefabName, animal.PrefabName);
        }

        private void OnEntitySpawned(LootContainer animal)
        {
            if (animal == null || crates.ContainsKey(animal.ShortPrefabName)) return;
            crates.Add(animal.ShortPrefabName, animal.PrefabName);
        }

        private void OnEntitySpawned(TreeEntity animal)
        {
            if (animal == null || trees.ContainsKey(animal.ShortPrefabName)) return;
            trees.Add(animal.ShortPrefabName, animal.PrefabName);
        }

        private void Unload()
        {
            foreach (var check in BasePlayer.activePlayerList) CuiHelper.DestroyUi(check, Layer + ".bg");
        }

        #endregion

        #region Commands

        [ChatCommand("admin")]
        private void cmdChatAdmin(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                SendReply(player, "You haven't permission for use this command");
                return;
            }
            ShowUIMain(player);
        }

        [ConsoleCommand("UI_AP")]
        private void cmdConsole(ConsoleSystem.Arg arg)
        {
            if (arg?.Args == null || arg.Args.Length < 1) return;
            var player = arg.Player();
            var list = Facepunch.Pool.GetList<BasePlayer>();
            switch (arg.Args[0])
            {
                case "players":
                    ShowUIPlayerPanel(player, arg.Args[1], arg.GetInt(2));
                    break;
                case "spawnEntity":
                    ShowUISpawnEntityPanel(player, arg.Args[1], arg.GetInt(2));
                    break;
                case "tp":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    player.Teleport(list.FirstOrDefault(x => x.UserIDString == arg.Args[1]).transform.position);
                    CuiHelper.DestroyUi(player, Layer + ".bg");
                    break;
                case "tpforme":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    list.FirstOrDefault(x => x.UserIDString == arg.Args[1]).Teleport(player.transform.position);
                    CuiHelper.DestroyUi(player, Layer + ".bg");
                    break;
                case "kick":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    list.FirstOrDefault(x => x.UserIDString == arg.Args[1])?.Kick("AdminPanel kick");
                    break;
                case "ban":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    list.FirstOrDefault(x => x.UserIDString == arg.Args[1])?.IPlayer?.Ban("Banned by AdminPanel");
                    break;
                case "spawn":
                    var input = arg.Args[1];
                    var prefab = "";
                    RaycastHit info1;
                    if (!Physics.Raycast(player.eyes.HeadRay(), out info1, 15)) return;
                    if (animals.ContainsKey(input)) prefab = animals[input];
                    else if (crates.ContainsKey(input)) prefab = crates[input];
                    else if (trees.ContainsKey(input)) prefab = trees[input];
                    else if (vehicles.ContainsKey(input)) prefab = vehicles[input];
                    if (string.IsNullOrEmpty(prefab)) return;
                    var obj = GameManager.server.CreateEntity(prefab, info1.point);
                    obj.Spawn();
                    CuiHelper.DestroyUi(player, Layer + ".bg");
                    break;
                case "owner":
                    RaycastHit info;
                    if (!Physics.Raycast(player.eyes.HeadRay(), out info, 15)) return;
                    var ent = info.GetEntity();
                    if (ent == null) return;
                    player.ChatMessage(ent.OwnerID.IsSteamId() ? $"Entity Owner Name: {BasePlayer.Find(ent.OwnerID.ToString())?.displayName}\nEntity OwnerID: {ent.OwnerID}\nEntity class: {ent.GetType()}" : $"Entity OwnerID: {ent.OwnerID}\nEntity class: {ent.GetType()}");
                    CuiHelper.DestroyUi(player, Layer + ".bg");
                    break;
                case "inventorycheck":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    var target = list.FirstOrDefault(x => x.UserIDString == arg.Args[1]);
                    if (target == null) break;
                    ShowUIPlayerInventory(player, target);
                    break;
                case "removeitem":
                    foreach (var check in BasePlayer.activePlayerList) list.Add(check);
                    foreach (var check in BasePlayer.sleepingPlayerList)
                    {
                        if (!check.userID.IsSteamId()) continue;
                        list.Add(check);
                    }

                    var targetPlayer = list.FirstOrDefault(x => x.UserIDString == arg.Args[1]);
                    if (targetPlayer == null) break;
                    targetPlayer.inventory.AllItems().FirstOrDefault(x => x.uid.Value == arg.GetUInt(2))?.DoRemove();
                    ShowUIPlayerInventory(player, targetPlayer);
                    break;
            }

            Facepunch.Pool.FreeList(ref list);
        }

        #endregion

        #region UI

        private void ShowUIMain(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0.95", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"}
            }, "Overlay", Layer + ".bg");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.948 0.907", AnchorMax = "0.99 0.98"},
                Button = {Color = "0 0 0 0", Close = Layer + ".bg"},
                Text =
                {
                    Text = "×", Font = "robotocondensed-regular.ttf", FontSize = 46, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer + ".bg", Layer + ".buttonClose");
            Outline(ref container, Layer + ".buttonClose");


            CuiHelper.DestroyUi(player, Layer + ".bg");
            CuiHelper.AddUi(player, container);

            ShowUIPanel(player);
            ShowUIPlayerPanel(player);
        }

        private void ShowUIPlayerInventory(BasePlayer player, BasePlayer target)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0.96", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"}
            }, Layer + ".bg", Layer + ".playerInventoryPanel");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-185 -181", OffsetMax = "185 181"},
                Image = {Color = "0 0 0 0.7"}
            }, Layer + ".playerInventoryPanel", Layer + ".playerInventory");
            Outline(ref container, Layer + ".playerInventory");

            var itemSize = 46;
            var itemsSpace = 5;
            var items = target.inventory.containerWear.itemList;
            var itemsLength = items.Count;
            var posX = itemsSpace;
            var posY = -itemsSpace;

            for (int i = 0; i < 7; i++)
            {
                DrawPanels(ref container, i, posX, posY, itemSize, itemsLength, items, target.UserIDString);

                posY -= itemSize + itemsSpace;
            }

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{itemSize + itemsSpace * 2} {(itemSize * 7 + itemsSpace * 7) * -1}", OffsetMax = $"{itemSize + itemsSpace * 2 + 2} {-itemsSpace}"},
                Image = {Color = "1 1 1 0.65"}
            }, Layer + ".playerInventory");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"{itemSize + itemsSpace * 2} {itemsSpace - itemSize * 2.25}"},
                Text =
                {
                    Text = "PLAYER INVENTORY", Font = "robotocondensed-bold.ttf", FontSize = 35, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer + ".playerInventory");
            var name = "";
            foreach (var @char in target.displayName) if (Letters.Contains(@char.ToString().ToLower().ToCharArray()[0])) name += @char;
            if (name.Length != target.displayName.Length) name = "IRREGULAR NAME";
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"{itemSize + itemsSpace * 2} {itemsSpace - itemSize * 3.5f}"},
                Text =
                {
                    Text = name, Font = "robotocondensed-bold.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter,
                    Color = "1.00 1.00 0.00 1.00"
                }
            }, Layer + ".playerInventory");

            items = target.inventory.containerMain.itemList;
            itemsLength = items.Count;
            posX = itemSize + itemsSpace * 3 + 2;
            posY = (itemsSpace * 3 + itemSize * 2) * -1;
            for (int i = 0; i < 24; i++)
            {
                DrawPanels(ref container, i, posX, posY, itemSize, itemsLength, items, target.UserIDString);

                posX += itemSize + itemsSpace;
                if (posX < itemSize * 7) continue;
                posY -= itemSize + itemsSpace;
                posX = itemSize + itemsSpace * 3 + 2;
            }

            items = target.inventory.containerBelt.itemList;
            itemsLength = items.Count;
            posX = itemSize + itemsSpace * 3 + 2;
            posY = (itemsSpace * 7 + itemSize * 6) * -1;
            for (int i = 0; i < 6; i++)
            {
                DrawPanels(ref container, i, posX, posY, itemSize, itemsLength, items, target.UserIDString);

                posX += itemSize + itemsSpace;
            }

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-28 -28", OffsetMax = "0 0"},
                Button = {Color = "0 0 0 0", Close = Layer + ".playerInventoryPanel"},
                Text =
                {
                    Text = "×", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer + ".playerInventory", Layer + ".buttonClose");
            Outline(ref container, Layer + ".buttonClose");

            CuiHelper.DestroyUi(player, Layer + ".playerInventoryPanel");
            CuiHelper.AddUi(player, container);
        }

        private void DrawPanels(ref CuiElementContainer container, int i, int posX, int posY, int itemSize, int itemsLength, List<Item> items, string id)
        {
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY - itemSize}", OffsetMax = $"{posX + itemSize} {posY}"},
                Image = {Color = "0.2 0.2 0.2 0.75"}
            }, Layer + ".playerInventory", Layer + "playerInventoryPanel" + posX + posY);
            if (i < itemsLength)
            {
                var item = items[i];

                container.Add(new CuiElement
                {
                    Parent = Layer + "playerInventoryPanel" + posX + posY,
                    Components =
                    {
                        new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", item.info.shortname, item.skin)},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "5 5", OffsetMax = "41 41"}
                    }
                });



                var held = item.GetHeldEntity();
                var weapon = held != null && held is BaseProjectile ? held as BaseProjectile : null;
                if (weapon == null)
                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "43 46"},
                        Text =
                        {
                            Text = $"x{item.amount}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.LowerRight,
                            Color = "1 1 1 1"
                        }
                    }, Layer + "playerInventoryPanel" + posX + posY);
                else
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "43 46"},
                        Text =
                        {
                            Text = $"x{weapon.primaryMagazine.contents}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.LowerRight,
                            Color = "1 1 1 1"
                        }
                    }, Layer + "playerInventoryPanel" + posX + posY);

                    if (item.contents != null)
                    {
                        var xSwitch = 6;
                        foreach (var attacment in item.contents.itemList)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = Layer + "playerInventoryPanel" + posX + posY,
                                Components =
                                {
                                    new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", attacment.info.shortname, attacment.skin)},
                                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xSwitch} -12", OffsetMax = $"{xSwitch + 10} -2"}
                                }
                            });

                            xSwitch += 10;
                        }
                    }
                }

                if (item.hasCondition)
                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "-0.03 0", AnchorMax = "-0.03 0", OffsetMax = $"4 {46 * (item.condition / item.maxCondition)}"},
                        Image = {Color = "0.38 0.46 0.25 1"}
                    }, Layer + "playerInventoryPanel" + posX + posY);
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-15 -15", OffsetMax = "0 0"},
                    Button = {Color = "0 0 0 0", Command = $"UI_AP removeitem {id} {item.uid}"},
                    Text =
                    {
                        Text = "×", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1.00"
                    }
                }, Layer + "playerInventoryPanel" + posX + posY, Layer + ".buttonClose");
            }

        }

        private void ShowUIPlayerPanel(BasePlayer player, string type = "ONLINE", int page = 0)
        {
            var container = new CuiElementContainer();
            var posX = 0.29f;

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.185 0.15", AnchorMax = "0.815 0.81"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".bg", Layer + ".infoPanel");

            foreach (var check in playerTypes)
            {
                var thisCategory = check == type;
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX} 0.85", AnchorMax = $"{posX + 0.14} 0.9"},
                    Button = {Color = "0 0 0 0", Command = thisCategory ? "" : $"UI_AP players {check} 0"},
                    Text =
                    {
                        Text = check, Font = "robotocondensed-Bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                        Color = thisCategory ? "1 1 1 1" : "0.3 0.3 0.3 1"
                    }
                }, Layer + ".infoPanel");
                posX += 0.14f;
            }

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.3 0.85", AnchorMax = "0.7 0.85", OffsetMin = "0 0", OffsetMax = "0 1"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".infoPanel");

            var countOfPlayer = new List<BasePlayer>();
            if (type == "OFFLINE") countOfPlayer = BasePlayer.sleepingPlayerList.Where(x => x.userID.IsSteamId()).ToList();
            else if (type == "ONLINE") countOfPlayer = BasePlayer.activePlayerList.Where(x => x.IsConnected).ToList();
            else
            {
                foreach (var check in BasePlayer.activePlayerList) if (check.IsConnected) countOfPlayer.Add(check);
                foreach (var check in BasePlayer.sleepingPlayerList) if (check.userID.IsSteamId()) countOfPlayer.Add(check);
            }

            posX = 0.025f;
            var posY = 0.675f;
            foreach (var check in countOfPlayer.Skip(16 * page).Take(16))
            {
                var name = "";
                foreach (var @char in check.displayName) if (Letters.Contains(@char.ToString().ToLower().ToCharArray()[0])) name += @char;
                if (name.Length != check.displayName.Length) name = "IRREGULAR NAME";
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = $"{posX} {posY}", AnchorMax = $"{posX + 0.075f} {posY + 0.13f}"},
                    Image = {Color = "0 0 0 0.5"}
                }, Layer + ".infoPanel", Layer + ".player");
                container.Add(new CuiElement
                {
                    Parent = Layer + ".player",
                    Components =
                    {
                        new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", check.UserIDString)},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0.984 0.984"}
                    }
                });
                Outline(ref container, Layer + ".player", check.IsConnected ? "0.20 0.69 0.04 1.00" : "0.69 0.02 0.09 1.00", "2");
                container.Add(new CuiLabel
                {
                    RectTransform = {AnchorMin = $"{posX + 0.085f} {posY}", AnchorMax = $"{posX + 0.24f} {posY + 0.185f}"},
                    Text =
                    {
                        Text = name, Font = "robotocondensed-bold.ttf", FontSize = 15,
                        Align = TextAnchor.MiddleLeft,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiLabel
                {
                    RectTransform = {AnchorMin = $"{posX + 0.085f} {posY}", AnchorMax = $"{posX + 0.24f} {posY + 0.125f}"},
                    Text =
                    {
                        Text = check.UserIDString, Font = "robotocondensed-regular.ttf", FontSize = 12,
                        Align = TextAnchor.MiddleLeft,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX + 0.085f} {posY}", AnchorMax = $"{posX + 0.11f} {posY + 0.04f}"},
                    Button = {Color = "0.67 0.87 1.00 0.98", Sprite = "assets/icons/examine.png", Command = $"UI_AP inventorycheck {check.UserIDString}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.LowerCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX + 0.12f} {posY + 0.005f}", AnchorMax = $"{posX + 0.14f} {posY + 0.035f}"},
                    Button = {Color = "1 1 1 1", Sprite = "assets/icons/press.png", Command = $"UI_AP tpforme {check.UserIDString}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX + 0.155f} {posY + 0.005f}", AnchorMax = $"{posX + 0.175f} {posY + 0.035f}"},
                    Button = {Color = "1 1 1 1", Sprite = "assets/icons/maximum.png", Command = $"UI_AP tp {check.UserIDString}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX + 0.185f} {posY + 0.005f}", AnchorMax = $"{posX + 0.215f} {posY + 0.035f}"},
                    Button = {Color = "1.00 0.95 0.37 1.00", Sprite = "assets/icons/fall.png", Command = $"UI_AP kick {check.UserIDString}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX + 0.22f} {posY + 0.005f}", AnchorMax = $"{posX + 0.24f} {posY + 0.035f}"},
                    Button = {Color = "1.00 0.00 0.00 1.00", Sprite = "assets/icons/demolish_cancel.png", Command = $"UI_AP ban {check.UserIDString}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

                posX += 0.24f;
                if (posX < 0.9f) continue;
                posX = 0.025f;
                posY -= 0.18f;
            }
            
            if (page > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.45 0.02", AnchorMax = "0.48 0.125"},
                    Button = {Color = "0 0 0 0", Command = $"UI_AP players {type} {page - 1}"},
                    Text =
                    {
                        Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 35,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

            if (countOfPlayer.Count - 16 * (page + 1) > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.52 0.02", AnchorMax = "0.545 0.125"},
                    Button = {Color = "0 0 0 0", Command = $"UI_AP players {type} {page + 1}"},
                    Text =
                    {
                        Text = ">", Font = "robotocondensed-regular.ttf", FontSize = 35,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

            CuiHelper.DestroyUi(player, Layer + ".infoPanel");
            CuiHelper.AddUi(player, container);
        }

        private void ShowUISpawnEntityPanel(BasePlayer player, string type = "ANIMALS", int page = 0)
        {
            var container = new CuiElementContainer();
            var posX = 0.2f;

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.185 0.15", AnchorMax = "0.815 0.81"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".bg", Layer + ".infoPanel");

            foreach (var check in spawnEntityTypes)
            {
                var thisCategory = check == type;
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX} 0.85", AnchorMax = $"{posX + 0.14} 0.9"},
                    Button = {Color = "0 0 0 0", Command = thisCategory ? "" : $"UI_AP spawnEntity {check} 0"},
                    Text =
                    {
                        Text = check, Font = "robotocondensed-Bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                        Color = thisCategory ? "1 1 1 1" : "0.3 0.3 0.3 1"
                    }
                }, Layer + ".infoPanel");
                posX += 0.14f;
            }

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.18 0.85", AnchorMax = "0.82 0.85", OffsetMin = "0 0", OffsetMax = "0 1"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".infoPanel");

            Dictionary<string, string> countOfPlayer;
            switch (type)
            {
                case "VEHICLES":
                    countOfPlayer = vehicles;
                    break;
                case "CRATES":
                    countOfPlayer = crates;
                    break;
                case "TREES":
                    countOfPlayer = trees;
                    break;
                default:
                    countOfPlayer = animals;
                    break;
            }

            posX = 0.025f;
            var posY = 0.745f;
            foreach (var check in countOfPlayer.Skip(35 * page).Take(35))
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = $"{posX} {posY}", AnchorMax = $"{posX + 0.15} {posY + 0.06}"},
                    Button = {Color = "0.3 0.3 0.3 0.45", Command = $"UI_AP spawn {check.Key}"},
                    Text =
                    {
                        Text = check.Key, Font = "robotocondensed-bold.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel", Layer + ".someItem");
                Outline(ref container, Layer + ".someItem");

                posX += 0.2f;
                if (posX < 0.875f) continue;
                posX = 0.025f;
                posY -= 0.1f;
            }

            if (page > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.45 0.02", AnchorMax = "0.48 0.125"},
                    Button = {Color = "0 0 0 0", Command = $"UI_AP spawnEntity {type} {page - 1}"},
                    Text =
                    {
                        Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 35,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

            if (countOfPlayer.Count - 35 * (page + 1) > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.52 0.02", AnchorMax = "0.545 0.125"},
                    Button = {Color = "0 0 0 0", Command = $"UI_AP spawnEntity {type} {page + 1}"},
                    Text =
                    {
                        Text = ">", Font = "robotocondensed-regular.ttf", FontSize = 35,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".infoPanel");

            CuiHelper.DestroyUi(player, Layer + ".infoPanel");
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.185 0.15", AnchorMax = "0.815 0.85"},
                Image = {Color = "0.07 0.00 0.56 0.2"}
            }, Layer + ".bg", Layer + ".mainPanel");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0.52 0.87 0.99 0.3", Sprite = "assets/content/ui/ui.background.transparent.linear.psd"}
            }, Layer + ".mainPanel");
            Outline(ref container, Layer + ".mainPanel", "1 1 1 1", "2");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.185 0.81", AnchorMax = "0.815 0.81", OffsetMin = "0 0", OffsetMax = "0 1"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".bg");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.185 0.81", AnchorMax = "0.24 0.85"},
                Button = {Color = "0 0 0 0", Command = "UI_AP players ONLINE 0"},
                Text =
                {
                    Text = "PLAYERS", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer + ".bg");

            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0.24 0.81", AnchorMax = "0.24 0.85", OffsetMin = "0 0", OffsetMax = "1 0"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".bg");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.24 0.81", AnchorMax = "0.33 0.85"},
                Button = {Color = "0 0 0 0", Command = "UI_AP spawnEntity ANIMALS 0"},
                Text =
                {
                    Text = "SPAWN ENTITY", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer + ".bg");
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.33 0.81", AnchorMax = "0.33 0.85", OffsetMin = "0 0", OffsetMax = "1 0"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".bg");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.33 0.81", AnchorMax = "0.4 0.85"},
                Button = {Color = "0 0 0 0", Command = "UI_AP owner"},
                Text =
                {
                    Text = "GET OWNER", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer + ".bg");
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.4 0.81", AnchorMax = "0.4 0.85", OffsetMin = "0 0", OffsetMax = "1 0"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".bg");

            CuiHelper.DestroyUi(player, Layer + ".mainPanel");
            CuiHelper.AddUi(player, container);
        }

        private void Outline(ref CuiElementContainer container, string parent, string color = "1 1 1 1",
            string size = "1")
        {
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"0 0", OffsetMax = $"0 {size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = $"0 0"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}"},
                Image = {Color = color}
            }, parent);
        }

        #endregion
    }
}