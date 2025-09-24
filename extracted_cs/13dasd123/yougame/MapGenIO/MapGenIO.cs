using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("MapGenIO", "Fartus/SkiTles", "1.0.2")]
    [Description("Простой генератор картинки карты для вашего сервера")]
	//Данный плагин - переработка плагина SeederyIoMapGen от автора Pauli 'Dids' Jokela
	//Ссылка - https://github.com/Dids/seedery.io-mapgen
    class MapGenIO : RustPlugin
    {
        #region Config
        private int _mapWidth;
        private int _mapHeight;
        private Terrain _terrain;
        private bool NewWipe = false;

		public bool AutoMapGen = true;
		public string FileName = "map";
		public string AutoFileType = ".png";
		public int AutoFileSize = 720;

        private void LoadConfigValues()
        {
			GetConfig("Автоматическая генерация новой карты после вайпа", ref AutoMapGen);
			GetConfig("Имя генерируемого файла карты (без расширения)", ref FileName);
			GetConfig("Расширение файла при автоматической генерации (jpg или png)", ref AutoFileType);
			GetConfig("Размер изображения карты при автоматической генерации", ref AutoFileSize);

			SaveConfig();
		}

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
            Config.Clear();
		}

        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
		#endregion

        #region Commands
        [ConsoleCommand("savemap")]
        private void SaveMapCMD(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            int size = 0;
            string filetype = ".jpg";
            if (arg.HasArgs())
            {
                if (!Int32.TryParse(arg.Args[0], out size)) PrintWarning("Неправильно указан размер изображения. При генерации будет установлено дефолтное значение (размер карты / 2)");
                if (arg.Args.Length > 1 && arg.Args[1] == "png") filetype = ".png";
            }
            GenerateMap(size, filetype);
        }
        #endregion

        #region Hooks
        void OnNewSave(string FileName)
        {
			NewWipe = true;
        }

        void OnServerInitialized()
        {
			LoadConfigValues();

            if (AutoMapGen && NewWipe)
			{
				PrintWarning("Обнаружен вайп сервера!");
				GenerateMap(AutoFileSize, AutoFileType);
			}
        }
        #endregion

        #region Core
        private void GenerateMap(int size = 0, string filetype = ".jpg", float brightness = 2.5f)
        {
            PrintWarning("Создается изображение карты. Сервер может подвиснуть на несколько секунд!");
            TerrainMeta.HeightMap.GenerateTextures();
            Texture2D Heightmap = TerrainMeta.HeightMap.NormalTexture;

            int width = TerrainMeta.Terrain.terrainData.heightmapWidth - 1;
            int height = TerrainMeta.Terrain.terrainData.heightmapHeight - 1;

            Texture2D finalTexture = new Texture2D(width, height);
            _terrain = TerrainMeta.Terrain;

            float lowestTerrainheight = GetLowestTerrainHeight();
            float highestTerrainHeight = GetHighestTerrainHeight();

            Color waterColor = new Color(0.15f, 0.25f, 0.35f, 1f); // цвет воды

            for (int y = 0; y < finalTexture.height; y++)
            {
                for (int x = 0; x < finalTexture.width; x++)
                {
                    float terrainStartX = TerrainMeta.Terrain.GetPosition().x;
                    float terrainStartY = TerrainMeta.Terrain.GetPosition().z;
                    float terrainSizeX = TerrainMeta.Size.x;
                    float terrainSizeY = TerrainMeta.Size.z;
                    float terrainScaleUpX = TerrainMeta.Size.x / width;
                    float terrainScaleUpY = TerrainMeta.Size.z / height;
                    float terrainScaleDownX = width / TerrainMeta.Size.x;
                    float terrainScaleDownY = height / TerrainMeta.Size.z;
                    float startX = x * terrainScaleUpX;
                    float startY = y * terrainScaleUpY;
                    float calculatedTerrainX = startX + terrainStartX;
                    float calculatedTerrainY = startY + terrainStartY;
                    Vector3 terrainWorldPosition = new Vector3(calculatedTerrainX, 0, calculatedTerrainY);
                    float waterDepth = TerrainMeta.WaterMap.GetDepth(terrainWorldPosition);
                    float terrainHeight = _terrain.terrainData.GetHeight(x, y) - lowestTerrainheight;
                    float currentHeight = terrainHeight / (highestTerrainHeight - lowestTerrainheight);

					if (currentHeight>=waterDepth)
					{
                        Color pixelColor = TerrainMeta.Colors.GetColor(terrainWorldPosition, -1);
                        pixelColor.a = TerrainMeta.AlphaMap.GetAlpha(x, y);
                        finalTexture.SetPixel(x, y, pixelColor);
					}
					else
					{
                        Color pixelColor = TerrainMeta.Colors.GetColor(terrainWorldPosition, 0);
                        pixelColor.a = TerrainMeta.AlphaMap.GetAlpha(x, y);
                        finalTexture.SetPixel(x, y, pixelColor);
					}

                    if (Heightmap != null)
                    {
                        var finalTextureColor = finalTexture.GetPixel(x, y);
                        var heightmapNormalTextureColor = Heightmap.GetPixel(x, y);
                        var alphaBlendedColor = AdvBlend(heightmapNormalTextureColor, finalTextureColor);

						float h, s, v;
						Color.RGBToHSV(alphaBlendedColor, out h, out s, out v);
						// Изменение яркости HSV цветов
						s *= brightness;
						v *= 1.0f;
						// Конвертация обратно в RGB
						alphaBlendedColor = Color.HSVToRGB(h, s, v);
						finalTexture.SetPixel(x, y, alphaBlendedColor);

                        if (currentHeight<waterDepth)
                        {
							Color waterColor2 = alphaBlendedColor;
                            waterColor2 = new Color(waterColor.r, waterColor.g, waterColor.b, Sigma(waterDepth/50.0f));

                            var alphaBlendedColor2 = AdvBlend(waterColor2, finalTextureColor);
                            finalTexture.SetPixel(x, y, alphaBlendedColor2);
                        }
                    }
                }
            }
            finalTexture.Apply();
            SaveIMG(finalTexture, size, filetype);
            UnityEngine.Object.Destroy(finalTexture);
        }

        private float Sigma(float a) { return float.Parse((1 / (1 + Math.Pow(Math.E, -12.0f*(a-0.04f)))).ToString()); } // обводка контуров
        private Color Sigma(Color a) { return new Color(Sigma(a.r), Sigma(a.g), Sigma(a.b),a.a); }

        private float Parab(float a) { return float.Parse((2.0f / (1 + Math.Pow(2.9f, Math.Pow(a - 25.0f, 2) / 320.0f))).ToString()); }
        private Color Parab(Color a) { return new Color(Parab(a.r), Parab(a.g), Parab(a.b), a.a); }

        private float GetLowestTerrainHeight()
        {
            float lowestHeight = TerrainMeta.Size.y;
            for (var x = 0; x < TerrainMeta.Size.x; x++)
            {
                for (var y = 0; y < TerrainMeta.Size.z; y++)
                {
                    var h = _terrain.terrainData.GetHeight(x, y);
                    if (h < lowestHeight) lowestHeight = h;
                }
            }
            return lowestHeight;
        }

        private float GetHighestTerrainHeight()
        {
            float highestHeight = 0;
            for (var x = 0; x < TerrainMeta.Size.x; x++)
            {
                for (var y = 0; y < TerrainMeta.Size.z; y++)
                {
                    var h = _terrain.terrainData.GetHeight(x, y);
                    if (h > highestHeight) highestHeight = h;
                }
            }
            return highestHeight;
        }

        public Color AdvBlend(Color a, Color b)
        {
            Color col = new Color();
            col = b * (1.0f - a.a) + a * a.a;
            return col;
        }

        private void SaveIMG(Texture2D texture, int size = 0, string filetype = ".jpg")
        {
            if (filetype != ".jpg" && filetype != ".png") return;
            byte[] bytes = null;
            if (filetype == ".jpg") bytes = texture.EncodeToJPG();
            if (filetype == ".png") bytes = texture.EncodeToPNG();
            if (bytes == null) return;
            Stream stream = new MemoryStream(bytes);
            System.Drawing.Image mapimage = System.Drawing.Image.FromStream(stream);
            if (size != 0 && size != mapimage.Height)
            {
                System.Drawing.Image.GetThumbnailImageAbort myCallback = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
                mapimage = mapimage.GetThumbnailImage(size, size, myCallback, IntPtr.Zero);
            }
            if (filetype == ".jpg") mapimage.Save(Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "RustMap" + Path.DirectorySeparatorChar + FileName + filetype, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filetype == ".png") mapimage.Save(Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "RustMap" + Path.DirectorySeparatorChar + FileName + filetype, System.Drawing.Imaging.ImageFormat.Png);
            PrintWarning($"Изображение карты размером {mapimage.Height}px сохранено: {Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar}RustMap{Path.DirectorySeparatorChar + FileName + filetype}");
        }

        public bool ThumbnailCallback()
        {
            return false;
        }
        #endregion
    }
}
