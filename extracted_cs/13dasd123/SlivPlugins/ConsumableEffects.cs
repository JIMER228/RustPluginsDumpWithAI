using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;
using VLB;
using Network;

namespace Oxide.Plugins
{
    [Info("ConsumableEffects", "David", "1.4.61")]
    [Description("Rust buffs and metabolism utilized for custom items.")]

    public class ConsumableEffects : RustPlugin
    {
        static ConsumableEffects plugin;
        private Dictionary<BasePlayer, float> overdose = new Dictionary<BasePlayer, float>();

        #region [Hooks]

        private void Init() => plugin = this;

        private void OnServerInitialized()
        {
            LoadData();
            LoadConfig();
            timer.Once(0.5f, () => { PlayerComponent(true); });

            if (!config.od.enabled || !config.od.onDeath)
                Unsubscribe("OnPlayerRespawned");

            if (!config.od.whenHeld)
                Unsubscribe("OnActiveItemChanged");
        }

        private void OnPlayerConnected(BasePlayer player) => PlayerComponent(true, player);

        private void OnPlayerDisconnected(BasePlayer player) => PlayerComponent(false, player);

        private void Unload() => PlayerComponent(false);

        void OnPlayerRespawned(BasePlayer player)
        {
            if (overdose.ContainsKey(player))
                player.metabolism.dirtyness.value = overdose[player];
        }

        #endregion

        #region [Functions]

        private void StoreDirty(BasePlayer player)
        {
            if (!config.od.enabled || config.od.onDeath) return;

            float value = player.metabolism.dirtyness.value;
            if (value < 0)
                value = 0;

            if (!overdose.ContainsKey(player))
                overdose.Add(player, value);
            else
                overdose[player] = value;
        }

        private object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item.name != null && buffData.ContainsKey(item.name))
            {
                if (action != "consume") return null;

                if (buffData[item.name].effectOverTime != null)
                {
                    if (buffData[item.name].effectOverTime.EffectDuration > 0)
                    {
                        var run = _monoBehavior[player];
                        if (run == null)
                        {
                            if (!_monoBehavior.ContainsKey(player))
                                _monoBehavior.Add(player, player.GetOrAddComponent<Overlay>());

                            run = _monoBehavior[player];
                        }
                        if (run != null)
                        {
                            string currentEffect = run.isEffectTicking();

                            if (currentEffect == null)
                            {
                                run.ApplyEoT(item.name);
                            }

                            if (currentEffect != null)
                            {
                                SendReply(player, $"You are under effect of {currentEffect}, you need to wait till it expires before applying new one.");
                                return false;
                            }
                        }
                    }
                }

                item.UseItem(1);

