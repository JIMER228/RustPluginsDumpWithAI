// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("RockEvent", "cheese", "1.0.2")]
    public class RockEvent : RustPlugin
    {
        [PluginReference] private Plugin ComponentsEvent;
        Timer timers;
        bool EventHasStart = false;
        string WorkLayer = "RockEvent.Main";
        DateTime canceldate;
        int count;
        int allcount;
        private List<BaseEntity> SpawnedStones = new List<BaseEntity>();
        private HashSet<Tuple<Vector3, Quaternion>> _spawnData = new HashSet<Tuple<Vector3, Quaternion>>();
        private const int ScanHeight = 100;
        private static int GetBlockMask => LayerMask.GetMask("Construction", "Prevent Building", "Water");
        private static bool MaskIsBlocked(int mask) => GetBlockMask == (GetBlockMask | (1 << mask));
        private Dictionary<MonumentInfo, float> monuments { get; set; } = new Dictionary<MonumentInfo, float>();

        #region config

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public Settings MainSettings = new Settings();
            [JsonProperty("Настройки выигрышей")]
            public AccesSets AccesSettings = new AccesSets();
            [JsonProperty("Дополнительные настройки")]
            public AdditionalSettings AddSettings = new AdditionalSettings();

            public class Settings
            {
                [JsonProperty("Включить автоматичесский старт ивента?")]
                public bool AutoStartEvent = true;
                [JsonProperty("Включить ли минимальное количество игроков для старта ивента?")]
                public bool MinPlayers = true;
                [JsonProperty("Минимальное количество игроков для старта ивента")]
                public int MinPlayersCount = 5;
                [JsonProperty("Время для начала ивента после старта сервера, перезагрузки плагина (первый раз)")]
                public float FirstStartTime = 300f;
                [JsonProperty("Время для начала ивента в последующие разы (второй, третий и тд)")]
                public float RepeatTime = 86400f;
                [JsonProperty("Время до конца ивента (в минутах)")]
                public double EventDuration = 60.0;
            }
            public class AccesSets
            {
                [JsonProperty("Название пермишна для использования команды /rockevent (с приставкой RockEvent)")]
                public string StartPermission = "RockEvent.Use";
            }
            public class AdditionalSettings
            {
                [JsonProperty("Цвет выделения текста в интерфейсе")]
                public string Color = "#CF1E1E";
            }
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        #endregion

        #region Hooks

        void Unload()
        {
            StopEvent();
            InvokeHandler.Instance.CancelInvoke(StartEvent);
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, WorkLayer);
        }

        [PluginReference] private Plugin ImageLibrary;

        private void OnServerInitialized()
        {
            permission.RegisterPermission(cfg.AccesSettings.StartPermission, this);
            if (cfg.MainSettings.AutoStartEvent) InvokeHandler.Instance.InvokeRepeating(StartEvent, cfg.MainSettings.FirstStartTime, cfg.MainSettings.RepeatTime);
            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);

            _spawnData.Clear();
            SpawnedStones.Clear();
            GeneratePositions();

            ImageLibrary?.Call("AddImage", "https://imgur.com/p32BmxD.png", "rockss");

            AddCovalenceCommand("openInforock", nameof(CmdMenuOpen1rock));
            AddCovalenceCommand("closeInforock", nameof(CmdMenuClose1rock));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsNpc) return;
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (EventHasStart)
            {
                MainGUIrock(player);
            }
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (SpawnedStones.Contains(entity))
            {
                allcount -= 1;
                SpawnedStones.Remove(entity);
            }
        }

        #endregion

        #region API

        [HookMethod("RockEventIsStart")]
        public object RockEventIsStart()
        {
            if (EventHasStart) return "";
            else return null;
        }

        bool ComponentsEventIsStart()
        {
            var result = ComponentsEvent?.Call("ComponentsEventIsStart");
            if (result != null) return true;
            else return false;
        }

        #endregion

        #region Methods

        void StartEvent()
        {
            if (EventHasStart == true) return;

            if (ComponentsEventIsStart() == true)
            {
                Puts("Невозможно начать ивент так как в данный момент запущен ивент \"Фарм компонентов\"");
                EventLog("Невозможно начать ивент так как в данный момент запущен ивент \"Фарм компонентов\"");
                return;
            }

            EventHasStart = true;
            if (cfg.MainSettings.MinPlayers == true)
            {
                if (BasePlayer.activePlayerList.Count < cfg.MainSettings.MinPlayersCount)
                {
                    EventLog("Недостаточно игроков для старта ивента");
                    Puts("Недостаточно игроков для старта ивента");
                    return;
                }
            }
            canceldate = DateTime.Now.AddMinutes(cfg.MainSettings.EventDuration);
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage("Начался ивент <color=#a0c4a6>Двойной фарм</color>");
                //player.ChatMessage(Messages["StartEvent"]);
                MainGUIrock(player);
                linesrock(player);
            }

            timers = timer.Every(1f, ()=> 
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (openPanelrock.ContainsKey(player))
                    {
                        if (openPanelrock.ContainsValue(true))
                        {
                            float rocktime = TimeRockEvent();
                            timerUIrock(player, TimeSpan.FromSeconds(rocktime));
                        }
                    }
                    linesrock(player);
                }
            });

            EventLog("Ивент успешно начался!");
            Puts("Ивент успешно начался!");

            InvokeHandler.Instance.InvokeRepeating(SpawnStone, 1f, 900f);
            InvokeHandler.Instance.InvokeRepeating(StopEventRepeat, 0, 1f);
        }
        void StopEventRepeat()
        {
            int time = TimeRockEvent();
            if(time < 0)
            StopEvent();
        }

        void StopEvent()
        {
            if (EventHasStart == false) return;
            EventHasStart = false;
            InvokeHandler.Instance.CancelInvoke(SpawnStone);
            EventLog("Ивент \"Двойные камни\" остановлен или закончился!");
            Puts("Ивент \"Двойные камни\" остановлен или закончился!");
            foreach (var player in BasePlayer.activePlayerList) 
            {
                CuiHelper.DestroyUi(player, WorkLayer);
                player.ChatMessage("Закончился ивент <color=#a0c4a6>Двойной фарм</color>");

                CuiHelper.DestroyUi(player, "MainGUIrock");
                timers.Destroy();
                openPanelrock[player] = false;
            }

            Puts($"Удалено: {allcount} камней");
            foreach (var entity in SpawnedStones)
                if (!entity.IsDestroyed) entity.AdminKill();

            _spawnData.Clear();
            SpawnedStones.Clear();
            count = 0;
            allcount = 0;

        }
        private void GeneratePositions()
        {
            _spawnData.Clear();
            var generationSuccess = 0;
            var islandSize = ConVar.Server.worldsize / 2;
            for (var i = 0; i < 500 * 6; i++)
            {
                if (generationSuccess >= 500 * 2)
                {
                    break;
                }
                var x = Core.Random.Range(-islandSize, islandSize);
                var z = Core.Random.Range(-islandSize, islandSize);
                var original = new Vector3(x, ScanHeight, z);

                while (IsMonumentPosition(original) || IsOnRoad(original))
                {
                    x = Core.Random.Range(-islandSize, islandSize);
                    z = Core.Random.Range(-islandSize, islandSize);
                    original = new Vector3(x, ScanHeight, z);
                }

                var data = GetClosestValidPosition(original);
                if (data.Item1 != Vector3.zero)
                {
                    _spawnData.Add(data);
                    generationSuccess++;
                }
            }
        }

        private bool IsMonumentPosition(Vector3 target)
        {
            foreach (var monument in monuments)
            {
                if (InRange(monument.Key.transform.position, target, monument.Value))
                {
                    return true;
                }
            }

            return false;
        }
        private void SetupMonuments()
        {
            foreach (var monument in TerrainMeta.Path?.Monuments?.ToArray() ?? UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (string.IsNullOrEmpty(monument.displayPhrase.translated))
                {
                    float size = monument.name.Contains("power_sub") ? 35f : Mathf.Max(monument.Bounds.size.Max(), 75f);
                    monuments[monument] = monument.name.Contains("cave") ? 75f : monument.name.Contains("OilrigAI") ? 150f : size;
                }
                else
                {
                    monuments[monument] = GetMonumentFloat(monument.displayPhrase.translated.TrimEnd());
                }
            }
        }
        private float GetMonumentFloat(string monumentName)
        {
            switch (monumentName)
            {
                case "Abandoned Cabins":
                    return 54f;
                case "Abandoned Supermarket":
                    return 50f;
                case "Airfield":
                    return 200f;
                case "Bandit Camp":
                    return 125f;
                case "Giant Excavator Pit":
                    return 225f;
                case "Harbor":
                    return 150f;
                case "HQM Quarry":
                    return 37.5f;
                case "Large Oil Rig":
                    return 200f;
                case "Launch Site":
                    return 300f;
                case "Lighthouse":
                    return 48f;
                case "Military Tunnel":
                    return 100f;
                case "Mining Outpost":
                    return 45f;
                case "Oil Rig":
                    return 100f;
                case "Outpost":
                    return 250f;
                case "Oxum's Gas Station":
                    return 65f;
                case "Power Plant":
                    return 140f;
                case "Satellite Dish":
                    return 90f;
                case "Sewer Branch":
                    return 100f;
                case "Stone Quarry":
                    return 27.5f;
                case "Sulfur Quarry":
                    return 27.5f;
                case "The Dome":
                    return 70f;
                case "Train Yard":
                    return 150f;
                case "Water Treatment Plant":
                    return 185f;
                case "Water Well":
                    return 24f;
                case "Wild Swamp":
                    return 24f;
            }

            return 300f;
        }

        private static bool InRange(Vector3 a, Vector3 b, float distance, bool ex = true)
        {
            if (!ex)
            {
                return (a - b).sqrMagnitude <= distance * distance;
            }

            return (new Vector3(a.x, 0f, a.z) - new Vector3(b.x, 0f, b.z)).sqrMagnitude <= distance * distance;
        }

        private Tuple<Vector3, Quaternion> GetClosestValidPosition(Vector3 original)
        {
            var target = original - new Vector3(0, 200, 0);
            RaycastHit hitInfo;
            if (Physics.Linecast(original, target, out hitInfo) == false)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            var position = hitInfo.point;
            var collider = hitInfo.collider;
            var colliderLayer = 4;
            if (collider != null && collider.gameObject != null)
            {
                colliderLayer = collider.gameObject.layer;
            }

            if (collider == null)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            if (MaskIsBlocked(colliderLayer) || colliderLayer != 23)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            if (IsValidPosition(position) == false)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            var rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * Quaternion.Euler(Vector3.zero);
            return new Tuple<Vector3, Quaternion>(position, rotation);
        }

        private bool IsValidPosition(Vector3 position)
        {
            var entities = new List<BuildingBlock>();
            Vis.Entities(position, 25, entities);
            return entities.Count == 0;
        }

        bool IsOnRoad(Vector3 target)
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(target, Vector3.down, out hitInfo, 66f, LayerMask.GetMask("Terrain", "World", "Construction", "Water"), QueryTriggerInteraction.Ignore) || hitInfo.collider == null)
                return false;

            if (hitInfo.collider.name.ToLower().Contains("road"))
                return true;
            return false;
        }

        string RandomStonePrefab()
        {
            var number = Oxide.Core.Random.Range(1, 3);
            if (number == 1) return "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
            if (number == 2) return "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab";
            if (number == 3) return "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab";

            return "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
        }

        void SpawnStone()
        {
            GenerateStones();
            timer.Once(2f, () =>
            {
                Puts($"Сгенерировано: {count} камней, всего за ивент: {allcount}");
                count = 0;
            });
        }

        private int GenerateStones()
        {
            var counter = 0;
            var neededCount = 1000;
            for (var i = 0; i < neededCount; i++)
            {
                var spawnData = GetValidSpawnData();
                if (spawnData.Item1 == Vector3.zero)
                {
                    GeneratePositions();
                }
                spawnData = GetValidSpawnData();

                var stone = GameManager.server.CreateEntity(RandomStonePrefab(), spawnData.Item1, spawnData.Item2);
                stone.Spawn();
                Vector3 pos = new Vector3(2, 0, 0);
                var stone2 = GameManager.server.CreateEntity(RandomStonePrefab(), stone.GetNetworkPosition() + pos, stone.GetNetworkRotation());
                stone2.Spawn();

                SpawnedStones.Add(stone);
                SpawnedStones.Add(stone2);
                count++;
                allcount++;
                count++;
                allcount++;
            }
            return counter;
        }



        private Tuple<Vector3, Quaternion> GetValidSpawnData()
        {
            if (!_spawnData.Any())
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }
            for (var i = 0; i < 25; i++)
            {
                var number = Core.Random.Range(0, _spawnData.Count);
                var spawnData = _spawnData.ElementAt(number);
                _spawnData.Remove(spawnData);
                if (IsValidPosition(spawnData.Item1))
                    return spawnData;
            }
            return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
        }

        #endregion

        #region Commands [Команды]

        [ConsoleCommand("rockevent.start")]
        private void ConsoleForcedEventStart(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin || args.IsClientside) return;
            if (EventHasStart == true) return;

            Puts("Принудительный старт ивента \"Двойные камни\"");
            EventLog("CONSOLE запустил принудительный старт ивента \"Двойные камни\"");

            StartEvent();
        }

        [ConsoleCommand("rockevent.stop")]
        private void ConsoleForcedEventStop(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin || args.IsClientside) return;
            if (EventHasStart == false) return;
            Puts("Принудительная остановка ивента \"Двойные камни\"");
            EventLog("CONSOLE принудительно остановил ивент \"Двойные камни\"");
            StopEvent();
        }

        [ConsoleCommand("rockevent.ui.close")]
        void ConsoleUIClose(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (EventHasStart == false) return;

            CuiHelper.DestroyUi(player, "text");
            CuiHelper.DestroyUi(player, "button");
            CuiHelper.DestroyUi(player, "helptext");

            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                FadeOut = 0.1f,
                Parent = WorkLayer,
                Name = "text",
                Components =
                {
                    new CuiTextComponent { Text = ">", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.478572 0.5733339", AnchorMax = "0.5404768 0.9200007"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "rockevent.ui.open", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "text", "button");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("rockevent.ui.open")]
        void ConsoleUIOpen(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (EventHasStart == false) return;

            CuiHelper.DestroyUi(player, "text");
            CuiHelper.DestroyUi(player, "button");

            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = WorkLayer,
                Name = "helptext",
                Components =
                {
                    new CuiTextComponent { Text = $"На карте спавнятся двойные камни, собирайте их пока есть время!", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.02380949 0", AnchorMax = "1 0.5866665"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiElement
            {
                FadeOut = 0.1f,
                Parent = WorkLayer,
                Name = "text",
                Components =
                {
                    new CuiTextComponent { Text = "x", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.478572 0.5733339", AnchorMax = "0.5404768 0.9200007"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "rockevent.ui.close", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "text", "button");
            CuiHelper.AddUi(player, container);
        }

        [ChatCommand("rockevent")]
        void ChatForcedEventHandler(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, cfg.AccesSettings.StartPermission))
                return;

            if (args.Length < 1)
            {
                player.ChatMessage(" <color=#CF1E1E>/rockevent start</color> - запустить ивент Двойные камни\n <color=#CF1E1E>/rockevent stop</color> - остановить ивент Двойные камни\n <color=#CF1E1E>/rockevent tp</color> - телепортироваться к рандомному камню");
                return;
            }

            if (args[0] == "start")
            {
                if (EventHasStart == true)
                {
                    player.ChatMessage(" Ивент <color=#CF1E1E>Двойные камни</color> уже запущен");
                    return;
                }

                player.ChatMessage(" Вы успешно запустили ивент <color=#CF1E1E>Двойные камни</color>");
                Puts($"{player.displayName}/{player.userID} запустил принудительный старт ивента \"Двойные камни\"");
                EventLog($"{player.displayName}/{player.userID} запустил принудительный старт ивента \"Двойные камни\"");

                StartEvent();
            }
            if (args[0] == "stop")
            {
                if (EventHasStart == false)
                {
                    player.ChatMessage(" Ивент <color=#CF1E1E>Двойные камни</color> не был запущен");
                    return;
                }

                player.ChatMessage(" Вы успешно остановили ивент <color=#CF1E1E>Двойные камни</color>");
                Puts($"{player.displayName}/{player.userID} запустил принудительную остановку ивента \"Двойные камни\"");
                EventLog($"{player.displayName}/{player.userID} запустил принудительную остановку ивента \"Двойные камни\"");

                StopEvent();
            }

            if (args[0] == "tp")
            {
                if (EventHasStart == false)
                {
                    player.ChatMessage(" Ивент <color=#CF1E1E>Двойные камни</color> не был запущен");
                    return;
                }

                foreach (var entity in SpawnedStones)
                {
                    if (entity == null || entity.IsDestroyed) return;
                    player.Teleport(entity.transform.position);
                }
            }
        }

        #endregion

        #region Helpers

        void EventLog(string text)
        {
            LogToFile("Events", text, this, true);
        }

        #endregion

         #region GUI

        public Dictionary<BasePlayer, bool> openPanelrock = new Dictionary<BasePlayer, bool>();

        public void MainGUIrock(BasePlayer player) 
        {
            var c = new CuiElementContainer();
            UI.AddImage(ref c, "Overlay", "MainGUIrock", "0 0 0 0", "", "", "1 0.5", "1 0.5", $"-44.182 -77.661", $"-3.618 -48.735");
            UI.AddImage(ref c, "MainGUIrock", "mainrock", "0.5 0.5 0.5 0.25", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-20.282 -41.715", "20.283 -2.035");
            UI.AddRawImage(ref c, "mainrock", "rock.icons", ImageLibrary?.Call<string>("GetImage", "rockss"), "1 1 1 0.9", "", "", "0 0", "1 1", "6 7", "-7 -6");
            UI.AddImage(ref c, "MainGUIrock", "linesrock", "0 0 0 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-20.282 -41.715", "20.283 -39.185");
            UI.AddButton(ref c, "mainrock", "openrock", "openInforock", "", "0 0 0 0", "", "", "0 0", "1 1", "", "");
            CuiHelper.DestroyUi(player, "MainGUIrock");
            CuiHelper.AddUi(player, c);
        }
        public void InfoMenurock(BasePlayer player) 
        {
            var c = new CuiElementContainer();
            UI.AddImage(ref c, "MainGUIrock", "inforock", "0.5 0.5 0.5 0.25", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-223.521 -41.715", "-24.017 -2.035");
            UI.AddText(ref c, "inforock", "textrock", "1 1 1 0.9", $"По карте больше камней \nДобывайте больше ресурсов!", TextAnchor.UpperLeft, 10, "0.5 0.5", "0.5 0.5", "-94.187 -20", "91.911 6.411");
            UI.AddButton(ref c, "inforock", "closerock", "closeInforock", "", "0.70 0.00 0.00 0.8", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "85.23 5.691", "99.752 19.875");
            UI.AddText(ref c, "closerock","closesrock", "1 1 1 0.9", $"☓", TextAnchor.MiddleCenter, 10, "0.5 0.5", "0.5 0.5", "-6.737 -6.201", "6.737 6.202");
            CuiHelper.DestroyUi(player, "inforock");
            CuiHelper.AddUi(player, c);
        }
        public void timerUIrock(BasePlayer player, TimeSpan time)
        {
            var c = new CuiElementContainer();
            
            if(time.Seconds < 10)
            {
                UI.AddText(ref c, "inforock", "rock", "1 1 1 0.9", $"До конца двойного фарма [{time.Minutes}:0{time.Seconds}]", TextAnchor.UpperLeft, 12, "0.5 0.5", "0.5 0.5", "-94.187 -4.775", "85.183 19.875");
            }
            else
            {
                UI.AddText(ref c, "inforock", "rock", "1 1 1 0.9", $"До конца двойного фарма [{time.Minutes}:{time.Seconds}]", TextAnchor.UpperLeft, 12, "0.5 0.5", "0.5 0.5", "-94.187 -4.775", "85.183 19.875");
            }

            CuiHelper.DestroyUi(player, "rock");
            CuiHelper.AddUi(player, c);
        }
        public void linesrock(BasePlayer player)
        {
            var c = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "linerock");
            double timeLines = ((double)TimeRockEvent() / (cfg.MainSettings.EventDuration * 60));

            UI.AddImage(ref c, "linesrock", "linerock", "1 1 1 1", "", "assets/icons/greyout.mat", "0 0", $"{timeLines} 1", "", "");
            
            CuiHelper.AddUi(player, c);
        }

        #endregion

        #region Hooks

        private void CmdMenuOpen1rock(IPlayer user, string cmd, string[] args)
        {
            var player = user?.Object as BasePlayer;
            if (player == null) return;

            consoleOpenrock(player);
            openPanelrock[player] = true;
        }

        private void CmdMenuClose1rock(IPlayer user, string cmd, string[] args)
        {
            var player = user?.Object as BasePlayer;
            if (player == null) return;

            CuiHelper.DestroyUi(player, "inforock");
            openPanelrock[player] = false;
        }
        public void consoleOpenrock(BasePlayer player)
        {
            float rocktime = TimeRockEvent();
            InfoMenurock(player);
            timerUIrock(player, TimeSpan.FromSeconds(rocktime));
        }

        #endregion

        #region Support

        private int TimeRockEvent()
        {
            TimeSpan timetocancel = DateTime.Now - canceldate;
            var pp = (int)timetocancel.TotalSeconds * -1;
            return pp;
        }

        private static string FormatTime(TimeSpan time)
        {
            return ($"{FormatMinutes(time.Minutes)}:{FormatSeconds(time.Seconds)}");
        }
        private static string FormatMinutes(int minutes) => FormatUnits2(minutes);

        private static string FormatSeconds(int seconds) => FormatUnits(seconds);

        private static string FormatUnits2(int units)
        {
            var tmp = units % 10;

            if (units >= 10)
                return $"{units}";

            if (units >= 0 && units <= 10)
                return $"{units}";

            return $"{units}";
        }

        private static string FormatUnits(int units)
        {
            var tmp = units % 10;

            if (units >= 10)
                return $"{units}";

            if (units >= 0 && units <= 10)
                return $"0{units}";

            return $"0{units}";
        }

        public static class UI
        {
            public static void AddImage(ref CuiElementContainer container, string parrent, string name, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax, string outline = "", string dist = "")
            {
                if (string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                        {
                            new CuiImageComponent{Color = color, Material = "assets/icons/greyout.mat"},
                            new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                        }
                    });

                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiImageComponent{Color = color},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });
            }

            public static void AddRawImage(ref CuiElementContainer container, string parrent, string name, string png, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax)
            {
                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiRawImageComponent{Color = color, Png = png},
                        new CuiOutlineComponent { Color = "0 0 0 0", Distance = "0 0"},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });
            }

            public static void AddText(ref CuiElementContainer container, string parrent, string name, string color, string text, TextAnchor align, int size, string aMin, string aMax, string oMin, string oMax, string outColor = "0 0 0 0", string font = "robotocondensed-bold.ttf", string dist = "0.5 0.5", float FadeIN = 0f, float FadeOut = 0f)
            {
                container.Add(new CuiElement()
                {
                    Parent = parrent,
                    Name = name,
                    FadeOut = FadeOut,
                    Components =
                    {
                        new CuiTextComponent{Color = color,Text = text, Align = align, FontSize = size, Font = font, FadeIn = FadeIN},
                        new CuiOutlineComponent{Color = outColor, Distance = dist},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                });

            }

            public static void AddButton(ref CuiElementContainer container, string parrent, string name, string cmd, string close, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax, string outline = "", string dist = "")
            {
                if (!string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                        {
                            new CuiButtonComponent{Command = cmd, Color = color, Close = close, Sprite = sprite, Material = "assets/icons/greyout.mat", },
                            new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                        }
                    });

                if (!string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat) && !string.IsNullOrEmpty(outline))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                        {
                            new CuiButtonComponent{Command = cmd, Color = color, Close = close, Sprite = sprite, Material = "assets/icons/greyout.mat", },
                            new CuiOutlineComponent{Color = outline, Distance = dist},
                            new CuiRectTransformComponent{ AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax }
                        }
                    });

                if (string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiButtonComponent{Command = cmd, Color = color, Close = close, Material = "assets/icons/greyout.mat", },
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });

                if (!string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiButtonComponent{Command = cmd, Color = color, Close = close, Sprite = sprite},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });

                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiButtonComponent{Command = cmd, Color = color, Close = close, },
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });
            }
        }

        #endregion
    }
}