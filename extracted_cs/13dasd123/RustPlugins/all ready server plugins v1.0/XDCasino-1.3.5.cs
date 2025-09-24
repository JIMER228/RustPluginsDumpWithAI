using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using VLB;
using Rust;

namespace Oxide.Plugins
{
    [Info("XDCasino", "DezLife", "1.3.5")]
    [Description("Казино, играем на ресурсы :3")]
    public class XDCasino : RustPlugin
    {
        #region Var
        private const string TerminalLayer = "UI_TerminalLayer";
        private const string NotiferLayer = "UI_NotiferLayers";
        private readonly ulong Id = 23556438051;
        private const string FileName = "CasinoRoomNew";
        private const string AuthorContact = "DezLife#1480 \nvk.com/dezlife";
        List<BaseEntity> CasinoEnt = new List<BaseEntity>();
        BigWheelGame bigWheelGame;
        BaseEntity WoodenTrigger;
        MonumentInfo monument;
        [PluginReference] Plugin CopyPaste;
        #endregion

        #region Configuration

        public static Configuration config = new Configuration();
        public class Configuration
        {
            public class PluginSettings
            {
                public class Setings
                {
                    [JsonProperty("Запретить вставать игроку со стула если он учавствует в ставке")]
                    public bool mountUse;
                }
                public class CCTV
                {
                    [JsonProperty("идентификатор для подключения к ней")]
                    public string identifier;
                }

                [JsonProperty("Список предметов для ставок (ShortName/максимальное количество за 1 ставку)")]
                public Dictionary<string, int> casinoItems = new Dictionary<string, int>();

                [JsonProperty("Настройка публичной камеры в казино")]
                public CCTV cCTV;
                [JsonProperty("Основные настройки")]
                public Setings setings;
            }

            [JsonProperty("Настройки спавна")]
            public PluginSettings pluginSettings;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    pluginSettings = new PluginSettings
                    {
                        setings = new PluginSettings.Setings
                        {
                            mountUse = true,
                        },
                        casinoItems = new Dictionary<string, int>
                        {
                            ["cloth"] = 100,
                            ["metal.refined"] = 10,
                            ["lowgradefuel"] = 50,
                            ["wood"] = 1000,
                            ["stones"] = 1000,
                            ["metal.fragments"] = 300,
                        },
                        cCTV = new PluginSettings.CCTV
                        {
                            identifier = "casino"
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка чтения конфигурации 'oxide/config/', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CASINO_PRIZE"] = "Collect your winnings first!",
                ["CASINO_NOTGAMEITEM"] = "This item is not on the approved list.",
                ["CASINO_ITEMAMOUNTFULL"] = "You are trying to put more than the allowed amount (Maximum for this item {0})",
                ["CASINO_MOUNTNOT"] = "You cannot get up during an active bet! Also, don't forget to take your prize",
                ["CASINO_ERROR"] = "Something went wrong. Move the item to another slot and try again!",
                ["CASINO_UITITLE"] = "<b>Roulette for resources</b>",
                ["CASINO_UITITLEITEM"] = "<b><color=#EAD093FF>ALLOWED ITEMS AND RESTRICTIONS</color></b>\n",
                ["CASINO_UIRULES"] = "<b><color=#EAD093FF>REGULATIONS</color></b>\n" +
                "1. You will not be able to place a new bet without collecting your winnings.\n" +
                "2. You can only use certain resources for betting,\nthere is also a limit on the maximum rate.",
                ["CASINO_UIRULES3"] = "\n3. You cannot get up from your chair during a bet or if you have not collected your winnings",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CASINO_PRIZE"] = "Сначала забери выигрыш!",
                ["CASINO_NOTGAMEITEM"] = "Этого предмета нет в списке разрешенных",
                ["CASINO_ITEMAMOUNTFULL"] = "Вы пытаетесь положить больше чем разрешено (Максимум для этого предмета {0})",
                ["CASINO_MOUNTNOT"] = "Нельзя вставать во время активной ставки! Так же не забудьте забрать приз",
                ["CASINO_ERROR"] = "Что то пошло не так. Перенесите предмет в другой слот и попробуйте еще раз!",
                ["CASINO_UITITLE"] = "<b>Рулетка на ресурсы</b>",
                ["CASINO_UITITLEITEM"] = "<b><color=#EAD093FF>РАЗРЕШЕННЫЕ ПРЕДМЕТЫ И ОГРАНИЧЕНИЯ</color></b>\n",
                ["CASINO_UIRULES"] = "<b><color=#EAD093FF>ПРАВИЛА</color></b>\n" +
                "1. Вы не сможете сделать новую ставку не забрав выигрыш.\n" +
                "2. Для ставок вы сможете использовать только определенные ресурсы,\nтак же существует ограничения на максимальную ставку.",
                ["CASINO_UIRULES3"] = "\n3. Вы не можете вставать со стула во время ставки или если вы не забрали свой выигрыш",
            }, this, "ru");
        }

