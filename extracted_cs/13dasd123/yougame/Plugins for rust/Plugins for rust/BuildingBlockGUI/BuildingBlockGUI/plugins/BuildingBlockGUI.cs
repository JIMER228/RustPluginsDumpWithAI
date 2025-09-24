        #region Header

using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System.Reflection;
using Oxide.Core;
using System.Linq;
using System.Globalization;
using Facepunch;
using System.IO;
using Oxide.Game.Rust.Cui;
using Network;
using Network.Visibility;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using Rust;
namespace Oxide.Plugins
{
    /*
	написал https://vk.com/id320737533
    */
    [Info("BuildingBlockGUI", "S1m0n", "1.0.0")]
    [Description("BuildingBlockGUI ;)")]
    public class BuildingBlockGUI : RustPlugin
    {
        #endregion

        ImageCache ImageAssets;
        GameObject CupObject;

        #region ImageDownloader

        private void cacheImage()
        {
            CupObject = new GameObject();
            ImageAssets = CupObject.AddComponent<ImageCache>();
            ImageAssets.imageFiles.Clear();
            string dataDirectory = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar;
            ImageAssets.getImage("cup", dataDirectory + "cup.png");
            download();
        }

        class ImageCache : MonoBehaviour
        {
            public Dictionary<string, string> imageFiles = new Dictionary<string, string>();

            List<Queue> queued = new List<Queue>();

            class Queue
            {
                public string url { get; set; }
                public string name { get; set; }
            }

            public void OnDestroy()
            {
                foreach (var value in imageFiles.Values)
                {
                    FileStorage.server.RemoveEntityNum(uint.MaxValue, Convert.ToUInt32(value));
                }
            }

            public void getImage(string name, string url)
            {
                queued.Add(new Queue
                {
                    url = url,
                    name = name
                });
            }

            IEnumerator WaitForRequest(Queue queue)
            {
                using (var www = new WWW(queue.url))
                {
                    yield return www;

                    if (string.IsNullOrEmpty(www.error))
                    {
                        var stream = new MemoryStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        imageFiles.Add(queue.name, FileStorage.server.Store(stream, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    }
                    else
                    {
                        Debug.LogWarning("\n\n!!!!!!!!!!!!!!!!!!!!!\n\nError downloading image files (cup.png)\nThey must be in your oxide/data/ !\n\n!!!!!!!!!!!!!!!!!!!!!\n\n");
                        ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, "oxide.unload BuildingBlockGUI");
                    }
                }
            }

            public void process()
            {
                for (int i = 0; i < 1; i++)
                    StartCoroutine(WaitForRequest(queued[i]));
            }
        }

        public string fetchImage(string name)
        {
            string result;
            if (ImageAssets.imageFiles.TryGetValue(name, out result))
                return result;
            return string.Empty;
        }

        void download()
        {
            ImageAssets.process();
        }

        #endregion

        void OnServerInitialized()
        {
            cacheImage();
        }

        void Unloaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "gui");
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                Destroy(player);
            }
            UnityEngine.Object.Destroy(CupObject);
        }

        List<ulong> activeGUI = new List<ulong>();

        void Gui(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            if (activeGUI.Contains(player.userID)) return;
            CuiHelper.DestroyUi(player, "BuildingBlockGUI");
            CuiElement element = new CuiElement
            {
                Name = "BuildingBlockGUI",
                Components = {
                        new CuiRawImageComponent {
                            Png = fetchImage("cup"),
                            Color = "1 1 1 0.92",
                            Sprite = "assets/content/textures/generic/fulltransparent.tga"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.795 0.017",
                            AnchorMax = "0.836 0.092"
                        }
                    }
            };
            container.Add(element);
            CuiHelper.AddUi(player, container);
            activeGUI.Add(player.userID);
        }

        float lastTick = UnityEngine.Time.realtimeSinceStartup;

        void OnTick()
        {
            if (UnityEngine.Time.realtimeSinceStartup - lastTick < 0.1f) return;
            lastTick = UnityEngine.Time.realtimeSinceStartup;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.HasPlayerFlag(global::BasePlayer.PlayerFlags.InBuildingPrivilege) && !player.HasPlayerFlag(global::BasePlayer.PlayerFlags.HasBuildingPrivilege))
                {
                    Gui(player);
                    continue;
                }
                if (!player.HasPlayerFlag(global::BasePlayer.PlayerFlags.InBuildingPrivilege))
                {
                    Destroy(player);
                    continue;
                }
                if (player.HasPlayerFlag(global::BasePlayer.PlayerFlags.HasBuildingPrivilege))
                {
                    Destroy(player);
                    continue;
                }
            }
        }

        void Destroy(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BuildingBlockGUI");
            activeGUI.Remove(player.userID);
        }
        
        #region Footer
    }
}
#endregion