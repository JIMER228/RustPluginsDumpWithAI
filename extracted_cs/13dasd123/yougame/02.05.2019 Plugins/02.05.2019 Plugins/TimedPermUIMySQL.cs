using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("TimedPermUIMySQL", "SNAK", "0.2.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    class TimedPermUIMySQL : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;

        private Timer TimerUpdate;

        private List<ulong> UIMainIsOpen = new List<ulong>();

        Core.MySql.Libraries.MySql Sql = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();
        Core.Database.Connection Sql_conn;

        public class PlayerPerms
        {
            public string Name;
            public string Id;
            public List<Permissions> Permissions;
            public List<Permissions> Groups;

        }
        public class Permissions
        {
            public string Value;
            public DateTime ExpireDate;
        }

        #region Hooks & Loading
        void Init()
        {
            LoadConfigVariables();
            LoadDefaultMessages();

            if (conf.MySQL.Enable)
            {
                Sql_conn = Sql.OpenDb(conf.MySQL.sql_host, conf.MySQL.sql_port, conf.MySQL.sql_db, conf.MySQL.sql_user, conf.MySQL.sql_pass + ";CharSet=utf8mb4", this);
                LoadMySQL(false);
                UpdateData();
                TimerUpdate = timer.Every(conf.MySQL.timer, () => { UpdateData(); });
            }
        }
        void OnServerInitialized()
        {
            if (ImageLibrary)
                foreach (Perms Parm in conf.Perms)
                    AddImage(Parm.Image, Parm.Perm.Replace(" ", ""), 99);
        }
        void Unload()
        {
            if (TimerUpdate != null) TimerUpdate.Destroy();

            if (Sql_conn != null) Sql_conn.Con.Dispose();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UIMain);
            }

        }

        #endregion

        #region MySQL
        private void LoadMySQL(bool wipe)
        {
            try { Sql_conn.Con.Open(); }
            catch (Exception e) { PrintWarning(e.Message); return; }

            try
            {
                if (Sql_conn == null || Sql_conn.Con == null)
                {
                    Puts("MySQL connection has failed. Please check your credentials.");
                    return;
                }
                if (wipe)
                {
                    Sql.Insert(Core.Database.Sql.Builder.Append($"DROP TABLE IF EXISTS {conf.MySQL.tablename}"), Sql_conn);
                    Puts("TimedPermissions MySQL Table Was Dropped.");
                }

                Sql.Insert(Core.Database.Sql.Builder.Append($"CREATE TABLE IF NOT EXISTS {conf.MySQL.tablename} (`id` varchar(17) NOT NULL,`permission` varchar(128) NOT NULL,`group` varchar(128) NOT NULL,`time` int(11) NOT NULL, UNIQUE KEY `id` (`id`,`permission`,`group`)) ENGINE=InnoDB DEFAULT CHARSET=utf8;"), Sql_conn);
            }
            catch (Exception e)
            {
                Puts("TimedPermissions did not succesfully create a table." + e.Message);
            }

        }

        private void MySQLUpdatePerm(string Id, string Perm, int time)
        {
            try
            {
                Sql.Insert(Core.Database.Sql.Builder.Append($"INSERT INTO {conf.MySQL.tablename} SET `id` = @0, `permission` = @1, `time` = @2 ON DUPLICATE KEY UPDATE `time` = @2;", Id, Perm, time), Sql_conn);
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }
        }
        private void MySQLUpdateGroup(string Id, string Group, int time)
        {
            try
            {
                Sql.Insert(Core.Database.Sql.Builder.Append($"INSERT INTO {conf.MySQL.tablename} SET `id` = @0, `group` = @1, `time` = @2 ON DUPLICATE KEY UPDATE `time` = @2;", Id, Group, time), Sql_conn);
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }
        }
        private void UpdateData()
        {
            int Time = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds) - 10800;
            try
            {
                Sql.Insert(Core.Database.Sql.Builder.Append($"DELETE FROM {conf.MySQL.tablename} WHERE `time` < '{Time}';"), Sql_conn);
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            List<PlayerPerms> data = new List<PlayerPerms>();
            LoadData(ref data, "TimedPermissions");

            foreach (PlayerPerms playerData in data)
            {
                string Id = playerData.Id;

                foreach (Permissions Perm in playerData.Permissions)
                {
                    Time = ((int)(Perm.ExpireDate - new DateTime(1970, 1, 1)).TotalSeconds) - 10800;
                    MySQLUpdatePerm(Id, Perm.Value, Time);
                }
                foreach (Permissions Group in playerData.Groups)
                {
                    Time = ((int)(Group.ExpireDate - new DateTime(1970, 1, 1)).TotalSeconds) - 10800;
                    MySQLUpdateGroup(Id, Group.Value, Time);
                }
            }

            Puts("Update TimedPermissions MySQL");
        }
        #endregion

        #region Commands
        [ConsoleCommand("perminfo")]
        void CmdPlayerPermInfo(ConsoleSystem.Arg arg)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            BasePlayer Player = BasePlayer.FindByID(arg.Connection.userid);

            ShowUI(Player);

        }
        [ChatCommand("perminfo")]
        private void PlayerPermInfo(BasePlayer player, string command, string[] args)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            ShowUI(player);
        }
        #endregion

        #region UI
        private string UIMain = "TimedPermUI.main";
        private void CreateUIMain(BasePlayer player)
        {
            CuiElementContainer UI = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0 0 0 0.7", FadeIn = 0.2f},
                        RectTransform = {AnchorMin = "0.05 0.15",AnchorMax = "0.95 0.95"},
                        FadeOut = 0.2f,
                        CursorEnabled = true
                    },
                    new CuiElement().Parent,
                    UIMain
                }
            };
            UI.Add(new CuiButton
            {
                Button = { Command = "perminfo", Color = "1 0 0 1", FadeIn = 0.2f },
                Text = { Color = "1 1 1 1", FontSize = 14, Align = TextAnchor.MiddleCenter, Text = msg("Close", player.UserIDString) },
                RectTransform = { AnchorMin = "0.8 0.95", AnchorMax = "0.98 0.99" },
                FadeOut = 0.2f
            }, UIMain, UIMain + ".closebtn");
            /*
            UI.Add(new CuiButton
            {
                Button = { Command = "openshop", Color = "1 0 0 1", FadeIn = 0.2f },
                Text = { Color = "1 1 1 1", FontSize = 14, Align = TextAnchor.MiddleCenter, Text = msg("Shop", player.UserIDString) },
                RectTransform = { AnchorMin = "0.02 0.95", AnchorMax = "0.2 0.99" },
                FadeOut = 0.2f
            }, UIMain, "TimedPermUI.main.Shopbtn");
            */
            UI.Add(new CuiPanel
            {
                Image = { Color = "0.2 0.2 0.0 0", FadeIn = 0.2f },
                RectTransform = { AnchorMin = "0.01 0.05", AnchorMax = ".99 0.93" },
                FadeOut = 0.2f
            }, UIMain, UIMain + ".list");


            UI.Add(new CuiElement()
            {
                Parent = UIMain,
                Name = UIMain + ".title",
                Components =
                    {
                        new CuiTextComponent() { Text = msg("Title", player.UserIDString), FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 0.33 1" },
                        new CuiOutlineComponent() { Color = "0.1 0.1 0.1 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent() { AnchorMin = "0.2 0.95", AnchorMax = "0.80 0.99", OffsetMin = "0 0", OffsetMax = "1 1"}
                    }
            });


            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, UI);
            UIMainIsOpen.Add(player.userID);

        }

        private void CreateUIPerm(BasePlayer player, List<Permissions> Perms, string UIPanel, int colum, int AllCount)
        {

            CuiElementContainer UI = new CuiElementContainer();

            int CountPermConfig = conf.Perms.Count;

            foreach (Permissions Perm in Perms)
            {
                float[] pos = SquarePos(colum, AllCount);

                string Time = Perm.ExpireDate.ToString("dd.MM.yy hh:mm");

                Perms PermName = conf.Perms.Find(p => p.Perm == Perm.Value);

                if (PermName == null)
                {
                    PermName = new Perms { Name = "", Perm = Perm.Value };
                    conf.Perms.Add(PermName);
                }

                string Name = (PermName.Name == "") ? PermName.Perm : PermName.Name;
                
                UI.Add(new CuiPanel
                {
                    Image = { Color = "1 1 1 .1", FadeIn = 0.2f },
                    RectTransform = { AnchorMin = $"{pos[0]} {pos[1]}", AnchorMax = $"{pos[2]} {pos[3]}" },
                    FadeOut = 0.2f
                }, UIPanel, UIPanel + ".box");

                if(PermName.Image!=null)
                    UI.Add(new CuiElement()
                    {
                        Parent = UIPanel + ".box",
                        Name = UIPanel + ".box.image",
                        Components =
                        {
                            new CuiRawImageComponent() { Url = PermName.Image },
                            new CuiRectTransformComponent() { AnchorMin = "0 0",AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "1 1"},
                        }
                    });

                UI.Add(new CuiElement()
                {
                    Parent = UIPanel + ".box",
                    Name = UIPanel + ".box.name",
                    Components =
                    {
                        new CuiTextComponent() { Text = $"{Name}", Align = TextAnchor.UpperCenter, Color = ColorExtensions.ToRustFormatString(conf.UIConf.NameColor) },
                        new CuiRectTransformComponent() { AnchorMin = $"0 0.5",AnchorMax = $"1 1", OffsetMin = "0 0", OffsetMax = "1 1"},
                        new CuiOutlineComponent() { Color = "0.1 0.1 0.1 0.5", Distance = "1 -1"}
                    }
                });
                UI.Add(new CuiElement()
                {
                    Parent = UIPanel + ".box",
                    Name = UIPanel + ".box.time",
                    Components =
                    {
                        new CuiTextComponent() { Text = $"{msg("Valid until",player.UserIDString)}:\n{Time}", Align = TextAnchor.LowerCenter, Color = ColorExtensions.ToRustFormatString(conf.UIConf.TimeColor) },
                        new CuiRectTransformComponent() { AnchorMin = $"0 0",AnchorMax = $"1 0.5", OffsetMin = "0 0", OffsetMax = "1 1"},
                        new CuiOutlineComponent() { Color = "0.1 0.1 0.1 0.5", Distance = "1 -1"}
                    }
                });
                colum++;
            }
            CuiHelper.AddUi(player, UI);

            if (CountPermConfig != conf.Perms.Count) SaveConfig(conf);
        }

        private void CreateUINoPerm(BasePlayer player, string UIPanel)
        {

            CuiElementContainer UI = new CuiElementContainer()
            {
                new CuiElement()
                {
                    Parent = UIPanel,
                    Name = UIPanel + ".notperms",
                    Components =
                        {
                            new CuiTextComponent() { FontSize = 36, Text = $"{msg("NotPerms",player.UserIDString)}", Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                            new CuiRectTransformComponent() { AnchorMin = "0.01 0.05", AnchorMax = ".99 0.93", OffsetMin = "0 0", OffsetMax = "1 1"},
                            new CuiOutlineComponent() { Color = "0.1 0.1 0.1 0.5", Distance = "1 -1"}
                        }
                }
            };
            CuiHelper.AddUi(player, UI);
        }
        void ShowUI(BasePlayer Player)
        {
            if (UIMainIsOpen.Contains(Player.userID))
            {
                UIMainIsOpen.Remove(Player.userID);
                CuiHelper.DestroyUi(Player, UIMain);
                return;
            }

            CreateUIMain(Player);

            List<PlayerPerms> data = new List<PlayerPerms>();
            LoadData(ref data, "TimedPermissions");

            PlayerPerms playerData = data.Find(p => p.Id == Player.UserIDString);

            if (playerData != null)
            {
                int AllCount = playerData.Groups.Count + playerData.Permissions.Count;

                CreateUIPerm(Player, playerData.Groups, UIMain + ".list", 0, AllCount);
                CreateUIPerm(Player, playerData.Permissions, UIMain + ".list", playerData.Groups.Count, AllCount);
            }
            else
            {
                CreateUINoPerm(Player, UIMain);
            }
        }

        #endregion

        #region Functions
        private float[] SquarePos(int number, double count)
        {
            Vector2 position = new Vector2(0.015f, 0.75f);
            Vector2 dimensions = new Vector2(0.10f, 0.2f);
            float offsetY = 0;
            float offsetX = 0;

            int colum = (int)Math.Floor((decimal)((1 - position.x * 2) / dimensions.x));

            int row = (int)Math.Floor((decimal)(number / colum));

            offsetY = (-0.01f - dimensions.y) * row;

            if(colum*(row + 1) > count) count = count - colum * row;

            if(count > colum) count = colum;

            position.x = (float)(1 - ((dimensions.x + .005f) * count)) / 2;

            offsetX = (.005f + dimensions.x) * (number - (row* colum));
            
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }
        private string TryForImage(string shortname, ulong skin = 99)
        {
            if (shortname.Contains("http")) return shortname;
            return GetImage(shortname, skin, true);
        }
        #endregion

        #region config
        private ConfigData conf;
        public class ConfigData
        {
            
            public MySQL MySQL = new MySQL();
            public UIConf UIConf = new UIConf();
            public List<Perms> Perms = new List<Perms>();
        }
        public class MySQL
        {
            public bool Enable = false;
            public string sql_host = "localhost";
            public int sql_port = 3306;
            public string sql_db = "rust";
            public string sql_user = "rust";
            public string sql_pass = "";
            public string tablename = "TimedPermissions";
            public float timer = 300f;
        }
        public class Perms
        {
            public string Perm;
            public string Name;
            public string Image;
        }
        public class UIConf
        {
            public string NameColor = "#FFFFFFFF";
            public string TimeColor = "#FFFFFFFF";
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            var config = new ConfigData();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            conf = Config.ReadObject<ConfigData>();
            SaveConfig(conf);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Valid until","Valid until"},
                {"Close","Close"},
                {"Title","Temporary privileges"},
                {"NotPerms","You do not have temporary variables."},
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Valid until","Действует до"},
                {"Close","Закрыть"},
                {"Title","Временные привелегии"},
                {"NotPerms","У Вас нет временных переменных"},
            }, this, "ru");
        }
        #endregion

        public static class ColorExtensions
        {
            public static string ToRustFormatString(Color color)
            {
                return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
            }
            public static string ToRustFormatString(string hexString)
            {
                return ToRustFormatString(FromHexString(hexString));
            }
            public static bool TryParseHexString(string hexString, out Color color)
            {
                try
                {
                    color = FromHexString(hexString);
                    return true;
                }
                catch
                {
                    color = Color.white;
                    return false;
                }
            }

            private static Color FromHexString(string hexString)
            {
                if (string.IsNullOrEmpty(hexString))
                {
                    throw new InvalidOperationException("Cannot convert an empty/null string.");
                }
                var trimChars = new[] { '#' };
                var str = hexString.Trim(trimChars);
                switch (str.Length)
                {
                    case 3:
                        {
                            var chArray2 = new[] { str[0], str[0], str[1], str[1], str[2], str[2], 'F', 'F' };
                            str = new string(chArray2);
                            break;
                        }
                    case 4:
                        {
                            var chArray3 = new[] { str[0], str[0], str[1], str[1], str[2], str[2], str[3], str[3] };
                            str = new string(chArray3);
                            break;
                        }
                    default:
                        if (str.Length < 6)
                        {
                            str = str.PadRight(6, '0');
                        }
                        if (str.Length < 8)
                        {
                            str = str.PadRight(8, 'F');
                        }
                        break;
                }
                var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
                var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

                return new Color32(r, g, b, a);
            }
        }

        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        private static void LoadData<T>(ref T data, string filename) => data = Interface.Oxide.DataFileSystem.ReadObject<T>(filename);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname.ToLower(), skin);
        public string GetImage(string shortname, ulong skin = 0, bool returnUrl = true) => (string)ImageLibrary.Call("GetImage", shortname.ToLower(), skin, returnUrl);
        public bool HasImage(string shortname, ulong skin = 0) => (bool)ImageLibrary.Call("HasImage", shortname.ToLower(), skin);
    }
}