        #endregion

        #region Hooks
        void Init()
        {
            Unsubscribe("CanDismountEntity");
        }
        private void OnServerInitialized()
        {
            monument = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower() == "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab");
            if (!CopyPaste)
            {
                PrintError("Проверьте установлен ли у вас плагин 'CopyPaste'");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            else if (CopyPaste.Version < new VersionNumber(4, 1, 27))
            {
                PrintError("У вас старая версия CopyPaste!\nПожалуйста обновите плагин до последней версии (4.1.27 или выше) - https://umod.org/plugins/copy-paste");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (monument == null)
            {
                PrintError("Походу у вас отсутствует 'Город НПС' !\nПожалуйста обратитесь к разработчику" + AuthorContact);
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            LoadDataCopyPaste();
           
            if (config.pluginSettings.setings.mountUse)
                Subscribe("CanDismountEntity");
        }

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (info.damageTypes.Has(DamageType.Decay))
            {
                if (victim?.OwnerID == Id)
                {
                    info.damageTypes.Scale(DamageType.Decay, 0);
                }
            }
        }
        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container?.entityOwner is BigWheelBettingTerminal && container?.entityOwner?.OwnerID == Id && !container.IsLocked())
            {
                BasePlayer player = container.playerOwner;          
                if (player == null)
                    return ItemContainer.CanAcceptResult.CannotAccept;

                if (targetPos == 5)
                    return ItemContainer.CanAcceptResult.CannotAccept;
                
                if (!config.pluginSettings.casinoItems.ContainsKey(item.info.shortname))
                {
                    HelpUiNottice(player, lang.GetMessage("CASINO_NOTGAMEITEM", this, player.UserIDString));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
                int maxAmount = config.pluginSettings.casinoItems[item.info.shortname];
                if (container.GetSlot(5) != null)
                {
                    HelpUiNottice(player, lang.GetMessage("CASINO_PRIZE", this, player.UserIDString));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
                int s = 0;
                for (int i = 0; i < 5; i++)
                {
                    Item slot = container.GetSlot(i);
                    if (slot != null)
                    {
                        if (slot.info.shortname == item.info.shortname)
                            s += slot.amount;
                    }
                }
                if (item.GetRootContainer()?.entityOwner?.OwnerID == Id)
                {
                    if (item.GetRootContainer()?.GetSlot(5) != null || targetPos == 5)
                        return ItemContainer.CanAcceptResult.CannotAccept;
                }
                else if (item.amount + s > maxAmount)
                {
                    HelpUiNottice(player, string.Format(lang.GetMessage("CASINO_ITEMAMOUNTFULL", this, player.UserIDString), maxAmount));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            return null;
        }

        object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (entity?.OwnerID == Id)
            {
                foreach (var item in bigWheelGame?.terminals?.Where(x => x.skinID == player.userID))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var s = item.inventory.GetSlot(i);
                        if (s != null)
                        {
                            HelpUiNottice(player, lang.GetMessage("CASINO_MOUNTNOT", this, player.UserIDString));
                            return false;
                        }
                    }
                    item.skinID = 0;
                }
            }
            return null;
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is BigWheelBettingTerminal && entity.OwnerID == Id)
            {
                if (player == null)
                    return;
                var sss = entity as BigWheelBettingTerminal;
                sss.GetComponent<StorageContainer>().inventory.playerOwner = player;
                entity.skinID = player.userID;

                CuiHelper.DestroyUi(player, TerminalLayer);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "192 475", OffsetMax = "573 660" },
                    Image = { Color = "0.968627453 0.921631568632 0.882352948 0.03529412", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                }, "Overlay", TerminalLayer);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.8090092", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("CASINO_UITITLE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16, Color = HexToRustFormat("#d6ccc3"), }
                }, TerminalLayer);

                string rules = lang.GetMessage("CASINO_UIRULES", this, player.UserIDString);
                if (config.pluginSettings.setings.mountUse)
                    rules += lang.GetMessage("CASINO_UIRULES3", this, player.UserIDString);
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.01924771 0.3405407", AnchorMax = "0.9833772 0.8018017", OffsetMax = "0 0" },
                    Text = { Text = rules, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = HexToRustFormat("#FFFFFFFF"), }
                }, TerminalLayer);

                string itemRules = lang.GetMessage("CASINO_UITITLEITEM", this, player.UserIDString);
                int i = 0;
                foreach (var cfg in config.pluginSettings.casinoItems)
                {
                    i++;
                    string Zapitaya = i == config.pluginSettings.casinoItems.Count ? "" : ",";
                    itemRules += ItemManager.itemList.First(x => x.shortname == cfg.Key).displayName.english + $":{cfg.Value}{Zapitaya} ";
                }

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.01924774 0.01081081", AnchorMax = "0.9833772 0.3369368", OffsetMax = "0 0" },
                    Text = { Text = itemRules, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = HexToRustFormat("#FFFFFFFF"), }
                }, TerminalLayer);

                CuiHelper.AddUi(player, container);
            }
        }
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) 
        {
            CuiHelper.DestroyUi(player, TerminalLayer); 
            CuiHelper.DestroyUi(player, NotiferLayer);
            CuiHelper.DestroyUi(player, "NotiferLayer2");
            CuiHelper.DestroyUi(player, "NotiferLayer1");
        }
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                CuiHelper.DestroyUi(player, TerminalLayer);
                CuiHelper.DestroyUi(player, NotiferLayer);        
            }
            foreach (BaseEntity ent in CasinoEnt)
            {
                if (ent == null)
                    continue;
                ent.Kill();
            }
        }

        #endregion

        #region UiNotifer
        private void HelpUiNottice(BasePlayer player, string msg, string sprite = "assets/icons/info.png")
        {
            CuiHelper.DestroyUi(player, NotiferLayer);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "192 664", OffsetMax = "573 710" },
                Image = { Color = "0.968627453 0.921631568632 0.882352948 0.03529412", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = 0.40f },
            }, "Overlay", NotiferLayer);
           
            container.Add(new CuiElement
            {
                Parent = NotiferLayer,
                Name = "NotiferLayer1",
                FadeOut = 0.30f,
                Components =
                {
                    new CuiImageComponent {Sprite = sprite, Color = HexToRustFormat("#AA7575FF"), FadeIn = 0.45f },
                    new CuiRectTransformComponent{ AnchorMin = "0.02672293 0.192029", AnchorMax = "0.09671418 0.7717391"},
                }
            });

            container.Add(new CuiLabel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.1139241 0.089991037", AnchorMax = "0.9423349 0.8999914", OffsetMax = "0 0" },
                Text = { Text = msg, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#d6ccc3"), FadeIn = 0.50f }
            }, NotiferLayer, "NotiferLayer2");

            CuiHelper.AddUi(player, container);
            timer.Once(3.5f, () => 
            { 
                CuiHelper.DestroyUi(player, NotiferLayer); 
                CuiHelper.DestroyUi(player, "NotiferLayer1"); 
                CuiHelper.DestroyUi(player, "NotiferLayer2"); 
            });
        }
        #endregion

        #region Metods
        void GenerateBuilding()
        {
            var options = new List<string> { "stability", "true", "deployables", "true", "autoheight", "false", "entityowner", "false" };

            Vector3 resultVector = GetResultVector();

            WoodenTrigger = GameManager.server.CreateEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab", new Vector3(resultVector.x - 2.3f, resultVector.y + 6.0f, resultVector.z - 2.1f));
            WoodenTrigger.OwnerID = Id;
            WoodenTrigger.Spawn();
            CasinoEnt.Add(WoodenTrigger);

            var success = CopyPaste.Call("TryPasteFromVector3", resultVector, (monument.transform.rotation.eulerAngles * Mathf.Deg2Rad).y - 1.70f, "CasinoRoomNew", options.ToArray());

            if (success is string)
            {
                PrintWarning("Ошибка #1 \nПлагин не будет работать, Обратитесь к разработчику" + AuthorContact);
                return;
            }  
        }
        void PreSpawnEnt()
        {
            List<BaseEntity> obj = new List<BaseEntity>();
            Vis.Entities(GetResultVector(), 25f, obj, LayerMask.GetMask("Construction", "Deployable", "Deployed", "Debris"));
            foreach (BaseEntity item in obj?.Where(x => x.OwnerID == Id))
            {
                if (item == null) continue;
                item.Kill();
            }
            NextTick(() => { GenerateBuilding(); });
        }

        void OnPasteFinished(List<BaseEntity> pastedEntities, string fileName)
        {
            try
            {
                if (fileName != "CasinoRoomNew")
                    return;
                CasinoEnt.AddRange(pastedEntities);
                foreach (BaseEntity item in CasinoEnt)
                {
                    if (item == null) continue;
                    item.OwnerID = Id;
                    if (item is BaseChair || item is BigWheelBettingTerminal || item is RepairBench || item is BaseArcadeMachine)
                    {
                        var ent = item as BaseCombatEntity;
                        if (ent == null) continue;
                        ent.pickup.enabled = false;
                        continue;
                    }
                    if (item is CCTV_RC)
                    {
                        var ent = item as CCTV_RC;
                        if (ent == null)
                            continue;
                        ent.rcIdentifier = config.pluginSettings.cCTV.identifier;
                        ent.pickup.enabled = false;
                        ent?.SetFlag(BaseEntity.Flags.Reserved8, true);
                        continue;
                    }
                    if (item is ElectricGenerator)
                    {
                        WoodenTrigger.transform.position = item.transform.position;
                        WoodenTrigger.transform.rotation = item.transform.rotation;
                        WoodenTrigger.SendNetworkUpdate();
                    }
                    if (item is Door)
                    {
                        var ent = item as Door;
                        if (ent == null)
                            continue;
                        ent.pickup.enabled = false;
                        ent.canTakeLock = false;
                        ent.canTakeCloser = false;
                        continue;
                    }
                    if (item is BuildingBlock)
                    {
                        var build = item as BuildingBlock;
                        build?.SetFlag(BaseEntity.Flags.Reserved1, false);
                        build?.SetFlag(BaseEntity.Flags.Reserved2, false);
                    }
                    if (item as ElectricalHeater)
                    {
                        item?.SetFlag(BaseEntity.Flags.Reserved8, true);
                    }
                    if (item as HBHFSensor)
                    {
                        bigWheelGame = GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/big_wheel.prefab") as BigWheelGame;
                        bigWheelGame.SetParent(item, false, true);
                        bigWheelGame.transform.position = item.transform.position;
                        bigWheelGame.transform.rotation = item.transform.rotation * Quaternion.Euler(0f, 270f, -90f);
                        bigWheelGame.gameObject.GetOrAddComponent<SphereCollider>();
                        bigWheelGame.Spawn();
                    }
                    else if (item.name.Contains("light") || item.name.Contains("lantern"))
                    {
                        item.enableSaving = true;
                        item?.SendNetworkUpdate();
                        item?.SetFlag(BaseEntity.Flags.Reserved8, true);
                        item?.SetFlag(BaseEntity.Flags.On, true);
                    }
                    item?.SetFlag(BaseEntity.Flags.Busy, true);
                    item?.SetFlag(BaseEntity.Flags.Locked, true);
                }
                PrintWarning($"Постройка обработана успешно {CasinoEnt.Count}");
                NextTick(() => {
                    CheckEnt();
                });
            }
            catch (Exception ex)
            {
                PrintError("Ошибка при загрузке постройки! Подробности в лог файле!!\nОбратитесь к разработчику" + AuthorContact);
                Log($"exception={ex}", "LogError");
            }
        }

        private void CheckEnt()
        {
            foreach (var item in CasinoEnt)
            {
                if (item.PrefabName == "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab")
                {
                    BigWheelBettingTerminal bettingTerminal = GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab") as BigWheelBettingTerminal;
                    bettingTerminal.allowedItem = null;
                    bettingTerminal.OwnerID = Id;
                    bettingTerminal.SetParent(WoodenTrigger, false, true);
                    bettingTerminal.transform.position = item.transform.position;
                    bettingTerminal.transform.rotation = item.transform.rotation;
                    bigWheelGame.terminals.Add(bettingTerminal);
                    bettingTerminal.Spawn();
                    item.Kill();
                }
            }
        }
        #endregion

        #region Data
        public void LoadDataCopyPaste()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("copypaste/" + FileName))
            {
                PrintError($"Файл постройки не найден!\nНачинаем импортировать...");
                webrequest.Enqueue("http://utilite.skyplugins.ru/XDCasinoOutPost/CasinoRoomNew.json", null, (i, s) =>
                {
                    {
                        if (i == 200)
                        {
                            PasteData obj = JsonConvert.DeserializeObject<PasteData>(s);
                            Interface.Oxide.DataFileSystem.WriteObject("copypaste/" + FileName, obj);
                        }
                        else
                        {
                            PrintError("Ошибка при загрузке постройки!\nПробуем загрузить еще раз"); Log(i.ToString(), "LogError");
                            timer.Once(10f, () => LoadDataCopyPaste());
                            return;
                        }
                    }
                }, this, RequestMethod.GET);
            }
            timer.Once(5f, () =>
            {
                PreSpawnEnt();
            });
        }

        public class PasteData
        {
            public Dictionary<string, object> @default;
            public ICollection<Dictionary<string, object>> entities;
            public Dictionary<string, object> protocol;
        }

        #endregion

        #region Helps
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        void Log(string msg, string file)
        {
            LogToFile(file, $"[{DateTime.Now}] {msg}", this);
        }
        private Vector3 GetResultVector()
        {
            return monument.transform.position + monument.transform.rotation * new Vector3(-36.72f, 2.87f, 20.65f);
        }
        #endregion
    }
}
