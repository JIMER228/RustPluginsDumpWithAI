/*
* 
* Copyright © 2025 headtapper
* 
* You may not copy, modify, share, distribute, publish, or sell copies of this software.
* 
* You may use this plugin for personal use on your own Rust server.
* 
* You may not use components of the software's code base for any other purpose.
* 
*/

using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using static ProtoBuf.PatternFirework;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace Oxide.Plugins
{
    [Info("FireworksLogger", "headtapper", "1.0.0")]
    [Description("Log designs players create with fireworks in-game.")]

    public class FireworksLogger : RustPlugin
    {
        private void OnFireworkDesignChange(PatternFirework instance, Design design, BasePlayer player)
        {
            int imageSize = 256;
            Bitmap bmp = new Bitmap(imageSize, imageSize);

            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.Black);

                foreach (ProtoBuf.PatternFirework.Star star in design.stars)
                {
                    int x = (int)((star.position.x + 1) * (imageSize / 2));
                    int y = imageSize - (int)((star.position.y + 1) * (imageSize / 2));

                    Color starColor = Color.FromArgb(
                        (int)(star.color.r * 255),
                        (int)(star.color.g * 255),
                        (int)(star.color.b * 255)
                    );

                    int starSize = 6;
                    gfx.FillEllipse(new SolidBrush(starColor), x - (starSize / 2), y - (starSize / 2), starSize, starSize);
                }

                string folderPath = Path.Combine(Interface.Oxide.DataDirectory, "FireworksLogger");
                Directory.CreateDirectory(folderPath);
                string fileName = $"{player.displayName}_{player.userID}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}FireworksDesign.png";
                bmp.Save(Path.Combine(folderPath, fileName), ImageFormat.Png);
                bmp.Dispose();
            }
        }
    }
}
