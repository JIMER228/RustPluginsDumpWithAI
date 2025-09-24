using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("Notify", "Hougan", "0.0.1")]
    public class Notify : RustPlugin
    {
        #region eNums

        private enum Type
        {
            Small,
            Big
        }

        #endregion
         
        #region Class

        private class Notification
        {
            public string Header;
            public string Description;

            public int Time;
            public string ImageID;
            public Type Type;
        }
        
        private class NotifyPlayer : MonoBehaviour
        {
            private string Layer = "UI_NotifactionLayer";
            private BasePlayer Player;

            private void Awake() => Player = GetComponent<BasePlayer>();
            public void MakeNotification(Notification notification)
            {
                switch (notification.Type)
                {
                    case Type.Small: SmallNotify(notification); break;
                    case Type.Big: BigNotify(notification); break;
                }
                
                if (IsInvoking(nameof(IdleDestroy))) CancelInvoke(nameof(IdleDestroy));
                Invoke(nameof(IdleDestroy), notification.Time);
            }

            private void SmallNotify(Notification notification)
            {
                IdleDestroy();
                
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                { 
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"-200 81", OffsetMax = $"181 105"},
                    Image         = {FadeIn = 1f, Color     = "0.968627453 0.921568632 0.882352948 0.2"}
                }, "Overlay", Layer);

                if (!notification.ImageID.IsNullOrEmpty())
                {
                    container.Add(new CuiPanel
                    { 
                        FadeOut       = 1f,
                        RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"-290 18", OffsetMax = $"-202 105"},
                        Image         = {FadeIn    = 1f, Color          = "0.968627453 0.921568632 0.882352948 0.2"}
                    }, "Overlay", Layer + ".C");

                    container.Add(new CuiElement
                    {
                        FadeOut = 1f,
                        Parent = Layer + ".C",
                        Name   = Layer + ".I",
                        Components =
                        {
                            new CuiRawImageComponent() {FadeIn = 1f, Png          = notification.ImageID, Color = "1 1 1 0.6"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3"}
                        }
                    });
                }

                container.Add(new CuiLabel
                {
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0 0", AnchorMax           = "1 1", OffsetMax              = "0 0"},
                    Text          = {FadeIn = 1f, Text      = notification.Header, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 0.8"}
                }, Layer, Layer + ".H");
 
                CuiHelper.AddUi(Player, container);
            }
            
            private void BigNotify(Notification notification)
            {
                IdleDestroy();
                
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                { 
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0.5 0.7", AnchorMax = "0.5 0.7", OffsetMin = $"-200 0", OffsetMax = $"200 200"},
                    Image         = {FadeIn = 1f, Color     = "0 0 0 0"}
                }, "Hud", Layer);

                if (!notification.ImageID.IsNullOrEmpty())
                {
                    container.Add(new CuiPanel
                    { 
                        FadeOut       = 1f,
                        RectTransform = {AnchorMin = "0.5 0.7", AnchorMax = "0.5 0.7", OffsetMin = $"-50 100", OffsetMax = $"50 200"},
                        Image         = {FadeIn    = 1f, Color          = "0 0 0 0"}
                    }, "Hud", Layer + ".C");

                    container.Add(new CuiElement
                    {
                        FadeOut = 1f,
                        Parent = Layer + ".C",
                        Name   = Layer + ".I",
                        Components =
                        { 
                            new CuiRawImageComponent() {FadeIn = 1f, Png          = notification.ImageID, Color = "1 1 1 0.9"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3"}
                        }
                    });
                }

                container.Add(new CuiLabel
                {
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0 0", AnchorMax           = "1 1", OffsetMin = "0 -400", OffsetMax              = "0 -100"},
                    Text          = {FadeIn = 1f, Text      = notification.Header, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf", FontSize = 24, Color = "1 1 1 0.8"}
                }, Layer, Layer + ".H");
                container.Add(new CuiLabel
                {
                    FadeOut       = 1f,
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0.8", OffsetMin = "-200 -500", OffsetMax           = "200 -90"},
                    Text          = {FadeIn    = 1f, Text         = notification.Description, Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.6"}
                }, Layer, Layer + ".D");
 
                CuiHelper.AddUi(Player, container);
            } 

            private void IdleDestroy() => OnDestroy();
            private void OnDestroy()
            {
                CuiHelper.DestroyUi(Player, Layer + ".H");
                CuiHelper.DestroyUi(Player, Layer + ".D");
                CuiHelper.DestroyUi(Player, Layer + ".C");
                CuiHelper.DestroyUi(Player, Layer + ".I");
                CuiHelper.DestroyUi(Player, Layer);
            }
        }
        
        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            timer.Once(1, () => { BasePlayer.activePlayerList.ToList().ForEach(OnPlayerInit); });
        }
        
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var obj = player.GetComponent<NotifyPlayer>();
            if (obj != null) UnityEngine.Object.Destroy(obj); 
        }

        private void OnPlayerInit(BasePlayer player)
        {
            var obj = player.GetComponent<NotifyPlayer>();
            if (obj == null) player.gameObject.AddComponent<NotifyPlayer>();
        }

        private void Unload() => UnityEngine.Object.FindObjectsOfType<NotifyPlayer>().ToList().ForEach(UnityEngine.Object.Destroy);

        #endregion

        #region Commands

        [HookMethod("API_DrawNotification")] 
        private void API_DrawNotification(BasePlayer player, string header, string description, string imageId, int time, bool small)
        {
            var obj = player.GetComponent<NotifyPlayer>();
            if (obj == null) return;
            
            obj.MakeNotification(new Notification
            {
                Header = header,
                Description = description,
                ImageID = imageId,
                Time = time ,
                Type = small ? Type.Small : Type.Big
            });
        }

        #endregion
    }
} 