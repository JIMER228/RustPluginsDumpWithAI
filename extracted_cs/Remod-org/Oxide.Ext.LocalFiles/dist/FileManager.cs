using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.LocalFiles;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Plugins
{
    [Info("LocalFiles Extension Manager", "RFC1920", "1.0.2")]
    [Description("Default file management plugin using LocalFiles Ext")]
    internal class FileManager : RustPlugin
    {
        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        void Init()
        {
            AddCovalenceCommand("file", "CmdFMGui");
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
                ["catset"] = "Category for {0} was set to {1}",
                ["renamed"] = "{0} was renamed to {1}",
                ["filelist"] = "Available files:\n{0}",
                ["fileinfo"] = "File Info:\n{0}",
                ["ok"] = "OK"
            }, this);
        }

        [Command("file")]
        private void CmdFMGui(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.IsAdmin) { Message(iplayer, "notauthorized"); return; }

            if (args.Length == 0)
            {
                //showhelp
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "list":
                        string output = "";
                        foreach (KeyValuePair<int, LocalFilesExt.FileMeta> finfo in LocalFilesExt.localFiles)
                        {
                            try
                            {
                                output += $"{finfo.Key.ToString()}: {finfo.Value.FileName} Category: {finfo.Value.Category.ToString()}\n";
                            }
                            catch
                            {
                                output += $"{finfo.Key.ToString()}: {finfo.Value.FileName} Category: none\n";
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
                            bool success = false;
                            if (index == 0)
                            {
                                index = LocalFilesExt.fileList[args[1]];
                            }
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            success = LocalFilesExt.DeleteFile(finfo.FileName);

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
                            bool success = false;
                            if (index == 0)
                            {
                                index = LocalFilesExt.fileList[args[1]];
                            }
                            LocalFilesExt.FileMeta finfo = LocalFilesExt.localFiles[index];
                            success = LocalFilesExt.RenameFile(finfo.FileName, args[2]);
                            if(success)
                            {
                                Message(iplayer, "renamed", args[1], args[2]);
                            }
                        }
                        break;
                    case "category":
                    case "cat":
                        {
                            int index = int.Parse(args[1]);
                            bool success = false;
                            if (index > 0)
                            {
                                success = LocalFilesExt.SetCategory(index, args[2]);
                            }
                            else
                            {
                                success = LocalFilesExt.SetCategory(args[1], args[2]);
                            }
                            if(success)
                            {
                                Message(iplayer, "catset", args[1], args[2]);
                            }
                        }
                        break;
                }
            }
        }
    }
}
