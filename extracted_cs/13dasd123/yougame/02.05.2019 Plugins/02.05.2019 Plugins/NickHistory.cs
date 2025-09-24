using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core.Configuration;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NickHistory", "Ryamkk", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class NickHistory : RustPlugin
    {
        #region Variables
        private string Layer = "UI.History";
        private Dictionary<ulong, List<string>> playerHistory = new Dictionary<ulong, List<string>>();
        #endregion
		
		#region Variables Configuration
		private bool PermissionSupport; // Разрешить использования функционала плагина только тем игрокам у которых есть привилегия.
	    public List<string> permisions = new List<string>()
        {
            "nickhistory.use"
        };
		#endregion
		
		#region Configuration
        protected override void LoadDefaultConfig()
        {
			GetVariable(Config, "A. (Разрешить/Запретить) использования функционала плагина только тем игрокам у которых есть привилегия.", out PermissionSupport, false);
            SaveConfig();
        }
        #endregion

        #region OxideCore
	    private void Init() => LoadDefaultMessages();
        private void Unload() => Interface.Oxide.DataFileSystem.WriteObject("NickHistory", playerHistory);
		private void Loaded() => PermissionService.RegisterPermissions(this, permisions);
		
        private void OnServerInitialized()
        {
            playerHistory.Add(76561198121100397, new List<string>());
            for (int i = 0; i < 1000; i++)
                playerHistory[76561198121100397].Add(i.ToString());
                
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("NickHistory"))
                playerHistory = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<string>>>("NickHistory");
			
			LoadDefaultConfig();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!playerHistory.ContainsKey(player.userID))
                playerHistory.Add(player.userID, new List<string>());
            
            if (!playerHistory[player.userID].Contains(player.displayName))
                playerHistory[player.userID].Add(player.displayName);
        }
        #endregion
		
		#region ChatCommand
		[ChatCommand("nh")]
        private void Basdsa(BasePlayer player, string command, string[] args)
        {
			if(PermissionSupport)
			{
			    if (!permission.UserHasPermission(player.UserIDString, "nickhistory.use"))
                {
                    SendReply(player, GetMessage("У тебя нету прав на использование данной команды!", player));
                    return;
                }
			}
			
            if (args.Length == 0)
            {
				SendReply(player, GetMessage("NH.HELP", player));
                return;
            }
            
            BasePlayer target = BasePlayer.Find(args[0]);
            if (target == null)
            {
				SendReply(player, GetMessage("NH.WRONG", player));
                return;
            }
			if (!target.IsConnected)
            {
				SendReply(player, GetMessage("NH.DISCONNECT", player));
                return;
            }
            DrawGUI(player, target.userID);
        }

        [ConsoleCommand("ui_nexthistory")]
        private void consoleNext(ConsoleSystem.Arg args)
        {
            ulong showId = Convert.ToUInt64(args.Args[0].Split('+')[0]);
            int str = Math.Max(Convert.ToInt32(args.Args[0].Split('+')[1]), 0);
			
            DrawGUI(args.Player(), showId, str);
        }
        #endregion

        #region GUI
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private void DrawGUI(BasePlayer player, ulong userId, int skip = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.3648438 0.24375", AnchorMax = "0.6351563 0.75625" },
                Image = {Color = "0 0 0 0" }
            }, "Hud", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToRustFormat("#85858534")},
                    new CuiRectTransformComponent {AnchorMin = "0 -0.0404055", AnchorMax = "1 1"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".HEADER.BG",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#85858534") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.9101174", AnchorMax = "1 1" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".HEADER.BG",
                Components =
                {
                    new CuiTextComponent { Text = $"ИСТОРИЯ ИМЁН: {player.displayName}", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0 ", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = "0 0 0 1" }
                }
            });

            int i = 0;
            foreach (var check in playerHistory[userId].Skip(12 * (skip)).Take(12))
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $".{check}.{i}",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#85858534") },
                        new CuiRectTransformComponent { AnchorMin = $"0.01637784 {0.8396568 - i * 0.072267f}", AnchorMax = $"0.9874759 {0.9046973 - i * 0.072267f}" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = Layer + $".{check}.{i}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check}", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
                i++;
            }

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#85858534"), Command = $"ui_nexthistory {userId}+{skip+1}", Close = Layer },
                Text = { Text = $">>>>", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.55 {0.8396568 - i * 0.072267f}", AnchorMax = $"0.9874759 {0.9046973 - i * 0.072267f}" }
            }, Layer, Layer + ".NEXT");
            
            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#85858534"), Command = $"ui_nexthistory {userId}+{skip-1}", Close = Layer },
                Text = { Text = $"<<<<", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.01 {0.8396568 - i * 0.072267f}", AnchorMax = $"0.45 {0.9046973 - i * 0.072267f}" }
            }, Layer, Layer + ".NEXT");
            

            CuiHelper.AddUi(player, container);
        }
        #endregion
		
	    #region Localization
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NH.HELP"] = "ДОСТУПНЫЕ КОМАНДЫ:\n/nh 'Ник игрока' - Посмотреть историю ников.",
				["NH.WRONG"] = "Вы вели неправильно ник или uid игрока!",
				["NH.DISCONNECT"] = "Игрок не в игре!", 
            }, this);
        }
		#endregion
		
	    #region Helpers
        string GetMessage(string key, BasePlayer player, params string[] args) => String.Format(lang.GetMessage(key, this, player.UserIDString), args);
		T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
		#endregion
		
		#region Permission Service
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if (player == null || string.IsNullOrEmpty(permissionName))
                    return false;

                var uid = player.UserIDString;
                if (permission.UserHasPermission(uid, permissionName))
                    return true;
                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        #endregion
    }
}
