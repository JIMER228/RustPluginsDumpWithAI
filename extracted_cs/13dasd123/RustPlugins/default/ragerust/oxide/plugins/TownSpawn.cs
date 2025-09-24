using System.Collections.Generic;
using System;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("TownSpawn", "Aliluya/SNAK/Alexandr/BeDLaM", "1.3.0")]

    class TownSpawn : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary, Duel, EventManager;
        private Dictionary<ulong, Timer> timerteleport = new Dictionary<ulong, Timer>();
        private List<Vector3> OutpostSpawns = new List<Vector3>();
        private List<ulong> UIButton = new List<ulong>();
        string Layer = "layer";
        string Icon = "https://i.imgur.com/YXx3oam.png";
        string FontName = "robotocondensed-bold.ttf";
        string tptotown = "teleport.to.town";
        float kd = 15f;
        string EffectPrefab = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        bool playerWounded = true;
        bool CanBleedin = true;
        bool BuldingBloked = true;
		
		#region Reference API
		private bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false; //Duel
		
		private bool CheckEvents(BasePlayer player) //EventManager
        {
            object isPlaying = EventManager?.Call("isPlaying", new object[] { player });
            if (isPlaying is bool)
            if ((bool)isPlaying)
            return true;
            return false;
        }
		
		private void JoinedEvent(BasePlayer player) //EventManager
        {
            DestroyTimer(player);
            DestroyCUI(player);
        }
        
        #endregion

        protected override void LoadDefaultConfig()
        {
            GetConfig("Настройки", "Иконка", ref Icon);
            GetConfig("Настройки", "Шрифт", ref FontName);
            GetConfig("Настройки", "Время перед телепортом в город", ref kd);
            Config.Save();
        }

        private bool GetConfig<T>(string mainMenu, string key, ref T var)
        {
            if (Config[mainMenu, key] != null)
            {
                var = Config.ConvertValue<T>(Config[mainMenu, key]);
                return false;
            }
            Config[mainMenu, key] = var;
            return true;
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyCUI(player);
                DestroyTimer(player);
            }
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyTimer(player);
            DestroyCUI(player);
        }

        void OnServerInitialized()
        {
            FindTowns();
            if (OutpostSpawns.Count == 0)
            {
                PrintError("Сейф зона не найдена на карте, плагин выгружен!");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("Плагин ImageLibrary не найден, плагин выгружен!");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            
            LoadDefaultConfig();

            lang.RegisterMessages(Messages, this, "ru");
            Messages = lang.GetMessages("ru", this);

            permission.RegisterPermission("TownSpawn.command", this);
            permission.RegisterPermission("TownSpawn.button", this);
            permission.RegisterPermission("TownSpawn.admin", this);
            
            ImageLibrary.Call("AddImage", $"{Icon}", "Image");
        }

        [ChatCommand("cancel")]
        private void TPC(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "TownSpawn.command")) return;
            DestroyTimer(player);
            SendReply(player, Messages["cancel"]);
        }

        [ChatCommand("outpost")]
        private void StartTeleportChat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "TownSpawn.command")) return;

            if (player.IsDead() || player.IsWounded())
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["dead"]);
                return;
            }

            if (player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["cold"]);
                return;
            }
            if (player.metabolism.bleeding.value > 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["damage"]);
                return;
            }
            if (BuldingBloked)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["bblock"]);
                    return;
                }
            }

            if (timerteleport.ContainsKey(player.userID))
            {
                SendReply(player, Messages["tptimer"]);
                return;
            }
                
            SendReply(player, Messages["tpinfo"], kd);
            timerteleport[player.userID] = timer.Once(kd, () =>
            {
                if (player.IsDead() || player.IsWounded())
                {
                    SendReply(player, Messages["dead"]);
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                }
                else
                {
                    SendReply(player, Messages["tpsucess"]);
                    TeleportCommand(player, OutpostSpawns.GetRandom());
                }
                timerteleport.Remove(player.userID);
            });
        }

        [ConsoleCommand("teleport.to.town")]
        private void StartTeleport(ConsoleSystem.Arg args, string town)
        {
            var player = args.Player();
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, "TownSpawn.admin") && !UIButton.Contains(player.userID)) return;
            Teleport(player, OutpostSpawns.GetRandom());
        }

        private object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (permission.UserHasPermission(player.UserIDString, "TownSpawn.button"))
            {
                if (!UIButton.Contains(player.userID))
                {
                    if (InDuel(player) || CheckEvents(player)) return null;
                    timer.Once(5f, () =>
                    {
                        DestroyTimer(player);
                        cmdDieSpawn(player);
                    });
                }
            }
            if (permission.UserHasPermission(player.UserIDString, "TownSpawn.command"))
            {
                DestroyTimer(player);
            }
            return null;
        }
        private void OnPlayerRespawned(BasePlayer player)
		{
			if (UIButton.Contains(player.userID))
			{
				UIButton.Remove(player.userID);
				NextTick(() => { CuiHelper.DestroyUi(player, Layer); });
			}
		}

        private void cmdDieSpawn(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.735 0.0156", AnchorMax = "0.84 0.278", OffsetMin = "1 1", OffsetMax = "0 0" },
                CursorEnabled = false,
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0735 0.0156", AnchorMax = "0.84 0.278" },
                Button = { Close = Layer, Command = tptotown, Color = HexToRustFormat("#AA4834FF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = "ГОРОД", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#E4DBD1FF"), Font = FontName, FontSize = 25, FadeIn = 0.5f },
            }, Layer);
            
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            if(!UIButton.Contains(player.userID))
                UIButton.Add(player.userID);
        }

        private void FindTowns()
        {
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (monument.name.Contains("compound"))
                {
                    List<BaseEntity> list = new List<BaseEntity>();
                    Vis.Entities(monument.transform.position, 25, list);
                    foreach (BaseEntity entity in list)
                    {
                        if (entity.name.Contains("chair"))
                        {
                            Vector3 chairPos = entity.transform.position;
                            chairPos.y += 1;
                            if (!OutpostSpawns.Contains(chairPos)) OutpostSpawns.Add(chairPos);
                        }
                    }
                }
            }
        }
        
        private void DestroyTimer(BasePlayer player)
        {
            if (timerteleport.ContainsKey(player.userID))
            {
                timerteleport[player.userID].Destroy();
                timerteleport.Remove(player.userID);
            }
        }

        private void DestroyCUI(BasePlayer player)
        {
            if (UIButton.Contains(player.userID))
            {
                CuiHelper.DestroyUi(player, Layer);
                UIButton.Remove(player.userID);
            }
        }

        private void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            player.Respawn();
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate();
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(); } catch { }
            player.SendFullSnapshot();
        }

        private void TeleportCommand(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate();
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(); } catch { }
            player.SendFullSnapshot();
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping()) return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"cold", "Телепорт отклонен \n<color=#F5DA81>ПРИЧИНА:</color> вам холодно" },
            {"dead", "Телепорт отклонен \n<color=#F5DA81>ПРИЧИНА:</color> вы мертвы" },
            {"damage", "Телепорт отклонен \n<color=#F5DA81>ПРИЧИНА:</color> вы получили урон" },
            {"bblock", "Телепорт отклонен \n<color=#F5DA81>ПРИЧИНА:</color> вы находитесь в зоне чужого шкафа"},
            {"tpsucess", "Вы телепортировались в город!"},
            {"cancel", "Вы отклонили телепорт!"},
            {"nocancel", "У вас нет запросов на телепортацию!"},
            {"tpinfo", "Вы телепортируетесь в город через <color=#BEF781>{0}</color> секунд! \nЧтобы отменить телепорт используйте '/cancel'"},
            {"tptimer", "Вы уже запросили телепорт в город!\nЧтобы отменить телепорт используйте '/cancel'"}

        };

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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
    }
}
