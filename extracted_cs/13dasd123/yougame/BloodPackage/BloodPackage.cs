using Oxide.Core.Plugins; using UnityEngine; using Oxide.Game.Rust.Cui; using System.Collections.Generic; using System; using Random = UnityEngine.Random; using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BloodPackage", "Johny", "1.0.1")]



    public class BloodPackage : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        void OnServerInitialized()
        {
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("Плагин ImageLibrary не загружен");
                Unload();
                return;
            }
            ImageLibrary.Call("AddImage", "https://i.imgur.com/hGNLb0C.png", "blood");
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            if (!setfps)
            {

                PrintWarning("Autor - for yougame.biz");
                return;
            }
        }



        /// <summary>
        /// config
        /// </summary>
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Настройка")]
            public krov Blood { get; set; }

            public class krov
            {
                [JsonProperty(PropertyName = "Основные настройки")]
                public bloodkrov houganlox { get; set; }

                public class bloodkrov
                {
                    [JsonProperty(PropertyName = "Сколько пакетиков с кровью надо, чтобы встать?")] public int blood { get; set; }
                    [JsonProperty(PropertyName = "Включить тряску экрана?")] public bool ShakeShake = false;
                    [JsonProperty(PropertyName = "Из чего можно сделать пакетик с кровью? (itemid предмета)")] public int craftblood = 1325935999;
                    [JsonProperty(PropertyName = "Кол-во предмета для крафта пакетика с кровью")] public int craftblood1 = 3;
                }
            }
        }

        [PluginReference] private Plugin setfps;
        protected override void LoadConfig()
        {
            base.LoadConfig(); configData = Config.ReadObject<ConfigData>(); Config.WriteObject(configData, true);
        }
        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private ConfigData GetBaseConfig() => new ConfigData
        {
            Blood = new ConfigData.krov
            {
                houganlox = new ConfigData.krov.bloodkrov
                {
                    blood = 1,
                }
            }
        };



        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            ImageLibrary.Call("GetImage", "blood");
        }
        void OnPlayerWound(BasePlayer player)
        {
            if (player == null) return;
            DrawUI(player);
        }
        void OnPlayerRespawn(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BloodPackage_UI");
        }
        private void OnPlayerRespawned(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BloodPackage_UI");
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            CuiHelper.DestroyUi(player, "BloodPackage_UI");
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
            {
                if (Random.Range(0, 100) <= 100)
                {
                    if (dispenser.GetComponent<BaseEntity>().ShortPrefabName == "bear.corpse" || dispenser.GetComponent<BaseEntity>().ShortPrefabName == "wolf.corpse" || dispenser.GetComponent<BaseEntity>().ShortPrefabName == "boar.corpse" || dispenser.GetComponent<BaseEntity>().ShortPrefabName == "player.corpse")
                    {
                        (entity as BasePlayer).inventory.GiveItem(ItemManager.CreateByName("blood", 1));
                        (entity as BasePlayer).SendConsoleCommand($"note.inv {item.info.itemid} 1 \"Кровь\"");
                    }
                }
            }
        }


        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "BloodPackage_UI");
            }
        }

        [ChatCommand("craftbp")]
        private void bloodcraft(BasePlayer player)
        {
            {
                var rawmeat = player.inventory.GetAmount(configData.Blood.houganlox.craftblood);
                if (rawmeat >= configData.Blood.houganlox.craftblood)
                {
                    player.inventory.Take(null, configData.Blood.houganlox.craftblood, configData.Blood.houganlox.craftblood1);
                }
                else
                {
                    player.ChatMessage($"Не хватает <color=#D3442E>вещей для крафта</color>.");
                    return;
                }
                player.inventory.GiveItem(ItemManager.CreateByItemID(93832698, 1));
                player.ChatMessage($"Вы успешно скрафтили <color=#DCFF66>пакетик с кровью</color>");
            }
        }

        [ConsoleCommand("BloodPackageUIHandler")]
        private void CmdHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!player.IsWounded()) return;

            var itemcount = player.inventory.GetAmount(ItemManager.FindItemDefinition("blood").itemid);
            if (itemcount >= configData.Blood.houganlox.blood)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition("blood").itemid, 1);
            }
            else
            {
                SendReply(player, "У вас недостаточно пакетов с кровью");
                return;
            }

            player.StopWounded();
            if (configData.Blood.houganlox.ShakeShake)
            {
                Shake(player, 0);
            }
            SendReply(player, "Вы <color=#689656>успешно</color> использовали пакет крови");
        }
        void DrawUI(BasePlayer player)
        {
            var itemcount = player.inventory.GetAmount(ItemManager.FindItemDefinition("blood").itemid);
            string Layer = "BloodPackage_UI";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-264 18", OffsetMax = "-204 78" }
            }, "Overlay", Layer);
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "blood"), Color = "0 0 0 0.4", },
                    new CuiRectTransformComponent(){  AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0.968627453 0.92451568632 0.882352948 0.03529412", Command = "BloodPackageUIHandler", Close = Layer },
                Text = { Text = $"У ВАС: {itemcount}", Align = TextAnchor.MiddleCenter, FontSize = 10 }
            }, Layer);

            CuiHelper.AddUi(player, container);
        }
        private void GetConfig<T>(string menu, string key, ref T varObject)
        {
            if (Config[menu, key] != null)
            {
                varObject = Config.ConvertValue<T>(Config[menu, key]);
            }
            else
            {
                Config[menu, key] = varObject;
            }
        }

        private static List<string> ShakeEffects = new List<string>
        {
            "assets/prefabs/tools/jackhammer/effects/strike_screenshake.prefab", "assets/prefabs/weapons/doubleshotgun/effects/attack_shake.prefab", "assets/prefabs/weapons/hatchet/effects/strike_screenshake.prefab", "assets/prefabs/weapons/rock/effects/strike_screenshake.prefab", "assets/prefabs/weapons/smg/effects/attack_shake.prefab", "assets/prefabs/weapons/torch/effects/strike_screenshake.prefab"
        };

        private void Shake(BasePlayer player, float amount)
        {
            if (Math.Abs(amount - 0.25f * 100) < 0.5 || player.IsDead())
                return;

            Effect effect = new Effect(ShakeEffects.GetRandom(), player, 0, new Vector3(), new Vector3()); EffectNetwork.Send(effect, player.Connection); amount += 0.25f; timer.Once(0.25f, () => Shake(player, amount)); //hougan не бей :)
        }
    }
}