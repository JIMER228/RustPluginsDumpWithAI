#region License (GPL v2)
/*
    DESCRIPTION
    Copyright (c) 2024 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Photog", "RFC1920", "1.0.2")]
    [Description("Paste photo from Instant Camera to a PhotoFrame")]
    internal class Photog : RustPlugin
    {
        private ConfigData configData;
        private const string permUse = "photog.use";

        private Dictionary<ulong, NetworkableId> frames = new Dictionary<ulong, NetworkableId>();

        private void DoLog(string message)
        {
            if (configData.debug)
            {
                Interface.GetMod().LogInfo($"{Name}: {message}");
            }
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            AddCovalenceCommand("mf", "cmdMarkFrame");
            LoadConfigVariables();
        }

        private void OnPhotoCaptured(PhotoEntity photo, Item item, BasePlayer player, byte[] numArray)
        {
            DoLog($"{player.displayName} took a photo {item.name}");
            if (configData.overlayPhotographerName || configData.overlayTimestamp || configData.overlayPosition)
            {
                string text = null;

                if (configData.overlayPhotographerName) text += $"{player.displayName} ";
                if (configData.overlayTimestamp) text += DateTime.Now.ToString("dd MMM yyyy");
                if (configData.overlayPosition) text += $" @ {player.transform.position}";

                numArray = OverlayText(numArray, text);
            }
            if (frames.ContainsKey(player.userID))
            {
                DoLog("Try to copy photo");
                photo.SetImageData(player.userID, numArray); // This overwrites the photoItem in inventory
                BaseNetworkable frame = BaseNetworkable.serverEntities.Find(frames[player.userID]);
                if (frame is PhotoFrame)
                {
                    DoLog("Found a photo frame");
                    PhotoFrame photoFrame = (PhotoFrame)frame;
                    BaseNetworkable.LoadInfo info = new BaseNetworkable.LoadInfo()
                    {
                        msg = new ProtoBuf.Entity()
                        {
                            photoFrame = new ProtoBuf.PhotoFrame()
                            {
                                ShouldPool = true,
                                overlayImageCrc = photo.ImageCrc,
                                photoEntityId = photo.net.ID,
                                editHistory = photo.EditingHistory
                            }
                        }
                    };
                    DoLog("Loading image to photo frame");
                    photoFrame.Load(info);
                    photoFrame.SendNetworkUpdateImmediate();
                    if (configData.lockOnPaint)
                    {
                        photoFrame.SetFlag(BaseEntity.Flags.Locked, true);
                    }
                }
                if (!configData.leaveOpen)
                {
                    frames.Remove(player.userID);
                }
            }
        }

        [Command("mf")]
        private void cmdMarkFrame(IPlayer iplayer, string command, string[] args)
        {
            if (!configData.RequirePermission || (configData.RequirePermission && permission.UserHasPermission(iplayer?.Id, permUse)))
            {
                BasePlayer player = iplayer.Object as BasePlayer;
                PhotoFrame frame = FindEntity(player) as PhotoFrame;
                if (frame != null)
                {
                    if (frames.ContainsKey(player.userID))
                    {
                        frames.Remove(player.userID);
                        iplayer.Reply("Frame has been unmarked");
                        return;
                    }
                    frames.Add(player.userID, frame.NetworkID);
                    iplayer.Reply("Frame has been marked");
                }
            }
        }

        private byte[] OverlayText(byte[] numArray, string text)
        {
            // https://stackoverflow.com/questions/40041373/add-text-to-image-without-saving
            DoLog("OverlayText");
            Bitmap bmp;

            using (MemoryStream m = new MemoryStream(numArray))
            {
                bmp = new Bitmap(m);
            }
            DoLog($"IMG w{bmp.Width}/h{bmp.Height}");
            // Always 854x480
            int sLeft = bmp.Width / 6;// * 4;
            int sHeight = bmp.Height / 5 * 4;
            DoLog($"Overlay text '{text}' at {sHeight}/{sLeft} h 50 w 650");
            RectangleF rectf = new RectangleF(sLeft, sHeight, 650, 50);
            int fontsize = configData.overlayFontSize > 10 ? configData.overlayFontSize : 10;
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            {
                using (System.Drawing.Font arialFont = new System.Drawing.Font("Arial", fontsize))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.DrawString(text, new System.Drawing.Font("courier sans", fontsize, System.Drawing.FontStyle.Bold), Brushes.White, rectf);
                }
            }
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];

            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();

            return arr;
        }

        private static BaseEntity FindEntity(BasePlayer player)
        {
            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit)) return null;
            return hit.GetEntity();
        }

        public class ConfigData
        {
            public bool lockOnPaint;
            public bool leaveOpen;
            public bool overlayPhotographerName;
            public bool overlayTimestamp;
            public bool overlayPosition;
            public int overlayFontSize;
            public bool debug;
            public bool RequirePermission;
            public VersionNumber Version;
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultConfig()
        {
            DoLog("Creating new config file.");
            ConfigData config = new ConfigData
            {
                lockOnPaint = false,
                leaveOpen = false,
                overlayPhotographerName = true,
                overlayFontSize = 12,
                debug = false,
                RequirePermission = false
            };
        }
    }
}
