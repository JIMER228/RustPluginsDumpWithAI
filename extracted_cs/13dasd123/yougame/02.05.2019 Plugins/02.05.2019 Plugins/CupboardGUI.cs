using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("CupboardGUI", "RustPlugin.ru", "1.0.5")]
      //  Слив плагинов server-rust by Apolo YouGame

    public class CupboardGUI : RustPlugin
    {

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
                        imageFiles.Add(queue.name, FileStorage.server.Store(www.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    }
                    else
                    {
                        Debug.LogWarning("\n\n!!!!!!!!!!!!!!!!!!!!!\n\nError downloading image files (cup.png)\nThey must be in your oxide/data/ !\n\n!!!!!!!!!!!!!!!!!!!!!\n\n");
                        ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, "oxide.unload CupboardGUI");
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
        void OnPlayerDesconected(BasePlayer player)
        { }
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
            CuiHelper.DestroyUi(player, "CupboardGui");
            CuiElement element = new CuiElement
            {
                Name = "CupboardGui",
                Components = {
                        new CuiRawImageComponent {
                            Png = fetchImage("cup"),
                            Color = "1 1 1 0.92",
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            FadeIn = 0.53f
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.344 0.11",
                            AnchorMax = "0.641 0.143"
                        },
                    },
                FadeOut = 0.53f
            };
            container.Add(element);
            CuiHelper.AddUi(player, container);
            activeGUI.Add(player.userID);
        }

        float lastTick = UnityEngine.Time.realtimeSinceStartup;

        void OnTick(BaseEntity entity)
        {
            if (UnityEngine.Time.realtimeSinceStartup - lastTick < 0.4f) return;
            lastTick = UnityEngine.Time.realtimeSinceStartup;
            foreach (var player in BasePlayer.activePlayerList)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Gui(player);
                    continue;
                }
                else
                { 
                    Destroy(player);
                    continue;
                }

            }
        }
        void OnPlayerDisconnected()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "CupboardGui");
                activeGUI.Remove(player.userID);
            }
        }
        void Destroy(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CupboardGui");
            activeGUI.Remove(player.userID);
        }

        #region Footer
    }
}
#endregion
