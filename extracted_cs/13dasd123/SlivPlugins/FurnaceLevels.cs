// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
//patched for entity ids

using System;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{
    [Info("FurnaceLevels", "David", "1.4.6")]
    [Description("Furnace progression system")]

    public class FurnaceLevels : RustPlugin
    {

        //temp stacking issue
        object CanStackItem(Item source, Item target)
        {
            var storage = source.GetEntityOwner() as BaseOven;

            if (storage == null) return null;

            if (storage.ShortPrefabName == "furnace.large" || storage.ShortPrefabName == "refinery_small_deployed")
            {
                return (target.amount + source.amount) >= target.info.stackable ? (object)false : null;
            }

            return null;
        }


        [PluginReference]
        private Plugin PortableFurnaces;

        private bool flatOutput = false;
        private bool global = false;

        private string chatcommand = "furnace";

        private static FurnaceLevels _instance;

        private readonly List<FurnaceUpgrades> FurnControl = new List<FurnaceUpgrades>();

        #region Hooks

        private void OnServerInitialized()
        {
            _instance = this;
            LoadFurData();
            timer.Once(4f, () =>
            {
                if (PortableFurnaces != null)
                    chatcommand = "furnaceinfo";

                cmd.AddChatCommand(chatcommand, this, "furnace_chatcmd");
            });


            foreach (ulong furn in _furnaceData.Keys)
            {
                if (_furnaceData[furn].Speed > _config.speedSet.mod.Count() - 1)
                    _furnaceData[furn].Speed = _config.speedSet.mod.Count() - 1;

                if (_furnaceData[furn].Fuel > _config.fuelSet.mod.Count() - 1)
                    _furnaceData[furn].Fuel = _config.fuelSet.mod.Count() - 1;

                if (_furnaceData[furn].Output > _config.outSet.mod.Count() - 1)
                    _furnaceData[furn].Output = _config.outSet.mod.Count() - 1;
            }
            SaveFurData();

            permission.RegisterPermission($"furnacelevels.use", this);

            var ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>();

            for (var i = 0; i < ovens.Length; i++)
            {
                var oven = ovens[i];
                OnEntitySpawned(oven);
            }

            timer.Once(2f, () =>
            {
                for (var i = 0; i < ovens.Length; i++)
                {
                    var oven = ovens[i];
                    var component = oven.gameObject.GetComponent<FurnaceUpgrades>();
                    if (component == null) continue;

                    if (oven == null || oven.IsDestroyed || !oven.IsOn())
                        continue;

                    component.StartCooking();
                }
            });
        }

        private void OnServerSave()
        {
            SaveFurData();
        }

        private void Unload()
        {
            SaveFurData();

            var ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>();
            if (ovens == null) return;
            for (var i = 0; i < ovens.Length; i++)
            {
                var oven = ovens[i];
                if (oven == null) return;
                var component = oven.GetComponent<FurnaceUpgrades>();
                if (component == null) return;
                if (oven.IsOn())
                {
                    component.StopCooking();
                    oven.StartCooking();
                }

                UnityEngine.Object.Destroy(component);
            }
        }


        void OnLootEntity(BasePlayer player, BaseOven entity)
        {
            CreateOvenButton(player, entity.net.ID.Value, entity.PrefabName);
        }

        void OnLootEntityEnd(BasePlayer player, BaseOven entity)
        {
            DestroyCui(player);
        }

        void OnEntityKill(BaseOven oven)
        {
            _furnaceData.Remove(oven.net.ID.Value);
        }

        void OnNewSave(string filename)
        {
            _furnaceData.Clear();
        }

        object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {

            if (_furnaceData.ContainsKey(entity.net.ID.Value) && entity.ShortPrefabName.Contains("furnace"))
            {
                //Puts($"{_furnaceData[entity.net.ID.Value].Speed}/{_furnaceData[entity.net.ID.Value].Fuel}/{_furnaceData[entity.net.ID.Value].Output}");
                var item = ItemManager.CreateByName("furnace", 1, entity.skinID);
                if (item != null)
                {
                    item.text = $"{_furnaceData[entity.net.ID.Value].Speed}/{_furnaceData[entity.net.ID.Value].Fuel}/{_furnaceData[entity.net.ID.Value].Output}";
                    item.name += $"Furnace  Lvl.({_furnaceData[entity.net.ID.Value].Speed + 1}/{_furnaceData[entity.net.ID.Value].Fuel + 1}/{_furnaceData[entity.net.ID.Value].Output + 1})";
                    player.GiveItem(item);
                }
                _furnaceData.Remove(entity.net.ID.Value);
                entity.Kill();
                return false;
            }

            return null;

        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            try
            {
                var player = plan.GetOwnerPlayer();
                Item item = player.GetActiveItem();
                if (item.name == null) return;
                if (item.name.Contains("Lvl."))
                {
                    NextTick(() => {

                        BaseEntity entity = go.GetComponent<BaseEntity>();
                        CheckData(entity.net.ID.Value);
                        string[] levels = item.text.Split('/');
                        _furnaceData[entity.net.ID.Value].Speed = Convert.ToInt32(levels[0]);
                        _furnaceData[entity.net.ID.Value].Fuel = Convert.ToInt32(levels[1]);
                        _furnaceData[entity.net.ID.Value].Output = Convert.ToInt32(levels[2]);
                        SaveFurData();
                    });
                }
            }
            catch
            {
                // it's lit
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var oven = entity as BaseOven;
            if (oven == null)
                return;
            var ovenId = oven.net.ID.Value;
            if (!global)
            {
                if (oven.PrefabName == "assets/prefabs/deployable/furnace/furnace.prefab" ||
                oven.PrefabName == "assets/prefabs/deployable/furnace.large/furnace.large.prefab" ||
                oven.PrefabName == "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab")
                {
                    oven.gameObject.AddComponent<FurnaceUpgrades>();
                    CheckData(ovenId);
                }
                return;
            }
            oven.gameObject.AddComponent<FurnaceUpgrades>();
            CheckData(ovenId);
        }

        private object OnOvenToggle(StorageContainer oven, BasePlayer player)
        {
            if (!global)
            {
                if (oven.PrefabName != "assets/prefabs/deployable/furnace/furnace.prefab"
                && oven.PrefabName != "assets/prefabs/deployable/furnace.large/furnace.large.prefab"
                && oven.PrefabName != "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab") return null;
            }

            var component = oven.gameObject.GetComponent<FurnaceUpgrades>();
            if (oven.IsOn())
            {
                component.StopCooking();
            }
            else
            {
                component.StartCooking();
            }

            return false;
        }

        #endregion

        #region Methods / Functions

        private int GetMaxLevel(List<int> priceList, List<float> modList)
        {
            int value = priceList.Count();
            int value2 = modList.Count();
            int[] values = { value, value2 };
            return values.Min();
        }

        private int GetNextPrice(int index, string modType)
        {
            int price = 1;
            if (modType == "speed")
            {
                int maxLevel = GetMaxLevel(_config.speedSet.price, _config.speedSet.mod);
                if (index >= maxLevel) return 0;
                price = _config.speedSet.price[index];
            }
            if (modType == "fuel")
            {
                int maxLevel = GetMaxLevel(_config.fuelSet.price, _config.fuelSet.mod);
                if (index >= maxLevel) return 0;
                price = _config.fuelSet.price[index];
            }
            if (modType == "output")
            {
                int maxLevel = GetMaxLevel(_config.outSet.price, _config.outSet.mod);
                if (index >= maxLevel) return 0;
                price = _config.outSet.price[index];
            }
            return price;
        }


        private int GetUpgradeLevel(BasePlayer player, ulong ovenId, string modType)
        {
            int _mod = 0;
            if (modType == "speed") _mod = _furnaceData[ovenId].Speed;
            if (modType == "fuel") _mod = _furnaceData[ovenId].Fuel;
            if (modType == "output") _mod = _furnaceData[ovenId].Output;
            return _mod;
        }



        private void UpgradeLevel(BasePlayer player, ulong entityId, string mod, string prefab = "")
        {
            if (mod == "speed")
            {
                int index = _furnaceData[entityId].Speed + 1;

                if (prefab == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
                {
                    if (GetCurrency(player, Convert.ToInt32(Math.Round(_config.speedSet.price[index] * _config.mainSet.largecost, MidpointRounding.ToEven))))
                    {
                        _furnaceData[entityId].Speed++; SaveFurData(); RestartFurnace(entityId); return;
                    }
                    else { return; }
                }

                if (GetCurrency(player, _config.speedSet.price[index]))
                { _furnaceData[entityId].Speed++; SaveFurData(); RestartFurnace(entityId); return; }
                return;
            }
            if (mod == "fuel")
            {
                int index = _furnaceData[entityId].Fuel + 1;

                if (prefab == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
                {
                    if (GetCurrency(player, Convert.ToInt32(Math.Round(_config.fuelSet.price[index] * _config.mainSet.largecost, MidpointRounding.ToEven))))
                    {
                        _furnaceData[entityId].Fuel++; SaveFurData(); RestartFurnace(entityId); return;
                    }
                    else { return; }
                }

                if (GetCurrency(player, _config.fuelSet.price[index]))
                { _furnaceData[entityId].Fuel++; SaveFurData(); RestartFurnace(entityId); return; }
                return;
            }
            if (mod == "output")
            {
                int index = _furnaceData[entityId].Output + 1;

                if (prefab == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
                {
                    if (GetCurrency(player, Convert.ToInt32(Math.Round(_config.outSet.price[index] * _config.mainSet.largecost, MidpointRounding.ToEven))))
                    {
                        _furnaceData[entityId].Output++; SaveFurData(); RestartFurnace(entityId); return;
                    }
                    else { return; }
                }

                if (GetCurrency(player, _config.outSet.price[index]))
                { _furnaceData[entityId].Output++; SaveFurData(); RestartFurnace(entityId); return; }
                return;
            }
        }

        private void RestartFurnace(ulong ovenid)
        {
            var oven = BaseNetworkable.serverEntities.Find(new NetworkableId(ovenid)) as BaseOven;
            if (oven == null) return;
            if (oven.IsOn())
            {
                oven.StopCooking();
                var component = oven.gameObject.GetComponent<FurnaceUpgrades>();
                component.StartCooking();
            }

        }

        private void CheckData(ulong entityId)
        {
            if (!_furnaceData.ContainsKey(entityId))
            {
                _furnaceData.Add(entityId, new FurData());
                //SaveFurData();
            }
            return;
        }

        private void furnace_chatcmd(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (args.Length > 2) return;
            if (args.Length == 0) { SendReply(player, GetLang("chatCommand")); return; }
            string arg0 = args[0].ToLower();
            if (arg0 == "speed")
            {
                string allValues = GetLang("chatCommandSpeed");
                int level = 1;
                foreach (float value in _config.speedSet.mod)
                {
                    allValues = allValues + $"\n <color=#FFD6BA>Level {level}</color> ({value})";
                    level++;
                }
                SendReply(player, allValues); return;
            }
            if (arg0 == "fuel")
            {
                string allValues = GetLang("chatCommandFuel");
                int level = 1;
                foreach (float value in _config.fuelSet.mod)
                {
                    allValues = allValues + $"\n <color=#FFD6BA>Level {level}</color> ({value})";
                    level++;
                }
                SendReply(player, allValues); return;
            }
            if (arg0 == "output")
            {
                string allValues = GetLang("chatCommandOut");
                int level = 1;
                foreach (float value in _config.outSet.mod)
                {
                    allValues = allValues + $"\n <color=#FFD6BA>Level {level}</color> ({value}x)";
                    level++;
                }
                SendReply(player, allValues); return;
            }
        }


        [ConsoleCommand("furnace_ui")]
        private void furnace_ui(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() == null) return;
            if (args[0] == "openmenu")
            {
                if (!permission.UserHasPermission(player.UserIDString, "furnacelevels.use")) { SendReply(player, GetLang("noPerms")); return; }
                ulong netId = 1;
                if (args[1].Contains("oil"))
                { netId = Convert.ToUInt64(args[3]); }
                else { netId = Convert.ToUInt64(args[2]); }
                DestroyOvenButton(player);
                CheckData(netId);
                CreateCui(player, netId, args[1]);
                return;
            }
            if (args[0] == "upgrade")
            {
                ulong netId = Convert.ToUInt64(args[2]);
                UpgradeLevel(player, netId, args[3], args[1]);
                CreateCui(player, netId, args[1]);
                return;
            }
        }

        private void PlayEffect(BasePlayer player, string fx)
        {
            Effect.server.Run(fx, (BaseEntity)player, 0U, Vector3.zero, Vector3.zero);
        }


        #endregion

        #region Furnace Controller

        public class FurnaceUpgrades : FacepunchBehaviour
        {
            private int _ticks;

            private BaseOven _oven;

            private BaseOven Furnace
            {
                get
                {
                    if (_oven == null)
                        _oven = GetComponent<BaseOven>();

                    return _oven;
                }
            }
            private float speedMulti;
            private float fuelEffMulti;
            private float outputMulti;

            private Dictionary<string, float> _outputModifiers;

            private float OutputMultiplier(string shortname)
            {
                float modifier;
                modifier = 1.0f;
                return modifier;
            }

            private void Awake()
            {
                float modifierF;
                float modifierI;
                modifierF = 1.0f;
                speedMulti = 0.5f / modifierF;
                modifierI = 2.0f;
                fuelEffMulti = modifierI;
            }

            private Item FindBurnable()
            {
                if (Furnace.inventory == null)
                    return null;

                foreach (var item in Furnace.inventory.itemList)
                {
                    var component = item.info.GetComponent<ItemModBurnable>();
                    if (component && (Furnace.fuelType == null || item.info == Furnace.fuelType))
                    {
                        return item;
                    }
                }

                return null;
            }

            public void Cook()
            {
                var item = FindBurnable();
                if (item == null)
                {
                    StopCooking();
                    return;
                }

                SmeltItems();
                var slot = Furnace.GetSlot(BaseEntity.Slot.FireMod);
                if (slot)
                {
                    slot.SendMessage("Cook", 0.5f, SendMessageOptions.DontRequireReceiver);
                }
                var ovenId = Furnace.net.ID.Value;
                int fuelData = _furnaceData[ovenId].Fuel;
                if (fuelData == null)
                {
                    fuelEffMulti = 2.0f;
                }
                else
                {
                    fuelEffMulti = _config.fuelSet.mod[fuelData];
                }


                var component = item.info.GetComponent<ItemModBurnable>();
                item.fuel -= 0.5f * (Furnace.cookingTemperature / 200f) * fuelEffMulti;
                if (!item.HasFlag(global::Item.Flag.OnFire))
                {
                    item.SetFlag(global::Item.Flag.OnFire, true);
                    item.MarkDirty();
                }
                if (item.fuel <= 0f)
                {
                    ConsumeFuel(item, component);
                }

                _ticks++;
            }

            private void ConsumeFuel(Item fuel, ItemModBurnable burnable)
            {
                if (Furnace.allowByproductCreation && burnable.byproductItem != null && UnityEngine.Random.Range(0f, 1f) > burnable.byproductChance)
                {
                    var def = burnable.byproductItem;
                    var item = ItemManager.Create(def, (int)(burnable.byproductAmount * OutputMultiplier(def.shortname)));
                    if (!item.MoveToContainer(Furnace.inventory))
                    {
                        StopCooking();
                        item.Drop(Furnace.inventory.dropPosition, Furnace.inventory.dropVelocity);
                    }
                }

                if (fuel.amount <= 1)
                {
                    fuel.Remove();
                    return;
                }

                fuel.amount -= 1;
                fuel.fuel = burnable.fuelAmount;
                fuel.MarkDirty();
            }

            private void SmeltItems()
            {
                if (_ticks % 2 != 0)
                    return;

                for (var i = 0; i < Furnace.inventory.itemList.Count; i++)
                {
                    var item = Furnace.inventory.itemList[i];
                    if (item == null || !item.IsValid())
                        continue;

                    var cookable = item.info.GetComponent<ItemModCookable>();
                    if (cookable == null)
                        continue;

                    var temperature = item.temperature;

                    if ((temperature < cookable.lowTemp || temperature > cookable.highTemp))
                    {
                        if (!cookable.setCookingFlag || !item.HasFlag(global::Item.Flag.Cooking)) continue;
                        item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                        continue;
                    }

                    if (cookable.cookTime > 0 && _ticks * 1f / 1 % cookable.cookTime > 0)
                        continue;

                    if (cookable.setCookingFlag && !item.HasFlag(global::Item.Flag.Cooking))
                    {
                        item.SetFlag(global::Item.Flag.Cooking, true);
                        item.MarkDirty();
                    }

                    var position = item.position;
                    if (item.amount > 1)
                    {
                        item.amount--;
                        item.MarkDirty();
                    }
                    else
                    {
                        item.Remove();
                    }

                    if (cookable.becomeOnCooked == null) continue;
                    var ovenId = Furnace.net.ID.Value;
                    int itemData = _furnaceData[ovenId].Output;
                    if (itemData == null)
                    {
                        outputMulti = 1;
                    }
                    else
                    {
                        outputMulti = _config.outSet.mod[itemData];
                    }

                    var item2 = ItemManager.Create(cookable.becomeOnCooked,
                        (int)(cookable.amountOfBecome + RandomProc(outputMulti)));

                    if (_instance.flatOutput)
                    {
                        item2 = ItemManager.Create(cookable.becomeOnCooked,
                        (int)(cookable.amountOfBecome * outputMulti));
                    }

                    if (_oven.temperature == global::BaseOven.TemperatureType.Cooking)
                    {
                        item2 = ItemManager.CreateByName(cookable.becomeOnCooked.shortname.Replace("burned", "cooked"),
                        (int)(cookable.amountOfBecome + RandomProc(outputMulti)));
                    }

                    if (item2 == null || item2.MoveToContainer(item.parent, position) ||
                        item2.MoveToContainer(item.parent))
                        continue;

                    item2.Drop(item.parent.dropPosition, item.parent.dropVelocity);
                    if (!item.parent.entityOwner) continue;
                    StopCooking();
                }
            }

            private int RandomProc(float value)
            {
                float random = UnityEngine.Random.Range(1, 1001);

                if (random <= value * 10)
                    return 1;
                else
                    return 0;
            }

            public void StartCooking()
            {
                if (FindBurnable() == null)
                {
                    return;
                }

                StopCooking();

                Furnace.inventory.temperature = Furnace.cookingTemperature;
                Furnace.UpdateAttachmentTemperature();
                var ovenId = Furnace.net.ID.Value;
                int speedData = _furnaceData[ovenId].Speed;
                if (speedData == null)
                {
                    speedMulti = 0.5f;
                }
                else
                {
                    speedMulti = _config.speedSet.mod[speedData];
                }
                Furnace.InvokeRepeating(Cook, speedMulti, speedMulti);
                Furnace.SetFlag(BaseEntity.Flags.On, true);
            }

            public void StopCooking()
            {
                Furnace.CancelInvoke(Cook);
                Furnace.StopCooking();
            }
        }

        #endregion

        #region Furnace GUI

        private void CreateOvenButton(BasePlayer player, ulong ovenId, string prefabName)
        {

            if (prefabName != "assets/prefabs/deployable/furnace/furnace.prefab"
            && prefabName != "assets/prefabs/deployable/furnace.large/furnace.large.prefab"
            && prefabName != "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab") return;

            string[] mainOffsets = { "313 494", "365 515" };

            if (prefabName == "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab")
            { mainOffsets[0] = "410 494"; mainOffsets[1] = "462 515"; }

            if (prefabName == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
            { mainOffsets[0] = "400 566"; mainOffsets[1] = "451 588"; }


            var _upgradeButton = CUIClass.CreateOverlay("ref1", "0 0 0 0", "0 0", "0 0", false, 0.0f, $"assets/icons/iconmaterial.mat");

            CUIClass.CreatePanel(ref _upgradeButton, "f_upgrade_button_div", "Overlay", "0 0 0 0", "0.5 0", "0.5 0", false, 0f, "assets/content/ui/uibackgroundblur.mat", mainOffsets[0], mainOffsets[1]);
            CUIClass.CreateButton(ref _upgradeButton, "f_upgrade_button", "f_upgrade_button_div", "0.40 0.48 0.25 0.75", GetLang("upgradeBtn"), 12, "0 0", "1 1", $"furnace_ui openmenu {prefabName} {ovenId}", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");

            DestroyOvenButton(player);
            CuiHelper.AddUi(player, _upgradeButton);
        }

        private void DestroyOvenButton(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ref1");
            CuiHelper.DestroyUi(player, "f_upgrade_button_div");
        }

        private void CreateCui(BasePlayer player, ulong ovenId, string prefabName)
        {
            string[] titleOffsets = { "0 0", "0 0" };
            string[] mainOffsets = { "193 525", "570 665" };
            if (prefabName == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
            {
                titleOffsets[0] = "-205 560"; titleOffsets[1] = "180 600";
                mainOffsets[0] = "-199 385"; mainOffsets[1] = "180 554";
            }

            int speedLevel = GetUpgradeLevel(player, ovenId, "speed") + 1;
            int fuelLevel = GetUpgradeLevel(player, ovenId, "fuel") + 1;
            int outputLevel = GetUpgradeLevel(player, ovenId, "output") + 1;

            string currency = _config.mainSet.currencyDisplayName;
            int speedCost = GetNextPrice(speedLevel, "speed");
            int fuelCost = GetNextPrice(fuelLevel, "fuel");
            int outputCost = GetNextPrice(outputLevel, "output");

            if (prefabName == "assets/prefabs/deployable/furnace.large/furnace.large.prefab")
            {
                speedCost = Convert.ToInt32(Math.Round(speedCost * _config.mainSet.largecost, MidpointRounding.ToEven));
                fuelCost = Convert.ToInt32(Math.Round(fuelCost * _config.mainSet.largecost, MidpointRounding.ToEven));
                outputCost = Convert.ToInt32(Math.Round(outputCost * _config.mainSet.largecost, MidpointRounding.ToEven));
            }

            string speedText = $"<size=15><b>{GetLang("speedTitle")} (Lvl.{speedLevel})</b></size> \n{GetLang("speedDesc")}";
            string fuelText = $"<size=15><b>{GetLang("fuelTitle")} (Lvl.{fuelLevel})</b></size> \n{GetLang("fuelDesc")}";
            string outText = $"<size=15><b>{GetLang("outTitle")} (Lvl.{outputLevel})</b></size> \n{GetLang("outDesc")}";
            string infoText = GetLang("info");

            var _furnaceCui = CUIClass.CreateOverlay("ref2", "0 0 0 0", "0 0", "0 0", false, 0.0f, $"assets/icons/iconmaterial.mat");


            CUIClass.CreatePanel(ref _furnaceCui, "f_title_div", "Overlay", "0.70 0.67 0.65 0.0", "0.5 0", "0.5 0", false, 0f, "assets/icons/iconmaterial.mat", titleOffsets[0], titleOffsets[1]);
            CUIClass.CreateText(ref _furnaceCui, "f_title_text", "f_title_div", "0.94 0.91 0.87 1", $"UPGRADES", 28, "0.023 0", "1 1", TextAnchor.LowerLeft, "robotocondensed-bold.ttf", "0 0 0 0", "0 0");
            CUIClass.CreatePanel(ref _furnaceCui, "f_content_div", "Overlay", "0.70 0.67 0.65 0.07", "0.5 0", "0.5 0", false, 0f, "assets/content/ui/uibackgroundblur.mat", mainOffsets[0], mainOffsets[1]);
            CUIClass.CreatePanel(ref _furnaceCui, "f_content_div_1", "f_content_div", "0 0 0 0", "0.01 0.68", "0.99 0.98", false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreatePanel(ref _furnaceCui, "f_content_div_2", "f_content_div", "0 0 0 0", "0.01 0.36", "0.99 0.66", false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreatePanel(ref _furnaceCui, "f_content_div_3", "f_content_div", "0 0 0 0", "0.01 0.04", "0.99 0.34", false, 0f, "assets/icons/iconmaterial.mat");
            //CUIClass.CreatePanel(ref _furnaceCui, "f_content_div_4", "f_content_div", "0 0 0 0", "0.01 0.01", "0.99 0.16", false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreateText(ref _furnaceCui, "speed_text", "f_content_div_1", "1 1 1 0.65", speedText, 10, "0.03 0.0", "1 1", TextAnchor.MiddleLeft, "robotocondensed-regular.ttf", "0 0 0 0", "0 0");
            CUIClass.CreateText(ref _furnaceCui, "fuel_text", "f_content_div_2", "1 1 1 0.65", fuelText, 10, "0.03 0.0", "1 1", TextAnchor.MiddleLeft, "robotocondensed-regular.ttf", "0 0 0 0", "0 0");
            CUIClass.CreateText(ref _furnaceCui, "out_text", "f_content_div_3", "1 1 1 0.65", outText, 10, "0.03 0.0", "1 1", TextAnchor.MiddleLeft, "robotocondensed-regular.ttf", "0 0 0 0", "0 0");
            //CUIClass.CreateText(ref _furnaceCui, "info_text", "f_content_div_4", "1 1 1 0.45", infoText, 10, "0.05 0.0", "0.95 1", TextAnchor.MiddleCenter, "robotocondensed-regular.ttf", "0 0 0 0", "0 0");

            if (speedCost != 0)
            {
                CUIClass.CreateButton(ref _furnaceCui, "speed_btn", "f_content_div_1", "0.40 0.48 0.25 0.65", $"{GetLang("upFor")} {speedCost}{currency}", 12, "0.55 0.18", "0.98 0.82", $"furnace_ui upgrade {prefabName} {ovenId} speed", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
            }
            else { CUIClass.CreateButton(ref _furnaceCui, "speed_btn", "f_content_div_1", "0.70 0.67 0.65 0.20", $"{GetLang("maxLevel")}", 12, "0.55 0.18", "0.98 0.82", $"", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf"); }
            if (fuelCost != 0)
            {
                CUIClass.CreateButton(ref _furnaceCui, "fuel_btn", "f_content_div_2", "0.40 0.48 0.25 0.65", $"{GetLang("upFor")} {fuelCost}{currency}", 12, "0.55 0.18", "0.98 0.82", $"furnace_ui upgrade {prefabName} {ovenId} fuel", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
            }
            else { CUIClass.CreateButton(ref _furnaceCui, "fuel_btn", "f_content_div_2", "0.70 0.67 0.65 0.20", $"{GetLang("maxLevel")}", 12, "0.55 0.18", "0.98 0.82", $"", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf"); }
            if (outputCost != 0)
            {
                CUIClass.CreateButton(ref _furnaceCui, "out_btn", "f_content_div_3", "0.40 0.48 0.25 0.65", $"{GetLang("upFor")} {outputCost}{currency}", 12, "0.55 0.18", "0.98 0.82", $"furnace_ui upgrade {prefabName} {ovenId} output", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
            }
            else { CUIClass.CreateButton(ref _furnaceCui, "out_btn", "f_content_div_3", "0.70 0.67 0.65 0.20", $"{GetLang("maxLevel")}", 12, "0.55 0.18", "0.98 0.82", $"", "", "1 1 1 0.7", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf"); }

            DestroyCui(player);
            CuiHelper.AddUi(player, _furnaceCui);
        }

        private void DestroyCui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "f_content_div");
            CuiHelper.DestroyUi(player, "f_title_div");
            CuiHelper.DestroyUi(player, "title_div2");
            CuiHelper.DestroyUi(player, "f_upgrade_button_div");
            CuiHelper.DestroyUi(player, "ref1");
            CuiHelper.DestroyUi(player, "ref2");

        }

        #endregion

        #region Get Payments

        [PluginReference]
        private Plugin Economics;

        [PluginReference]
        private Plugin ServerRewards;

        private bool GetCurrency(BasePlayer player, int _currencyAmount)
        {
            string currencyType = _config.mainSet.currencyType.ToLower();
            if (currencyType == "serverrewards")
            {
                if (GetRP(player, _currencyAmount))
                {
                    SendReply(player, GetLang("upSucc"));
                    PlayEffect(player, _config.fx.succ);
                    return true;
                }
                SendReply(player, GetLang("noFunds"));
                PlayEffect(player, _config.fx.dnd);
                return false;
            }

            if (currencyType == "economics")
            {
                if (GetEco(player, _currencyAmount))
                {
                    SendReply(player, GetLang("upSucc"));
                    PlayEffect(player, _config.fx.succ);
                    return true;
                }
                SendReply(player, GetLang("noFunds"));
                PlayEffect(player, _config.fx.dnd);
                return false;
            }

            if (currencyType == "item")
            {
                if (GetInventoryItem(player, _config.mainSet.currencyItem, _currencyAmount))
                {
                    SendReply(player, GetLang("upSucc"));
                    PlayEffect(player, _config.fx.succ);
                    return true;
                }
                SendReply(player, GetLang("noFunds"));
                PlayEffect(player, _config.fx.dnd);
                return false;
            }
            SendReply(player, GetLang("noFunds"));
            PlayEffect(player, _config.fx.dnd);
            return false;
        }

        private bool GetRP(BasePlayer player, int _currencyAmount)
        {
            var checkRP = ServerRewards?.Call<int>("CheckPoints", player.userID);
            if (checkRP >= _currencyAmount)
            {
                ServerRewards?.Call("TakePoints", player.userID, _currencyAmount);
                return true;
            }
            return false;
        }

        private bool GetEco(BasePlayer player, int _currencyAmount)
        {
            double checkEco = Economics.Call<double>("Balance", player.UserIDString);
            double _ecoConvert = Convert.ToDouble(_currencyAmount);
            int playerBalance = Convert.ToInt32(checkEco);
            if (playerBalance >= _currencyAmount)
            {
                Economics.CallHook("Withdraw", player.UserIDString, _ecoConvert);
                return true;

            }
            return false;
        }

        private bool GetInventoryItem(BasePlayer player, int _itemID, int _itemCost)
        {

            int pInventory = player.inventory.GetAmount(_itemID);

            if (pInventory < _itemCost)
            {
                return false;
            }
            player.inventory.Take(null, _itemID, _itemCost);
            return true;
        }

        #endregion

        #region CUI Classes

        public class CUIClass
        {
            public static CuiElementContainer CreateOverlay(string _name, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 0f, string _mat = "")
            {


                var _element = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = _color, Material = _mat, FadeIn = _fade},
                            RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                            CursorEnabled = _cursorOn
                        },
                        new CuiElement().Parent = "Overlay",
                        _name
                    }
                };
                return _element;
            }

            public static void CreatePanel(ref CuiElementContainer _container, string _name, string _parent, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 0f, string _mat2 = "", string _OffsetMin = "", string _OffsetMax = "")
            {
                _container.Add(new CuiPanel
                {
                    Image = { Color = _color, Material = _mat2, FadeIn = _fade },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax, OffsetMin = _OffsetMin, OffsetMax = _OffsetMax },
                    CursorEnabled = _cursorOn
                },
                _parent,
                _name);
            }

            public static void CreateImage(ref CuiElementContainer _container, string _parent, string _image, string _anchorMin, string _anchorMax, float _fade = 1f)
            {
                if (_image.StartsWith("http") || _image.StartsWith("www"))
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Url = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
                else
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Png = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
            }

            public static void CreateInput(ref CuiElementContainer _container, string _name, string _parent, string _color, int _size, string _anchorMin, string _anchorMax, string _font = "permanentmarker.ttf", string _command = "command.processinput", TextAnchor _align = TextAnchor.MiddleCenter)
            {
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,

                    Components =
                    {
                        new CuiInputFieldComponent
                        {

                            Text = "0",
                            CharsLimit = 250,
                            Color = _color,
                            IsPassword = false,
                            Command = _command,
                            Font = _font,
                            FontSize = _size,
                            Align = _align
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _anchorMin,
                            AnchorMax = _anchorMax

                        }

                    },
                });
            }

            public static void CreateText(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "robotocondensed-bold.ttf", string _outlineColor = "", string _outlineScale = "")
            {


                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _text,
                            FontSize = _size,
                            Font = _font,
                            Align = _align,
                            Color = _color,
                            FadeIn = 0f,
                        },

                        new CuiOutlineComponent
                        {

                            Color = _outlineColor,
                            Distance = _outlineScale

                        },

                        new CuiRectTransformComponent
                        {
                             AnchorMin = _anchorMin,
                             AnchorMax = _anchorMax
                        }
                    },
                });
            }

            public static void CreateButton(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, string _command = "", string _close = "", string _textColor = "0.843 0.816 0.78 1", float _fade = 1f, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "")
            {

                _container.Add(new CuiButton
                {
                    Button = { Close = _close, Command = _command, Color = _color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = _fade },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    Text = { Text = _text, FontSize = _size, Align = _align, Color = _textColor, Font = _font, FadeIn = _fade }
                },
                _parent,
                _name);
            }

        }
        #endregion

        #region Furnace Data

        private void SaveFurData()
        {
            if (_furnaceData != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/FurnaceData", _furnaceData);
        }

        private static Dictionary<ulong, FurData> _furnaceData = new Dictionary<ulong, FurData>();

        private class FurData
        {
            public int Speed;
            public int Fuel;
            public int Output;
        }

        private void LoadFurData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/FurnaceData"))
            {
                _furnaceData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, FurData>>($"{Name}/FurnaceData");
            }
            else
            {
                SaveFurData();
            }
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noFunds"] = "Not enough funds to upgrade furnace.",
                ["upSucc"] = "Furnace was successfully upgraded.",
                ["noPerms"] = "You missing permissions to access upgrade menu.",
                ["info"] = $"Upgrades are tied to particular furnace, after picking up or destroying furnace, upgrades are lost. Type /{chatcommand} for more info.",
                ["speedTitle"] = "Smelting Speed",
                ["fuelTitle"] = "Fuel Efficiency",
                ["outTitle"] = "Resource Output",
                ["speedDesc"] = "Speeding up smelting process.",
                ["fuelDesc"] = "Decreasing fuel usage per tick.",
                ["outDesc"] = "Chance to receive extra smelted item.",
                ["maxLevel"] = "✓ Already Max Level",
                ["upFor"] = "Upgrade for",
                ["upgradeBtn"] = "Upgrade",
                ["chatCommand"] = $"<size=19><color=#FF6800>Furnace Levels</color></size> \n\n Upgrades are tied to particular furnace, after picking up or destroying furnace, upgrades are lost. \n\n To apply purchased upgrades just toggle furnace <color=#FFD6BA>'off'</color> and <color=#FFD6BA>'on'</color>. \n\n <size=16><color=#FFB888>Available Upgrades</color></size>\n• Smelting Speed   <color=#FFD6BA><size=10>/{chatcommand} speed</size></color>\n• Fuel Usage   <color=#FFD6BA><size=10>/{chatcommand} fuel</size></color> \n• Output Modifier   <color=#FFD6BA><size=10>/{chatcommand} output</size></color>",
                ["chatCommandSpeed"] = "<size=14><color=#FF6800>Speed Upgrades</color></size>\n<size=11>Lower value equals higher smelting speed.</size>\n",
                ["chatCommandFuel"] = "<size=14><color=#FF6800>Fuel Upgrades</color></size>\n<size=11>Lower value means wood lower food consuption when smelting.</size>\n",
                ["chatCommandOut"] = "<size=14><color=#FF6800>Output Upgrades</color></size>\n<size=11>Resource output multiplier.</size>\n",
            }, this);
        }

        private string GetLang(string _message) => lang.GetMessage(_message, this);

        #endregion

        #region [Config] 

        private static Configuration _config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);



        class Configuration
        {
            [JsonProperty(PropertyName = "Main Settings")]
            public MainSet mainSet { get; set; }

            public class MainSet
            {
                [JsonProperty("Currency Type")]
                public string currencyType { get; set; }

                [JsonProperty("Currency Display Name")]
                public string currencyDisplayName { get; set; }

                [JsonProperty("Currency Item ID (only if type is Item)")]
                public int currencyItem { get; set; }

                [JsonProperty("Large Furnace Cost Multiplier - Default 1.0x")]
                public float largecost { get; set; }
            }

            [JsonProperty(PropertyName = "Speed (Default is 0.5 - Lowering value increases smelting speed)")]
            public SpeedSet speedSet { get; set; }

            public class SpeedSet
            {
                [JsonProperty("Modifier")]
                public List<float> mod { get; set; }

                [JsonProperty("Upgrade Price")]
                public List<int> price { get; set; }
            }

            [JsonProperty(PropertyName = "Fuel (Default is 2.0 - Lower value means wood lasts longer when smelting)")]
            public FuelSet fuelSet { get; set; }

            public class FuelSet
            {
                [JsonProperty("Modifier")]
                public List<float> mod { get; set; }

                [JsonProperty("Upgrade Price")]
                public List<int> price { get; set; }
            }

            [JsonProperty(PropertyName = "Chance to receive extra product when item is smelted.")]
            public OutSet outSet { get; set; }

            public class OutSet
            {
                [JsonProperty("Modifier")]
                public List<float> mod { get; set; }

                [JsonProperty("Upgrade Price")]
                public List<int> price { get; set; }
            }

            [JsonProperty(PropertyName = "Sound Fx")]
            public FxSet fx { get; set; }

            public class FxSet
            {
                [JsonProperty("Effects Enabled")]
                public bool enabled { get; set; }

                [JsonProperty("On Furnace Upgrade")]
                public string succ { get; set; }

                [JsonProperty("Not Enough Currency")]
                public string dnd { get; set; }

            }

            public static Configuration CreateConfig()
            {
                return new Configuration
                {
                    mainSet = new FurnaceLevels.Configuration.MainSet
                    {
                        currencyType = "Economics",
                        currencyItem = 0,
                        currencyDisplayName = "$",
                        largecost = 1.0f,

                    },
                    speedSet = new FurnaceLevels.Configuration.SpeedSet
                    {
                        mod = new List<float>
                        {
                                0.5f,
                                0.4f,
                                0.3f,
                                0.2f
                        },
                        price = new List<int>
                        {
                                0,
                                30,
                                50,
                                70
                        },
                    },
                    fuelSet = new FurnaceLevels.Configuration.FuelSet
                    {
                        mod = new List<float>
                        {
                                2.0f,
                                1.5f,
                                1.00f,
                                0.5f
                        },
                        price = new List<int>
                        {
                                0,
                                30,
                                50,
                                70
                        },
                    },
                    outSet = new FurnaceLevels.Configuration.OutSet
                    {
                        mod = new List<float>
                        {
                                1.0f,
                                2.0f,
                                3.0f,
                                4.0f
                        },
                        price = new List<int>
                        {
                                0,
                                30,
                                50,
                                70
                        },
                    },
                    fx = new FurnaceLevels.Configuration.FxSet
                    {
                        enabled = true,
                        succ = "assets/bundled/prefabs/fx/build/promote_stone.prefab",
                        dnd = "assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab",
                    },
                };
            }

        }
        #endregion

    }
}