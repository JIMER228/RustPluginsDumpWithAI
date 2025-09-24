using System.Collections.Generic;
using System;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Oxide.Core;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    /* ПЛАГИН БЫЛ РАЗЫГРАН В ДИСКОРДЕ RUST EDIT PRO https://discord.gg/8CWJadmA5w */ [Info("XBuildingSkinMenu", "https://discord.gg/8CWJadmA5w", "1.0.5")]
    class XBuildingSkinMenu : RustPlugin
    {

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (StoredData.ContainsKey(player.userID))
            {
                SaveData(player);
                StoredData.Remove(player.userID);
            }
        }
        protected override void SaveConfig() => Config.WriteObject(config);

        private void OnEntityKill(BuildingPrivlidge entity)
        {
            ulong netID = entity.net.ID.Value;

            if (_coroutine_list.ContainsKey(netID))
            {
                ServerMgr.Instance.StopCoroutine(_coroutine_list[netID]);
                _coroutine_list.Remove(netID);
            }

            if (_coroutine_listEnt.ContainsKey(netID))
            {
                ServerMgr.Instance.StopCoroutine(_coroutine_listEnt[netID]);
                _coroutine_listEnt.Remove(netID);
            }
        }

        private readonly Dictionary<uint, string> _colours = new Dictionary<uint, string>
        {
            [1] = "0.38 0.56 0.74 1",
            [2] = "0.45 0.71 0.35 1",
            [3] = "0.57 0.29 0.83 1",
            [4] = "0.42 0.16 0.11 1",
            [5] = "0.82 0.46 0.13 1",
            [6] = "0.87 0.87 0.87 1",
            [7] = "0.20 0.20 0.18 1",
            [8] = "0.40 0.33 0.28 1",
            [9] = "0.20 0.22 0.33 1",
            [10] = "0.24 0.35 0.20 1",
            [11] = "0.73 0.29 0.18 1",
            [12] = "0.78 0.53 0.39 1",
            [13] = "0.86 0.66 0.22 1",
            [14] = "0.34 0.33 0.31 1",
            [15] = "0.21 0.34 0.37 1",
            [16] = "0.66 0.61 0.56 1"
        };

        private void BuildingSkinGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 0.5", OffsetMin = "0 -157.5", OffsetMax = "0 157.5" },
                Image = { Color = "0 0 0 0" }
            }, ".BSkinGUI", ".BBSkinGUI", ".BBSkinGUI");

            int x = 0, y = 0;

            foreach (var building in config.Setting.BuildingSkins)
            {
                string key = building.Key;
                ulong player_data = StoredData[player.userID].BuildingSkins[key];

                foreach (var skinID in building.Value)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-155 + (x * 80)} {77.5 - (y * 75)}", OffsetMax = $"{-85 + (x * 80)} {147.5 - (y * 75)}" },
                        Image = { Color = player_data == skinID.Key ? config.GUI.ActiveBlockColor : config.GUI.BlockColor, Material = "assets/icons/greyout.mat" }
                    }, ".BBSkinGUI", ".BSkin");

                    container.Add(new CuiElement
                    {
                        Parent = ".BSkin",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", $"B_IMG_{key + skinID.Key}") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"skin_build {key} {skinID.Key}" },
                        Text = { Text = "" }
                    }, ".BSkin");

                    y++;
                }

                x++;
                y = 0;
            }

            CuiHelper.AddUi(player, container);
        }

        private const string permUse = "xbuildingskinmenu.use";



        internal class Data
        {
            [JsonProperty(LanguageEnglish ? "Change the skin of a building block with a hammer" : "Изменять скин строительного блока киянкой")] public bool HChange;
            [JsonProperty(LanguageEnglish ? "Selected color" : "Выбранный цвет")] public uint ColourID;
            [JsonProperty(LanguageEnglish ? "Skins for buildings" : "Скины на постройки")] public Dictionary<string, ulong> BuildingSkins = new Dictionary<string, ulong> { ["Wood"] = 0, ["Stone"] = 0, ["Metal"] = 0, ["TopTier"] = 0 };
        }


        [PluginReference] private Plugin ImageLibrary, XSkinMenu;
        private const bool LanguageEnglish = false;

        [ConsoleCommand("set_skin_building")]
        private void ccmdSetSkinBuilding(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            BaseEntity entity = player?.inventory?.loot?.entitySource;

            if (entity == null || args.Args == null || args.Args.Length == 0 || !(entity is BuildingPrivlidge)) return;
            if (config.Setting.HouseOwner && player.userID != entity.OwnerID) return;

            switch (args.Args[0])
            {
                case "block":
                    {
                        if (permission.UserHasPermission(player.UserIDString, permUse))
                        {
                            if (_coroutine_list.ContainsKey(entity.net.ID.Value))
                            {
                                SendReply(player, string.Format(lang.GetMessage("PAINT_ACTIVE", this, player.UserIDString), entity.net.ID.Value));

                                return;
                            }

                            _coroutine_list[entity.net.ID.Value] = ServerMgr.Instance.StartCoroutine(SetBuildingSkins(player, entity));
                            EffectNetwork.Send(new Effect("assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
                        }

                        break;
                    }
                case "entity":
                    {
                        if (config.Setting.UseXSM && permission.UserHasPermission(player.UserIDString, permEnt))
                        {
                            if (_coroutine_listEnt.ContainsKey(entity.net.ID.Value))
                            {
                                SendReply(player, string.Format(lang.GetMessage("PAINT_ENT_ACTIVE", this, player.UserIDString), entity.net.ID.Value));

                                return;
                            }

                            _coroutine_listEnt[entity.net.ID.Value] = ServerMgr.Instance.StartCoroutine(SetEntitySkins(player, entity));
                            EffectNetwork.Send(new Effect("assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
                        }

                        break;
                    }
            }
        }

        private void Unload()
        {
            foreach (var coroutine in _coroutine_list)
                ServerMgr.Instance.StopCoroutine(coroutine.Value);

            foreach (var coroutine in _coroutine_listEnt)
                ServerMgr.Instance.StopCoroutine(coroutine.Value);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, ".SetSkinBUTTON");
                CuiHelper.DestroyUi(player, ".SetSkinEntBUTTON");
                CuiHelper.DestroyUi(player, ".BgBSkinGUI");

                SaveData(player);
            }
        }

        [ConsoleCommand("skin_build")]
        private void ccmdSetBuildingSkin(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            if (player == null || args.Args == null || !permission.UserHasPermission(player.UserIDString, permUse)) return;

            if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;

            Cooldowns[player] = DateTime.Now.AddSeconds(1.5f);

            if (args.Args.Length >= 1 && args.Args[0] == "hammer")
            {
                StoredData[player.userID].HChange = !StoredData[player.userID].HChange;

                ButtonGUI(player);
                EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);

                return;
            }

            if (args.Args.Length >= 2)
            {
                string key = args.Args[0];

                if (config.Setting.BuildingSkins.ContainsKey(key) && StoredData[player.userID].BuildingSkins.ContainsKey(key))
                {
                    ulong skinID;
                    ulong.TryParse(args.Args[1], out skinID);

                    if (config.Setting.BuildingSkins[key].ContainsKey(skinID))
                    {
                        StoredData[player.userID].BuildingSkins[key] = skinID;
                        SaveData(player);

                        BuildingSkinGUI(player);
                        EffectNetwork.Send(new Effect("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
                    }
                }
            }
        }

        private readonly Dictionary<string, string> _shortnamesEntity = new Dictionary<string, string>();

        private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XDataSystem/XBuildingSkinMenu/{player.userID}", StoredData[player.userID]);

        private void OnLootEntityEnd(BasePlayer player, BuildingPrivlidge entity)
        {
            CuiHelper.DestroyUi(player, ".SetSkinBUTTON");
            CuiHelper.DestroyUi(player, ".SetSkinEntBUTTON");
        }


        [ChatCommand("bskin")]
        private void cmdOpenGUIB(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permUse))
                GUI(player);
            else
                SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
        }

        private IEnumerator SetEntitySkins(BasePlayer player, BaseEntity entity)
        {
            yield return CoroutineEx.waitForSeconds(2);

            int count = 0;
            ulong netID = entity.net.ID.Value;
            var player_data = ((Dictionary<string, ulong>)XSkinMenu.Call("API_GetSkinsPlayer", player.userID)).ToDictionary(x => x.Key, x => x.Value);

            if (player_data.ContainsKey("discofloor")) player_data.Remove("discofloor");
            if (player_data.ContainsKey("skylantern")) player_data.Remove("skylantern");
            if (player_data.ContainsKey("skull.trophy")) player_data.Remove("skull.trophy");
            if (player_data.ContainsKey("door.hinged.metal") && (player_data["door.hinged.metal"] == 10189 || player_data["door.hinged.metal"] == 10198)) player_data.Remove("door.hinged.metal");

            SendReply(player, string.Format(lang.GetMessage("PAINT_ENT_START", this, player.UserIDString), netID));

            foreach (var ent in entity.GetBuildingPrivilege().GetBuilding().decayEntities.Where(x => !(x is BuildingBlock)))
            {
                if (_shortnamesEntity.ContainsKey(ent.ShortPrefabName))
                {
                    string shortname = _shortnamesEntity[ent.ShortPrefabName];

                    if (player_data.ContainsKey(shortname))
                    {
                        if (ent.skinID != player_data[shortname] && player_data[shortname] != 0)
                        {
                            ent.skinID = player_data[shortname];
                            ent.SendNetworkUpdate();

                            if (config.Setting.UseEffectE)
                            {
                                Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", ent.transform.localPosition);
                                Effect.server.Run("assets/prefabs/tools/spraycan/reskineffect.prefab", ent.transform.localPosition);
                            }

                            count++;

                            yield return CoroutineEx.waitForSeconds(0.15f);
                        }
                        else
                            yield return CoroutineEx.waitForSeconds(0.05f);
                    }
                    else
                        yield return CoroutineEx.waitForSeconds(0.05f);
                }
                else
                    yield return CoroutineEx.waitForSeconds(0.05f);
            }

            yield return CoroutineEx.waitForSeconds(1);

            SendReply(player, string.Format(lang.GetMessage("PAINT_ENT_END", this, player.UserIDString), netID, count));

            _coroutine_listEnt.Remove(netID);
            yield return 0;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (StoredData.ContainsKey(player.userID))
                player.LastBlockColourChangeId = StoredData[player.userID].ColourID;
        }

        private class BuildingSkinConfig
        {
            [JsonProperty(LanguageEnglish ? "GUI setting" : "Настройки GUI")]
            public GUISetting GUI = new GUISetting();

            public static BuildingSkinConfig GetNewConfiguration()
            {
                return new BuildingSkinConfig
                {
                    Setting = new GeneralSetting
                    {
                        HouseOwner = false,
                        UseEffect = true,
                        UseEffectE = true,
                        UseEffectH = false,
                        UseXSM = false,
                        BuildingSkins = new Dictionary<string, Dictionary<ulong, string>>
                        {
                            ["Wood"] = new Dictionary<ulong, string> { [0] = "https://i.ibb.co/XyZsdWn/111.png" },
                            ["Stone"] = new Dictionary<ulong, string> { [0] = "https://i.ibb.co/Hdcdzw3/222.png", [10220] = "https://i.ibb.co/dBQpY6B/333.png", [10223] = "https://i.ibb.co/0JPt5Xq/777.png" },
                            ["Metal"] = new Dictionary<ulong, string> { [0] = "https://i.ibb.co/2Mds7d2/444.png", [10221] = "https://i.ibb.co/sbvfN7F/666.png" },
                            ["TopTier"] = new Dictionary<ulong, string> { [0] = "https://i.ibb.co/94dwD3p/555.png" }
                        }
                    },
                    GUI = new GUISetting
                    {
                        BColor1 = "0.517 0.521 0.509 0.95",
                        BColor2 = "0.217 0.221 0.209 0.95",
                        BlockColor = "0.517 0.521 0.509 0.5",
                        ActiveBlockColor = "0.53 0.77 0.35 0.8",
                        IconColor = "1 1 1 0.75",
                        ButtonColorCup = "0.35 0.45 0.25 1",
                        TButtonColorCup = "0.75 0.95 0.41 1",
                        ButtonColorHOn = "0.35 0.45 0.25 1",
                        TButtonColorHOn = "0.75 0.95 0.41 1",
                        ButtonColorHOff = "0.65 0.29 0.24 1",
                        TButtonColorHOff = "0.92 0.79 0.76 1",
                        AnchorMinB1 = "0.5 0",
                        AnchorMaxB1 = "0.5 0",
                        OffsetMinB1 = "395 585",
                        OffsetMaxB1 = "572.5 605",
                        AnchorMinB2 = "0.5 0",
                        AnchorMaxB2 = "0.5 0",
                        OffsetMinB2 = "395 610",
                        OffsetMaxB2 = "572.5 630"
                    }
                };
            }

            internal class GUISetting
            {
                [JsonProperty(LanguageEnglish ? "Color_background_1" : "Цвет_фон_1")] public string BColor1;
                [JsonProperty(LanguageEnglish ? "Color_background_2" : "Цвет_фон_2")] public string BColor2;
                [JsonProperty(LanguageEnglish ? "Skin background color" : "Цвет заднего фона скинов")] public string BlockColor;
                [JsonProperty(LanguageEnglish ? "Background color of the selected skin" : "Цвет заднего фона выбранного скина")] public string ActiveBlockColor;
                [JsonProperty(LanguageEnglish ? "Close button (icon) color" : "Цвет кнопки (иконки) закрыть")] public string IconColor;
                [JsonProperty(LanguageEnglish ? "Color of buttons in cupboard" : "Цвет кнопок в шкафу")] public string ButtonColorCup;
                [JsonProperty(LanguageEnglish ? "Button text color in cupboard" : "Цвет текста кнопок в шкафу")] public string TButtonColorCup;
                [JsonProperty(LanguageEnglish ? "Hammer button color - On" : "Цвет кнопки вкл киянки")] public string ButtonColorHOn;
                [JsonProperty(LanguageEnglish ? "Hammer button text color - On" : "Цвет текста кнопки вкл киянки")] public string TButtonColorHOn;
                [JsonProperty(LanguageEnglish ? "Hammer button color - Off" : "Цвет кнопки выкл киянки")] public string ButtonColorHOff;
                [JsonProperty(LanguageEnglish ? "Hammer button text color - Off" : "Цвет текста кнопки выкл киянки")] public string TButtonColorHOff;
                [JsonProperty(LanguageEnglish ? "AnchorMin - button 1" : "AnchorMin - кнопка 1")] public string AnchorMinB1 = "0.5 0";
                [JsonProperty(LanguageEnglish ? "AnchorMax - button 1" : "AnchorMax - кнопка 1")] public string AnchorMaxB1 = "0.5 0";
                [JsonProperty(LanguageEnglish ? "OffsetMin - button 1" : "OffsetMin - кнопка 1")] public string OffsetMinB1 = "395 585";
                [JsonProperty(LanguageEnglish ? "OffsetMax - button 1" : "OffsetMax - кнопка 1")] public string OffsetMaxB1 = "572.5 605";
                [JsonProperty(LanguageEnglish ? "AnchorMin - button 2" : "AnchorMin - кнопка 2")] public string AnchorMinB2 = "0.5 0";
                [JsonProperty(LanguageEnglish ? "AnchorMax - button 2" : "AnchorMax - кнопка 2")] public string AnchorMaxB2 = "0.5 0";
                [JsonProperty(LanguageEnglish ? "OffsetMin - button 2" : "OffsetMin - кнопка 2")] public string OffsetMinB2 = "395 610";
                [JsonProperty(LanguageEnglish ? "OffsetMax - button 2" : "OffsetMax - кнопка 2")] public string OffsetMaxB2 = "572.5 630";
            }

            [JsonProperty(LanguageEnglish ? "General setting" : "Общие настройки")]
            public GeneralSetting Setting = new GeneralSetting();
            internal class GeneralSetting
            {
                [JsonProperty(LanguageEnglish ? "Use effects when painting home" : "Использовать эффекты при покраске дома")] public bool UseEffect;
                [JsonProperty(LanguageEnglish ? "Skins for buildings" : "Скины на постройки")] public Dictionary<string, Dictionary<ulong, string>> BuildingSkins;
                [JsonProperty(LanguageEnglish ? "Only the owner of the cupboard can start painting the house" : "Только владелец шкафа может начать покраску дома")] public bool HouseOwner;
                [JsonProperty(LanguageEnglish ? "Use effects when painting items/constructions in the house" : "Использовать эффекты при покраске предметов/конструкций в доме")] public bool UseEffectE;
                [JsonProperty(LanguageEnglish ? "Enable painting of items/constructions in the house using player skins from the XSkinMenu plugin" : "Включить покраску предметов/конструкций в доме используя скины игроков из плагина XSkinMenu")] public bool UseXSM;
                [JsonProperty(LanguageEnglish ? "Use effects when painting building blocks by hitting a hammer" : "Использовать эффекты при покраске строительного блоки ударом киянки")] public bool UseEffectH;
            }
        }

        private object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (player == null || block == null) return null;

            if (permission.UserHasPermission(player.UserIDString, permUse))
            {
                string g = grade.ToString();

                if (StoredData[player.userID].BuildingSkins.ContainsKey(g))
                {
                    ulong skinID = StoredData[player.userID].BuildingSkins[g];

                    if (skinID != 0 && player.blueprints.steamInventory.HasItem((int)skinID)) return null;
                    if (block.grade == grade || block.SecondsSinceAttacked <= 30f) return false;

                    NextTick(() =>
                    {
                        block.ChangeGradeAndSkin(block.grade, skinID, false, true);
                        if (g == "Metal" && skinID == 10221) block.SetCustomColour(player.LastBlockColourChangeId);
                    });
                }
            }

            return null;
        }

        private void ButtonGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            bool hchange = StoredData[player.userID].HChange;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-160 -195", OffsetMax = "160 -167.5" },
                Button = { Color = hchange ? config.GUI.ButtonColorHOn : config.GUI.ButtonColorHOff, Command = "skin_build hammer" },
                Text = { Text = "" }
            }, ".BSkinGUI", ".BHChange", ".BHChange");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = lang.GetMessage(hchange ? "HAMMER_ON" : "HAMMER_OFF", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf", Color = hchange ? config.GUI.TButtonColorHOn : config.GUI.TButtonColorHOff }
            }, ".BHChange");

            CuiHelper.AddUi(player, container);
        }

        private void ColourGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            int x = 0, y = 0;
            uint colourID = player.LastBlockColourChangeId;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -85", OffsetMax = "0 -5" },
                Image = { Color = config.GUI.BColor1, Material = "assets/icons/greyout.mat" }
            }, ".BgBSkinGUI", ".BgBColourGUI", ".BgBColourGUI");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = config.GUI.BColor2 }
            }, ".BgBColourGUI", ".BColourGUI");

            foreach (var colour in _colours)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-160 + (x * 32.5)} {2.5 - (y * 32.5)}", OffsetMax = $"{-132.5 + (x * 32.5)} {30 - (y * 32.5)}" },
                    Image = { Color = colourID == colour.Key ? config.GUI.ActiveBlockColor : "0 0 0 0.5", Material = "assets/icons/greyout.mat" }
                }, ".BColourGUI", ".BColour");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = colour.Value, Command = $"skin_build_colour {colour.Key}" },
                    Text = { Text = "" }
                }, ".BColour");

                x++;

                if (x == 10)
                {
                    x = 0;
                    y++;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        private const string permEnt = "xbuildingskinmenu.entity";


        private void OnServerInitialized()
        {
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);

            if (!ImageLibrary)
            {
                PrintError(LanguageEnglish ? "You don't have the plugin installed - ImageLibrary!" : "У вас не установлен плагин - ImageLibrary!");
                Interface.Oxide.UnloadPlugin(Name);

                return;
            }

            if (config.Setting.UseXSM && !XSkinMenu)
            {
                PrintError(LanguageEnglish ? "You don't have the plugin installed - XSkinMenu!" : "У вас не установлен плагин - XSkinMenu!");
                Interface.Oxide.UnloadPlugin(Name);

                return;
            }

            if (config.Setting.UseXSM && XSkinMenu)
                if (new Version(XSkinMenu.Version.ToString()) < new Version("1.1.14"))
                {
                    PrintError(LanguageEnglish ? "XSkinMenu plugin version must be 1.1.14 and higher!" : "Версия плагина XSkinMenu должна быть 1.1.14 и выше!");
                    Interface.Oxide.UnloadPlugin(Name);

                    return;
                }

            foreach (var building in config.Setting.BuildingSkins)
                foreach (var skinID in building.Value)
                    ImageLibrary.Call("AddImage", skinID.Value, $"B_IMG_{building.Key + skinID.Key}");

            InitializeLang();

            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permEnt, this);

            foreach (var item in ItemManager.GetItemDefinitions())
            {
                var prefab = item.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(prefab)) continue;

                var shortPrefabName = Utility.GetFileNameWithoutExtension(prefab);
                if (!string.IsNullOrEmpty(shortPrefabName) && !_shortnamesEntity.ContainsKey(shortPrefabName))
                    _shortnamesEntity.Add(shortPrefabName, item.shortname);
            }
        }



        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "COOL SERVER SKINS MENU",
                ["NOPERM"] = "No permissions!",
                ["SETSKIN"] = "PAINT THE HOUSE",
                ["SETSKIN_ENT"] = "PAINTING ITEMS IN THE HOUSE",
                ["PAINT_ACTIVE"] = "House <color=#fc9090>#{0}</color> is already being painted!",
                ["PAINT_ENT_ACTIVE"] = "Items/Constructions in the house <color=#fc9090>#{0}</color> is already being painted!",
                ["PAINT_START"] = "House <color=#9bfc90>#{0}</color> painting started.",
                ["PAINT_ENT_START"] = "Painting of items/constructions in house <color=#9bfc90>#{0}</color> has started.",
                ["PAINT_END"] = "House <color=#9bfc90>#{0}</color> painting completed.\n<size=12>Painted <color=#9bfc90>{1}</color> building blocks.</size>",
                ["PAINT_ENT_END"] = "Painting of items/constructions in house <color=#9bfc90>#{0}</color> completed.\n<size=12>Painted <color=#9bfc90>{1}</color> items/constructions.</size>",
                ["HAMMER_ON"] = "PAINTING BUILDING BLOCKS WITH A HAMMER - ON",
                ["HAMMER_OFF"] = "PAINTING BUILDING BLOCKS WITH A HAMMER - OFF"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "МЕНЮ СКИНОВ КРУТОГО СЕРВЕРА",
                ["NOPERM"] = "Недостаточно прав!",
                ["SETSKIN"] = "ПОКРАСИТЬ ДОМ",
                ["SETSKIN_ENT"] = "ПОКРАСИТЬ ПРЕДМЕТЫ В ДОМЕ",
                ["PAINT_ACTIVE"] = "Дом <color=#fc9090>#{0}</color> уже красят!",
                ["PAINT_ENT_ACTIVE"] = "Предметы/Конструкции в доме <color=#fc9090>#{0}</color> уже красят!",
                ["PAINT_START"] = "Покраска дома <color=#9bfc90>#{0}</color> началась.",
                ["PAINT_ENT_START"] = "Покраска предметов/конструкций в доме <color=#9bfc90>#{0}</color> началась.",
                ["PAINT_END"] = "Покраска дома <color=#9bfc90>#{0}</color> завершена.\n<size=12>Покрашено <color=#9bfc90>{1}</color> строительных блоков.</size>",
                ["PAINT_ENT_END"] = "Покраска предметов/конструкций в доме <color=#9bfc90>#{0}</color> завершена.\n<size=12>Покрашено <color=#9bfc90>{1}</color> предметов/конструкций.</size>",
                ["HAMMER_ON"] = "ПОКРАСКА СТРОИТЕЛЬНЫХ БЛОКОВ КИЯНКОЙ - ВКЛ",
                ["HAMMER_OFF"] = "ПОКРАСКА СТРОИТЕЛЬНЫХ БЛОКОВ КИЯНКОЙ - ВЫКЛ"
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "МЕНЮ СКІНІВ КРУТОГО СЕРВЕРУ",
                ["NOPERM"] = "Недостатньо прав!",
                ["SETSKIN"] = "ПОФАРБУВАТИ БУДИНОК",
                ["SETSKIN_ENT"] = "ПОФАРБУВАТИ ПРЕДМЕТИ У БУДИНКУ",
                ["PAINT_ACTIVE"] = "Будинок <color=#fc9090>#{0}</color> вже фарбують!",
                ["PAINT_ENT_ACTIVE"] = "Предмети/Конструкції у будинку <color=#fc9090>#{0}</color> вже фарбують!",
                ["PAINT_START"] = "Фарбування будинку <color=#9bfc90>#{0}</color> розпочалося.",
                ["PAINT_ENT_START"] = "Фарбування предметів/конструкцій у будинку <color=#9bfc90>#{0}</color> розпочалося.",
                ["PAINT_END"] = "Фарбування будинку <color=#9bfc90>#{0}</color> завершено.\n<size=12>Пофарбовано <color=#9bfc90>{1}</color> будівельних блоків.</size>",
                ["PAINT_ENT_END"] = "Фарбування предметів/конструкцій у будинку <color=#9bfc90>#{0}</color> завершено.\n<size=12>Пофарбовано <color=#9bfc90>{1}</color> предметів/конструкцій.</size>",
                ["HAMMER_ON"] = "ФАРБУВАННЯ БУДІВЕЛЬНИХ БЛОКІВ КИЯНКОЮ - УВІМК",
                ["HAMMER_OFF"] = "ФАРБУВАННЯ БУДІВЕЛЬНИХ БЛОКІВ КИЯНКОЮ - ВИМК"
            }, this, "uk");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "NOMBRE DEL SERVIDOR",
                ["NOPERM"] = "¡No tienes permisos!",
                ["SETSKIN"] = "PINTA LA CASA",
                ["SETSKIN_ENT"] = "ARTÍCULOS DE PINTURA EN CASA",
                ["PAINT_ACTIVE"] = "¡La casa <color=#fc9090>#{0}</color> ya se está pintando!",
                ["PAINT_ENT_ACTIVE"] = "¡Ya se están pintando los artículos/construcciones de la casa <color=#fc9090>#{0}</color>!",
                ["PAINT_START"] = "Comenzó la pintura de la casa <color=#9bfc90>#{0}</color>.",
                ["PAINT_ENT_START"] = "Se ha iniciado la pintura de artículos/construcciones en la casa <color=#9bfc90>#{0}</color>.",
                ["PAINT_END"] = "Pintura de la casa <color=#9bfc90>#{0}</color> completada.\n<size=12>Pintado <color=#9bfc90>{1}</color> bloques de construcción.</size>",
                ["PAINT_ENT_END"] = "Pintura de artículos/construcciones en la casa <color=#9bfc90>#{0}</color> completada.\n<size=12>Pintado <color=#9bfc90>{1}</color> artículos/construcciones.</size>",
                ["HAMMER_ON"] = "PINTAR BLOQUES DE CONSTRUCCIÓN CON UN MARTILLO - ON",
                ["HAMMER_OFF"] = "PINTAR BLOQUES DE CONSTRUCCIÓN CON UN MARTILLO - OFF"
            }, this, "es-ES");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<BuildingSkinConfig>();
            }
            catch
            {
                PrintWarning(LanguageEnglish ? "Configuration read error! Creating a default configuration!" : "Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
                LoadDefaultConfig();
            }

            SaveConfig();
        }
        protected override void LoadDefaultConfig() => config = BuildingSkinConfig.GetNewConfiguration();


        private IEnumerator SetBuildingSkins(BasePlayer player, BaseEntity entity)
        {
            yield return CoroutineEx.waitForSeconds(2);

            int count = 0;
            uint colourID = player.LastBlockColourChangeId;
            ulong netID = entity.net.ID.Value;
            var player_data = StoredData[player.userID].BuildingSkins.ToDictionary(x => x.Key, x => x.Value);

            SendReply(player, string.Format(lang.GetMessage("PAINT_START", this, player.UserIDString), netID));

            foreach (var block in entity.GetBuildingPrivilege().GetBuilding().buildingBlocks)
            {
                string g = block.grade.ToString();

                if (player_data.ContainsKey(g))
                {
                    ulong skinID = player_data[g];

                    if (block.skinID != skinID)
                    {
                        block.ChangeGradeAndSkin(block.grade, skinID, false, true);
                        if (g == "Metal" && skinID == 10221) block.SetCustomColour(colourID);

                        if (config.Setting.UseEffect)
                        {
                            Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", block.transform.localPosition);
                            Effect.server.Run("assets/prefabs/tools/spraycan/reskineffect.prefab", block.transform.localPosition);
                        }

                        count++;

                        yield return CoroutineEx.waitForSeconds(0.15f);
                    }
                    else
                        yield return CoroutineEx.waitForSeconds(0.05f);
                }
                else
                    yield return CoroutineEx.waitForSeconds(0.05f);
            }

            yield return CoroutineEx.waitForSeconds(1);

            SendReply(player, string.Format(lang.GetMessage("PAINT_END", this, player.UserIDString), netID, count));

            _coroutine_list.Remove(netID);
            yield return 0;
        }

        private Dictionary<ulong, Data> StoredData = new Dictionary<ulong, Data>();
        private Dictionary<ulong, Coroutine> _coroutine_listEnt = new Dictionary<ulong, Coroutine>();

        private void OnLootEntity(BasePlayer player, BuildingPrivlidge entity)
        {
            if (config.Setting.HouseOwner && player.userID != entity.OwnerID) return;

            if (permission.UserHasPermission(player.UserIDString, permUse) && !_coroutine_list.ContainsKey(entity.net.ID.Value))
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = config.GUI.AnchorMinB1, AnchorMax = config.GUI.AnchorMaxB1, OffsetMin = config.GUI.OffsetMinB1, OffsetMax = config.GUI.OffsetMaxB1 },
                    Button = { Color = config.GUI.ButtonColorCup, Command = "set_skin_building block", Close = ".SetSkinBUTTON" },
                    Text = { Text = "" }
                }, "Overlay", ".SetSkinBUTTON", ".SetSkinBUTTON");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("SETSKIN", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 10, Color = config.GUI.TButtonColorCup }
                }, ".SetSkinBUTTON");

                CuiHelper.AddUi(player, container);
            }

            if (config.Setting.UseXSM && permission.UserHasPermission(player.UserIDString, permEnt) && !_coroutine_listEnt.ContainsKey(entity.net.ID.Value))
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = config.GUI.AnchorMinB2, AnchorMax = config.GUI.AnchorMaxB2, OffsetMin = config.GUI.OffsetMinB2, OffsetMax = config.GUI.OffsetMaxB2 },
                    Button = { Color = config.GUI.ButtonColorCup, Command = "set_skin_building entity", Close = ".SetSkinEntBUTTON" },
                    Text = { Text = "" }
                }, "Overlay", ".SetSkinEntBUTTON", ".SetSkinEntBUTTON");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("SETSKIN_ENT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 10, Color = config.GUI.TButtonColorCup }
                }, ".SetSkinEntBUTTON");

                CuiHelper.AddUi(player, container);
            }
        }

        [ConsoleCommand("skin_build_colour")]
        private void ccmdSetColour(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            if (player == null || args.Args == null || !permission.UserHasPermission(player.UserIDString, permUse)) return;

            if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;

            Cooldowns[player] = DateTime.Now.AddSeconds(1.0f);

            if (args.Args.Length >= 1)
            {
                uint colourID;
                uint.TryParse(args.Args[0], out colourID);

                if (_colours.ContainsKey(colourID))
                {
                    player.LastBlockColourChangeId = colourID;
                    StoredData[player.userID].ColourID = colourID;

                    ColourGUI(player);
                    EffectNetwork.Send(new Effect("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
                }
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            LoadData(player);
        }

        private void LoadData(BasePlayer player)
        {
            var Data = Interface.Oxide.DataFileSystem.ReadObject<Data>($"XDataSystem/XBuildingSkinMenu/{player.userID}");

            StoredData[player.userID] = Data ?? new Data();

            if (StoredData[player.userID].BuildingSkins == null || StoredData[player.userID].BuildingSkins.Count == 0)
                StoredData[player.userID].BuildingSkins = new Dictionary<string, ulong> { ["Wood"] = 0, ["Stone"] = 0, ["Metal"] = 0, ["TopTier"] = 0 };

            player.LastBlockColourChangeId = StoredData[player.userID].ColourID;
        }



        private BuildingSkinConfig config;



        private void GUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            bool xsmLoad = XSkinMenu;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -147.5", OffsetMax = "170 262.5" },
                Image = { Color = config.GUI.BColor1, Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".BgBSkinGUI", ".BgBSkinGUI");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = config.GUI.BColor2 }
            }, ".BgBSkinGUI", ".BSkinGUI");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-32.5 -32.5", OffsetMax = "-5 -5" },
                Button = { Color = config.GUI.IconColor, Sprite = "assets/icons/close.png", Close = ".BgBSkinGUI" },
                Text = { Text = "" }
            }, ".BSkinGUI");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-42.5 -42.5", OffsetMax = "-37.5 0" },
                Image = { Color = config.GUI.BColor1, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".BSkinGUI");

            if (xsmLoad)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -32.5", OffsetMax = "32.5 -5" },
                    Button = { Color = config.GUI.IconColor, Sprite = "assets/icons/clothing.png", Command = "chat.say /skin", Close = ".BgBSkinGUI" },
                    Text = { Text = "" }
                }, ".BSkinGUI");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "37.5 -42.5", OffsetMax = "42.5 0" },
                    Image = { Color = config.GUI.BColor1, Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".BSkinGUI");
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = xsmLoad ? "-122.5 162.5" : "-165 162.5", OffsetMax = "122.5 200" },
                Text = { Text = lang.GetMessage("TITLE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = xsmLoad ? 13 : 15, Color = "1 1 1 0.750" }
            }, ".BSkinGUI");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 157.5", OffsetMax = "170 162.5" },
                Image = { Color = config.GUI.BColor1, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".BSkinGUI");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -162.5", OffsetMax = "170 -157.5" },
                Image = { Color = config.GUI.BColor1, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".BSkinGUI");

            CuiHelper.AddUi(player, container);

            ButtonGUI(player);
            BuildingSkinGUI(player);
            ColourGUI(player);
        }

        private Dictionary<ulong, Coroutine> _coroutine_list = new Dictionary<ulong, Coroutine>();


        private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            BuildingBlock block = info?.HitEntity as BuildingBlock;

            if (player == null || block == null || !player.CanBuild() || !permission.UserHasPermission(player.UserIDString, permUse)) return;
            if (config.Setting.HouseOwner && block.GetBuildingPrivilege()?.OwnerID != player.userID) return;

            var player_data = StoredData[player.userID];

            if (player_data.HChange)
            {
                string g = block.grade.ToString();

                if (player_data.BuildingSkins.ContainsKey(g))
                {
                    ulong skinID = player_data.BuildingSkins[g];

                    if (skinID != 0 && player.blueprints.steamInventory.HasItem((int)skinID)) return;

                    if (block.skinID != skinID)
                    {
                        block.ChangeGradeAndSkin(block.grade, skinID, false, true);
                        if (g == "Metal" && skinID == 10221) block.SetCustomColour(player.LastBlockColourChangeId);

                        if (config.Setting.UseEffectH)
                        {
                            Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", block.transform.localPosition);
                            Effect.server.Run("assets/prefabs/tools/spraycan/reskineffect.prefab", block.transform.localPosition);
                        }
                    }
                }
            }
        }

    }
}
