using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.LocalFiles;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LocalFiles Extension Manager", "RFC1920", "1.0.4")]
    [Description("Default file management plugin using LocalFiles Ext")]
    internal class FileManager : RustPlugin
    {
        private const string FMGUI = "filemanager.gui";
        private const string FMCAT = "filemanager.cat";
        private List<ulong> isopen = new List<ulong>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        private void Init()
        {
            AddCovalenceCommand("file", "CmdFile");
        }

        private void IsOpen(ulong uid, bool set = false)
        {
            if (set)
            {
                if (!isopen.Contains(uid)) isopen.Add(uid);
                return;
            }
            isopen.Remove(uid);
        }

        private object OnUserCommand(BasePlayer player, string command, string[] args)
        {
            if (command != "fm" && isopen.Contains(player.userID))
            {
                return true;
            }
            return null;
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (command != "fm" && isopen.Contains(player.userID))
            {
                return true;
            }
            return null;
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, FMGUI);
                CuiHelper.DestroyUi(player, FMCAT);
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You don't have permission to use this command.",
                ["title"] = "File Manager{0}",
                ["add"] = "Add",
                ["authtest"] = "Authtest",
                ["save"] = "Save",
                ["deleted"] = "File {0} was deleted from {1}",
                ["scanned"] = "(Re)scanned the content folder",
                ["catset"] = "Category for {0} was set to {1}",
                ["catunset"] = "File {0} was removed from category {1}",
                ["renamed"] = "{0} was renamed to {1}",
                ["filelist"] = "Available files:\n{0}",
                ["fileinfo"] = "File Info:\n{0}",
                ["ok"] = "OK"
            }, this);
        }

        [Command("file")]
        private void CmdFile(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.IsAdmin) { Message(iplayer, "notauthorized"); return; }
            var player = iplayer.Object as BasePlayer;

            if (args.Length == 0)
            {
                //showhelp
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "gui":
                        FMGui(player);
                        break;
                    case "rescan":
                    case "scan":
                        LocalFilesExt.ScanDir();
                        break;
                    case "list":
                        string output = "";
                        foreach (KeyValuePair<int, LocalFilesExt.FileMeta> finfo in LocalFilesExt.localFiles)
                        {
                            string cats = "";
                            foreach (var cat in LocalFilesExt.categories)
                            {
                                if (cat.Value.Contains(finfo.Key))
                                {
                                    cats += cat.Key + " ";
                                }
                            }
                            if (cats == "")
                            {
                                output += $"{finfo.Key}: {finfo.Value.FileName} Categories: none\n";
                            }
                            else
                            {
                                output += $"{finfo.Key}: {finfo.Value.FileName} Categories: {cats}\n";
                            }
                        }
                        Message(iplayer, "filelist", output);
                        break;
                }
            }
            else if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "get":
                    case "url":
                    case "fetch":
                        {
                            int index = LocalFilesExt.FetchUrl(args[1]);
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            if (finfo != null)
                            {
                                string output = finfo.Dir + Path.DirectorySeparatorChar + finfo.FileName + $": ({finfo.FileType}) {finfo.Width}x{finfo.Height}";
                                Message(iplayer, "fileinfo", output);
                            }
                        }
                        break;
                    case "info":
                        {
                            int index = int.Parse(args[0]);
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            if (finfo != null)
                            {
                                string output = finfo.Dir + Path.DirectorySeparatorChar + finfo.FileName + $": ({finfo.FileType}) {finfo.Width}x{finfo.Height}";
                                Message(iplayer, "fileinfo", output);
                            }
                        }
                        break;
                    case "delete":
                    case "remove":
                        {
                            int index = int.Parse(args[1]);
                            if (index == 0)
                            {
                                index = LocalFilesExt.fileList[args[1]];
                            }
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            bool success = LocalFilesExt.DeleteFile(finfo.FileName);

                            if (success)
                            {
                                Message(iplayer, "deleted", finfo.FileName, finfo.Dir);
                            }
                        }
                        break;
                }
            }
            else if (args.Length == 3)
            {
                switch (args[0])
                {
                    case "rename":
                        {
                            int index = int.Parse(args[1]);
                            if (index == 0)
                            {
                                index = LocalFilesExt.fileList[args[1]];
                            }
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            bool success = LocalFilesExt.RenameFile(finfo.FileName, args[2]);
                            if (success)
                            {
                                Message(iplayer, "renamed", args[1], args[2]);
                            }
                        }
                        break;
                    case "category":
                    case "cat":
                        {
                            int index = int.Parse(args[1]);
                            bool success;
                            if (index > 0)
                            {
                                success = LocalFilesExt.SetCategory(index, args[2]);
                            }
                            else
                            {
                                success = LocalFilesExt.SetCategory(args[1], args[2]);
                            }
                            if (success)
                            {
                                Message(iplayer, "catset", args[1], args[2]);
                            }
                        }
                        break;
                    case "uncat":
                        {
                            int index = int.Parse(args[1]);
                            bool success;
                            if (index > 0)
                            {
                                success = LocalFilesExt.UnsetCategory(index, args[2]);
                            }
                            else
                            {
                                success = LocalFilesExt.UnsetCategory(args[1], args[2]);
                            }
                            if (success)
                            {
                                Message(iplayer, "catunset", args[1], args[2]);
                            }
                        }
                        break;
                }
            }
            if (isopen.Contains(player.userID)) FMGui(player);
        }

        private void FMGui(BasePlayer player)
        {
            //IsOpen(player.userID, true);
            //CuiHelper.DestroyUi(player, FMGUI);
            //CuiHelper.AddUi(player, container);
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);
        private float[] GetButtonPositionP(int rowNumber, int columnNumber, float colspan = 1f)
        {
            float offsetX = 0.05f + (0.116f * columnNumber);
            float offsetY = (0.87f - (rowNumber * 0.054f));

            return new float[] { offsetX, offsetY, offsetX + (0.226f * colspan), offsetY + 0.03f };
        }

        private float[] GetButtonPositionS(int rowNumber, int columnNumber, float colspan = 1f)
        {
            float offsetX = 0.05f + (0.116f * columnNumber);
            float offsetY = (0.87f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + (0.206f * colspan), offsetY + 0.03f };
        }

        private float[] GetButtonPositionZ(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.156f * columnNumber);
            float offsetY = (0.75f - (rowNumber * 0.054f));

            return new float[] { offsetX, offsetY, offsetX + 0.296f, offsetY + 0.03f };
        }

        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                return new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
            }
            public static void Panel(ref CuiElementContainer container, string panel, string color, string min, string max, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    CursorEnabled = cursor
                },
                panel);
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel);

            }
            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            public static void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = align,
                            CharsLimit = 30,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent { AnchorMin = min, AnchorMax = max },
                        new CuiNeedsCursorComponent()
                    }
                });
            }
            public static void Icon(ref CuiElementContainer container, string panel, string color, string imageurl, string min, string max)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = imageurl,
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Color = color
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min,
                            AnchorMax = max
                        }
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
    }
}
