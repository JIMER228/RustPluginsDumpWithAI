using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Oxide.Core;
using UnityEngine;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("ZealImageResizer", "Kira", "1.0.0")]
    [Description("Автономный image resizer")]
    public class ZealImageResizer : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;
        StoredData DataBase = new StoredData();

        #region [Vars]

        private Coroutine Images;

        #endregion

        #region [Resize-Image]

        static byte[] Resize(byte[] bytes, int width, int height)
        {
            Image img = (Bitmap) new ImageConverter().ConvertFrom(bytes);
            var cutPiece = new Bitmap(width, height);
            var graphic = System.Drawing.Graphics.FromImage(cutPiece);

            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.SmoothingMode = SmoothingMode.AntiAlias;
            graphic.CompositingMode = CompositingMode.SourceCopy;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

            graphic.DrawImage(img, new Rectangle(0, 0, width, height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
            graphic.Dispose();

            var ms = new MemoryStream();
            cutPiece.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }

        public byte[] OpenImg(string fPath)
        {
            byte[] byteArray;
            var image = Image.FromFile(fPath);

            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Jpeg);
                stream.Close();

                byteArray = stream.ToArray();
            }

            return byteArray;
        }

        #endregion

        #region [Hooks]

        void OnServerInitialized()
        {
            LoadData();

            foreach (var image in DataBase.imageList)
            {
                Puts(image.Key);
            }
        }

        private byte[] GetImg(string name)
        {
            var strId = (string) ImageLibrary?.Call("GetImage", name) ?? "";
            uint imageId;
            if (!uint.TryParse(strId, out imageId)) return null;
            return FileStorage.server.Get(imageId, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);
        }

        void Unload()
        {
        }

        #endregion

        #region [Helpers]

        public void SaveImg(byte[] bytes, string fNmae)
        {
            Stream stream = new MemoryStream(bytes);
            var mapImage = Image.FromStream(stream);

            var mapFilePath = Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + fNmae;

            mapImage.Save(mapFilePath, ImageFormat.Png);
        }

        private void LoadData()
        {
            try
            {
                DataFileSystem images = new DataFileSystem(Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar +
                                                           "ImageLibrary" + Path.DirectorySeparatorChar);
                DataBase = images.ReadObject<StoredData>("image_data.json");
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("resize")]
        private void ZS(ConsoleSystem.Arg args)
        {
            ResizeImage_In_ImageLibrary(args.Args[0], 16486);
        }

        #endregion

        #region [API]

        public void ResizeImage_In_ImageLibrary(string name, int size)
        {
            byte[] img = GetImg(name);
            byte[] resized = Resize(img, size, size);

            SaveImg(resized, $"{name}.png");
            Puts($"Разрешение изображения [{name}] изменено на [{size}x{size}]");
        }

        private IEnumerator ResizeAllIcons()
        {
            yield return 0;
        }

        #endregion

        #region [DataBase]

        class StoredData
        {
            public string loadName;
            public bool loadSilent;

            public Dictionary<string, string> imageList;
            public Dictionary<string, byte[]> imageData;
        }

        #endregion
    }
}