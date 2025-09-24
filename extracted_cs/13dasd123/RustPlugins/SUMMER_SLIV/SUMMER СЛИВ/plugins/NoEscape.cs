// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("NoEscape", "https://topplugin.ru/", "3.2.1")]
    public class NoEscape : RustPlugin
    {
        [PluginReference]
        private Plugin Friends;

        public WItem DefaultBlock = new WItem("Ваш", "Строительный блок");
        
        public Dictionary<string, WItem> InfoBlocks = new Dictionary<string, WItem>()
        {
            {"floor.grill", new WItem("Ваш", "Решетчатый настил")},
            {"floor.triangle.grill", new WItem("Ваш", "Треугольный решетчатый настил")},
            {"door.hinged.toptier", new WItem("Вашу", "Бронированную дверь")},
            {"door.double.hinged.toptier", new WItem("Вашу", "Двойную бронированную дверь")},
            {"gates.external.high.stone", new WItem("Ваши", "Высокие внешние каменные ворота")},
            {"wall.external.high.stone", new WItem("Вашу", "Высокую внешнюю каменную стену")},
            {"gates.external.high.wood", new WItem("Ваши", "Высокие внешние деревянные ворота")},
            {"wall.external.high", new WItem("Вашу", "Высокую внешнюю деревянную стену")},
            {"floor.ladder.hatch", new WItem("Ваш", "Люк с лестницей")},
            {"floor.triangle.ladder.hatch", new WItem("Ваш", "Треугольный люк с лестницей")},
            {"shutter.metal.embrasure.a", new WItem("Вашу", "Металлическую горизонтальную бойницу")},

            {"shutter.metal.embrasure.b", new WItem("Вашу", "Металлическую вертикальную бойницу")},
            {"wall.window.bars.metal", new WItem("Ваши", "Металлические оконные решетки")},
            {"wall.frame.cell.gate", new WItem("Вашу", "Тюремную дверь")},
            {"wall.frame.cell", new WItem("Вашу", "Тюремную решетку")},
            {"wall.window.bars.toptier", new WItem("Ваши", "Укрепленные оконные решетки")},

            {"wall.window.glass.reinforced", new WItem("Ваше", "Укрепленное оконное стекло")},

            {"door.hinged.metal", new WItem("Вашу", "Металлическую дверь")},
            {"door.double.hinged.metal", new WItem("Вашу", "Двойную металлическую дверь")},
            {"door.hinged.wood", new WItem("Вашу", "Деревянную дверь")},
            {"door.double.hinged.wood", new WItem("Вашу", "Двойную деревянную дверь")},
            {"wall.frame.garagedoor", new WItem("Вашу", "Гаражную дверь")},
            {"wall.frame.shopfront.metal", new WItem("Вашу", "Металлическую витрину магазина")},

            {"Wood,foundation.triangle", new WItem("Ваш", "Деревянный треугольный фундамент")},
            {"Stone,foundation.triangle", new WItem("Ваш", "Каменный треугольный фундамент")},
            {"Metal,foundation.triangle", new WItem("Ваш", "Металлический треугольный фундамент")},
            {"TopTier,foundation.triangle", new WItem("Ваш", "Бронированный треугольный фундамент")},

            {"Wood,foundation.steps", new WItem("Ваши", "Деревянные ступеньки для фундамента")},
            {"Stone,foundation.steps", new WItem("Ваши", "Каменные ступеньки для фундамента")},
            {"Metal,foundation.steps", new WItem("Ваши", "Металлические ступеньки для фундамента")},
            {"TopTier,foundation.steps", new WItem("Ваши", "Бронированные ступеньки для фундамента")},

            {"Wood,foundation", new WItem("Ваш", "Деревянный фундамент")},
            {"Stone,foundation", new WItem("Ваш", "Каменный фундамент")},
            {"Metal,foundation", new WItem("Ваш", "Металлический фундамент")},
            {"TopTier,foundation", new WItem("Ваш", "Бронированный фундамент")},

            {"Wood,wall.frame", new WItem("Ваш", "Деревянный настенный каркас")},
            {"Stone,wall.frame", new WItem("Ваш", "Каменный настенный каркас")},
            {"Metal,wall.frame", new WItem("Ваш", "Металлический настенный каркас")},
            {"TopTier,wall.frame", new WItem("Ваш", "Бронированный настенный каркас")},

            {"Wood,wall.window", new WItem("Ваш", "Деревянный оконный проём")},
            {"Stone,wall.window", new WItem("Ваш", "Каменный оконный проём")},
            {"Metal,wall.window", new WItem("Ваш", "Металлический оконный проём")},
            {"TopTier,wall.window", new WItem("Ваш", "Бронированный оконный проём")},

            {"Wood,wall.doorway", new WItem("Ваш", "Деревянный дверной проём")},
            {"Stone,wall.doorway", new WItem("Ваш", "Каменный дверной проём")},
            {"Metal,wall.doorway", new WItem("Ваш", "Металлический дверной проём")},
            {"TopTier,wall.doorway", new WItem("Ваш", "Бронированный дверной проём")},

            {"Wood,wall", new WItem("Вашу", "Деревянную стену")},
            {"Stone,wall", new WItem("Вашу", "Каменную стену")},
            {"Metal,wall", new WItem("Вашу", "Металлическую стену")},
            {"TopTier,wall", new WItem("Вашу", "Бронированную стену")},

            {"Wood,floor.frame", new WItem("Ваш", "Деревянный потолочный каркас")},
            {"Stone,floor.frame", new WItem("Ваш", "Каменный потолочный каркас")},
            {"Metal,floor.frame", new WItem("Ваш", "Металлический потолочный каркас")},
            {"TopTier,floor.frame", new WItem("Ваш", "Бронированный потолочный каркас")},

            {"Wood,floor.triangle.frame", new WItem("Ваш", "Деревянный треугольный потолочный каркас")},
            {"Stone,floor.triangle.frame", new WItem("Ваш", "Каменный треугольный потолочный каркас")},
            {"Metal,floor.triangle.frame", new WItem("Ваш", "Металлический треугольный потолочный каркас")},
            {"TopTier,floor.triangle.frame", new WItem("Ваш", "Бронированный треугольный потолочный каркас")},

            {"Wood,floor.triangle", new WItem("Ваш", "Деревянный треугольный потолок")},
            {"Stone,floor.triangle", new WItem("Ваш", "Каменный треугольный потолок")},
            {"Metal,floor.triangle", new WItem("Ваш", "Металлический треугольный потолок")},
            {"TopTier,floor.triangle", new WItem("Ваш", "Бронированный треугольный потолок")},

            {"Wood,floor", new WItem("Ваш", "Деревянный потолок")},
            {"Stone,floor", new WItem("Ваш", "Каменный потолок")},
            {"Metal,floor", new WItem("Ваш", "Металлический потолок")},
            {"TopTier,floor", new WItem("Ваш", "Бронированный потолок")},

            {"Wood,roof", new WItem("Вашу", "Деревянную крышу")},
            {"Stone,roof", new WItem("Вашу", "Каменную крышу")},
            {"Metal,roof", new WItem("Вашу", "Металлическую крышу")},
            {"TopTier,roof", new WItem("Вашу", "Бронированную крышу")},

            {"Wood,roof.triangle", new WItem("Вашу", "Деревянную треугольную крышу")},
            {"Stone,roof.triangle", new WItem("Вашу", "Каменную треугольную крышу")},
            {"Metal,roof.triangle", new WItem("Вашу", "Металлическую треугольную крышу")},
            {"TopTier,roof.triangle", new WItem("Вашу", "Бронированную треугольную крышу")},

            {"Wood,block.stair.lshape", new WItem("Вашу", "Деревянную лестницу")},
            {"Stone,block.stair.lshape", new WItem("Вашу", "Каменную лестницу")},
            {"Metal,block.stair.lshape", new WItem("Вашу", "Металлическую лестницу")},
            {"TopTier,block.stair.lshape", new WItem("Вашу", "Бронированную лестницу")},

            {"Wood,block.stair.ushape", new WItem("Вашу", "Деревянную лестницу")},
            {"Stone,block.stair.ushape", new WItem("Вашу", "Каменную лестницу")},
            {"Metal,block.stair.ushape", new WItem("Вашу", "Металлическую лестницу")},
            {"TopTier,block.stair.ushape", new WItem("Вашу", "Бронированную лестницу")},

            {"Wood,block.stair.spiral", new WItem("Вашу", "Деревянную спиральную лестницу")},
            {"Stone,block.stair.spiral", new WItem("Вашу", "Каменную спиральную лестницу")},
            {"Metal,block.stair.spiral", new WItem("Вашу", "Металлическую спиральную лестницу")},
            {"TopTier,block.stair.spiral", new WItem("Вашу", "Бронированную спиральную лестницу")},

            {"Wood,block.stair.spiral.triangle", new WItem("Вашу", "Деревянную треугольную спиральную лестницу")},
            {"Stone,block.stair.spiral.triangle", new WItem("Вашу", "Каменную треугольную спиральную лестницу")},
            {"Metal,block.stair.spiral.triangle", new WItem("Вашу", "Металлическую треугольную спиральную лестницу")},
            {"TopTier,block.stair.spiral.triangle", new WItem("Вашу", "Бронированную треугольную спиральную лестницу")},

            {"Wood,pillar", new WItem("Вашу", "Деревянную опору")},
            {"Stone,pillar", new WItem("Вашу", "Каменную опору")},
            {"Metal,pillar", new WItem("Вашу", "Металлическую опору")},
            {"TopTier,pillar", new WItem("Вашу", "Бронированную опору")},

            {"Wood,wall.low", new WItem("Вашу", "Деревянную низкую стену")},
            {"Stone,wall.low", new WItem("Вашу", "Каменную низкую стену")},
            {"Metal,wall.low", new WItem("Вашу", "Металлическую низкую стену")},
            {"TopTier,wall.low", new WItem("Вашу", "Бронированную низкую стену")},

            {"Wood,wall.half", new WItem("Вашу", "Деревянную полустенку")},
            {"Stone,wall.half", new WItem("Вашу", "Каменную полустенку")},
            {"Metal,wall.half", new WItem("Вашу", "Металлическую полустенку")},
            {"TopTier,wall.half", new WItem("Вашу", "Бронированную полустенку")},

            {"Wood,ramp", new WItem("Ваш", "Деревянный скат")},
            {"Stone,ramp", new WItem("Ваш", "Каменный скат")},
            {"Metal,ramp", new WItem("Ваш", "Металлический скат")},
            {"TopTier,ramp", new WItem("Ваш", "Бронированный скат")}
        };
        
        public class WItem
        {
            public string pre;
            public string name;
            public WItem(string pre, string name)
            {
                this.pre = pre;
                this.name = name;
            }
        }
        
        #region Class
        private static List<SphereComponent> BlockerList = new List<SphereComponent>();

        private class PlayerBlockStatus : FacepunchBehaviour
        {
            private BasePlayer Player;
            public SphereComponent CurrentBlocker;
            public double CurrentTime = config.BlockSettings.BlockLength;

            public static PlayerBlockStatus Get(BasePlayer player)
            {
                return player.GetComponent<PlayerBlockStatus>() ?? player.gameObject.AddComponent<PlayerBlockStatus>();
            }

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
            }

            private void ControllerUpdate()
            {
                if (CurrentBlocker != null)
                    UpdateUI();
                else
                    UnblockPlayer();
            }

            public void CreateUI()
            {
                CuiHelper.DestroyUi(Player, "NoEscape");
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = config.UISettings.AnchorMin, AnchorMax = config.UISettings.AnchorMax, OffsetMax = "0 0" },
                    Image = { Color = config.UISettings.InterfaceColorBP }
                }, "Hud", "NoEscape");
                CuiHelper.AddUi(Player, container);
                if (CurrentBlocker != null) UpdateUI();
            }

            public void BlockPlayer(SphereComponent blocker, bool justCreated)
            {
                if (ins.permission.UserHasPermission(Player.UserIDString, config.BlockSettings.PermissionToIgnore))
                {
                    UnblockPlayer();
                    return;
                }
                if (justCreated)
                    Player.ChatMessage(string.Format(ins.Messages["blockactiveAttacker"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength)));
                CurrentBlocker = blocker;
                CurrentTime = CurrentBlocker.CurrentTime;
                CreateUI();
                InvokeRepeating(ControllerUpdate, 1f, 1f);
            }

            public void UpdateUI()
            {
                CurrentTime++;
                CuiHelper.DestroyUi(Player, "NoEscape_update");
                CuiHelper.DestroyUi(Player, "NoEscape" + ".Info");

                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = "NoEscape",
                    Name = "NoEscape_update",
                    Components =
                    {
                        new CuiImageComponent { Color = config.UISettings.InterfaceColor },
                        new CuiRectTransformComponent {AnchorMin = $"0 0", AnchorMax = $"{(float) (CurrentBlocker.TotalTime - CurrentTime) / CurrentBlocker.TotalTime} 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = string.Format(ins.Messages["guitimertext"], ins.GetFormatTime(TimeSpan.FromSeconds(CurrentBlocker.TotalTime - CurrentTime))), Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.9", FontSize = 16, Align = TextAnchor.MiddleCenter }
                }, "NoEscape", "NoEscape" + ".Info");

                CuiHelper.AddUi(Player, container);
                if (CurrentTime >= config.BlockSettings.BlockLength)
                    UnblockPlayer();
            }

            public void UnblockPlayer()
            {
                if (Player == null)
                {
                    Destroy(this);
                    return;
                }
                Player.ChatMessage(ins.Messages["blocksuccess"]);
                CancelInvoke(ControllerUpdate);
                CuiHelper.DestroyUi(Player, "NoEscape");
                CurrentBlocker = null;
            }
            private void OnDestroy()
            {
                CuiHelper.DestroyUi(Player, "NoEscape");
                Destroy(this);
            }
        }

        public class SphereComponent : FacepunchBehaviour
        {
            SphereCollider sphereCollider;
            public BasePlayer initPlayer;
            public List<ulong> Privilage = null;
            public ulong OwnerID;
            public double CurrentTime = 0;
            public double TotalTime = config.BlockSettings.BlockLength;
            void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = config.BlockSettings.BlockerDistance;
            }

            public void Init(BasePlayer player, ulong owner, List<ulong> privilage)
            {
                initPlayer = player;
                OwnerID = owner;
                Privilage = privilage;
            }

            private void OnTriggerEnter(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target == null) return;

                if (PlayerBlockStatus.Get(target).CurrentBlocker != null && PlayerBlockStatus.Get(target).CurrentBlocker == this && PlayerBlockStatus.Get(target).CurrentTime > CurrentTime)
                {
                    PlayerBlockStatus.Get(target).CurrentTime = CurrentTime;
                    return;
                }
                if (PlayerBlockStatus.Get(target).CurrentBlocker != null && PlayerBlockStatus.Get(target).CurrentBlocker != this && PlayerBlockStatus.Get(target).CurrentTime > CurrentTime)
                {
                    target.ChatMessage(string.Format(ins.Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - CurrentTime)));
                    PlayerBlockStatus.Get(target).CurrentTime = CurrentTime;
                    PlayerBlockStatus.Get(target).CurrentBlocker = this;
                    return;
                }
                if (config.BlockSettings.ShouldBlockEnter && (PlayerBlockStatus.Get(target).CurrentBlocker == null || PlayerBlockStatus.Get(target).CurrentBlocker != this))
                {
                    PlayerBlockStatus.Get(target).BlockPlayer(this, false);
                    target.ChatMessage(string.Format(ins.Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - CurrentTime)));
                    return;
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (!config.BlockSettings.UnBlockExit) return;
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && target.userID.IsSteamId() && PlayerBlockStatus.Get(target).CurrentBlocker == this)
                    PlayerBlockStatus.Get(target).UnblockPlayer();
            }

            public void FixedUpdate()
            {
                CurrentTime += Time.deltaTime;
                if (CurrentTime > TotalTime)
                {
                    if (BlockerList.Contains(this))
                        BlockerList.Remove(this);
                    Destroy(this);
                }
            }

            public void OnDestroy()
            {
                Destroy(this);
            }

            public bool IsInBlocker(BaseEntity player) => Vector3.Distance(player.transform.position, transform.position) < config.BlockSettings.BlockerDistance;
        }
        #endregion

        #region Variables

        static PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина на сайте TopPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        void Loaded()
        {
            if (!config.PlayerBlockSettings.CanRepair) Unsubscribe(nameof(OnStructureRepair));
            else Subscribe(nameof(OnStructureRepair));
            if (!config.PlayerBlockSettings.CanUpgrade) Unsubscribe(nameof(CanAffordUpgrade));
            else Subscribe(nameof(CanAffordUpgrade));
            if (!config.PlayerBlockSettings.CanDefaultremove) Unsubscribe(nameof(OnStructureDemolish));
            else Subscribe(nameof(OnStructureDemolish));
            if (!config.PlayerBlockSettings.CanBuild && !config.PlayerBlockSettings.CanPlaceObjects) Unsubscribe(nameof(CanBuild));
            else Subscribe(nameof(CanBuild));
            permission.RegisterPermission(config.BlockSettings.PermissionToIgnore, this);
            //permission.RegisterPermission(config.VkBotMessages.VkPrivilage, this);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            LoadVKData();
        }

        public void LoadVKData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Vk/Data"))
            {
                baza = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("Vk/Data");
            }
            else
            {
                PrintWarning($"Error reading config, creating one new data!");
                baza = new Dictionary<ulong, string>();
            }

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Vk/Names"))
            {
                _PlayerNicknames = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("Vk/Names");
            }
            else
                _PlayerNicknames = new Dictionary<ulong, string>();

        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config update detected! Updating config values...");

                if (config.PluginVersion < new VersionNumber(2, 2, 0))
                {
                    config.BlockSettings.WriteListDestroyEntity = new List<string>()
                    {
                        "barricade.metal",
                         "bed_deployed"
                    };
                    PrintWarning("Added Write List entity");
                }
                if (config.PluginVersion < new VersionNumber(2, 3, 1))
                {
                    config.PlayerBlockSettings.BlackListCommands = new List<string>()
                    {
                        "/bp",
                        "backpack.open",
                        "/trade"
                    };

                    PrintWarning("Added Black List commands");
                }
                PrintWarning("Config update completed!");
                config.PluginVersion = Version;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        
        public string NotTranslatedObject = "неизвестный объект";

        public class UISettings
        {
            [JsonProperty("Цвет полосы активный полосы")]
            public string InterfaceColor = "0.121568628 0.419607848 0.627451 0.784313738";

            [JsonProperty("Цвет фона")]
            public string InterfaceColorBP = "1 1 1 0.3";

            [JsonProperty("Позиция AnchorMin")]
            public string AnchorMin = "0.3447913 0.112037";

            [JsonProperty("Позиция AnchorMax")]
            public string AnchorMax = "0.640625 0.1398148";
        }

        public class BlockSettings
        {
            [JsonProperty("Время блокировки при нанесение урона по игрокам (Блокировка инициатора и жертвы)")]
            
            public int blockAttackTime = 10;
            
            [JsonProperty("Блокировать игроков при нанесение урона (Блокировка инициатора и жертвы)")]
            public bool blockAttack = false;
            
            [JsonProperty("Радиус зоны блокировки")]
            public float BlockerDistance = 150;

            [JsonProperty("Общее время блокировки в секундах")]
            public float BlockLength = 150;

            [JsonProperty("Блокировать создателя объекта какой разрушили, даже если он вне зоны рейда")]
            public bool BlockOwnersIfNotInZone = true;

            [JsonProperty("Блокировать игрока, который вошёл в активную зону блокировки")]
            public bool ShouldBlockEnter = true;

            [JsonProperty("Снимать блокировку с игрока если он вышел из зоны блокировки?")]
            public bool UnBlockExit = false;

            [JsonProperty("Не создавать блокировку если разрушенный объект не в зоне шкафа (Нету билды)")]
            public bool EnabledBuildingBlock = false;

            [JsonProperty("Блокировать всех игроков какие авторизаваны в шкафу (Если шкаф существует, и авторизованный игрок на сервере)")]
            public bool EnabledBlockAutCupboard = false;

            [JsonProperty("Привилегия, игроки с которой игнорируются РБ (на них он не действует")]
            public string PermissionToIgnore = "noescape.ignore";

            [JsonProperty("Белый список entity при разрушении каких не действует блокировка")]
            public List<string> WriteListDestroyEntity = new List<string>();
        }
        public class SenderConfig
        {
            [JsonProperty("Название сервера отправки сообщений в VK")]
            public string ServerName;

            [JsonProperty("Настройки отправки сообщений в VK")]
            public VkSettings VK = new VkSettings();

            [JsonProperty("Оповещения о начале рейда (%OBJECT%, %INITIATOR%, %SQUARE%, %SERVER%)")]
            public List<string> StartRaidMessages = new List<string>();
            
            [JsonProperty("Оповещения об убийстве, когда игрок не в сети")]
            public List<string> KillMessage = new List<string>();
        }

        public class VkSettings
        {
            [JsonProperty("Включить отправку сообщения в ВК оффлайн игроку")]
            public bool EnabledVk = false;
            [JsonProperty("Access токен группы ВК с правом отправки сообщений")]
            public string VKAccess = "Вставьте сюда токен для отправки сообщений в вк";
        }

        public class PlayerBlockSettings
        {
            [JsonProperty("Блокировать использование китов")]
            public bool CanUseKits = true;

            [JsonProperty("Блокировать обмен между игроками (Trade)")]
            public bool CanUseTrade = true;

            [JsonProperty("Блокировать телепорты")]
            public bool CanTeleport = true;

            [JsonProperty("Блокировать удаление построек (CanRemove)")]
            public bool CanRemove = true;

            [JsonProperty("Блокировать улучшение построек (Upgrade, BuildingUpgrade и прочее)")]
            public bool CanBGrade = true;

            [JsonProperty("Блокировать удаление построек (стандартное)")]
            public bool CanDefaultremove = true;

            [JsonProperty("Блокировать строительство")]
            public bool CanBuild = true;

            [JsonProperty("Блокировать установку объектов")]
            public bool CanPlaceObjects = true;

            [JsonProperty("Блокировать ремонт построек (стандартный)")]
            public bool CanRepair = true;

            [JsonProperty("Блокировать улучшение построек (стандартное)")]
            public bool CanUpgrade = true;

            [JsonProperty("Белый список предметов какие можно строить при блокировке")]
            public List<string> WriteListBuildEntity = new List<string>();

            [JsonProperty("Черный список команд какие запрещены при рейд блоке (Чатовые и консольные)")]
            public List<string> BlackListCommands = new List<string>();

        }

        private class PluginConfig
        {
            [JsonProperty("Настройка UI")]
            public UISettings UISettings = new UISettings();

            [JsonProperty("Общая настройка блокировки")]
            public BlockSettings BlockSettings = new BlockSettings();

            [JsonProperty("Настройка запретов для игрока")]
            public PlayerBlockSettings PlayerBlockSettings = new PlayerBlockSettings();

            [JsonProperty("Настройка отправки сообщений")]
            public SenderConfig Sender = new SenderConfig();

            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonIgnore]
            [JsonProperty("Инициализация плагина⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
            public bool Init = false;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    BlockSettings = new BlockSettings()
                    {
                        blockAttackTime = 10,
                        blockAttack = false,
                        BlockerDistance = 150,
                        BlockLength = 150,
                        BlockOwnersIfNotInZone = true,
                        ShouldBlockEnter = true,
                        UnBlockExit = false,
                        EnabledBuildingBlock = false,
                        EnabledBlockAutCupboard = false,
                        PermissionToIgnore = "noescape.ignore",
                        WriteListDestroyEntity = new List<string>()
                        {
                            "barricade.metal",
                            "bed_deployed"
                        }
                    },
                    PlayerBlockSettings = new PlayerBlockSettings()
                    {
                        CanUseKits = true,
                        CanUseTrade = true,
                        CanTeleport = true,
                        CanRemove = true,
                        CanBGrade = true,
                        CanDefaultremove = true,
                        CanBuild = true,
                        CanPlaceObjects = true,
                        CanRepair = true,
                        CanUpgrade = true,
                        WriteListBuildEntity = new List<string>()
                        {
                             "wall.external.high.stone",
                             "barricade.metal"
                        }
                    },
                    UISettings = new UISettings()
                    {
                        InterfaceColor = "0.12 0.41 0.62 0.78",
                        InterfaceColorBP = "1 1 1 0.3",
                        AnchorMin = "0.3447913 0.112037",
                        AnchorMax = "0.640625 0.1398148",
                    },
                    Sender = new SenderConfig()
                    {
                        ServerName = "SUMMER RUST",
                        VK = new VkSettings()
                        {
                            EnabledVk = false,
                            VKAccess = "Вставьте сюда токен для отправки сообщений в вк"
                        },
                        StartRaidMessages = new List<string>()
                        {
                            "💣 Прекрасен звук поломанных строений. %OBJECT% в квадрате %SQUARE% была раздолбана игроком %INITIATOR%. Залетайте на %SERVER% и настучите ему по голове, чтоб знал куда полез!",
                            "🔥 Произошел рейд! %OBJECT% пол в квадрате %SQUARE% был выпилен игроком %INITIATOR%. Залетайте на %SERVER% и настучите ему по голове, чтоб знал куда полез.",
                            "⚠ Рота, подъём! %OBJECT% в квадрате %SQUARE% была уничтожена игроком %INITIATOR%. Коннект ту %SERVER% и скажите ему, что он поступает плохо.",
                            "💥 ВЖУХ! Вас рейдят! %OBJECT% в квадрате %SQUARE% был раздолбан игроком %INITIATOR%. Срочно заходите на %SERVER% и зарейдите его в ответ.",
                            "💥 Бывают в жизни огорчения. %OBJECT% в квадрате %SQUARE% был раздолбан игроком %INITIATOR%. Залетайте на %SERVER% и попробуйте разрулить ситуацию.",
                            "💣 Очередной оффлайн рейд, ничего нового. %OBJECT% в квадрате %SQUARE% был выпилен игроком %INITIATOR%. Заходите на %SERVER%, крикните в микрофон и он убежит от испуга :)",
                            "💥 Отложите свои дела, %OBJECT% в квадрате %SQUARE% был раздолбан игроком %INITIATOR%. Скорее на %SERVER% и вежливо попросите его прекратить это дело.",
                            "💥 Это не реклама, это не спам, %OBJECT% в квадрате %SQUARE% была расхреначена игроком %INITIATOR%. Скорее на %SERVER%, может быть ещё не поздно.",
                            "💥 Подъём, нападение! %OBJECT% в квадрате %SQUARE% был разрушен игроком %INITIATOR%. Срочно заходите на %SERVER% и настучите ему по голове, чтоб знал куда полез.",
                            "🔥 Нам жаль, но %OBJECT% в квадрате %SQUARE% была сломана игроком %INITIATOR%. Скорее на %SERVER%, крикните в микрофон и он убежит от испуга :)",
                            "💣 Пока Вас не было, %OBJECT% в квадрате %SQUARE% была разрушена игроком %INITIATOR%. Срочно заходите на %SERVER%, пока Вам ещё что-то не сломали.",
                            "💣 Плохие новости. %OBJECT% в квадрате %SQUARE% была демонтирована игроком %INITIATOR%. Бегом на %SERVER% и настучите ему по голове, чтоб знал куда полез.",
                            "💣 Он добрался и до Вас! %OBJECT% в квадрате %SQUARE% был демонтирован игроком %INITIATOR%. Срочно заходите на %SERVER% и скажите ему, что он ошибся дверью.",
                            "💥 Рейдят! %OBJECT% в квадрате %SQUARE% была вынесена игроком %INITIATOR%. Пулей летите на %SERVER%, крикните в микрофон и он убежит от испуга :)"
                        },
                        KillMessage = new List<string>()
                        {
                            "💀 Ох, как нехорошо получилось. Там на %SERVER% игрок %KILLER% отправил Вас в мир мёртвых.",
                            "🔪 Живой? Нет! А всё потому что на %SERVER% игрок %KILLER% убрал Вас со своего пути.",
                            "🔪 Пока Вы спали, на %SERVER% игрок %KILLER% проверил, бессмертны ли Вы. Результат не очень весёлый.",
                            "🔪 Кому-то Вы дорогу перешли. На %SERVER% игрок %KILLER% отправил Вас в мир мёртвых.",
                            "🔫 Кому-то Вы дорогу перешли. На %SERVER% игрок %KILLER% решил, что Вы не должны существовать.",
                            "🔫 Плохи дела... На %SERVER% игрок %KILLER% отправил Вас в мир мёртвых.",
                            "💀 Ой, а кто-то больше не проснётся? На %SERVER% игрок %KILLER% оборвал Вашу жизнь.",
                            "💀 Вы хорошо жили, но потом на %SERVER% игрок %KILLER% забил Вас до смерти.",
                            "☠ Всё было хорошо, но потом на  %SERVER% игрок %KILLER% убил Вас."
                        }, 
                    },
                    PluginVersion = new VersionNumber(),
                };
            }
        }

        #endregion

        #region Oxide
        private static NoEscape ins;
        private void OnServerInitialized()
        {
            ins = this;
            config.Init = true;
            LoadData();
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerInit);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            SphereComponent ActiveRaidZone = GetRaidZone(player.transform.position);
            if (ActiveRaidZone == null) return;
            if (PlayerBlockStatus.Get(player).CurrentBlocker != null)
            {
                if (PlayerBlockStatus.Get(player).CurrentBlocker != ActiveRaidZone)
                    PlayerBlockStatus.Get(player).BlockPlayer(ActiveRaidZone, false);
            }
            else
            {
                player.ChatMessage(string.Format(Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - ActiveRaidZone.CurrentTime)));
                PlayerBlockStatus.Get(player)?.BlockPlayer(ActiveRaidZone, false);
            }
        }
        
        Dictionary<ulong, double> timers = new Dictionary<ulong, double>();

        public static int GetCooldown(BasePlayer player, string key)
        {
            List<Cooldown> source = new List<Cooldown>();
            if (cooldowns.TryGetValue(key, out source))
            {
                Cooldown cooldown = source.FirstOrDefault<Cooldown>((Func<Cooldown, bool>)(p => (long)p.UserId == (long)player.userID));
                if (cooldown != null)
                    return (int)(cooldown.Expired - GrabCurrentTime());
            }
            return 0;
        }
        
        #region Cooldown

        DynamicConfigFile cooldownsFile = Interface.Oxide.DataFileSystem.GetFile("AttackCooldown");

        private class Cooldown
        {
            public ulong UserId;
            public double Expired;
            [JsonIgnore]
            public Action OnExpired;
        }
        private static Dictionary<string, List<Cooldown>> cooldowns;
        
        public static void SetCooldown(BasePlayer player, string key, int seconds, Action onExpired = null)
        {
            List<Cooldown> cooldownList;
            if (!cooldowns.TryGetValue(key, out cooldownList))
                cooldowns[key] = cooldownList = new List<Cooldown>();
            cooldownList.Add(new Cooldown()
            {
                UserId = player.userID,
                Expired = GrabCurrentTime() + (double)seconds,
                OnExpired = onExpired
            });
        }
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        #endregion
        
        #region DATA
        void OnServerSave()
        {
            cooldownsFile.WriteObject(cooldowns);
        }

        void LoadData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AttackCooldown", new Dictionary<string, FileInfo>());
            cooldowns = cooldownsFile.ReadObject<Dictionary<string, List<Cooldown>>>() ??
                        new Dictionary<string, List<Cooldown>>();
        }

        void SaveData()
        {
            cooldownsFile.WriteObject(cooldowns);
        }
        #endregion

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerInit(player));
                return;
            }
            if (PlayerBlockStatus.Get(player).CurrentBlocker != null)
                PlayerBlockStatus.Get(player).CreateUI();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (PlayerBlockStatus.Get(player) != null)
                    UnityEngine.Object.Destroy(PlayerBlockStatus.Get(player));
            }
            BlockerList.RemoveAll(x =>
            {
                UnityEngine.Object.Destroy(x);
                return true;
            });
            Interface.Oxide.DataFileSystem.WriteObject("Vk/Data", baza);
            Interface.Oxide.DataFileSystem.WriteObject("Vk/Names", _PlayerNicknames);
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (player.IsConnected || info == null || player.userID < 76561100000) return; 

            if (info.InitiatorPlayer == null || info?.InitiatorPlayer.userID == player.userID) return; 
            
            string killerInfo = info.InitiatorPlayer == null ? "неизвестного" : info.InitiatorPlayer.displayName;
            string vkid;
            
            if (baza.TryGetValue(player.userID, out vkid)) 
                GetRequest(vkid, config.Sender.KillMessage.GetRandom()
                    .Replace("%KILLER%", FixName(killerInfo))
                    .Replace("%SQUARE%", GetGrid(player.transform.position))
                    .Replace("%SERVER%", config.Sender.ServerName));
        } 
        
        private string GetGrid(Vector3 pos) 
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x+(ConVar.Server.worldsize/2)) / 146.3f)%26;
            var z = (Mathf.Floor(ConVar.Server.worldsize/146.3f)-1)-Mathf.Floor((pos.z+(ConVar.Server.worldsize/2)) / 146.3f);
            letter = (char)(((int)letter)+x);
            return $"{letter}{z}";
        }
        
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info, BaseCombatEntity victim)
        {
            try
            {
                if (entity == null || info?.InitiatorPlayer == null || entity.ToPlayer() == null || info?.InitiatorPlayer == entity.ToPlayer()) return;
                if (config.BlockSettings.blockAttack)
                {
                    if (entity is BasePlayer && info.Initiator is BasePlayer)
                    {
                        if (entity != null && entity.ToPlayer() != null)
                        {
                            BlockPlayer(info?.InitiatorPlayer);
                            BlockPlayer(entity.ToPlayer());
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        void BlockPlayer(BasePlayer player)
        {
            //if (player.IsSleeping()) return;
            if (player == null) return;
            
            var cooldown = GetCooldown(player, "combat");
            if (cooldown != 0)
            {
                return;
            }

            if (!timers.ContainsKey(player.userID))
            {
                player.ChatMessage(string.Format(Messages["blockattackactive"],
                    FormatTime(TimeSpan.FromSeconds(config.BlockSettings.blockAttackTime))));
            }

            if (!timers.ContainsKey(player.userID))
            {
                timers[player.userID] = config.BlockSettings.blockAttackTime;
                SetCooldown(player, "attack", config.BlockSettings.blockAttackTime);
                SaveData();
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!config.Init) return;
            if (entity == null || info == null || info.InitiatorPlayer == null || !(entity is StabilityEntity || entity is ShopFront || entity is BuildingPrivlidge)
                || config.BlockSettings.EnabledBuildingBlock && entity.GetBuildingPrivilege() == null || entity.OwnerID == 0) return;
            if (entity is BuildingBlock && (entity as BuildingBlock).currentGrade.gradeBase.type == BuildingGrade.Enum.Twigs
                || info?.damageTypes.GetMajorityDamageType() == DamageType.Decay || config.BlockSettings.WriteListDestroyEntity.Contains(entity.ShortPrefabName)) return;
            var alreadyBlock = BlockerList.FirstOrDefault(p => Vector3.Distance(entity.transform.position, p.transform.position) < (config.BlockSettings.BlockerDistance / 2));
            var position = GetGrid(entity.transform.position);

            if (alreadyBlock)
            {
                alreadyBlock.CurrentTime = 0;
                if (config.BlockSettings.BlockOwnersIfNotInZone)
                {
                    var OwnerPlayer = BasePlayer.FindByID(entity.OwnerID);
                    if (OwnerPlayer != null)
                        PlayerBlockStatus.Get(OwnerPlayer).BlockPlayer(alreadyBlock, false);
                }
                PlayerBlockStatus.Get(info.InitiatorPlayer).BlockPlayer(alreadyBlock, false);
                if (entity.GetBuildingPrivilege() != null && config.BlockSettings.EnabledBlockAutCupboard)
                {
                    foreach (var aplayer in entity.GetBuildingPrivilege().authorizedPlayers)
                    {
                        if (Friends != null)
                        {
                            var areFriends = Friends.CallHook("AreFriends", aplayer, info.InitiatorPlayer.displayName);
                            if (areFriends != null && areFriends is bool)						
                                if (Convert.ToBoolean(areFriends)==true)
                                    continue;							
                        }
                        
                        var AuthPlayer = BasePlayer.Find(aplayer.userid.ToString());
                        if (AuthPlayer != null && AuthPlayer != info.InitiatorPlayer && AuthPlayer.IsConnected)
                            PlayerBlockStatus.Get(AuthPlayer).BlockPlayer(alreadyBlock, false);
                        else if (AuthPlayer == null || !AuthPlayer.IsConnected) ALERTPLAYER(aplayer.userid, FixName(info.InitiatorPlayer.displayName), entity);
                    }
                }
                var col = Vis.colBuffer;
                var count = Physics.OverlapSphereNonAlloc(alreadyBlock.transform.position, config.BlockSettings.BlockerDistance, col, LayerMask.GetMask("Player (Server)"));
                for (int i = 0; i < count; i++)
                {
                    var player = Vis.colBuffer[i].gameObject.ToBaseEntity() as BasePlayer;
                    if (player == null) continue;
                    PlayerBlockStatus.Get(player).BlockPlayer(alreadyBlock, false);
                }
            }
            else
            {
                var obj = new GameObject();
                obj.transform.position = entity.transform.position;
                var sphere = obj.AddComponent<SphereComponent>();
                sphere.GetComponent<SphereComponent>().Init(info.InitiatorPlayer, entity.OwnerID, entity.GetBuildingPrivilege() != null ? entity.GetBuildingPrivilege().authorizedPlayers.Select(p => p.userid).ToList() : null);
                BlockerList.Add(sphere);
                PlayerBlockStatus.Get(info.InitiatorPlayer).BlockPlayer(sphere, true);
                var OwnerPlayer = BasePlayer.FindByID(entity.OwnerID);
                if (OwnerPlayer == null || !OwnerPlayer.IsConnected)
                {
                    ALERTPLAYER(entity.OwnerID, FixName(info.InitiatorPlayer.displayName), entity);
                    return;
                }
                else if (OwnerPlayer != null && OwnerPlayer != info.InitiatorPlayer)
                {
                    if (config.BlockSettings.BlockOwnersIfNotInZone)
                    {
                        PlayerBlockStatus.Get(OwnerPlayer)?.BlockPlayer(sphere, false);
                        if (OwnerPlayer != info?.InitiatorPlayer) OwnerPlayer.ChatMessage(string.Format(Messages["blockactive"], GetNameGrid(entity.transform.position), NumericalFormatter.FormatTime(config.BlockSettings.BlockLength)));
                    }
                    else
                        OwnerPlayer.ChatMessage(string.Format(Messages["blockactiveOwner"], GetNameGrid(entity.transform.position)));
                }
                var col = Vis.colBuffer;
                var count = Physics.OverlapSphereNonAlloc(sphere.transform.position, config.BlockSettings.BlockerDistance, col, LayerMask.GetMask("Player (Server)"));
                for (int i = 0; i < count; i++)
                {
                    var player = Vis.colBuffer[i].gameObject.ToBaseEntity() as BasePlayer;
                    if (player == null || !player.IsConnected) continue;
                    PlayerBlockStatus.Get(player).BlockPlayer(sphere, false);
                }

                if (entity.GetBuildingPrivilege() != null && config.BlockSettings.EnabledBlockAutCupboard)
                {
                    foreach (var aplayer in entity.GetBuildingPrivilege().authorizedPlayers)
                    {
                        if (Friends != null)
                        {
                            var areFriends = Friends.CallHook("AreFriends", aplayer, info.InitiatorPlayer.displayName);
                            if (areFriends != null && areFriends is bool)						
                                if (Convert.ToBoolean(areFriends)==true)
                                    continue;							
                        }
                        
                        var AuthPlayer = BasePlayer.Find(aplayer.userid.ToString());
                        if (AuthPlayer != null && AuthPlayer != info.InitiatorPlayer)
                            PlayerBlockStatus.Get(AuthPlayer).BlockPlayer(sphere, false);
                        else ALERTPLAYER(aplayer.userid, FixName(info.InitiatorPlayer.displayName), entity);
                    }
                }
            }
        }
        
        private static string FixName(string name) => name.Replace("&","_").Replace("#","_");
        
        public bool IsEntityRaidable(BaseCombatEntity entity)
        {						
            if (entity is BuildingBlock)				
                if ((entity as BuildingBlock).grade.ToString() == "Twigs") return false;

            string prefabName = entity is BuildingBlock ? (entity as BuildingBlock).grade + "," + entity.ShortPrefabName : entity.ShortPrefabName;
			
            foreach (var p in InfoBlocks)            
                if (p.Key == prefabName) return true;                            

            return false;
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null || !IsRaidBlocked(player)) return null;
            var shortname = prefab.hierachyName.Substring(prefab.hierachyName.IndexOf("/") + 1);
            if (config.PlayerBlockSettings.WriteListBuildEntity.Contains(shortname))
                return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null || component.CurrentBlocker == null) return null;
            player.ChatMessage(string.Format(Messages["blockbuld"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        private object OnUserCommand(IPlayer ipl, string command, string[] args)
        {
            if (ipl == null || !ipl.IsConnected) return null;
            var player = ipl.Object as BasePlayer;
            command = command.Insert(0, "/");
            if (player == null || !IsRaidBlocked(player)) return null;
            if (config.PlayerBlockSettings.BlackListCommands.Contains(command.ToLower()))
            {
                var component = PlayerBlockStatus.Get(player);
                if (component == null || component.CurrentBlocker == null) return null;
                player.ChatMessage(string.Format(Messages["commandBlock"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
                return false;
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var connection = arg.Connection;
            if (connection == null || string.IsNullOrEmpty(arg.cmd?.FullName)) return null;
            var player = arg.Player();
            if (player == null || !IsRaidBlocked(player)) return null;
            if (config.PlayerBlockSettings.BlackListCommands.Contains(arg.cmd.Name.ToLower()) || config.PlayerBlockSettings.BlackListCommands.Contains(arg.cmd.FullName.ToLower()))
            {
                var component = PlayerBlockStatus.Get(player);
                if (component == null || component.CurrentBlocker == null) return null;
                player.ChatMessage(string.Format(Messages["commandBlock"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
                return false;
            }
            return null;
        }

        #endregion

        #region Functions
        private string GetFormatTime(TimeSpan timespan)
        {
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        private static class NumericalFormatter
        {
            private static string GetNumEndings(int origNum, string[] forms)
            {
                string result;
                var num = origNum % 100;
                if (num >= 11 && num <= 19)
                {
                    result = forms[2];
                }
                else
                {
                    num = num % 10;
                    switch (num)
                    {
                        case 1: result = forms[0]; break;
                        case 2:
                        case 3:
                        case 4:
                            result = forms[1]; break;
                        default:
                            result = forms[2]; break;
                    }
                }
                return string.Format("{0} {1} ", origNum, result);
            }

            private static string FormatSeconds(int seconds) =>
                GetNumEndings(seconds, new[] { "секунду", "секунды", "секунд" });
            private static string FormatMinutes(int minutes) =>
                GetNumEndings(minutes, new[] { "минуту", "минуты", "минут" });
            private static string FormatHours(int hours) =>
                GetNumEndings(hours, new[] { "час", "часа", "часов" });
            private static string FormatDays(int days) =>
                GetNumEndings(days, new[] { "день", "дня", "дней" });
            private static string FormatTime(TimeSpan timeSpan)
            {
                string result = string.Empty;
                if (timeSpan.Days > 0)
                    result += FormatDays(timeSpan.Days);
                if (timeSpan.Hours > 0)
                    result += FormatHours(timeSpan.Hours);
                if (timeSpan.Minutes > 0)
                    result += FormatMinutes(timeSpan.Minutes);
                if (timeSpan.Seconds > 0)
                    result += FormatSeconds(timeSpan.Seconds).TrimEnd(' ');
                return result;
            }

            public static string FormatTime(int seconds) => FormatTime(new TimeSpan(0, 0, seconds));
            public static string FormatTime(float seconds) => FormatTime((int)Math.Round(seconds));
            public static string FormatTime(double seconds) => FormatTime((int)Math.Round(seconds));
        }
        #endregion

        #region API

        private bool IsBlocked(BasePlayer player) => IsRaidBlocked(player);

        private List<Vector3> ApiGetOwnerRaidZones(ulong playerid)
        {
            var OwnerList = BlockerList.Where(p => p.OwnerID == playerid || p.Privilage != null && p.Privilage.Contains(playerid)).Select(p => p.transform.position).ToList();
            return OwnerList;
        }

        private List<Vector3> ApiGetAllRaidZones()
          => BlockerList.Select(p => p.transform.position).ToList();

        private bool IsRaidBlock(ulong userId) => IsRaidBlocked(userId.ToString());

        private bool IsRaidBlocked(BasePlayer player)
        {
            var targetBlock = PlayerBlockStatus.Get(player);
            if (targetBlock == null) return false;
            if (targetBlock.CurrentBlocker == null) return false;

            return true;
        }

        private bool IsRaidBlocked(string player)
        {
            BasePlayer target = BasePlayer.Find(player);
            if (target == null) return false;

            return IsRaidBlocked(target);
        }

        private int ApiGetTime(ulong userId)
        {
            if (!IsRaidBlocked(userId.ToString()))
                return 0;
            var targetBlock = PlayerBlockStatus.Get(BasePlayer.Find(userId.ToString()));
            return (int)(targetBlock.CurrentBlocker.TotalTime - targetBlock.CurrentTime);
        }


        private string CanTeleport(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanTeleport) return null;
            var cooldown = GetCooldown(player, "attack");
            if (cooldown > 0 && !player.IsAdmin)
            {
                SendReply(player, "Телепортация вовремя комбат блока запрещена!");
                return null;
            }
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktp"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private int? CanBGrade(BasePlayer player, int grade, BuildingBlock block, Planner plan)
        {
            if (!config.PlayerBlockSettings.CanBGrade) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockupgrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return 0;
        }

        private string CanTrade(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanUseTrade) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private string canRemove(BasePlayer player)
        {

            if (!config.PlayerBlockSettings.CanRemove) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blockremove"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private string canTeleport(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanTeleport) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktp"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        object canRedeemKit(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanUseKits) return null;

            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blockKits"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private bool? CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockupgrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        private bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanRepair) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockrepair"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanDefaultremove) return null;
            if (player == null) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockremove"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return null;
        }

        private SphereComponent GetRaidZone(Vector3 pos) =>
             BlockerList.Where(p => Vector3.Distance(p.transform.position, pos) < config.BlockSettings.BlockerDistance).FirstOrDefault();

        #endregion

        #region VkAPI
        Dictionary<ulong, string> _PlayerNicknames = new Dictionary<ulong, string>();

        public Dictionary<ulong, string> baza;

        private void ALERTPLAYER(ulong ID, string name, BaseCombatEntity entity)
        {
            ALERT alert;
            if (!alerts.TryGetValue(ID, out alert))
            {
                alerts.Add(ID, new ALERT());
                alert = alerts[ID];
            }

            #region ОПОВЕЩЕНИЕ В ВК
            if (alert.vkcooldown < DateTime.Now)
            {
                string vkid;
                if (baza.TryGetValue(ID, out vkid))
                {
                    var obj = DefaultBlock;
					
                    string type = "";
                    if (entity is BuildingBlock) type = (entity as BuildingBlock).grade.ToString() + ",";
					
                    if (InfoBlocks.ContainsKey($"{type}{entity.ShortPrefabName}"))
                        obj = InfoBlocks[$"{type}{entity.ShortPrefabName}"];	
                    
                    if (IsEntityRaidable(entity))
                        GetRequest(vkid, config.Sender.StartRaidMessages.GetRandom()
                            .Replace("%INITIATOR%", name)
                            .Replace("%OBJECT%", $"{obj.pre} {obj.name}")
                            .Replace("%SERVER%", config.Sender.ServerName)
                            .Replace("%SQUARE%", GetGrid(entity.transform.position)));
                    
                    alert.vkcooldown = DateTime.Now.AddSeconds(1200);
                }
            }
            #endregion
        }

        private static Dictionary<string, Vector3> Grids = new Dictionary<string, Vector3>();
        private void CreateSpawnGrid()
        {
            Grids.Clear();
            var worldSize = (ConVar.Server.worldsize);
            float offset = worldSize / 2;
            var gridWidth = (0.0066666666666667f * worldSize);
            float step = worldSize / gridWidth;

            string start = "";

            char letter = 'A';
            int number = 0;

            for (float zz = offset; zz > -offset; zz -= step)
            {
                for (float xx = -offset; xx < offset; xx += step)
                {
                    Grids.Add($"{start}{letter}{number}", new Vector3(xx - 55f, 0, zz + 20f));
                    if (letter.ToString().ToUpper() == "Z")
                    {
                        start = "A";
                        letter = 'A';
                    }
                    else
                    {
                        letter = (char)(((int)letter) + 1);
                    }


                }
                number++;
                start = "";
                letter = 'A';
            }
        }

        private string GetNameGrid(Vector3 pos)
        {
            return Grids.Where(x => x.Value.x < pos.x && x.Value.x + 150f > pos.x && x.Value.z > pos.z && x.Value.z - 150f < pos.z).FirstOrDefault().Key;
        }

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
        private string GetOfflineName(ulong id)
        {
            string name = "";
            if (_PlayerNicknames.ContainsKey(id))
                name = _PlayerNicknames[id];

            return name;
        }
        bool IsOnline(ulong id)
        {
            foreach (BasePlayer active in BasePlayer.activePlayerList)
            {
                if (active.userID == id) return true;
            }

            return false;
        }
        #endregion

        #region Хуита
        class ALERT
        {
            public DateTime gamecooldown;
            public DateTime discordcooldown;
            public DateTime vkcooldown;
            public DateTime vkcodecooldown;
        }

        class CODE
        {
            public string id;
            public ulong gameid;
        }

        private static Dictionary<string, CODE> VKCODES = new Dictionary<string, CODE>();

        private static Dictionary<ulong, ALERT> alerts = new Dictionary<ulong, ALERT>();
        private string RANDOMNUM() => Random.Range(1000, 99999).ToString();

        [ChatCommand("vk")]
        void ChatVk(BasePlayer player)
        {
            string vkid;
            if (!baza.TryGetValue(player.userID, out vkid))
            {
                player.Command("vk add");
            }
            else
            {
                VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>");
            }
        }
        
        string URL_GetUserInfo = $"https://api.vk.com/method/users.get?v=5.86&user_ids={0}&access_token=b061e0a674715b46e8e00a7528da8a2b2b348d277186ec554abc1a1c0ad377499a5ade5c1da476065eff7";

        [ConsoleCommand("vk")]
        void ConsolePM(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                Puts(args.Args[0]);
                if (args.Args[0] == "add")
                {
                    if (args == null || args.Args.Length == 1)
                    {
                        string vkid;
                        if (!baza.TryGetValue(player.userID, out vkid))
                        {
                            VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", "Чтобы подключить оповещение о рейде\nдобавте выше <b>id вашего аккаунта</b>");
                        }
                        else
                        {
                            VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>");
                        }
                        return;
                    }
                    ALERT aLERT;
                    if (alerts.TryGetValue(player.userID, out aLERT) && aLERT.vkcodecooldown > DateTime.Now)
                    {
                        player.ChatMessage($"Отправить новый код вы сможете через {FormatTime(aLERT.vkcodecooldown - DateTime.Now).ToLower()}");
                        return;
                    }

                    webrequest.Enqueue(string.Format(URL_GetUserInfo, TryParseVkNameOrID(args.Args[1])), null, (code, response) =>
                    {
                        JObject res = JObject.Parse(response);
                        
                        string num = RANDOMNUM(); 
                        GetRequest((string) res["id"], $"Код подтверждения {num} аккаунта.", player, num);
                    }, this);
                }
                if (args.Args[0] == "accept")
                {
                    if (args == null || args.Args.Length == 1)
                    {
                        VkUI(player, "Укажите код из сообщения", "1 1 1 0.1", "vk accept ", "Вы не указали <b>код</b>!");
                        return;
                    }
                    
                    webrequest.Enqueue(string.Format(URL_GetUserInfo, TryParseVkNameOrID(args.Args[1])), null, (code, response) =>
                    {
                        JObject res = JObject.Parse(response);

                        CODE cODE;
                        if (VKCODES.TryGetValue((string) res["id"], out cODE) && cODE.gameid == player.userID)
                        {
                            string vkid;
                            if (baza.TryGetValue(player.userID, out vkid))
                            {
                                vkid = cODE.id;
                            }
                            else
                            {
                                baza.Add(player.userID, cODE.id);
                            }

                            VKCODES.Remove((string) res["id"]);
                            VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>");
                            Interface.Oxide.DataFileSystem.WriteObject("Vk/Data", baza);
                        }
                        else 
                        { 
                            VkUI(player, "Укажите код из сообщения", "1 1 1 0.1", "vk accept ", "Не верный <b>код</b>!"); 
                        }
                    }, this);
                }
                if (args.Args[0] == "delete")
                {
                    if (baza.ContainsKey(player.userID))
                    {
                        baza.Remove(player.userID);
                        VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", "Чтобы подключить оповещение о рейде\nдобавте выше <b>id вашего аккаунта</b>");
                    }
                }
            }
        }

        private void GetRequest(string reciverID, string msg, BasePlayer player = null, string num = null) => 
            webrequest.Enqueue("https://api.vk.com/method/messages.send?domain=" + reciverID + "&message=" + msg.Replace("#", "%23") 
                               + "&v=5.86&access_token=" + config.Sender.VK.VKAccess, null, (code2, response2) => 
                ServerMgr.Instance.StartCoroutine(GetCallback(code2, response2, reciverID, player, num)), this);

        private IEnumerator GetCallback(int code, string response, string id, BasePlayer player = null, string num = null)
        {
            if (player == null) yield break;
            if (response == null || code != 200)
            {
                ALERT alert;
                if (alerts.TryGetValue(player.userID, out alert)) alert.vkcooldown = DateTime.Now;
                Debug.Log("НЕ ПОЛУЧИЛОСЬ ОТПРАВИТЬ СООБЩЕНИЕ В ВК! => обнулили кд на отправку");
                yield break;
            }
            yield return new WaitForEndOfFrame();
            if (!response.Contains("error"))
            {
                ALERT aLERT;
                if (alerts.TryGetValue(player.userID, out aLERT))
                {
                    aLERT.vkcodecooldown = DateTime.Now.AddMinutes(10);
                }
                else
                {
                    alerts.Add(player.userID, new ALERT { vkcodecooldown = DateTime.Now.AddMinutes(10) });
                }
                if (VKCODES.ContainsKey(num)) VKCODES.Remove(num);
                VKCODES.Add(num, new CODE { gameid = player.userID, id = id });
                VkUI(player, "Укажите код из сообщения", "1 1 1 0.1", "vk accept ", $"Вы указали VK: <b>{id}</b>. Вам в <b>VK</b> отправлено сообщение с кодом.\nВставте <b>код</b> выше, чтобы подтвердить авторизацию");
            }
            else if (response.Contains("PrivateMessage"))
            {
                VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", $"Ваши настройки приватности не позволяют отправить вам\nсообщение <b>{id}</b>");
            }
            else if (response.Contains("ErrorSend"))
            {
                VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", $"Невозможно отправить сообщение.Проверьте правильность ссылки <b>{id}</b>\nили повторите попытку позже.");
            }
            else if (response.Contains("BlackList"))
            {
                VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", "Невозможно отправить сообщение. Вы добавили группу в черный список или не подписаны на нее, если это не так,\nто просто напишите в группу сервера любое сообщение и попробуйте еще раз.");
            }
            else
            {
                VkUI(player, "Укажите свой вк", "1 1 1 0.1", "vk add ", $"Вы указали неверный <b>VK ID {id}</b>, если это не так,\nто просто напишите в группу сервера любое сообщение и попробуйте еще раз.");
            }
            yield break;
        }

        private string TryParseVkNameOrID(string vk)
        {
            string vk_ = vk.ToLower();
			
            if ((vk_.Contains("/id")||vk_.StartsWith("id")) && vk_.Length>3)
            {
                string result = "";
                int count = 0;
                int startPos = 2;
                if (vk_.Contains("/id"))
                    startPos = vk_.IndexOf("/id")+3;
					
                foreach(var ch in vk_)
                {
                    if (count >= startPos && "0123456789".IndexOf(ch)>=0)					
                        result += ch;					
                    else
                    if (count >= startPos && "0123456789".IndexOf(ch)<0)
                        break;
					
                    count++;
                }
				
                if (string.IsNullOrEmpty(result)) return null;
                return result;
            }	
            else 
            if (vk_.Contains(".com/") && vk_.Length>5)
            {
                string result = "";
                int count = 0;
                int startPos = vk_.IndexOf(".com/")+5;											
						
                foreach(var ch in vk_)
                {
                    if (count >= startPos && "_0123456789abcdefghijklmnopqrstuvwxyz.".IndexOf(ch)>=0)					
                        result += ch;					
                    else
                    if (count >= startPos && "_0123456789abcdefghijklmnopqrstuvwxyz.".IndexOf(ch)<0)
                        break;
						
                    count++;
                }
					
                if (string.IsNullOrEmpty(result)) return null;					
                return result;
            }
            else
            {
                string result = "";
					
                bool notID = false;
					
                foreach(var ch in vk_)
                {
                    if ("0123456789".IndexOf(ch)>=0)					
                        result += ch;					
                    else
                    {
                        notID = true;		
                        break;
                    }													
                }
															
                if (!notID && !string.IsNullOrEmpty(result))
                    return result;
					
                bool notName = false;
					
                foreach(var ch in vk_)
                {
                    if ("_0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(ch)>=0)					
                        result += ch;					
                    else
                    {
                        notName = true;		
                        break;
                    }													
                }
					
                if (!notName && !string.IsNullOrEmpty(result))
                    return result;
            }	
				
            return null;		
        }

        string Layers = "Vk_UI";

        void VkUI(BasePlayer player, string vk = "", string color = "", string command = "", string text = "")
        {
            CuiHelper.DestroyUi(player, Layers);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" }
            }, "Overlay", Layers);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layers },
                Text = { Text = "" }
            }, Layers);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3 0.55", AnchorMax = $"0.7 0.66", OffsetMax = "0 0" },
                Text = { Text = "ОПОВЕЩЕНИЕ", Color = "1 1 1 0.6", Align = TextAnchor.MiddleCenter, FontSize = 60, Font = "robotocondensed-bold.ttf" }
            }, Layers);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3 0.52", AnchorMax = $"0.7 0.57", OffsetMax = "0 0" },
                Text = { Text = "ЭТО ВАШ АККАУНТ", Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, Layers);

            var anchorMax = command != "" ? "0.57 0.53" : "0.543 0.53";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.43 0.49", AnchorMax = anchorMax, OffsetMax = "0 0" },
                Image = { Color = color }
            }, Layers, "Enter");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04 0", AnchorMax = $"0.96 1", OffsetMax = "0 0" },
                Text = { Text = vk, Color = "1 1 1 0.05", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Enter");

            if (command != "")
            {
                container.Add(new CuiElement
                {
                    Parent = "Enter",
                    Components =
                    {
                        new CuiInputFieldComponent { Text = "ХУЙ", FontSize = 14, Align = TextAnchor.MiddleCenter, Command = command, Color = "1 1 1 0.6", CharsLimit = 40},
                        new CuiRectTransformComponent { AnchorMin = "0.04 0", AnchorMax = "0.96 1" }
                    }
                });
            }

            if (command == "")
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.545 0.49", AnchorMax = $"0.57 0.53", OffsetMax = "0 0" },
                    Button = { Color = "0.76 0.35 0.35 0.4", Command = "vk delete" },
                    Text = { Text = "✖", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, Layers);
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.435", AnchorMax = $"1 0.495", OffsetMax = "0 0" },
                Text = { Text = text, Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layers);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region ВРЕМЯ
        private static string m0 = "МИНУТ";
        private static string m1 = "МИНУТЫ";
        private static string m2 = "МИНУТУ";

        private static string s0 = "СЕКУНД";
        private static string s1 = "СЕКУНДЫ";
        private static string s2 = "СЕКУНДУ";

        private static string FormatTime(TimeSpan time)
        => (time.Minutes == 0 ? string.Empty : FormatMinutes(time.Minutes)) + ((time.Seconds == 0) ? string.Empty : FormatSeconds(time.Seconds));

        private static string FormatMinutes(int minutes) => FormatUnits(minutes, m0, m1, m2);

        private static string FormatSeconds(int seconds) => FormatUnits(seconds, s0, s1, s2);

        private static string FormatUnits(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9 || tmp == 0)
                return $"{units} {form1} ";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2} ";

            return $"{units} {form3} ";
        }
        #endregion



        #region Messages

        Dictionary<string, string> Messages = new Dictionary<string, string>() {
                {
                "blocksuccess", "Блок деактивирован. Функции разблокированы"
            }
            , {
                "guitimertext", "<b>Блокировка:</b> Осталось {0}"
            }
            , {
                "blockactive", "Ваше строение в квадрате <color=#ECBE13>{0}</color> разрушено, активирован рейд блок на <color=#ECBE13>{1}</color>\nНекоторые функции временно недоступны."
            }
             , {
                "blockactiveOwner", "Внимание! Ваше строение в квадрате <color=#ECBE13>{0}</color> разрушено."
            }
             , {
                "enterRaidZone", "Внимание! Вы вошли в зону рейд блока, активирован блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            }
             , {
                "blockactiveAuthCup", "Внимание! Строение в каком вы проживаете в квадрате <color=#ECBE13>{0}</color> было разрушено, активирован рейд блок на <color=#ECBE13>{1}</color>\nНекоторые функции временно недоступны."
            }
            , {
                "blockactiveAttacker", "Вы уничтожили чужой объект, активирован рейд блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            }
            , {
                "blockrepair", "Вы не можете ремонтировать строения во время рейда, подождите {0}"
            }
            , {
                "blocktp", "Вы не можете использовать телепорт во время рейда, подождите {0}"
            }
            , {
                "blockremove", "Вы не можете удалить постройки во время рейда, подождите {0}"
            }
            , {
                "blockupgrade", "Вы не можете использовать улучшение построек во время рейда, подождите {0}"
            }
            , {
                "blockKits", "Вы не можете использовать киты во время рейда, подождите {0}"
            }
            , {
                "blockbuld", "Вы не можете строить во время рейда, подождите {0}"
            },
            {
                "raidremove", "Вы не можете удалять обьекты во время рейда, подождите {0}"
            },
            {
                "blocktrade", "Вы не можете использовать обмен во время рейда, подождите {0} "
            },
            {
                "commandBlock", "Вы не можете использовать данную команду во время рейда, подождите {0}"
            },
            {"blockattackactive", "Включен режим боя, активирован блок на {0}! Некоторые функции временно недоступны."},
            {"VkExit", "У вас уже есть страница!" },
            {"VkVremExit", "У вас уже есть активный запрос на подтверждение!"},
            {"VkCodeError", "Неправильный код !"},
            {"VkSendError", "Ошибка при отправке проверочного кода" },
            {"VkSendError2", "Ошибка при отправке проверочного кода\nОтправьте сообщение в группу и попробуй еще раз" },
            {"VkCodeSend", "Код подтверждения отправлен!" },
            {"VkAdded", "Страница успешно добавлена" }
        };
        #endregion
    }
}