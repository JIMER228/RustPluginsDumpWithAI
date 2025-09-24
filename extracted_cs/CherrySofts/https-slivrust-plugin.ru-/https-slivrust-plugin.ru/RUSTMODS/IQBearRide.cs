using Newtonsoft.Json;
using Rust;
using Oxide.Game.Rust.Cui;
using Debug = UnityEngine.Debug;
using System.Text;
using System.Drawing;
using Color = UnityEngine.Color;
using UnityEngine;
using System.Collections.Generic;
using System;
using Facepunch;
using System.Collections;
using VLB;
using Rust.Ai;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQBearRide", "rustmods.ru", "1.1.11")]
    [Description("IQBearRide")]
    class IQBearRide : RustPlugin
    {

        void Unload()
        {
            PluginData.SaveData();
            _unloading = true;
            
            InterfaceManager.DestroyAll();

            try
            {
                foreach (var value in _bearControllers)
                {
                    var bearController = value.Value;
                    if (bearController == null)
                        continue;
                    
                    bearController.SetRider(null);
                    var bear = bearController.BearOwner;

                    if (bear != null)
                    {
                        var data = PluginData.Get(bear.net.ID.Value); 
                        data.Health = bear.Health();
                        data.LastPosition = bear.transform.position; 
                        bear.Kill();
                    }
                    
                }

                _bearControllers.Clear();
            }
            catch(Exception e)
            {
                Debug.LogError($"Error in Unload! Message: {e.Message}!\n{e.StackTrace}");
            } 
		   		 		  						  	   		  	 				  						   					   		 
            PluginData.SaveData();

            _plugin = null;
        }
        
        [ConsoleCommand("iqbr.teddy")]
        private void Console_YoungBearAdd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, PermissionsGiveTeddyBear)) return;

            if (arg.Args == null || arg.Args.Length < 1)
            {
                if(player == null) Puts("Wrong syntax! Usage: iqbr.teddy <SteamID:Name:IP>");
                else player.ConsoleMessage("Wrong syntax! Usage: iqbr.teddy <SteamID:Name:IP>");
                return;
            }

            string playerid = arg.Args[0];
            BasePlayer basePlayer = BasePlayer.Find(playerid);
            if (basePlayer == null)
            {
                if(player == null) Puts("Error! Player not found!");
                else player.ConsoleMessage("Error! Player not found!");   
                return;
            }

            basePlayer.GiveItem(_config.YoungBearItem.ToItem());

            if(player == null) Puts("Success! Item added to inventory!");
            else player.ConsoleMessage("Success! Item added to inventory!");
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var item = plan.GetItem(); 
            if (item == null)
                return;

            var entity = go.ToBaseEntity();
            if (entity == null)
                return;

            var player = plan.GetOwnerPlayer();
            if (player == null)
                return;

            var pos = entity.transform.position;
            if (_config.BearItem.CompareTo(item))
            {
                var bear = PluginData.ItemToBear(player.userID, item.uid.Value, pos,   false);
                NextTick(() => entity.Kill());
                return;
            }

            if (_config.YoungBearItem.CompareTo(item))
            { 
                var bear = PluginData.ItemToBear(player.userID, item.uid.Value, pos, true);
                bear.OwnerID = player.userID;
                NextTick(() => entity.Kill());
                return;
            }
        }
        
        private string GetImage(string name)
        {
            string ID = (string)ImageLibrary?.Call("GetImage", name);
            if (ID == "")
                ID = (string)ImageLibrary?.Call("GetImage", name) ?? ID;
        
            return ID;
        }
        
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                BearItem = new CustomItem("Медведь | Bear", "stash.small", 2445048695),
                YoungBearItem = new CustomItem("Медвеженок | Teddy bear", "stash.small", 2445033042),
                BearDamage = new Dictionary<string, float>()
                {
                    ["default"] = 50f,
                    ["player"] = 20f,
                },
            };
        }
        void Init()
        {
            Unsubscribe("OnEntityTakeDamage");
            permission.RegisterPermission(PermissionsGiveBear, this);
            permission.RegisterPermission(PermissionsGiveTeddyBear, this);
        }
        
        [ConsoleCommand("iqbr.bear")]
        private void Console_BearAdd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player != null)
            if(!permission.UserHasPermission(player.UserIDString, PermissionsGiveBear)) return;
            
            if (arg.Args == null || arg.Args.Length < 1)
            {
                if(player == null) Puts("Wrong syntax! Usage: iqbr.bear <SteamID:Name:IP>");
                else player.ConsoleMessage("Wrong syntax! Usage: iqbr.bear <SteamID:Name:IP>");
                return;
            }

            string playerid = arg.Args[0];
            BasePlayer basePlayer = BasePlayer.Find(playerid);
            if (basePlayer == null)
            {
                if(player == null) Puts("Error! Player not found!");
                else player.ConsoleMessage("Error! Player not found!");
                return;
            }
		   		 		  						  	   		  	 				  						   					   		 
            basePlayer.GiveItem(_config.BearItem.ToItem());
		   		 		  						  	   		  	 				  						   					   		 
            if(player == null) Puts("Success! Item added to inventory!");
            else player.ConsoleMessage("Success! Item added to inventory!");
        }
        private static bool _unloading = false;
        void OnEntitySpawned(BaseCorpse corpse)
        {
            if (corpse != null && corpse.ShortPrefabName.Contains("bear"))
            {
                int random = Core.Random.Range(0, 100);
                if (random > _config.YoungDropChance)
                    return;
                
                var def = ItemManager.FindItemDefinition("glue");
                ItemAmount itemBonus = new ItemAmount(def, 1f);
                corpse.resourceDispenser.finishBonus.Add(itemBonus);
                
                return;
            }
        }
		   		 		  						  	   		  	 				  						   					   		 
        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                _config = GetDefaultConfig();
            }
        }
        
        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (_config.BearItem.CompareTo(item.item) || _config.YoungBearItem.CompareTo(item.item))
            {
                return false;
            }
            
            if (_config.BearItem.CompareTo(targetItem.item) || _config.YoungBearItem.CompareTo(targetItem.item))
            {
                return false;
            }
            
            return null;
        }
        
        private void StartPluginLoad()
        {
            //if (ImageLibrary != null)
            //{
                //Load your images here
                AddImage(_config.FollowImage); 
                AddImage(_config.RideImage); 
                AddImage(_config.TakeImage); 
                AddImage(_config.InventoryImage); 
                AddImage(_config.InstructionImage); 
                CheckStatus();
            //}
            //else
            //{
            //    PrintError($"ImageLibrary not found! Please, check your plugins list.");
            //    _plugin.LogToFile("fjfjfj32341dsffagfdsa332572232", "IL NULLLLLLLLLLLLLL", this);
            //}
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BEAR_FARM_WOOD"] = "[Bear] Wood",
                ["BEAR_PICK_UP"] = "Only the owner can lift the bear",
                ["BEAR_PICK_UP_FULL_INVENTORY"] = "You need to pick up everything from the bear's inventory before picking it up",
                ["BEAR_GROWING_SECOND"] = "It will grow in <size=20>{0}</size> seconds",
                ["BEAR_GROWING_USE_MENU"] = "Press '<size=20>E</size>' for \ninteraction!",

                ["BEAR_MENU_TITLE"] = "BEAR MANAGEMENT",

            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BEAR_FARM_WOOD"] = "[Мишка] Дерево",
                ["BEAR_PICK_UP"] = "Только владелец может поднять медведя",
                ["BEAR_PICK_UP_FULL_INVENTORY"] = "Нужно забрать всё из инвентаря медведя перед тем как его поднять",
                ["BEAR_GROWING_SECOND"] = "Вырастет через <size=20>{0}</size> секунд",
                ["BEAR_GROWING_USE_MENU"] = "Нажмите '<size=20>E</size>' для \nвзаимодействия!",

                ["BEAR_MENU_TITLE"] = "УПРАВЛЕНИЕ МЕДВЕДЕМ",
		   		 		  						  	   		  	 				  						   					   		 
            }, this, "ru");
            PrintWarning(LanguageEn ? "The language file was uploaded successfully" : "Языковой файл загружен успешно");
        }
                
        
        private static bool _initiated = false;

        private const String PermissionsGiveBear = "iqbearride.givebear";
        
        private static PluginConfig _config;

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null)
                return null;

            var attacker = hitInfo.InitiatorPlayer;
            if (attacker == null || !(attacker is BasePlayer)) return null;
            {
                Bear bear = entity as Bear;
                if (bear != null && bear.OwnerID != 0 && bear.OwnerID.IsSteamId())
                    return false;
            }

            return null;
        }
        
        
        private static bool IsFriend(ulong player, ulong friend)
        {
            if (player == friend)
                return true;
            
            var team = RelationshipManager.ServerInstance.FindPlayersTeam(player);
            if (team == null)
                return false;

            return team.members.Contains(friend);
        }
        
                
                private class PluginConfig
        {
            [JsonProperty(LanguageEn ? "Forbid the bear to defecate (true - yes / false - no)" : "Запретить медведю испражняться (true - да/false - нет)")]
            public Boolean BlockDefecateBear = false;
            
            [JsonProperty(LanguageEn ? "Chance of a bear cub falling out" : "Шанс выпадения медвеженка")]
            public int YoungDropChance = 20;

            [JsonProperty(LanguageEn ? "Make a bear immortal (true - yes/false - no)" : "Сделать бессмертным медведя (true - да/false - нет)")]
            public Boolean Immortals = false;
		   		 		  						  	   		  	 				  						   					   		 
            [JsonProperty(LanguageEn ? "The number of hp of the bear" : "Количество хп медведя")]
            public int MaxHealthAmount = 1200;
		   		 		  						  	   		  	 				  						   					   		 
            [JsonProperty(LanguageEn ? "How long does a bear grow in seconds" : "Сколько времени растёт медведь в секундах")]
            public int GrowSeconds = 100;

            [JsonProperty(LanguageEn ? "Walking speed" : "Скорость ходьбы")]
            public float WalkSpeed = 5f;
		   		 		  						  	   		  	 				  						   					   		 
            [JsonProperty(LanguageEn ? "Leash speed" : "Скорость поводка")]
            public float LeadSpeed = 7.5f;

            [JsonProperty(LanguageEn ? "Running speed" : "Скорость бега")]
            public float RunSpeed = 7.5f;

            [JsonProperty(LanguageEn ? "Running speed with shift" : "Скорость бега с шифтом")]
            public float SprintSpeed = 10f;

            [JsonProperty(LanguageEn ? "Turning speed" : "Скорость поворота")]
            public float RotateSpeed = 150;

            [JsonProperty(LanguageEn ? "Step size up (If you don't know what it is, don't touch this function)" : "Размер шага вверх (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float StepHigh = 1f;

            [JsonProperty(LanguageEn ? "Step size down (If you don't know what it is, don't touch this function)" : "Размер шага вниз (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float StepDown = 1f;

            [JsonProperty(LanguageEn ? "Board size length (If you don't know what it is, don't touch this function)" : "Размер доски длина (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float BoardForward = 1.5f;

            [JsonProperty(LanguageEn ? "Board size width (If you don't know what it is, don't touch this function)" : "Размер доски ширина (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float BoardRight = 0.2f;

            [JsonProperty(LanguageEn ? "The maximum height that can climb" : "Максимальная высота на которую может залезть")]
            public float MaxWallClimbSlope = 50f;

            [JsonProperty(LanguageEn ? "The radius of checking for objects (If you don't know what it is, don't touch this function)" : "Радиус проверки на обьекты (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float ObsCheckDis = 0.6f;

            [JsonProperty(LanguageEn ? "The radius of the rotation balls check (If you do not know what it is, do not touch this function)" : "Радиус проверки шариков поворота (Если вы не знаете что это такое, не трогайте данную функцию)")]
            public float RotSphereDis = 0.35f;

            [JsonProperty(LanguageEn ? "Maximum depth" : "Максимальная глубина")]
            public float MaxWaterDepth = 150f;

            [JsonProperty(LanguageEn ? "Inventory Size" : "Размер инвентаря")]
            public int InventorySize = 24;

            [JsonProperty(LanguageEn ? "The damage inflicted is individual" : "Наносимый урон идивидуальный")]
            public Dictionary<string, float> BearDamage;

            [JsonProperty(LanguageEn ? "How much health to give when eating" : "Сколько здоровья давать при еде")]
            public float EatHealAmount = 50f;

            [JsonProperty(LanguageEn ? "IQ Chat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
            public String CustomPrefix = "[IQBearRide]";

            [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
            public String CustomAvatar = "0";

            [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI уведомления")]
            public Boolean UIAlertUse = false;

            [JsonProperty(LanguageEn ? "Setting up the Bear Item" : "Настройка предмета Медведь")]
            public CustomItem BearItem;

            [JsonProperty(LanguageEn ? "Setting up the Bear Cub item" : "Настройка предмета Медвеженок")]
            public CustomItem YoungBearItem;

            [JsonProperty(LanguageEn ? "Picture To Follow" : "Картинка Следовать")]
            public string FollowImage = "https://i.imgur.com/0sGNhqD.png";

            [JsonProperty(LanguageEn ? "Picture to ride" : "Картинка Ехать")]
            public string RideImage = "https://i.imgur.com/xar8gWn.png";

            [JsonProperty(LanguageEn ? "Picture Raise" : "Картинка Поднять")]
            public string TakeImage = "https://i.imgur.com/973xdCt.png";

            [JsonProperty(LanguageEn ? "Picture Inventory" : "Картинка Инвентарь")]
            public string InventoryImage = "https://i.imgur.com/OpjOCgL.png";

            [JsonProperty(LanguageEn ? "Picture Instructions" : "Картинка Инструкции (С русским переводом - https://i.imgur.com/D5rIpyM.png)")]
            public string InstructionImage = "https://i.imgur.com/32JQy4R.png";
        }
        
        private static void DrawText(BasePlayer player, Vector3 position, string text, float time = 1)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendEntityUpdate();
            player.SendConsoleCommand("ddraw.text", time, Color.white, position, text);
            player.SendConsoleCommand("camspeed 0");

            if (player.Connection.authLevel < 2)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);

            player.SendEntityUpdate();
        }

        private void DrawUI_BearMenu(BasePlayer player, ulong bear)
        {
            var controller = _bearControllers[bear];
            
            string gui = InterfaceManager.GetInterface("BearMain");
            
            gui = gui.Replace("{bear}", bear.ToString());
            gui = gui.Replace("%TEXT%", GetLang("BEAR_MENU_TITLE", player.UserIDString));
            gui = gui.Replace("{LeadColor}", GetColor(controller.HasLead() ? "#18CA77" : "#CA2539", 0.8f));
            
            CuiHelper.DestroyUi(player, InterfaceManager.UI_Layer);
            CuiHelper.AddUi(player, gui);
        }
        /// </summary>
        ///Обновление 1.0.x
        /// - Поправлено уведомление о добыче мишкой
        /// - Исправления после обновления игры
        /// - Добавлена возможность запретить медведю испражняться
		   		 		  						  	   		  	 				  						   					   		 
        [PluginReference] Plugin IQChat, StackSizeController;
        
        private void OnEntityDismounted(BaseVehicleSeat chair, BasePlayer player)
        {
            if (chair == null || player == null) return;

            var bear = chair.parentEntity.Get(true) as Bear;
            if (bear == null) return;

            var controller = bear.GetComponent<BearController>();
            if (controller == null) return;
            
            controller.SetRider(null);

            CuiHelper.DestroyUi(player, InterfaceManager.UI_Instruction);
        }
        
        private static string GetNameByURL(string url)
        {
            var splitted = url.Split('/');
            var endUrl = splitted[splitted.Length - 1];
            var name = endUrl.Split('.')[0];
            return name;
        }
		   		 		  						  	   		  	 				  						   					   		 
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        
        private void OpenContainer(BasePlayer player, ItemContainer container, int capacity)
        {
            var loot = player.inventory.loot;
            var position = player.transform.position - new Vector3(0, 500, 0);
            var entity = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position);
            if (entity == null)
            {
                return;
            }

            entity.enableSaving = false;
            entity.Spawn();
            container.entityOwner = entity;
            container.playerOwner = player;
            container.capacity = capacity;
            foreach (var item in container.itemList.ToArray())
            {
                if (item.position >= capacity)
                {
                    item.RemoveFromContainer();
                    item.MoveToContainer(container);
                }
            }

            entity.StartCoroutine(UserChecker(player, entity));
            
            _plugin.timer.Once(0.5f, () =>
            {
                if (player == null)
                {
                    return;
                }

                loot.Clear();
                loot.PositionChecks = false;
                loot.entitySource = entity;
                loot.itemSource = (Item) null;
                loot.MarkDirty();
                loot.AddContainer(container);
                loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
                player.SendNetworkUpdateImmediate();
            });
        }
        
        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser.name.Contains("bear.corpse") == false)
                return;

            if (item.info.shortname != "glue")
                return;
            
            item.Remove();
            player.GiveItem(_config.YoungBearItem.ToItem());
            
            return;
        }
        
        private static Dictionary<ulong, BearController> _bearControllers = new Dictionary<ulong, BearController>();
		   		 		  						  	   		  	 				  						   					   		 
        private static IQBearRide _plugin;
        
                void OnServerInitialized()
        {
            if (ImageLibrary == null)
            {
                NextTick(() => {
                    PrintError(LanguageEn ? "For the plugin to work correctly, you need to install the Image Library plugin (https://umod.org/plugins/image-library)" : "Для корректной работы плагина требуется установить плагин ImageLibrary (https://umod.org/plugins/image-library)");
                    Interface.Oxide.UnloadPlugin(Name);
                });
            }
		   		 		  						  	   		  	 				  						   					   		 
            _plugin = this;
            _unloading = false;

            StartPluginLoad();
        }

        private class InterfaceManager
        {
            
            public static InterfaceManager Instance;
            public const string UI_Layer = "UI_BearRide";
            public const string UI_Instruction = "UI_Instruction_Bear";
            public Dictionary<string, string> Interfaces;

            
            
            public InterfaceManager()
            {
                Instance = this;
                Interfaces = new Dictionary<string, string>();
                BuildInterface();
                BuildInstruction();
            }

            public static void AddInterface(string name, string json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _plugin.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }
                
                Instance.Interfaces.Add(name, json);
            }

            public static string GetInterface(string name)
            {
                string json = string.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _plugin.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, UI_Layer);
                    CuiHelper.DestroyUi(player, UI_Instruction);
                }
            }

            
            private void BuildInstruction()
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "300.426 -343.7",
                                OffsetMax = "427.374 -261.8922572" 
                            },
                            Image = { Color = "0 0 0 0" }
                        },
                        "Overlay", UI_Instruction
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Instruction,
                            Name = "PNG_Instruction",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = "1 1 1 1",
                                    Png = _plugin.GetImage(GetNameByURL(_config.InstructionImage)),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-17.806 -40.9042572", OffsetMax = "63.476 13.177" 
                                }
                            },
                        }
                    }
                };
                AddInterface("BearInstruciont", container.ToJson());
            }
        
            private void BuildInterface()
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            CursorEnabled = true,
                            RectTransform =
                            {
                                AnchorMin = $"0.5 0.5",
                                AnchorMax = $"0.5 0.5",
                                OffsetMin = "-165 -60",
                                OffsetMax = "165 70"
                            },
                            Image = { Color = "0 0 0 0" } 
                        },
                        "Overlay", UI_Layer
                    },
                    {
                        new CuiButton
                        {
                            RectTransform = {AnchorMin = $"-100 -100", AnchorMax = $"100 100"},
                            Button = {Color = "0 0 0 0", Close = UI_Layer, Command = "UI_BearControl close {bear}"},
                            Text = {Text = ""}
                        },
                        UI_Layer
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.LogoBar",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0 0 0 0"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"1 1",
                                    OffsetMin = "5 -60",
                                    OffsetMax = "-5 -60"
                                },
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = $"{UI_Layer}.LogoBar",
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = "%TEXT%", Align = TextAnchor.MiddleCenter, Color = GetColor("#FFFFFF", 1f), FontSize = 14,
                                    Font = "robotocondensed-bold.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1",
                                    OffsetMax = "0 40"
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1", Distance = "0.2 0.2" 
                                }
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.Follow",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "{LeadColor}"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "60 -100",
                                    OffsetMax = "110 -50"
                                },
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = $"{UI_Layer}.Follow",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _plugin.GetImage(GetNameByURL(_config.FollowImage))
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1"
                                }
                            }
                        }
                    },
                    {
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = "0 0",
                                OffsetMax = "1 1"
                            },
                            Button =
                            {
                                Color = "0 0 0 0", Command = "UI_BearControl lead {bear}", Close = UI_Layer
                            },
                            Text =
                            {
                                Text = ""
                            }
                        },
                        $"{UI_Layer}.Follow"
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.Ride",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = GetColor("#CA2539", 0.8f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "115 -100",
                                    OffsetMax = "165 -50"
                                },
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = $"{UI_Layer}.Ride",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _plugin.GetImage(GetNameByURL(_config.RideImage))
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1",
                                }
                            }
                        }
                    },
                    {
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                            },
                            Button =
                            {
                                Color = "0 0 0 0", Command = "UI_BearControl ride {bear}", Close = UI_Layer
                            },
                            Text =
                            {
                                Text = ""
                            }
                        },
                        $"{UI_Layer}.Ride"
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.Inventory",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = GetColor("#CA2539", 0.8f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "170 -100",
                                    OffsetMax = "220 -50"
                                },
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = $"{UI_Layer}.Inventory",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _plugin.GetImage(GetNameByURL(_config.InventoryImage))
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1"
                                }
                            }
                        }
                    },
                    {
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            Button =
                            {
                                Color = "0 0 0 0", Command = "UI_BearControl inventory {bear}", Close = UI_Layer
                            },
                            Text =
                            {
                                Text = ""
                            }
                        },
                        $"{UI_Layer}.Inventory" 
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.Take",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = GetColor("#CA2539", 0.8f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "225 -100",
                                    OffsetMax = "275 -50"
                                },
                            }
                        }
                    },
                    {
                        new CuiElement
                        {
                            Parent = $"{UI_Layer}.Take",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _plugin.GetImage(GetNameByURL(_config.TakeImage))
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1"
                                }
                            }
                        }
                    },
                    {
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                            },
                            Button =
                            {
                                Color = "0 0 0 0", Command = "UI_BearControl pickup {bear}", Close = UI_Layer
                            },
                            Text =
                            {
                                Text = ""
                            }
                        },
                        $"{UI_Layer}.Take"
                    },
                };
                AddInterface("BearMain", container.ToJson());
            }
        }
        private const String PermissionsGiveTeddyBear = "iqbearride.giveteddy";
        
        
        private class BearController : MonoBehaviour
        {
            
            private void ModifyRunState(int dir)
            {
                if (this.currentRunState == BaseRidableAnimal.RunState.stopped && dir < 0 || this.currentRunState == BaseRidableAnimal.RunState.sprint && dir > 0)
                    return;
                this.SwitchMoveState(this.currentRunState + dir);
            }

			private bool AttackReady()
			{
				return Time.realtimeSinceStartup >= nextAttackTime;
			}
            
            public void AnimalDecay()
            {
                if (BearOwner.healthFraction == 0.0 || BearOwner.IsDestroyed || (Time.time < (lastInputTime + 600.0) || Time.time < (lastEatTime + 600.0)))
                    return;
                
                BearOwner.Hurt(BearOwner.MaxHealth() * (1f / BaseRidableAnimal.decayminutes) * (!BearOwner.IsOutside() ? 1f : 0.5f), DamageType.Decay, BearOwner, false);
            }

            private float nextAttackTime;
            
            public float GetDesiredVelocity()
            {
                return this.MoveStateToVelocity(this.currentRunState);
            }

            private void Start()
            {
                var data = PluginData.Get(BearOwner.net.ID.Value);
                
                if (data.GrowSeconds > 0)
                {
                    ShouldGrow = true;
                    GrowingSeconds = data.GrowSeconds;
                    StartCoroutine(DoGrow(data));
                }
            }

            private float GetRunSpeed()
            {
                return MoveStateToVelocity(currentRunState);
            }
            
            public bool HasDriver()
            {
                return _rider != null;
            }
            
            public float MoveStateToVelocity(BaseRidableAnimal.RunState stateToCheck)
            {
                float num;
                switch (stateToCheck)
                {
                    case BaseRidableAnimal.RunState.walk:
                        num = HasLead() ? leadSpeed : walkSpeed;
                        break;
                    case BaseRidableAnimal.RunState.run:
                        num = runSpeed;
                        break;
                    case BaseRidableAnimal.RunState.sprint:
                        num = sprintSpeed;
                        break;
                    default:
                        num = 0.0f;
                        break;
                }
                return num;
            }
            
            public OBB WorldSpaceBounds()
            {
                return new OBB(BearOwner.transform.position, BearOwner.transform.lossyScale, BearOwner.transform.rotation, BearOwner.bounds);
            }
            private float desiredRotation;

            private float lastDdrawUpdate = Time.time;
            
            public virtual float WaterFactor()
            {
                return WaterLevel.Factor(this.WorldSpaceBounds().ToBounds(), BearOwner); 
            }
            private float nextEatTime = Time.time;
            
            private BaseRidableAnimal.RunState StateFromSpeed(float speedToUse)
            {
                if ((double) speedToUse <= (double) this.MoveStateToVelocity(BaseRidableAnimal.RunState.stopped))
                    return BaseRidableAnimal.RunState.stopped;
                if ((double) speedToUse <= (double) this.MoveStateToVelocity(BaseRidableAnimal.RunState.walk))
                    return BaseRidableAnimal.RunState.walk;
                return (double) speedToUse <= (double) this.MoveStateToVelocity(BaseRidableAnimal.RunState.run) ? BaseRidableAnimal.RunState.run : BaseRidableAnimal.RunState.sprint;
            }

            
            
            private float ObstacleSpeed(float obstacleDistance)
            {
                float num = 0;
                
                if (obstacleDistance > 3)
                {
                    float runSpeed = GetRunSpeed();
                    num = runSpeed * obstacleDistance / 30;
                    if (obstacleDistance > 4)
                    {
                        num = runSpeed;
                    }
                }
                
                return num;
            }
            private Vector3 targetUp = Vector3.up;
            public float maxWaterDepth = _config.MaxWaterDepth;
            
                        
            private float timeInMoveState;
            public TreeEntity CheckTree(Vector3 pos, Vector3 aim)
            {
                RaycastHit[] hit;
                hit = Physics.SphereCastAll(pos, 2.5f, Vector3.forward, 2.5f, LayerMask.GetMask("Tree"));
		   		 		  						  	   		  	 				  						   					   		 
                foreach (var hits in hit)
                {
                    if (hits.GetEntity() == null) continue;
		   		 		  						  	   		  	 				  						   					   		 
                    TreeEntity Tree = hits.GetEntity() as TreeEntity;
                    if (Tree == null) continue;
                    return Tree;
                }
                return null;
            }
		   		 		  						  	   		  	 				  						   					   		 
            public DamageType AttackDamageType = DamageType.Bite;
		   		 		  						  	   		  	 				  						   					   		 
            private bool IsVisible(Ray ray, float maxDistance = float.PositiveInfinity)
            {
                RaycastHit hit;
                if (ray.origin.IsNaNOrInfinity() || ray.direction.IsNaNOrInfinity() || ray.direction == Vector3.zero ||
                    !BearOwner.WorldSpaceBounds().Trace(ray, out hit, maxDistance))
                    return false;
                RaycastHit hitInfo;
                if (!GamePhysics.Trace(ray, 0.0f, out hitInfo, maxDistance, 1218519041))
                    return true;
                var entity = hitInfo.GetEntity();
                return entity == BearOwner ||
                       entity != null && (bool) BearOwner.GetParentEntity() &&
                       (BearOwner.GetParentEntity().EqualNetID(entity) && hitInfo.IsOnLayer(Rust.Layer.Vehicle_Detailed)) ||
                       hitInfo.distance > (double) hit.distance;
            }
            private float backwardHeldSeconds;
            private float lastForwardPressedTime;
            
            private bool IsVisible(Vector3 position, Vector3 target, float maxDistance = float.PositiveInfinity)
            {
                var vector3_1 = target - position;
                var magnitude = vector3_1.magnitude;
                if (magnitude < Mathf.Epsilon)
                    return true;

                var direction = vector3_1 / magnitude;
                var vector3_2 = direction * Mathf.Min(magnitude, 0.01f);
                return IsVisible(new Ray(position + vector3_2, direction), maxDistance);
            }
            
            private void RiderInput(InputState inputState, BasePlayer player)
            {
                float num1 = UnityEngine.Time.time - this.lastInputTime;
                this.lastInputTime = UnityEngine.Time.time;
                float num2 = Mathf.Clamp(num1, 0.0f, 1f); 
                Vector3 zero = Vector3.zero;
                this.timeInMoveState += num2; 
                if (inputState == null)
                    return;
                if (inputState.IsDown(BUTTON.FORWARD))
                {
                    this.lastForwardPressedTime = UnityEngine.Time.time;
                    this.forwardHeldSeconds += num2;
                }
                else
                    this.forwardHeldSeconds = 0.0f;

                if (inputState.IsDown(BUTTON.BACKWARD))
                {
                    this.lastBackwardPressedTime = UnityEngine.Time.time;
                    this.backwardHeldSeconds += num2;
                }
                else
                    this.backwardHeldSeconds = 0.0f;

                if (inputState.IsDown(BUTTON.SPRINT))
                {
                    this.lastSprintPressedTime = UnityEngine.Time.time;
                    this.sprintHeldSeconds += num2;
                } 
                else
                    this.sprintHeldSeconds = 0.0f;
                if ((double) this.forwardHeldSeconds > 0.0)
                {
                    if (this.currentRunState == BaseRidableAnimal.RunState.stopped)
                        this.SwitchMoveState(BaseRidableAnimal.RunState.walk);
                    else if (this.currentRunState == BaseRidableAnimal.RunState.walk)
                    {
                        if ((double) this.sprintHeldSeconds > 0.0)
                            this.SwitchMoveState(BaseRidableAnimal.RunState.run);
                    }
                    else if (this.currentRunState == BaseRidableAnimal.RunState.run && (double) this.sprintHeldSeconds > 1.0)// && this.CanInitiateSprint())
                        this.SwitchMoveState(BaseRidableAnimal.RunState.sprint);
                }
                else if ((double) this.backwardHeldSeconds > 1.0)
                {
                    this.ModifyRunState(-1);
                    this.backwardHeldSeconds = 0.1f;
                }
                else if ((double) this.backwardHeldSeconds == 0.0 && (double) this.forwardHeldSeconds == 0.0 && ((double) this.timeInMoveState > 1.0 && this.currentRunState != BaseRidableAnimal.RunState.stopped))
                    this.ModifyRunState(-1);

                if (this.currentRunState == BaseRidableAnimal.RunState.sprint && (/*!this.CanSprint() ||*/ (double) UnityEngine.Time.time - (double) this.lastSprintPressedTime > 5.0))
                    this.ModifyRunState(-1);
                if (inputState.IsDown(BUTTON.RIGHT))
                {
                    this.desiredRotation = 1f;
                }
                else if (inputState.IsDown(BUTTON.LEFT))
                {
                    this.desiredRotation = -1f;
                }
                else
                    this.desiredRotation = 0.0f;
            }
            public void Sphere(BasePlayer player, Vector3 pos, float radius, Color color, float duration)
            {
                player.SendConsoleCommand("ddraw.sphere", duration, color, pos, radius);
            }
            private TimeUntil dropUntilTime;

            
            
            private void EatNearbyFood()
            {
                if (Time.time < (double) nextEatTime || currentRunState != BaseRidableAnimal.RunState.stopped)
                    return;
				
                var num = BearOwner.healthFraction;
                nextEatTime =
                    (float) (Time.time + (double) Core.Random.Range(2f, 3f) + Mathf.InverseLerp(0.5f, 1f, num) * 4.0);
                if (num >= 1.0)
                    return;
                var list = Pool.GetList<BaseEntity>();
                var transform1 = BearOwner.transform;
                Vis.Entities(transform1.position + transform1.forward * 1.5f, 2f, list, 67109377);
                list.Sort((a, b) => (b is DroppedItem).CompareTo(a is DroppedItem));
                foreach (var baseEntity in list)
                    if (!baseEntity.isClient)
                    {
                        var droppedItem = baseEntity as DroppedItem;
                        if ((bool) droppedItem && droppedItem.item != null &&
                            droppedItem.item.info.category == ItemCategory.Food)
                        {
                            var component = droppedItem.item.info.GetComponent<ItemModConsumable>();
                            if ((bool) component)
                            {
                                lastEatTime = Time.time;
                                BearOwner.Heal(_config.EatHealAmount);
                                
                                BearOwner.SignalBroadcast(BaseEntity.Signal.Attack);
                                BearOwner.ClientRPC(null, "Attack", BearOwner.ServerPosition + Vector3.forward);
                                
                                droppedItem.item.UseItem();
                                if (droppedItem.item.amount <= 0)
                                {
                                    droppedItem.Kill();
                                }

                                break;
                            }
                        }

                        var collectibleEntity = baseEntity as CollectibleEntity;
                        if ((bool) collectibleEntity && collectibleEntity.IsFood())
                        {
                            collectibleEntity.DoPickup(null);
                            break;
                        }

                        var growableEntity = baseEntity as GrowableEntity;
                        if ((bool) growableEntity && growableEntity.CanPick())
                        {
                            growableEntity.PickFruit(null);
                            break;
                        }
                    }

                Pool.FreeList(ref list);
            }

            public float ObstacleDistanceCheck(float speed = 10f)
            {
                int num1 = Mathf.Max(2, Mathf.Min((int) speed, 10));
                float num2 = 0.5f;
                int num3 = Mathf.CeilToInt((float) num1 / num2);
                float num4 = 0.0f;
                Vector3 direction = QuaternionEx.LookRotationForcedUp(BearOwner.transform.forward, Vector3.up) * Vector3.forward;
                Vector3 vector3_1 = BearOwner.transform.position + BearOwner.transform.forward * 0.2f + new Vector3(0, 0.3f); 
                
                vector3_1.y = BearOwner.transform.position.y;
                Vector3 up = BearOwner.transform.up;
                for (int index1 = 0; index1 < num3; ++index1)
                {
                    float maxDistance = num2;
                    bool flag = false;
                    float num5 = 0.0f;
                    Vector3 pos1 = Vector3.zero;
                    Vector3 normal1 = Vector3.up;
                    Vector3 a = vector3_1;
                    Vector3 origin = a + Vector3.up * (this.maxStepHeight + this.obstacleDetectionRadius);
                    Vector3 vector3_2 = a + direction * maxDistance;
                    float num6 = this.maxStepDownHeight + this.obstacleDetectionRadius;
                    RaycastHit hitInfo;

                    if (UnityEngine.Physics.SphereCast(origin, this.obstacleDetectionRadius, direction, out hitInfo, maxDistance, 1486954753))
                    {
                        num5 = hitInfo.distance;
                        pos1 = hitInfo.point;
                        normal1 = hitInfo.normal;
                        flag = true;
                    }

                    if (!flag)
                    {
                        if (!TransformUtil.GetGroundInfo(vector3_2 + Vector3.up * 2f, out pos1, out normal1, 2f + num6, (LayerMask) 278986753, (Transform) null))
                            return num4;
                        num5 = Vector3.Distance(a, pos1);
                        if (WaterLevel.Test(pos1 + Vector3.one * this.maxWaterDepth, true, BearOwner))
                        {
                            return num4;
                        }

                        flag = true;
                    }

                    if (flag)
                    {
                        double num7 = (double) Vector3.Angle(up, normal1);
                        float num8 = Vector3.Angle(normal1, Vector3.up);
                        if (num7 > maxWallClimbSlope || (double) num8 > (double) this.maxWallClimbSlope)
                        {
                            Vector3 vector3_3 = normal1;
                            float y = pos1.y;
                            int num9 = 1;
                            for (int index2 = 0; index2 < this.normalOffsets.Length; ++index2)
                            {
                                Vector3 vector3_4 = vector3_2 + this.normalOffsets[index2].x * BearOwner.transform.right;
                                float num10 = this.maxStepHeight * 2.5f;
                                Vector3 vector3_5 = Vector3.up * num10;
                                Vector3 pos2;
                                Vector3 normal2;
                                if (TransformUtil.GetGroundInfo(vector3_4 + vector3_5 + this.normalOffsets[index2].z * BearOwner.transform.forward, out pos2, out normal2, num6 + num10, (LayerMask) 278986753, (Transform) null))
                                {
                                    ++num9;
                                    vector3_3 += normal2;
                                    y += pos2.y;
                                }
                            }
		   		 		  						  	   		  	 				  						   					   		 
                            float num11 = y / (float) num9;
                            vector3_3.Normalize();
                            double num12 = (double) Vector3.Angle(up, vector3_3);
                            float num13 = Vector3.Angle(vector3_3, Vector3.up);
                            if (num12 > maxWallClimbSlope || (double) num13 > (double) this.maxWallClimbSlope || (double) Mathf.Abs(num11 - vector3_2.y) > (double) this.maxStepHeight)
                                return num4;
                        }
                    }

                    num4 += num5;
                    direction = QuaternionEx.LookRotationForcedUp(BearOwner.transform.forward, normal1) * Vector3.forward;
                    vector3_1 = pos1;
                }

                return num4;
            }
            public float runSpeed = _config.RunSpeed; 

            public float AttackRate = 1f;
            public float obstacleDetectionRadius = _config.ObsCheckDis;
            
            public float currentSpeed;
            
            public float maxWallClimbSlope = _config.MaxWallClimbSlope;
            private float nextGroundNormalUpdateTime;

            
            private void ClearBear()
            {
                if (BearOwner == null)
                    return;
                
                
                Invoke(nameof(RemoveMovement), 0.1f);                  
            }
            
            public float GetObstacleDistance()
            {
                if ((double) UnityEngine.Time.time >= (double) this.nextObstacleCheckTime)
                {
                    float desiredVelocity = this.GetDesiredVelocity();
                    if ((double) this.currentSpeed > 0.0 || (double) desiredVelocity > 0.0)
                        this.cachedObstacleDistance = this.ObstacleDistanceCheck(Mathf.Max(desiredVelocity, 2f));
                    this.nextObstacleCheckTime = UnityEngine.Time.time + UnityEngine.Random.Range(0.25f, 0.35f);
                }
                return this.cachedObstacleDistance;
            }

            public float AttackDamage(string targetName)
            {
                float value = _config.BearDamage["default"];

                foreach (var keyValuePair in _config.BearDamage)
                {
                    if (targetName.Contains(keyValuePair.Key))
                        value = keyValuePair.Value;
                }
                
                return value;
            } 
            public int GrowingSeconds = 0;

            [NonSerialized] private BaseEntity[] SensesResults = new BaseEntity[64];
            
            private void TryAttack()
			{
				var inSphere = BaseEntity.Query.Server.GetInSphere(BearOwner.transform.position, VisionRange, SensesResults,
					AiCaresAbout);
				if (inSphere > 0)
                {
                    BaseEntity foundTarget = null;
                    float minDistance = float.MaxValue;
					for (var index = 0; index < inSphere; ++index)
					{
						var sensesResult = SensesResults[index];
						if (sensesResult == null || sensesResult.IsDestroyed
						                         || sensesResult.net.ID == _rider.net.ID
						                         || sensesResult.net.ID == BearOwner.net.ID) continue;
                        
                        var dist = Vector3.Distance(sensesResult.transform.position, BearOwner.transform.position);
                        if (minDistance > dist)
                        {
                            foundTarget = sensesResult;
                            minDistance = dist;
                        }
                    }
                    
                    if (foundTarget != null && WithinVisionCone(BearOwner, foundTarget))
                    {
                        var attackPosition = BearOwner.ServerPosition + BearOwner.transform.TransformDirection(BearOwner.AttackOffset);
                        if (IsVisible(attackPosition, foundTarget.CenterPoint()) || IsVisible(attackPosition, BearOwner.transform.position))
                        {
                            StartAttack(/*AttackOperator.AttackType.MediumRange,*/ foundTarget);
                            return;
                        }
                    }
                }
                
                StartAttack(/*AttackOperator.AttackType.CloseRange, */null);
			}
            private void StartAttack(/*AttackOperator.AttackType type,*/ BaseEntity entity)
			{
				if (!AttackReady() /*|| _bear.GetFact(BaseNpc.Facts.IsAttackReady) == 0*/)
					return;
				
				var target = entity as BaseCombatEntity;

                var Tree = CheckTree(BearOwner.transform.position, _rider.eyes.HeadForward());
                if (Tree != null)
                {
                    float DamageBear = AttackDamage(Tree.ShortPrefabName);
                    float MainFormul = Mathf.Min(DamageBear, Tree.Health()) / Tree.MaxHealth();
                    float Result = (Mathf.Clamp(DamageBear * UnityEngine.Random.Range(1, 5) / UnityEngine.Random.Range(1, 5), 0f, MainFormul * 8)) * 2.5f * 2f;
                    Int32 Amount = Mathf.FloorToInt(Result) + UnityEngine.Random.Range(1, 20);

                    global::Item item = global::ItemManager.CreateByName("wood", Amount, 0UL);
                    if (item == null)
                    {
                        return;
                    }

                    Interface.Call("OnDispenserGather", Tree.resourceDispenser, _rider, item);
                    var Data = _data.BearsData.Find(x => x.EntityId == BearOwner.net.ID.Value);
                    
                    _rider.SendConsoleCommand($"note.inv 605467368 {item.amount} \"{_plugin.GetLang("BEAR_FARM_WOOD", _rider.UserIDString)}\""); 
                    
                    item.MoveToContainer(Data.Container);

                    if (Tree.health > 0f)
                    {
                        Tree.ClientRPC<int>(null, "CrackSound", 1);
                        Tree.health -= DamageBear + UnityEngine.Random.Range(1, 50);
                    }
                    else
                    {
                        Tree.ClientRPC<Vector3>(null, "TreeFall", _rider.transform.position);
                        Tree.Invoke(() => Tree.Kill(BaseNetworkable.DestroyMode.None), 3f);
                    }
                }

				if (target != null)
				{
					var vector3 = target.ServerPosition - BearOwner.ServerPosition;
					var magnitude = vector3.magnitude;
					if (magnitude <= AttackRange)
                    {
                        if (magnitude > 1.0 / 1000.0)
                            BearOwner.ServerRotation = Quaternion.LookRotation(vector3.normalized);

                        nextAttackTime = Time.realtimeSinceStartup + AttackRate;
                        
                        _rider.MarkHostileFor(60f);
                        target.Hurt(AttackDamage(target.ShortPrefabName), AttackDamageType, BearOwner);

                        BearOwner.BusyTimer.Activate(0.5f);
                        BearOwner.SignalBroadcast(BaseEntity.Signal.Attack);
                        BearOwner.ClientRPC(null, "Attack", target.ServerPosition);
                    
                        return;
                    }
                }
				
                nextAttackTime = Time.realtimeSinceStartup + AttackRate;

                BearOwner.BusyTimer.Activate(0.5f);
                BearOwner.SignalBroadcast(BaseEntity.Signal.Attack);
                BearOwner.ClientRPC(null, "Attack", BearOwner.ServerPosition + Vector3.forward);
            }
            private BasePlayer _leadTarget;

            private void LeadUpdate()
            {
                if (_leadTarget == null)
                    return;
                
                Vector3 position = _leadTarget.transform.position;
                Vector3 lhs1 = Vector3Ex.Direction2D(BearOwner.transform.position + BearOwner.transform.right * 1f, BearOwner.transform.position);
                Vector3 lhs2 = Vector3Ex.Direction2D(BearOwner.transform.position + BearOwner.transform.forward * 0.01f, BearOwner.transform.position);
                Vector3 rhs1 = Vector3Ex.Direction2D(position, BearOwner.transform.position);
                float num2 = Vector3.Dot(lhs1, rhs1);
                Vector3 rhs2 = rhs1;
                float num3 = Vector3.Dot(lhs2, rhs2);
                bool flag = (double) Vector3Ex.Distance2D(position, BearOwner.transform.position) > 2.5;
                int num4 = (double) Vector3Ex.Distance2D(position, BearOwner.transform.position) > 10.0 ? 1 : 0;
                if (flag || (double) num3 < 0.949999988079071)
                {
                    float num5 = Mathf.InverseLerp(0.0f, 1f, num2);
                    float num6 = 1f - Mathf.InverseLerp(-1f, 0.0f, num2);
                    this.desiredRotation = 0.0f;
                    this.desiredRotation += num5 * 1f;
                    this.desiredRotation += num6 * -1f;
                    if ((double) Mathf.Abs(this.desiredRotation) < 1.0 / 1000.0)
                        this.desiredRotation = 0.0f;
                    if (flag)
                    {
                        acceleration = 1;
                        this.SwitchMoveState(BaseRidableAnimal.RunState.walk);
                    }
                    else
                        this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                }
                else
                {
                    this.desiredRotation = 0.0f;
                    this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                }
                if (num4 != 0)
                {
                    SetLead(null);
                    this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                }
            }
            private float forwardHeldSeconds;
            
            public bool ShouldGrow = false;
            
            public void UpdateMovement(float delta)
            {
                float num1 = this.WaterFactor();
                if (Math.Abs(desiredRotation) > 0.1f)
                    this.MarkObstacleDistanceDirty();
                if (num1 >= 0.3f && this.currentRunState > BaseRidableAnimal.RunState.run)
                    this.currentRunState = BaseRidableAnimal.RunState.run;
                else if (num1 >= 0.45f && this.currentRunState > BaseRidableAnimal.RunState.walk)
                    this.currentRunState = BaseRidableAnimal.RunState.walk;
                if (Time.time - this.lastInputTime > 3.0 && HasLead() == false)
                {
                    this.currentRunState = BaseRidableAnimal.RunState.stopped;
                    this.desiredRotation = 0.0f;
                }

                if (HasLead())
                {
                    Vector3 bearPosition = BearOwner.transform.position;
                    Vector3 leadPosition = _leadTarget.transform.position;
                    Vector3 lhs1 = Vector3Ex.Direction2D(bearPosition + BearOwner.transform.right * 1f, bearPosition);
                    Vector3 lhs2 = Vector3Ex.Direction2D(bearPosition + BearOwner.transform.forward * 0.01f, bearPosition);
                    Vector3 rhs1 = Vector3Ex.Direction2D(leadPosition, bearPosition);
                    float num2 = Vector3.Dot(lhs1, rhs1);
                    Vector3 rhs2 = rhs1;
                    float num3 = Vector3.Dot(lhs2, rhs2);
                    bool flag = Vector3Ex.Distance2D(leadPosition, bearPosition) > 2.5;
                    int num4 = (double) Vector3Ex.Distance2D(leadPosition, bearPosition) > 10.0 ? 1 : 0;
                    if (flag || num3 < 0.95f)
                    {
                        float num5 = Mathf.InverseLerp(0.0f, 1f, num2);
                        float num6 = 1f - Mathf.InverseLerp(-1f, 0.0f, num2);
                        this.desiredRotation = 0.0f;
                        this.desiredRotation += num5 * 1f;
                        this.desiredRotation += num6 * -1f;
                        if ((double) Mathf.Abs(this.desiredRotation) < 1.0 / 1000.0)
                            this.desiredRotation = 0.0f;
                        if (flag)
                            this.SwitchMoveState(BaseRidableAnimal.RunState.walk);
                        else
                            this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                    }
                    else
                    {
                        this.desiredRotation = 0.0f;
                        this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                    }

                    if (num4 != 0)
                    {
                        SetLead(null);
                        this.SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                    }
                }

                float obstacleDistance = this.GetObstacleDistance();
                BaseRidableAnimal.RunState newState = this.StateFromSpeed(obstacleDistance * this.GetRunSpeed());
                if (newState < this.currentRunState)
                    this.SwitchMoveState(newState);
                float desiredVelocity = this.GetDesiredVelocity();
                Vector3 direction = Vector3.forward * Mathf.Sign(desiredVelocity);
                float num7 = Mathf.InverseLerp(0.85f, 1f, obstacleDistance);
                float num8 = Mathf.InverseLerp(1.25f, 10f, obstacleDistance);
                double num9 = 1.0 - (double) Mathf.InverseLerp(20f, 45f, Vector3.Angle(Vector3.up, this.averagedUp));

                float b1 = (float) ((double) num7 * 0.100000001490116 + (double) num8 * 0.9f);
                float b2 = Mathf.Min(Mathf.Clamp01(Mathf.Min((float) (num9 + 0.2f), b1)) * this.GetRunSpeed(), desiredVelocity);
                float num10 = (double) b2 < (double) this.currentSpeed ? 3f : 1f;
                this.currentSpeed = (double) Mathf.Abs(this.currentSpeed) >= 2.0 || Math.Abs(desiredVelocity) > 0.1f ? Mathf.Lerp(this.currentSpeed, b2, delta * num10) : Mathf.MoveTowards(this.currentSpeed, 0.0f, delta * 3f);
                if (Math.Abs(b1) < 0.01f)
                    this.currentSpeed = 0.0f;
                float num11 = (float) (((1f - Mathf.InverseLerp(2f, 7f, this.currentSpeed)) + 1.0) / 2.0);
                
                Vector3 frontPosition = BearOwner.transform.position + BearOwner.transform.forward * 1.1f + new Vector3(0, 1.4f, 0);
                //BasePlayer.activePlayerList[0].SendConsoleCommand("ddraw.sphere", 0.1f, Color.green, frontPosition, 0.1f);
		   		 		  						  	   		  	 				  						   					   		 
                if (Math.Abs(this.desiredRotation) > 0.1f)
                {
                    var bearTransform = BearOwner.transform;
                    Quaternion rotation = bearTransform.rotation;
                    
                    bearTransform.Rotate(Vector3.up, this.desiredRotation * delta * this.rotateSpeed * num11);
                    Vector3 newfrontPosition = bearTransform.position + bearTransform.forward * 1.1f + new Vector3(0, 1.4f, 0) + (bearTransform.right * desiredRotation) * 0.2f;

                     if (!HasLead() && Vis.AnyColliders(newfrontPosition, this.obstacleDetectionRadius * _config.RotSphereDis, 1503731969, QueryTriggerInteraction.Ignore))
                     { 
                         BearOwner.transform.rotation = rotation;
                     }
                }

                Vector3 vector3_1 = BearOwner.transform.TransformDirection(direction);
                Vector3 normalized = vector3_1.normalized;
                float num12 = this.currentSpeed * delta;
                Vector3 vector3_2 = BearOwner.transform.position + normalized * (num12 * Mathf.Sign(this.currentSpeed));
                this.currentVelocity = vector3_1 * this.currentSpeed;
                this.UpdateGroundNormal(false);
                if ((double) this.currentSpeed <= 0.0 && (double) this.timeAlive >= 2.0 && (double) (float) this.dropUntilTime <= 0.0)
                    return;
                RaycastHit hitInfo;
                bool flag1 = UnityEngine.Physics.SphereCast(frontPosition, this.obstacleDetectionRadius, normalized, out hitInfo, num12, 1503731969);
                bool flag2 = UnityEngine.Physics.SphereCast(BearOwner.transform.position + BearOwner.transform.InverseTransformPoint(frontPosition).y * BearOwner.transform.up, this.obstacleDetectionRadius, normalized, out hitInfo, num12, 1503731969);
                
                if (!Vis.AnyColliders(frontPosition + normalized * num12, this.obstacleDetectionRadius, 1503731969, QueryTriggerInteraction.Ignore) && !flag1 && !flag2)
                {
                    if (this.DropToGround(vector3_2 + Vector3.up * this.maxStepHeight, false) == false)
                        this.currentSpeed = 0.0f;
                }
                else
                {
                    this.currentSpeed = 0.0f;
                }

            }
            private void Awake()
            {
                try
                {
                    BearOwner = GetComponent<Bear>();
                    if (BearOwner == null)
                    {
                        Destroy(this);
                        return;
                    }
                
                    BearOwner.SetMaxHealth(_config.MaxHealthAmount);
                    BearOwner.SetHealth(_config.MaxHealthAmount);
                
                    _bearControllers.Add(BearOwner.net.ID.Value, this);

                    ClearBear();

                    InvokeRepeating(nameof(PlayerInput), 0.01f, 0.01f);
                    InvokeRepeating(nameof(PlayersChecker), 0.1f, 0.1f);
                    InvokeRepeating(nameof(AnimalDecay), 60f, 60f);
                    
                    if (!_config.BlockDefecateBear)
                        InvokeRepeating(nameof(DoDung), 60f, 210f);
                
                    AddSaddle();
                }
                catch (Exception e)
                {
                    ulong id = BearOwner?.net.ID.Value ?? 0;
                    if(_bearControllers.ContainsKey(id))
                        _bearControllers.Remove(id);
                    Destroy(this);
                    
                    Debug.LogError($"[BearRide] Error in 'Awake' method! Message: {e.Message}\n {e.StackTrace}");
                }
		   		 		  						  	   		  	 				  						   					   		 
            }
            private BasePlayer _rider; 
            
            public void UpdateGroundNormal(bool force = false)
            {
                if ((double) UnityEngine.Time.time >= (double) this.nextGroundNormalUpdateTime | force)
                {
                    this.nextGroundNormalUpdateTime = UnityEngine.Time.time + UnityEngine.Random.Range(0.2f, 0.3f);
                    this.targetUp = this.averagedUp;

                    var bearPos = BearOwner.transform.position;
                    var forward = BearOwner.transform.forward;
                    var right = BearOwner.transform.right;

                    var totalForward = forward * _config.BoardForward;
                    var totalRight = right * _config.BoardRight;
                    
                    Vector3[] pawsPos = new Vector3[]
                    {
                        bearPos,
                        bearPos + totalForward + totalRight,
                        bearPos + totalForward - totalRight,
                        bearPos - totalForward + totalRight,
                        bearPos - totalForward - totalRight,
                    };
                    
                    foreach (var groundSampleOffset in pawsPos)
                    {
                        Vector3 normal;
                        Vector3 _;
                        if (TransformUtil.GetGroundInfo(groundSampleOffset + Vector3.up * 2f, out _, out normal, 4f, (LayerMask) 278986753, (Transform) null))
                            this.targetUp += normal;
                        else
                            this.targetUp += Vector3.up;
                    }
                    this.targetUp /= (float) (pawsPos.Length + 1);
                }
                this.averagedUp = Vector3.Lerp(this.averagedUp, this.targetUp, UnityEngine.Time.deltaTime * 2f);
            }
            
            //Obstacle check variables
            private float cachedObstacleDistance = float.PositiveInfinity;

            public bool DropToGround(Vector3 targetPos, bool force = false) 
            {
                float range = force ? 10000f : this.maxStepHeight + this.maxStepDownHeight;
                Vector3 pos = targetPos;
                Vector3 _;

                if (TransformUtil.GetGroundInfo(targetPos, out pos, out _, range, 278986753) == false)
                    return false;
                
                if (UnityEngine.Physics.CheckSphere(pos + Vector3.up * 1f, 0.2f, 278986753))
                    return false;
                
                float num = WaterFactor();
                if (num > 0.1f)
                {
                    pos.y += num;
                }

                BearOwner.transform.position = pos;
                return true;
            }
            public float maxStepHeight = _config.StepHigh;
            private float sprintHeldSeconds;

            
            
            public float VisionRange = 0.5f;

            
            
            private float lastEatTime = Time.time;
            
            
            
            private void DoDung()
            {
                ItemManager.CreateByName("horsedung", 1, 0UL).Drop(BearOwner.transform.position + -BearOwner.transform.forward + Vector3.up * 1.1f + UnityEngine.Random.insideUnitSphere * 0.1f, -BearOwner.transform.forward, default(Quaternion));
            }

            //Angle check variables
            private Vector3 averagedUp = Vector3.up;

            public void DelayedDropToGround()
            {
                this.DropToGround(BearOwner.transform.position, true);
                this.UpdateGroundNormal(true);
            }
            
            private void AddSaddle()
            {
                BaseVehicleSeat saddle = GameManager.server.CreateEntity("assets/prefabs/vehicle/seats/saddletest.prefab", new Vector3(0, 0.8f) - BearOwner.transform.forward * 0.15f) as BaseVehicleSeat;
                if (saddle == null)
                {
                    return;
                }

                saddle.SetParent(BearOwner);
                saddle.Spawn();
                _saddle = saddle;
            }
            
            //Time check variables
            private float nextObstacleCheckTime;

            public bool HasLead() => _leadTarget != null;
            private float lastBackwardPressedTime;
            private Vector3[] normalOffsets = new Vector3[7]
            {
                new Vector3(0.15f, 0.0f, 0.0f),
                new Vector3(-0.15f, 0.0f, 0.0f), 
                new Vector3(0.0f, 0.0f, 0.15f),
                new Vector3(0.0f, 0.0f, 0.3f),
                new Vector3(0.0f, 0.0f, 0.6f),
                new Vector3(0.15f, 0.0f, 0.3f),
                new Vector3(-0.15f, 0.0f, 0.3f)
            };
            private float timeAlive;

            public void SetLead(BasePlayer player)
            {
                if(player != null)
                    SetRider(null);
                
                _leadTarget = player;
            }
            public float walkSpeed = _config.WalkSpeed;
            
            private IEnumerator DoGrow(PluginData.BearData data)
            {                
                int startTime = GrowingSeconds;
                int curTime = 0;
                
                while (curTime < startTime)
                {
                    curTime++;
                    GrowingSeconds--;
                    data.GrowSeconds = GrowingSeconds;
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                
                ShouldGrow = false;

                yield break;
            }
            
            public void SwitchMoveState(BaseRidableAnimal.RunState newState)
            {
                if (newState == this.currentRunState)
                    return;
                this.currentRunState = newState;
                this.timeInMoveState = 0.0f;
                BearOwner.SetFlag(BaseEntity.Flags.Reserved8, this.currentRunState == BaseRidableAnimal.RunState.sprint, false, false);
                this.MarkObstacleDistanceDirty();
            }

            
            
            
            private void PlayerInput()
            {
                if (HasDriver() == false)
                    return;
                
                InputState inputState = _rider.serverInput;

                RiderInput(inputState, _rider);
                
                if (inputState.IsDown(BUTTON.USE))
                {
                    SwitchMoveState(BaseRidableAnimal.RunState.stopped);
                    TryAttack();
                }
            }
            public float sprintSpeed = _config.SprintSpeed; 
            
            
            
                        
            public float lastInputTime; 
            
            private void UpdateDropToGroundForDuration(float duration)
            {
                this.dropUntilTime = (TimeUntil) duration;
            }
            
            private static bool AiCaresAbout(BaseEntity ent)
            {
                if (ent is BasePlayer || ent is BaseNpc || ent.ShortPrefabName.Contains("barrel"))
                    return true;

                return false;
            }
            
            private void RemoveMovement()
            {
                if (BearOwner == null)
                    return;
                
                BearOwner.StopMoving();
                BearOwner.GetNavAgent.autoRepath = false; 
                BearOwner.Pause();
                
                BearOwner.CancelInvoke(BearOwner.TickAi);
                BearOwner.CancelInvoke(BearOwner.TickNavigation);
                BearOwner.CancelInvoke(BearOwner.TickStuck);
                BearOwner.CancelInvoke(BearOwner.TickNavigationWater);
                
                var script1 = BearOwner.GetComponent<AiManagedAgent>();
                UnityEngine.Object.Destroy(script1);
                //var script2 = BearOwner.GetComponent<UtilityAIComponent>();
                //UnityEngine.Object.Destroy(script2);
                
                AIThinkManager.RemoveAnimal(BearOwner);

                BearOwner.CurrentBehaviour = BaseNpc.Behaviour.Idle; 
            }
            public Bear BearOwner;
            
            private BaseRidableAnimal.RunState currentRunState = BaseRidableAnimal.RunState.stopped;

            public float AttackRange = 2f;
            public Vector3 currentVelocity;
		   		 		  						  	   		  	 				  						   					   		 
            private float acceleration;
            private BaseVehicleSeat _saddle;
            
            public void MarkObstacleDistanceDirty()
            {
                this.nextObstacleCheckTime = 0.0f;
            }
            
            private static bool WithinVisionCone(BaseNpc npc, BaseEntity other)
            {
                if (Mathf.Approximately(npc.Stats.VisionCone, -1f))
                    return true;
                var normalized = (other.ServerPosition - npc.ServerPosition).normalized;
                return Vector3.Dot(npc.transform.forward, normalized) >= npc.Stats.VisionCone;
            }
            public float maxStepDownHeight = _config.StepDown;

            public void FixedUpdate()
            {
                this.timeAlive += UnityEngine.Time.fixedDeltaTime;

                PlayerInput();
                EatNearbyFood();
                
                if (lastMovementUpdateTime == -1.0) 
                    this.lastMovementUpdateTime = UnityEngine.Time.realtimeSinceStartup;
                
                float delta = UnityEngine.Time.realtimeSinceStartup - this.lastMovementUpdateTime;
                UpdateMovement(delta); 
                
                this.lastMovementUpdateTime = UnityEngine.Time.realtimeSinceStartup;
            }
            
            private void IncreaseState(BaseRidableAnimal.RunState oldState)
            {
                switch (oldState)
                {
                    case BaseRidableAnimal.RunState.stopped:
                    {
                        SwitchMoveState(BaseRidableAnimal.RunState.walk);
                        break;
                    }
                    case BaseRidableAnimal.RunState.walk:
                    {
                        if (BearOwner.health < (BearOwner._maxHealth / 50))
                            break;
                        
                        SwitchMoveState(BaseRidableAnimal.RunState.run);
                        break;
                    }
                }
            }
            
            
            
            
            public void SetRider(BasePlayer player)
            {
                _leadTarget = null;

                if (_rider != null)
                {
                    CuiHelper.DestroyUi(player, InterfaceManager.UI_Instruction);
                    _saddle.DismountPlayer(_rider, true);
                }
                
                _rider = player;

                if (_rider != null)
                {
                    _plugin.DrawUI_BearMenu(_rider);
                    _saddle.MountPlayer(_rider);
                }
            }
            public float rotateSpeed = _config.RotateSpeed;
            public float leadSpeed = _config.LeadSpeed; 
            private float lastSprintPressedTime;
            
            private void OnPhysicsNeighbourChanged()
            {
                this.Invoke(nameof(DelayedDropToGround), UnityEngine.Time.fixedDeltaTime);
            }
            
            public List<BasePlayer> PlayersWithUI = new List<BasePlayer>();
            private float lastMovementUpdateTime = -1f;
            
            private void PlayersChecker()
            {
                if (HasDriver())
                    return;
                                
                List<BasePlayer> players = Facepunch.Pool.GetList<BasePlayer>();
                players.Clear();

                Vis.Entities(BearOwner.transform.position, 1f, players);
                try
                {
                    if (players.Count > 0)
                    {
                        foreach (var basePlayer in players)
                        {
                            if (IsFriend(BearOwner.OwnerID, basePlayer.userID) == false)
                                continue;
                            
                            if (ShouldGrow == false && basePlayer.serverInput.IsDown(BUTTON.USE))
                            {
                                _plugin.DrawUI_BearMenu(basePlayer, BearOwner.net.ID.Value);
                                
                                if(PlayersWithUI.Contains(basePlayer) == false)
                                    PlayersWithUI.Add(basePlayer);
                            }

                            if (lastDdrawUpdate <= Time.time)
                            {
                                string text = ShouldGrow ? _plugin.GetLang("BEAR_GROWING_SECOND", basePlayer.UserIDString, GrowingSeconds) : _plugin.GetLang("BEAR_GROWING_USE_MENU", basePlayer.UserIDString);
                                DrawText(basePlayer, BearOwner.transform.position + Vector3.up, text);
                                lastDdrawUpdate = Time.time + 1f;
                            }
                        }
                    }

                    for (var i = PlayersWithUI.Count - 1; i >= 0; i--)
                    {
                        var player = PlayersWithUI[i];
                        
                        var distance = Vector3.Distance(player.transform.position, this.transform.position);
                        if (distance > 2f)
                        {
                            //Debug.Log("Close " + distance);
                            CuiHelper.DestroyUi(player, InterfaceManager.UI_Layer);
                            PlayersWithUI.Remove(player);
                        }
                    }
                }
                catch
                {
                    // ignored
                }
                
                Facepunch.Pool.FreeList(ref players);
                
                //_plugin.TrackEnd();
            }
        }

        private void FullLoad()
        {
            _interface = new InterfaceManager();

            PluginData.LoadData();
            
            _initiated = true;
		   		 		  						  	   		  	 				  						   					   		 
            if(_config.Immortals)
                Subscribe("OnEntityTakeDamage");
        }
        
        private void AddImage(string name)
        {
            if (!(bool)ImageLibrary.Call("HasImage", GetNameByURL(name)))
                ImageLibrary.Call("AddImage", name, GetNameByURL(name));
        }
        public void SendChat(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (_config.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, _config.CustomPrefix, _config.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        
        public struct CustomItem
        {
            public CustomItem(string displayName, string shortName, ulong skinId)
            {
                DisplayName = displayName;
                ShortName = shortName;
                SkinId = skinId;
            }

            public string DisplayName;
            public string ShortName;
            public ulong SkinId;

            public Item ToItem(int amount = 1)
            {
                Item item = ItemManager.CreateByName(ShortName, amount, SkinId);
                if (item != null && string.IsNullOrEmpty(DisplayName) == false)
                    item.name = DisplayName;
                    
                return item;
            }

            public bool CompareTo(Item item)
            {
                return this.SkinId == item?.skin && this.ShortName == item?.info.shortname;
            }
        }
        
        void OnEntityKill(Bear Bear)
        {
            if (Bear == null) return;
            _data.BearKilled(Bear);
        }

                [PluginReference] private Plugin ImageLibrary;
        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;
            return null;
        }

        private void CheckStatus()
        {
            //if (!(bool)ImageLibrary.Call("IsReady"))
            //{
            //    PrintError("Plugin is not ready! Images are loading.");
            //    timer.Once(10f, () => CheckStatus());
            //}
            //else
            //{
                FullLoad();
                PrintWarning("Plugin succesfully loaded!");
           // }
        }
        
        private string GetSizedImade(string name, int size) => GetImage($"{name}_{size}");
        
        object OnMaxStackable(Item item)
        {
            if (StackSizeController) return null;
            if (_config.BearItem.CompareTo(item) || _config.YoungBearItem.CompareTo(item))
            {
                return 1;
            }
            
            return null;
        }
        
        private static double GetCurrentTime()
        {
            return new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        }
        private Item OnItemSplit(Item item, int amount)
        {
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            if (item.info.shortname == _config.BearItem.ShortName)
            {
                if (item.skin == _config.BearItem.SkinId)
                {
                    Item x = ItemManager.CreateByPartialName(_config.BearItem.ShortName, amount);
                    x.name = _config.BearItem.DisplayName;
                    x.skin = _config.BearItem.SkinId;
                    x.amount = amount;
                    item.amount -= amount;
                    return x;
                }
            }
            if (item.info.shortname == _config.YoungBearItem.ShortName)
            {
                if (item.skin == _config.YoungBearItem.SkinId)
                {
                    Item x = ItemManager.CreateByPartialName(_config.YoungBearItem.ShortName, amount);
                    x.name = _config.YoungBearItem.DisplayName;
                    x.skin = _config.YoungBearItem.SkinId;
                    x.amount = amount;
                    item.amount -= amount;
                    return x;
                }
            }

            return null;
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }
        
        private IEnumerator UserChecker(BasePlayer player, BaseEntity entity)
        {
            while (Vector3.Distance(player.transform.position, entity.transform.position + new Vector3(0, 500, 0)) < 2f)
            {
                yield return CoroutineEx.waitForSeconds(1f);

                if (entity == null || player == null)
                    break;

                if (player.IsConnected == false)
                    break;
            }
            
            player.EndLooting();
            
            //Puts("stopped!");
            yield break;
        }
        void OnServerSave()
        {
            PluginData.SaveData(); 
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }
        
        
        private static StringBuilder sb = new StringBuilder();
        
        private class PluginData
        {
            private const string BackpackPrefab = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";
            private const string DataPath = "IQBearRide/Data";
            public List<BearData> BearsData;

            public class BearData
            {
                public ulong OwnerId;
                public ulong EntityId;
                public float Health;
                public byte[] Inventory;
                public Vector3 LastPosition;
                public bool IsItem = false;
                public int GrowSeconds; 

                [JsonIgnore] private ItemContainer _itemContainer;

                [JsonIgnore]
                public ItemContainer Container
                {
                    get
                    {
                        if (_itemContainer == null)
                        {
                            _itemContainer = ByteToItemContainer(Inventory);
                            if (_itemContainer == null)
                            {
                                _itemContainer = new ItemContainer();
                                _itemContainer.ServerInitialize(null, 6);
                                _itemContainer.GiveUID();
                            }
                            
                            //Saving container to raw bytes
                            _itemContainer.onDirty += () =>
                            {
                                if (_itemContainer == null || _itemContainer.itemList.Count < 1)
                                {
                                    Inventory = null;
                                    return;
                                }
                    
                                Inventory = PluginData.ItemContainerToByte(_itemContainer);
                            };
                        }
                        
                        return _itemContainer;
                    }
                }
                
                public void Drop(Vector3 position)
                {
                    if (Container.itemList.Count > 0)
                    {
                        var dropContainer = Container.Drop(BackpackPrefab, position, Quaternion.identity, 0);
                        if (dropContainer == null) return;
                        
                        dropContainer.lootPanelName = "generic";
		   		 		  						  	   		  	 				  						   					   		 
                        Container.Clear();
                        ItemManager.DoRemoves();
                    }
                    else
                    {
                        //Debug.Log("Backpack empty");
                    }
                    
                }
            }

            public void BearKilled(Bear bear)
            {
                //Debug.Log("Bear dead");
                if (_unloading)
                    return;
                
                //Debug.Log("Searching bear in list");
                
                BearData data = _data.BearsData.Find(x => x.EntityId == bear.net.ID.Value);
                if (data == null)
                    return;
                
                //Debug.Log("Trying to drop backpack");
                data.Drop(bear.transform.position + new Vector3(0, 2f, 0));

                BasePlayer player = BasePlayer.FindByID(data.OwnerId);
                if(player != null)
                    CuiHelper.DestroyUi(player, InterfaceManager.UI_Instruction);
                _data.BearsData.Remove(data);
            }

            public static Item BearToItem(ulong bear)
            {
                Item bearItem = _config.BearItem.ToItem();
                
                var data = PluginData.Get(bear);
                data.EntityId = bearItem.uid.Value;
                data.IsItem = true;
                
                return bearItem;
            }
		   		 		  						  	   		  	 				  						   					   		 
            public static Bear ItemToBear(ulong player, ulong item, Vector3 position, bool young)
            {
                Bear bearEntity = CreateControlledBear(player, position, young);
                if (bearEntity == null)
                    return null;

                var data = PluginData.Get(item);
                data.EntityId = bearEntity.net.ID.Value;
                data.IsItem = false;
                data.OwnerId = player;
                
                if(data.Health > 0)
                    bearEntity.SetHealth(data.Health);

                if (young)
                {
                    data.GrowSeconds = _config.GrowSeconds;
                }

                return bearEntity;
            }

            public static Bear CreateControlledBear(ulong player, Vector3 position, bool young)
            {
                Bear bearEntity = GameManager.server.CreateEntity("assets/rust.ai/agents/bear/bear.prefab", position) as Bear;
               // Bear bearEntity = GameManager.server.CreateEntity("assets/rust.ai/agents/Bear/Bear.prefab", position) as Bear;
                if (bearEntity == null)
                    return null;

                bearEntity.Spawn();
                bearEntity.OwnerID = player;

                var controller = bearEntity.gameObject.GetOrAddComponent<BearController>();

                return bearEntity;
            }

            public static BearData Get(ulong bear)
            {
                BearData data = _data.BearsData.Find(x => x.EntityId == bear);
                if (data == null)
                {
                    data = new BearData();
                    data.EntityId = bear;
                    _data.BearsData.Add(data);
                }

                return data;
            }
            
            public static ItemContainer ByteToItemContainer(byte[] bItem)
            {
                if (bItem == null)
                    return null;
                
                ProtoBuf.ItemContainer validItem = ProtoBuf.ItemContainer.Deserialize(bItem);
                ItemContainer itm = Pool.Get<ItemContainer>();
                itm.isServer = true;
                itm.Load(validItem);
                return itm;
            }

            public static byte[] ItemContainerToByte(ItemContainer item)
            {
                var protoItem = item.Save();
                return protoItem.ToProtoBytes();
            }

            public static void SaveContainer(ulong bear, ItemContainer container)
            {
                var data = PluginData.Get(bear);
                data.Inventory = ItemContainerToByte(container);
            }
            
            public static void SaveData()
            {
                if (_data == null) return;
                Interface.Oxide.DataFileSystem.WriteObject(DataPath, _data);
            }

            public static void LoadData()
            {
                try
                {
                    if (Interface.Oxide.DataFileSystem.ExistsDatafile(DataPath))
                    {
                        _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(DataPath);
                    }
                    else
                    {
                        _data = new PluginData()
                        {
                            BearsData = new List<BearData>()
                        };
                    }

                    if (_data == null)
                    {
                        _data = new PluginData()
                        {
                            BearsData = new List<BearData>()
                        };
                    }

                    foreach (var bearData in _data.BearsData)
                    {
                        if (bearData.IsItem)
                            continue;

                        var bear = CreateControlledBear(bearData.OwnerId, bearData.LastPosition, bearData.GrowSeconds > 0);
                        bearData.EntityId = bear.net.ID.Value;
                    }
                }
                catch (Exception ex)
                {
                    _plugin.PrintWarning(LanguageEn ? "Date file upload error, contact the developer if you don't know how to solve the problem - Mercury#5212" : "Ошибка загрузки дата-файла, свяжитесь с разработчиком если не знаете как решить проблему - Mercury#5212");
                }
            }
        }

                private const Boolean LanguageEn = false;

        private void DrawUI_BearMenu(BasePlayer player)
        {
            string gui = InterfaceManager.GetInterface("BearInstruciont");
		   		 		  						  	   		  	 				  						   					   		 
            CuiHelper.DestroyUi(player, InterfaceManager.UI_Instruction);
            CuiHelper.AddUi(player, gui);
        }
        
        public static string GetColor(string hex, float alpha = 1f)
        {
            if (hex.Length != 7) hex = "#FFFFFF";
            if (alpha < 0 || alpha > 1f) alpha = 1f;

            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;

            return $"{r} {g} {b} {alpha}";
        }
        
                
        [ConsoleCommand("UI_BearControl")]
        private void Console_BearControl(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;
            
            if (arg.HasArgs(2) == false)
            {
                return;
            }
            
            
            ulong bear = 0;
            if (ulong.TryParse(arg.Args[1], out bear) == false)
                return;

            BearController controller;
            if (_bearControllers.TryGetValue(bear, out controller) == false)
                return;
            
            if (controller.PlayersWithUI.Contains(player))
                controller.PlayersWithUI.Remove(player);
            
            var data = PluginData.Get(bear);
            if (IsFriend(data.OwnerId, player.userID) == false)
                return;

            switch (arg.Args[0])
            {
                case "ride":
                {
                    controller.SetRider(player);
                    break;
                }

                case "inventory":
                {
                    OpenContainer(player, data.Container, _config.InventorySize);
                    break;
                }

                case "lead":
                    {
                        if (controller.HasLead())
                        {
                            controller.SetLead(null);
                            break;
                        }

                        controller.SetLead(player);
                        break;
                    }

                case "pickup":
                    {
                        if (data.OwnerId != player.userID)
                        {
                            SendChat(GetLang("BEAR_PICK_UP", player.UserIDString), player);
                            break;
                        }

                        if (data.Container.itemList.Count > 0)
                        {
                            SendChat(GetLang("BEAR_PICK_UP_FULL_INVENTORY", player.UserIDString), player);
                            break;
                        }

                        data.Health = controller.BearOwner.Health();
                        var item = PluginData.BearToItem(bear);
                        player.GiveItem(item);
                        controller.BearOwner.Kill();
                        break;
                    }
            }
        }
        
        
        
                
                private static PluginData _data;
        private void OnServerShutdown() => Unload();

                
        
        private static InterfaceManager _interface;
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
            }
}