                if (player.metabolism.dirtyness.value > config.od.dirtyLimit)
                {
                    ApplyBuff(player, config.od.debuffName);
                    if (buffData[item.name].dirtyness > 0)
                        StoreDirty(player);

                    return false;
                }
                else if (player.metabolism.dirtyness.value + buffData[item.name].dirtyness > config.od.dirtyLimit)
                {
                    NextTick(() => { ApplyBuff(player, item.name); });
                    ApplyBuff(player, config.od.debuffName);
                    StoreDirty(player);
                    return false;
                }
                ApplyBuff(player, item.name);
                StoreDirty(player);
                return false;
            }
            return null;

        }

        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (player == null) return null;
            var item = player.GetHeldEntity().GetItem();
            //var item = holder.GetItem();

            if (item.name != null && buffData.ContainsKey(item.name))
            {
                if (buffData[item.name].effectOverTime != null)
                {
                    if (buffData[item.name].effectOverTime.EffectDuration > 0)
                    {
                        var run = _monoBehavior[player];
                        if (run == null)
                        {
                            if (!_monoBehavior.ContainsKey(player))
                                _monoBehavior.Add(player, player.GetOrAddComponent<Overlay>());

                            run = _monoBehavior[player];
                        }
                        if (run != null)
                        {
                            string currentEffect = run.isEffectTicking();

                            if (currentEffect == null)
                            {
                                run.ApplyEoT(item.name);
                            }

                            if (currentEffect != null)
                            {
                                SendReply(player, $"You are under effect of {currentEffect}, you need to wait till it expires before applying new one.");
                                return false;
                            }
                        }
                    }
                }

                ApplyBuff(player, item.name);
                StoreDirty(player);
                return false;
            }
            return null;
        }


        private void ApplyBuff(BasePlayer player, string buffName)
        {
            //metabolism values
            if (buffData[buffName].calories != null || buffData[buffName].calories != 0)
                player.metabolism.calories.value = player.metabolism.calories.value + buffData[buffName].calories;

            if (buffData[buffName].hydration != null || buffData[buffName].hydration != 0)
                player.metabolism.hydration.value = player.metabolism.hydration.value + buffData[buffName].hydration;

            if (buffData[buffName].health != null || buffData[buffName].health != 0)
                player.health = player.health + buffData[buffName].health;

            if (buffData[buffName].healthRegen != null || buffData[buffName].healthRegen != 0)
                player.metabolism.pending_health.value = player.metabolism.pending_health.value + buffData[buffName].healthRegen;

            if (buffData[buffName].bleeding != null || buffData[buffName].bleeding != 0)
                player.metabolism.bleeding.value = player.metabolism.bleeding.value + buffData[buffName].bleeding;

            if (buffData[buffName].dirtyness != null || buffData[buffName].dirtyness != 0)
            {
                player.metabolism.dirtyness.value = player.metabolism.dirtyness.value + buffData[buffName].dirtyness;

                if (player.metabolism.dirtyness.value < 0)
                    player.metabolism.dirtyness.value = 0;
            }

            if (buffData[buffName].poison != null || buffData[buffName].poison != 0)
                player.metabolism.poison.value = player.metabolism.poison.value + buffData[buffName].poison;

            if (buffData[buffName].radiation != null || buffData[buffName].radiation != 0)
                player.metabolism.radiation_poison.value = player.metabolism.radiation_poison.value + buffData[buffName].radiation;

            //tea boost
            if (buffData[buffName].TeaBoost_enabled)
            {
                switch (buffData[buffName].boostType)
                {
                    case "wood":
                        TeaBoost(player, "wood", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                    case "ore":
                        TeaBoost(player, "ore", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                    case "scrap":
                        TeaBoost(player, "scrap", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                    case "health":
                        TeaBoost(player, "hp", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                    case "radiation_resistance":
                        TeaBoost(player, "rad", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                    case "radiation_exposure_resistance":
                        TeaBoost(player, "rad_expo", buffData[buffName].boostValue, buffData[buffName].boostDuration);
                        break;
                }
            }

            StoreDirty(player);

            if (player.metabolism.dirtyness.value > config.od.dirtyLimit)
            {
                if (buffName != config.od.debuffName)
                    return;
            }

            foreach (string fx in buffData[buffName].effects)
            {
                if (fx.StartsWith("assets")) PlayFx(player, fx);

                if (!fx.StartsWith("assets")) PlayGfx(player, fx);
            }
        }

        private void PlayFx(BasePlayer player, string fx)
        {
            if (player == null) return;
            var EffectInstance = new Effect();
            EffectInstance.Init(Effect.Type.Generic, player, 0, Vector3.up, Vector3.zero);
            EffectInstance.pooledstringid = StringPool.Get(fx);
            NetWrite netWrite = Net.sv.StartWrite();
            netWrite.PacketID(Message.Type.Effect);
            EffectInstance.WriteToStream(netWrite);
            netWrite.Send(new SendInfo(player.net.connection));
            EffectInstance.Clear();
        }

        private void PlayGfx(BasePlayer player, string fx)
        {
            string color = "1 1 1 1";
            string preset = config.ui.sprites[fx];
            if (config.ui.sprites[fx].Contains("("))
            {
                string[] split = config.ui.sprites[fx].Split('(');
                color = split[1].Remove(split[1].Length - 1);
                preset = split[0];
            }

            var run = _monoBehavior[player];
            if (run == null)
            {
                if (!_monoBehavior.ContainsKey(player))
                    _monoBehavior.Add(player, player.GetOrAddComponent<Overlay>());

                run = _monoBehavior[player];
            }
            if (run != null) run.RunOverlay(player, preset, color);
        }

        private void TeaBoost(BasePlayer player, string boostType, float _value, float _duration)
        {
            var bufftype = Modifier.ModifierType.Wood_Yield;
            switch (boostType)
            {
                case "wood":
                    bufftype = Modifier.ModifierType.Wood_Yield;
                    break;
                case "ore":
                    bufftype = Modifier.ModifierType.Ore_Yield;
                    break;
                case "scrap":
                    bufftype = Modifier.ModifierType.Scrap_Yield;
                    break;
                case "hp":
                    bufftype = Modifier.ModifierType.Max_Health;
                    break;
                case "rad":
                    bufftype = Modifier.ModifierType.Radiation_Resistance;
                    break;
                case "rad_expo":
                    bufftype = Modifier.ModifierType.Radiation_Exposure_Resistance;
                    break;
            }

            player.modifiers.Add(new List<ModifierDefintion>
            { new ModifierDefintion
                {
                    type = bufftype,
                    value = _value,
                    duration = _duration,
                    source = Modifier.ModifierSource.Tea
                }
            });
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null) return;
            if (newItem == null || oldItem == null)
            {
                player.SendConsoleCommand("gametip.hidegametip");
                return;
            }

            player.SendConsoleCommand("gametip.hidegametip");

            if (newItem.name != null && buffData.ContainsKey(newItem.name))
            {
                if (buffData[newItem.name].tooltipText == null) return;

                player.SendConsoleCommand("showtoast", 0, $"{buffData[newItem.name].tooltipText}");
            }
        }

        #endregion 

        #region [Comamnds]

        [ChatCommand("givetestdrugs")]
        private void givetestdrugs(BasePlayer player)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;

            var newItem = ItemManager.CreateByPartialName("antiradpills", 10);
            newItem.skin = 2556285147; newItem.name = "Health Pills";
            newItem.MarkDirty(); player.GiveItem(newItem);

            var newItem2 = ItemManager.CreateByPartialName("pickle", 10);
            newItem2.skin = 2561881327; newItem2.name = "Methamphetamine";
            newItem2.MarkDirty(); player.GiveItem(newItem2);
        }

        #endregion

        #region [CUI]

        private void CreateOverlay(BasePlayer player, string sprite, string color)
        {
            var overlay = new CuiElementContainer();
            overlay.Add(new CuiElement
            {
                Parent = "Overlay",
                Name = "overlay_main",
                Components =
                {
                    new CuiImageComponent { Material = "assets/icons/iconmaterial.mat", Sprite = sprite, Color = color, FadeIn = 0.3f},
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                FadeOut = 0.3f
            });
            CuiHelper.DestroyUi(player, "overlay_main");
            CuiHelper.AddUi(player, overlay);
        }

        /*
        assets/content/ui/overlay_poisoned.png
        assets/content/ui/overlay_freezing.png
        assets/content/ui/ui.background.transparent.radial.psd
        assets/content/ui/ui.background.transparent.linear.psd
        */

        #endregion

        #region [Behavior]

        private void PlayerComponent(bool add, BasePlayer player = null)
        {
            if (player != null)
            {
                if (add)
                {
                    if (!_monoBehavior.ContainsKey(player))
                        _monoBehavior.Add(player, player.GetOrAddComponent<Overlay>());
                }
                else
                {
                    _monoBehavior.Remove(player);
                    var run = player.GetComponent<Overlay>();
                    if (run != null)
                        UnityEngine.Object.Destroy(run);
                }
                return;
            }
            if (add)
            {
                foreach (var _player in BasePlayer.activePlayerList)
                {
                    if (!_monoBehavior.ContainsKey(_player))
                        _monoBehavior.Add(_player, _player.GetOrAddComponent<Overlay>());
                }
            }
            else
            {
                foreach (var _player in BasePlayer.activePlayerList)
                {
                    var run = _player.GetComponent<Overlay>();
                    if (run != null)
                        UnityEngine.Object.Destroy(run);

                }
            }
        }

        private Dictionary<BasePlayer, Overlay> _monoBehavior = new Dictionary<BasePlayer, Overlay>();

        private class Overlay : MonoBehaviour
        {
            BasePlayer player;
            string currentEoT;
            Data buff;
            float duration;

            void Awake() => player = GetComponent<BasePlayer>();

            void KillCui() => CuiHelper.DestroyUi(player, "overlay_main");

            public void RunOverlay(BasePlayer player, string sprite, string color)
            {
                plugin.CreateOverlay(player, sprite, color);
                if (player == null) return;

                if (IsInvoking(nameof(KillCui)) == false)
                {
                    CancelInvoke(nameof(KillCui));
                    Invoke(nameof(KillCui), 0.48f);
                }
                else
                {
                    Invoke(nameof(KillCui), 0.48f);
                }
            }

            public void ApplyEoT(string buffName)
            {
                currentEoT = buffName;
                buff = plugin.buffData[buffName];
                duration = buff.effectOverTime.EffectDuration;
                if (duration > 0)
                    InvokeRepeating(nameof(EffectOverTime), 1f, 1f);

            }

            private void EffectOverTime()
            {

                if (duration < 1)
                {
                    CancelInvoke(nameof(EffectOverTime));
                    duration = 0;
                    return;
                }

                if (buff.effectOverTime.calories != 0)
                    player.metabolism.calories.value = player.metabolism.calories.value + buff.effectOverTime.calories;

                if (buff.effectOverTime.hydration != 0)
                    player.metabolism.hydration.value = player.metabolism.hydration.value + buff.effectOverTime.hydration;

                if (buff.effectOverTime.health != 0)
                    player.health = player.health + buff.effectOverTime.health;

                if (buff.effectOverTime.healthRegen != 0)
                    player.metabolism.pending_health.value = player.metabolism.pending_health.value + buff.effectOverTime.healthRegen;

                if (buff.effectOverTime.comfort != 0)
                    player.metabolism.comfort.value += buff.effectOverTime.comfort;

                if (buff.effectOverTime.wetness != 0)
                    player.metabolism.wetness.value += buff.effectOverTime.wetness;

                if (buff.effectOverTime.oxygen != 0)
                    player.metabolism.oxygen.value += buff.effectOverTime.oxygen;

                if (buff.effectOverTime.temperature != 0)
                    player.metabolism.temperature.value += buff.effectOverTime.temperature;

                if (buff.effectOverTime.bleeding != 0)
                    player.metabolism.bleeding.value = player.metabolism.bleeding.value + buff.effectOverTime.bleeding;

                if (buff.effectOverTime.poison != 0)
                    player.metabolism.poison.value = player.metabolism.poison.value + buff.effectOverTime.poison;

                if (buff.effectOverTime.radiation != 0)
                    player.metabolism.radiation_poison.value = player.metabolism.radiation_poison.value + buff.effectOverTime.radiation;

                foreach (string fx in buff.effectOverTime.effects)
                {
                    if (fx.StartsWith("assets")) plugin.PlayFx(player, fx);

                    if (!fx.StartsWith("assets")) plugin.PlayGfx(player, fx);
                }

                duration -= 1;
            }

            public string isEffectTicking()
            {
                if (IsInvoking(nameof(EffectOverTime)))
                    return currentEoT;
                else
                    return null;
            }
        }


        #endregion

        #region [Data]

        private void SaveData()
        {
            if (buffData != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Effects", buffData);
        }

        private Dictionary<string, Data> buffData;

        private class Data
        {
            public class EoT
            {
                public float EffectDuration = 0f;
                public float calories = 0f;
                public float hydration = 0f;
                public float health = 0f;
                public float healthRegen = 0f;
                public float oxygen = 0f;
                public float temperature = 0f;
                public float comfort = 0f;
                public float wetness = 0f;
                public float bleeding = 0f;
                public float poison = 0f;
                public float radiation = 0f;
                public List<string> effects = new List<string> { };
            }

            public float calories;
            public float hydration;
            public float health;
            public float healthRegen;
            public float bleeding;
            public float poison;
            public float radiation;
            public float dirtyness;
            public bool TeaBoost_enabled;
            public string boostType;
            public float boostValue;
            public float boostDuration;
            public EoT effectOverTime;
            public string tooltipText;
            public List<string> effects = new List<string> { };


        }


        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/Effects"))
            {
                buffData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Data>>($"{Name}/Effects");
            }
            else
            {
                buffData = new Dictionary<string, Data>();
                buffData.Add("Default Entry", new Data());
                buffData["Default Entry"].effectOverTime = new Data.EoT();
                buffData["Default Entry"].boostType = "scrap";
                buffData["Default Entry"].tooltipText = "Tooltip Text";
                SaveData();
            }
        }

        #endregion

        #region [Config] 

        private Configuration config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);



        class Configuration
        {
            [JsonProperty(PropertyName = "Overdose")]
            public OD od { get; set; }

            public class OD
            {
                [JsonProperty("Overdose (dirty) Enabled.")]
                public bool enabled { get; set; }

                [JsonProperty("Reset Overdose (dirty) on death.")]
                public bool onDeath { get; set; }

                [JsonProperty("Overdose (dirty) Limit.")]
                public float dirtyLimit { get; set; }

                [JsonProperty("Buff applied when overdose limit is reached (buff name from data file).")]
                public string debuffName { get; set; }

                [JsonProperty("Show effect tooltips when item is held.")]
                public bool whenHeld { get; set; }
            }

            [JsonProperty(PropertyName = "UI Overlays (you can add your own)")]
            public UI ui { get; set; }

            public class UI
            {
                [JsonProperty("Formating 'unique name', 'asset link from game files(rust color code)'")]
                public Dictionary<string, string> sprites { get; set; }
            }

            public static Configuration CreateConfig()
            {
                return new Configuration
                {
                    od = new ConsumableEffects.Configuration.OD
                    {
                        enabled = false,
                        onDeath = true,
                        dirtyLimit = 150f,
                        debuffName = "overdose_debuff",
                        whenHeld = true
                    },
                    ui = new ConsumableEffects.Configuration.UI
                    {
                        sprites = new Dictionary<string, string>
                        {
                            { "healing_overlay", "assets/content/ui/overlay_poisoned.png(0.31 0.37 0.20 1.0)" },
                            { "orange_overlay", "assets/content/ui/overlay_freezing.png(0.67 0.36 0.05 1.0)" },
                            { "hurt_overlay", "assets/content/ui/ui.background.transparent.radial.psd(0.56 0.20 0.15 1.0)" },
                            { "blue_overlay", "assets/content/ui/ui.background.transparent.linear.psd(0.16 0.34 0.49 1.0)" },
                        },
                    },
                };

            }

        }
        #endregion
    }
}