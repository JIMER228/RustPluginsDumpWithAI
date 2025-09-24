using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SBagsManager", "SkiTles", "0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Менеджер спальников")]
    class SBagsManager : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Vars
        [PluginReference]
        Plugin RustMap;
        private Dictionary<BasePlayer, List<SleepingBag>> opens = new Dictionary<BasePlayer, List<SleepingBag>>();
        private ImageCache _imageAssets;
        private GameObject _hitObject;
        #endregion

        #region Config
        private static ConfigFile config;
        private class ConfigFile
        {
            [JsonProperty(PropertyName = "Использовать изображение карты RustMap (необходима дороботка плагина)")]
            public bool rustmapimage { get; set; }

            [JsonProperty(PropertyName = "Имя файла изображения карты")]
            public string mapfilename { get; set; }

            [JsonProperty(PropertyName = "Имя файла иконки спальника")]
            public string iconfilename { get; set; }

            [JsonProperty(PropertyName = "Размер иконки спальника")]
            public float iconsize { get; set; }

            [JsonProperty(PropertyName = "Размер карты")]
            public float mapSize { get; set; }

            [JsonProperty(PropertyName = "Использование привилегии")]
            public bool permreq { get; set; }

            [JsonProperty(PropertyName = "Привилегия")]
            public string perm { get; set; }

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    rustmapimage = false,
                    mapfilename = "/rustmap/icons/map.jpg",
                    iconfilename = "/SBagsManager/sbag.png",
                    iconsize = 0.03f,
                    mapSize = 0.5f,
                    permreq = false,
                    perm = "sbagsmanager.use"
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = ConfigFile.DefaultConfig();
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch { Regenerate(); }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private void Regenerate()
        {
            PrintWarning($"Конфигурационный файл 'oxide/config/{Name}.json' поврежден, создается новый...");
            LoadDefaultConfig();
        }
        #endregion

        #region OxideHooks
        void Loaded()
        {
            LoadMessages();
        }
        void Unload()
        {
            foreach (var pl in opens.Keys) CuiHelper.DestroyUi(pl, "SBagsUI");
            UnityEngine.Object.Destroy(_hitObject);
        }
        void OnServerInitialized()
        {
            CacheImage();
            if (config.permreq && !permission.PermissionExists(config.perm)) permission.RegisterPermission(config.perm, this);
        }
        #endregion

        #region Commands
        [ConsoleCommand("sbags")]
        private void SBagsCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (config.permreq && !player.IsAdmin && !permission.UserHasPermission(player.UserIDString, config.perm)) { PrintToChat(player, string.Format(GetMsg("noperm", player))); return; }
            if (arg.Args == null)
            {
                if (opens.ContainsKey(player)) { CuiHelper.DestroyUi(player, "SBagsUI"); opens.Remove(player); }
                else Bags(player);
            }
            else
            {
                if (!opens.ContainsKey(player)) return;
                if (arg.Args[0] == "remove" && arg.Args[1] != null)
                {
                    var b = opens[player].ElementAt(Convert.ToInt32(arg.Args[1]));
                    b.Kill();
                    opens.Remove(player);
                    CuiHelper.DestroyUi(player, "SBagsUI");
                    Bags(player);
                }
            }
        }

        [ChatCommand("sbags")]
        private void SBagsChCMD(BasePlayer player, string cmd, string[] args)
        {
            if (config.permreq && !player.IsAdmin && !permission.UserHasPermission(player.UserIDString, config.perm)) { PrintToChat(player, string.Format(GetMsg("noperm", player))); return; }
            if (opens.ContainsKey(player)) { CuiHelper.DestroyUi(player, "SBagsUI"); opens.Remove(player); }
            else Bags(player);
        }
        #endregion

        #region Main
        private void Bags(BasePlayer player)
        {
            if (opens.ContainsKey(player)) opens[player].Clear();
            var bags = SleepingBag.FindForPlayer(player.userID, true);
            if (bags.Length == 0) { PrintToChat(player, string.Format(GetMsg("nosbags", player))); return; }
            if (!opens.ContainsKey(player)) opens.Add(player, new List<SleepingBag>());
            string mimage = null;
            if (config.rustmapimage) mimage = (string)RustMap?.Call("APIGetMapImage") ?? null;
            else mimage = FetchImage("mapimage");

            var anchorMin = new Vector2(0.5f - config.mapSize * 0.5f, 0.5f - config.mapSize * 0.800f);
            var anchorMax = new Vector2(0.5f + config.mapSize * 0.5f, 0.5f + config.mapSize * 0.930f);

            CuiElementContainer container = new CuiElementContainer();
            container.Add(Panel("SBagsUI", "0 0 0 1", $"{anchorMin.x} {anchorMin.y}", $"{anchorMax.x} {anchorMax.y}", "Hud", true));
            if (mimage != null) container.Add(Image("SBagsUI", mimage, "0 0", "1 1"));
            int bcount = 0;
            foreach (var b in bags)
            {
                var anchors = ToAnchors(b.transform.position, config.iconsize);
                container.Add(Image("SBagsUI", FetchImage("sbagimage"), anchors[0], anchors[1]));
                var anchors2 = ToAnchors(b.transform.position, config.iconsize + 0.05f);
                container.Add(Text("SBagsUI", "1 1 1 1", b.niceName, TextAnchor.MiddleCenter, 9, anchors2[0], anchors2[1]));
                container.Add(Button($"bagr{bcount.ToString()}", "SBagsUI", $"sbags remove {bcount.ToString()}", "1 0 0 0", anchors[2], anchors[3]));
                container.Add(Text($"bagr{bcount.ToString()}", "1 0 0 1", "Удалить", TextAnchor.MiddleCenter, 9));
                opens[player].Add(b);
                bcount++;
            }
            container.Add(Panel("Title", "0 0 0 0", "0 0.95", "1 1", "SBagsUI"));
            container.Add(Text("Title", "1 1 1 1", "Менеджер спальников", TextAnchor.MiddleCenter, 16));
            container.Add(Button("Close", "Title", "sbags", "1 0 0 0.57", "0.9 0", "1 1"));
            container.Add(Text("Close", "1 1 1 1", "X", TextAnchor.MiddleCenter, 16));
            CuiHelper.AddUi(player, container);
        }

        string[] ToAnchors(Vector3 position, float size)
        {
            Vector2 center = new Vector2((position.x + (int)World.Size * 0.5f) / (int)World.Size, (position.z + (int)World.Size * 0.5f) / (int)World.Size);
            size *= 0.5f;
            return new[]
            {
                $"{center.x - size} {center.y - size}",
                $"{center.x + size} {center.y + size}",
                $"{center.x - 0.1} {center.y - size-0.04f}",
                $"{center.x + 0.1} {center.y - size+0.02}"
            };
        }
        #endregion

        #region GUIBuilder
        private CuiElement Panel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiImageComponent { Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            if (cursor)
            {
                Element.Components.Add(new CuiNeedsCursorComponent());
            }
            return Element;
        }
        private CuiElement Text(string parent, string color, string text, TextAnchor pos, int fsize, string anMin = "0 0", string anMax = "1 1", string fname = "robotocondensed-bold.ttf")
        {
            var Element = new CuiElement()
            {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent() { Color = color, Text = text, Align = pos, Font = fname, FontSize = fsize },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Button(string name, string parent, string command, string color, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiButtonComponent { Command = command, Color = color},
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Image(string parent, string name, string anMin, string anMax, string color = "1 1 1 1")
        {
            var Element = new CuiElement
            {
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent { Color = color, Png = name, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        #endregion

        #region ImageDownloader
        private void CacheImage()
        {
            _hitObject = new GameObject();
            _imageAssets = _hitObject.AddComponent<ImageCache>();
            _imageAssets.imageFiles.Clear();
            string dataDirectory = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar;
            if (!config.rustmapimage) _imageAssets.GetImage("mapimage", dataDirectory + config.mapfilename);
            _imageAssets.GetImage("sbagimage", dataDirectory + config.iconfilename);
            Download();
        }
        class ImageCache : MonoBehaviour
        {
            public Dictionary<string, string> imageFiles = new Dictionary<string, string>();
            private List<Queue> queued = new List<Queue>();
            class Queue
            {
                public string Url { get; set; }
                public string Name { get; set; }
            }
            public void OnDestroy()
            {
                foreach (var value in imageFiles.Values)
                {
                    FileStorage.server.RemoveEntityNum(uint.MaxValue, Convert.ToUInt32(value));
                }
            }
            public void GetImage(string name, string url)
            {
                queued.Add(new Queue
                {
                    Url = url,
                    Name = name
                });
            }
            IEnumerator WaitForRequest(Queue queue)
            {
                using (var www = new WWW(queue.Url))
                {
                    yield return www;

                    if (string.IsNullOrEmpty(www.error))
                        imageFiles.Add(queue.Name, FileStorage.server.Store(www.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    else
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, "oxide.unload SBagManager");
                    }
                }
            }
            public void Process()
            {
                foreach (var q in queued) StartCoroutine(WaitForRequest(q));
            }
        }
        private string FetchImage(string name)
        {
            string result;
            if (_imageAssets.imageFiles.TryGetValue(name, out result))
                return result;
            return string.Empty;
        }
        private void Download() => _imageAssets.Process();
        #endregion

        #region Lang
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"noperm", "<size=17>У вас нет прав для использования этой команды!</size>"},
                {"nosbags", "<size=17>У вас нет спальников!</size>"}
            }, this);
        }
        string GetMsg(string key, BasePlayer player = null) => GetMsg(key, player.UserIDString);
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}
