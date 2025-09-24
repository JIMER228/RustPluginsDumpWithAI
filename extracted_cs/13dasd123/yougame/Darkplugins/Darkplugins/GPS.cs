// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("GPS", "https://discord.gg/dNGbxafuJn", "0.0.1")]
    [Description("Реализация GPS трекера, в качестве уникального предмета")]
    public class GPS : RustPlugin
    {
        #region eNums

        private enum Mode
        {
            Players,
            Resources,
            Loots,
        }        

        #endregion
        
        #region Class

        private class Configuration
        {
            [JsonProperty("Основной цвет в чате")]
            public string PrimaryColor = "#4286f4";
            [JsonProperty("Цвет краёв монитора")]
            public string BGRoundsColor = "#333333FF";
            [JsonProperty("Уровень прозрачности экрана")]
            public float AlphaCenter = 0.9f;
            [JsonProperty("Скорость поломки локатора")]
            public float SpeedDestroy = 4;
        }

        private class GPSPlayer : MonoBehaviour
        {
            public BasePlayer Player;
            public Mode CurrentMode = Mode.Players;

            public List<BaseEntity> CurrentTargets = new List<BaseEntity>();
            public Coroutine CurrentProcess;
            public float LastUpdate = 4f;
            public bool IsReady;
            
            public void Awake()
            {
                Player = GetComponent<BasePlayer>();
                if (LastModes.ContainsKey(Player.userID))
                    CurrentMode = LastModes[Player.userID];
                
                Initialize();
            }

            private const string BGLayer = "UI_BG_LayerGPS";

            public void RefreshButtons()
            {
                CuiHelper.DestroyUi(Player, Layer + $".BTN.Players");
                CuiHelper.DestroyUi(Player, Layer + $".BTN.Resources");
                CuiHelper.DestroyUi(Player, Layer + $".BTN.Loot");

                CuiHelper.AddUi(Player, new CuiElementContainer
                {
                    {
                        new CuiButton
                        {
                            RectTransform   = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-100 -17.5", OffsetMax = "-10 2.5" },
                            Button = { Color = HexToRustFormat(CurrentMode == Mode.Players ? "#458242" : "#824242"), Command = "GPS_Handler switch 0"},
                            Text = { Text = "ИГРОКИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                        }, BGLayer, Layer + $".BTN.Players"
                    },
                    {
                        new CuiButton
                        {
                            RectTransform   = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-200 -17.5", OffsetMax = "-110 2.5" },
                            Button = { Color = HexToRustFormat(CurrentMode == Mode.Resources ? "#458242" : "#824242"), Command = "GPS_Handler switch 1" },
                            Text = { Text = "РЕСУРСЫ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                        }, BGLayer, Layer + $".BTN.Resources"
                    },
                    {
                        new CuiButton
                        {
                            RectTransform   = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-300 -17.5", OffsetMax = "-210 2.5" },
                            Button = { Color = HexToRustFormat(CurrentMode == Mode.Loots ? "#458242" : "#824242"), Command = "GPS_Handler switch 2" },
                            Text = { Text = "ЛУТ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                        }, BGLayer, Layer + $".BTN.Loot"
                    },
                });
            }
            
            public void Initialize()
            {
                IsReady = true;
                
                CuiHelper.AddUi(Player, new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-430 -280", OffsetMax = "430 280" },
                            Image = { Color = $"0 0 0 {Settings.AlphaCenter}", Material = "assets/content/ui/uibackgroundblur.mat"}
                        }, "Overlay", BGLayer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "-25 -25", OffsetMax = "10 25" },
                            Button = { Color = HexToRustFormat(Settings.BGRoundsColor), Material = ""},
                            Text = { Text = ""}
                        }, BGLayer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-25 -10", OffsetMax = "25 25" },
                            Button = { Color = HexToRustFormat(Settings.BGRoundsColor), Material = "" },
                            Text = { Text = ""}
                        }, BGLayer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "-10 -25", OffsetMax = "25 25" },
                            Button = { Color = HexToRustFormat(Settings.BGRoundsColor), Material = "" },
                            Text = { Text = ""}
                        }, BGLayer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "-25 -25", OffsetMax = "25 10" },
                            Button = { Color = HexToRustFormat(Settings.BGRoundsColor), Material = "" },
                            Text = { Text = ""}
                        }, BGLayer
                    },
                    {
                        new CuiButton
                        {
                            FadeOut = 0.5f,
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "40 -20.5", OffsetMax = "300 5.5" },
                            Button = { FadeIn = 0.5f, Color = "0 0 0 0" },
                            Text = { Text = "Ориентируйтесь по сторонам света, карта не будет крутиться\n" +
                                            "в след за вашим персонажем!", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 10, Color = "1 1 1 0.6"}
                        }, BGLayer, "Help"
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-256 -256", OffsetMax = "256 256" },
                            Button = { Color = "0 0 0 0" },
                            Text = { Text = ""}
                        }, BGLayer, Layer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-25 -25", OffsetMax = "25 0" },
                            Button = { Color = "0 0 0 0", Material = ""},
                            Text = { Text = "N", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter}
                        }, Layer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-30 -25", OffsetMax = "25 25" },
                            Button = { Color = "0 0 0 0", Material = ""},
                            Text = { Text = "W", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter}
                        }, Layer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-25 -25", OffsetMax = "30 25" },
                            Button = { Color = "0 0 0 0", Material = ""},
                            Text = { Text = "E", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter}
                        }, Layer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-25 0", OffsetMax = "25 25" },
                            Button = { Color = "0 0 0 0", Material = ""},
                            Text = { Text = "S", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter}
                        }, Layer
                    },
                    {
                        new CuiElement
                        {
                            Parent = Layer,
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) instance.ImageLibrary.Call("GetImage", "GPSCompas{DarkPluginID}") },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                            }
                        }
                    }
                });
                
                RefreshButtons();
                InvokeRepeating(nameof(RepeatFlash), 0, 2);
            }

            private void RepeatFlash()
            {
                CuiHelper.AddUi(Player, new CuiElementContainer
                {
                    {
                        new CuiButton
                        {
                            FadeOut = 0.5f,
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 -17.5", OffsetMax = "30 2.5" },
                            Button = { FadeIn = 0.5f, Color = HexToRustFormat("#458242") },
                            Text = { Text = "" }
                        }, BGLayer, "Flash"
                    }
                });

                Invoke(nameof(DelayRemoveFlash), 1);
            }

            private void DelayRemoveFlash()
            {
                CuiHelper.DestroyUi(Player, "Flash");
            }
            
            private Vector2 ToScreenCord(Vector3 pos)
            {
                float x = (pos.x + (int) 300 * 0.5f) / (int) 300;
                float y = (pos.z + (int) 300 * 0.5f) / (int) 300;
                
                return new Vector2(x, y);
            }

            public void Update()
            {
                LastUpdate += Time.deltaTime;
                if (LastUpdate > 5)
                {
                    Refresh();
                    LastUpdate = 0f;
                }
            }

            public void RefreshUI()
            {
                Player.GetActiveItem().LoseCondition(5);
                
                CuiElementContainer container = new CuiElementContainer();
                foreach (var check in CurrentTargets.Select((i,t) => new { A = i, B = t}))
                {
                    Vector3 localPosition = Player.transform.InverseTransformPoint(check.A.transform.position);
                    Vector2 anchorPos = ToScreenCord(localPosition);


                    var resDis = check.A.GetComponent<ResourceDispenser>();
                    if (CurrentMode == Mode.Resources && resDis != null)
                    {
                        if (resDis.gatherType != ResourceDispenser.GatherType.Tree)
                        {
                            float resLerp = Mathf.Lerp(10, 25, Vector3.Distance(check.A.transform.position, Player.transform.position) / 150);
                            container.Add(new CuiElement
                            {
                                FadeOut = 2f,
                                Name = Layer + $".Layer.{check.B}",
                                Parent = Layer,
                                Components =
                                {
                                    new CuiRawImageComponent { FadeIn = 0.5f, Png = (string) instance.ImageLibrary.Call("GetImage", check.A.GetComponent<ResourceDispenser>().containedItems.First().itemDef.shortname) },
                                    new CuiRectTransformComponent { AnchorMin = $"{anchorPos.x} {anchorPos.y}", AnchorMax = $"{anchorPos.x} {anchorPos.y}", OffsetMin = $"-{resLerp} -{resLerp}", OffsetMax = $"{resLerp} {resLerp}" }
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiButton
                                {
                                    FadeOut = 2f,
                                    RectTransform = { AnchorMin = $"{anchorPos.x} {anchorPos.y}", AnchorMax = $"{anchorPos.x} {anchorPos.y}", OffsetMin = "-1 -1", OffsetMax = "1 1" },
                                    Button = { FadeIn = 1f, Color = "1 1 1 1" },
                                    Text = { Text = ""}
                                }, Layer, Layer + $".Layer.{check.B}");
                        }
                    }
                    else
                    {
                        string color = "1 1 1 1";
                        if (CurrentMode == Mode.Players)
                            color = Player.currentTeam == 0 ? "1 0 0 1" : RelationshipManager._instance.FindTeam(Player.currentTeam).members.Contains(check.A.GetComponent<BasePlayer>().userID) ? "0 1 0 1" : "1 1 1 1"; 
                        container.Add(new CuiButton
                        {
                            FadeOut = 2f,
                            RectTransform = { AnchorMin = $"{anchorPos.x} {anchorPos.y}", AnchorMax = $"{anchorPos.x} {anchorPos.y}", OffsetMin = "-3 -3", OffsetMax = "3 3" },
                            Button = { FadeIn = 1f, Color = color },
                            Text = { Text = ""}
                        }, Layer, Layer + $".Layer.{check.B}");
                    }
                }
                
                this.Invoke(nameof(DelayRemove), 3);

                /*CuiHelper.DestroyUi(Player, Layer + ".ME");
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-3 -3", OffsetMax = "3 3"},
                    Button = {Color = "1 0 0 1"},
                    Text = {Text = ""}
                }, Layer, Layer + ".ME");*/
                CuiHelper.AddUi(Player, container);
            }

            private const string Layer = "UI_GPS_Layer";
            private void OnDestroy()
            {
                if (!LastModes.ContainsKey(Player.userID))
                    LastModes.Add(Player.userID, CurrentMode);

                LastModes[Player.userID] = CurrentMode;
                CuiHelper.DestroyUi(Player, BGLayer);
            }

            public void DelayRemove()
            {
                for (int i = 0; i < CurrentTargets.Count; i++)
                    CuiHelper.DestroyUi(Player, Layer + $".Layer.{i}");
            }

            public void Refresh()
            {
                if (!IsReady) return;

                for (int i = 0; i < CurrentTargets.Count; i++)
                    CuiHelper.DestroyUi(Player, Layer + $".Layer.{i}");
                
                CurrentTargets.Clear();
                var tempList = new List<BaseEntity>();
                Vis.Entities(Player.transform.position, 150, tempList);
                
                switch (CurrentMode)
                {
                    case Mode.Players:
                    {
                        tempList.Where(p => p is BasePlayer && p != Player).ToList().ForEach(p => CurrentTargets.Add(p));
                        break;
                    }
                    case Mode.Resources:
                    {
                        tempList.Where(p => p is ResourceEntity && !(p is TreeEntity) && !p.PrefabName.Contains("tree")).ToList().ForEach(p => CurrentTargets.Add(p));
                        break;
                    }
                    case Mode.Loots:
                    {
                        tempList.Where(p => p is LootContainer).ToList().ForEach(p => CurrentTargets.Add(p));
                        break;
                    }
                }
                
                RefreshUI();
            }
        }

        private static class GPSItem
        {
            private static string DisplayName = "GPS-Локатор";
            private static ulong SkinID = 1627796062;

            public static Item CreateItem()
            {
                Item item = ItemManager.CreateByPartialName("fuse", 1);
                item.skin = SkinID;
                item.name = DisplayName;

                return item;
            }

            public static void GiveToPlayer(BasePlayer player) => CreateItem().MoveToContainer(player.inventory.containerMain);
            public static bool IsThis(Item item) => item.skin == SkinID;
        }
        
        
        
        #endregion

        #region Variables

        private static GPS instance;
        [PluginReference]
        private Plugin ImageLibrary;
        private static Dictionary<ulong, Mode> LastModes = new Dictionary<ulong, Mode>();
        private static Configuration Settings = new Configuration();

        #endregion

        #region Hooks
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => Settings = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(Settings);

        private void OnServerInitialized()
        {
            instance = this;
            ImageLibrary.Call("AddImage", "https://i.imgur.com/cn9fhZj.png", "GPSCompas{DarkPluginID}");
        } 
        private void Unload() => UnityEngine.Object.FindObjectsOfType<GPSPlayer>().ToList().ForEach(UnityEngine.Object.Destroy);

        private void OnPlayerDie(BasePlayer player)
        {
            if (player.GetComponent<GPSPlayer>() != null)
                UnityEngine.Object.Destroy(player.GetComponent<GPSPlayer>());
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.GetComponent<GPSPlayer>() != null)
                UnityEngine.Object.Destroy(player.GetComponent<GPSPlayer>());
        }
        private void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && GPSItem.IsThis(newItem))
            {
                if (player.GetComponent<GPSPlayer>() == null)
                    player.gameObject.AddComponent<GPSPlayer>();
                else if (player.GetComponent<GPSPlayer>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<GPSPlayer>());
            }
            else
            {
                if (player.GetComponent<GPSPlayer>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<GPSPlayer>());
            }
        }

        #endregion

        #region Commands

        [ChatCommand("gps.admin")]
        private void CmdChatAdminGiveGPS(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            
            GPSItem.GiveToPlayer(player);
            SendMessage(player, "Вам успешно выдан <<GPS>> помощник!");
        }

        [ChatCommand("gps.switch")]
        private void CmdChatAdminActivate(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            if (player.GetComponent<GPSPlayer>() == null)
            {
                player.gameObject.AddComponent<GPSPlayer>();
            }
            else
            {
                UnityEngine.Object.Destroy(player.GetComponent<GPSPlayer>());
            }
        }

        [ConsoleCommand("GPS_Handler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs(1)) return;

            if (player.GetComponent<GPSPlayer>() == null) return;
            
            switch (args.Args[0].ToLower())
            {
                case "switch":
                {
                    int newModeId = -1;
                    if (!args.HasArgs(2) || !int.TryParse(args.Args[1], out newModeId)) return;
                    
                    Mode newMode = (Mode) newModeId;
                    player.GetComponent<GPSPlayer>().CurrentMode = newMode;
                    player.GetComponent<GPSPlayer>().RefreshButtons();
                    
                    SendMessage(player, $"Вы успешно переключили режим <<GPS>>");
                    break;
                }
            }
        }

        [ConsoleCommand("GPS_Admin")]
        private void CmdConsoleAdmin(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            ulong targetId;
            if (!ulong.TryParse(args.Args[0], out targetId)) return;
            BasePlayer target = BasePlayer.FindByID(targetId);

            if (target == null || !target.IsConnected) return;
            GPSItem.GiveToPlayer(target);
        }

        #endregion

        #region Utils

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

        private static string ParseString(string text)
        {
            while (text.Contains("<<") || text.Contains(">>"))
                text = text.Replace("<<", $"<color={Settings.PrimaryColor}>").Replace(">>", "</color>");
            
            return text;
        } 
        private static void SendMessage(BasePlayer player, string text) => player.SendConsoleCommand("chat.add", 76561198121100397, $"[GPS] {ParseString(text)}");

        #endregion
    }
}