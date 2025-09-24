// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System.Linq;

using Rust;

using UnityEngine;

using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using JSONObject = JSON.Object;
using JSONArray = JSON.Array;
using JSONValue = JSON.Value;
using JSONValueType = JSON.ValueType;

namespace Oxide.Plugins
{
    [Info("ItemConfig", "Nogrod", "1.0.11", ResourceId = 806)]
    class ItemConfig : RustPlugin
    {
        private string _configpath = "";

        void Loaded()
        {
            _configpath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
        }

        void LoadDefaultConfig()
        {

        }

        private static JSONObject ToJsonObject(object obj)
        {
            return JSONObject.Parse(ToJsonString(obj));
        }

        private static JSONArray ToJsonArray(object obj)
        {
            return JSONArray.Parse(ToJsonString(obj));
        }

        private static string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
                {
                    ContractResolver = new DynamicContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
                });
        }

        private static void StripObject(JSONObject obj)
        {
            if (obj == null) return;
            var keys = obj.Select(entry => entry.Key).ToList();
            foreach (var key in keys)
            {
                if (!key.Equals("shortname") && !key.Equals("itemid"))
                    obj.Remove(key);
            }
        }

        private static void StripArray(JSONArray arr, string key)
        {
            if (arr == null) return;
            foreach (var obj in arr)
            {
                StripObject(obj.Obj[key].Obj);
            }
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            Config["Version"] = Protocol.network;
            var itemList = Resources.LoadAll<ItemDefinition>("items/").ToList();
            var bpList = Resources.LoadAll<ItemBlueprint>("items/").ToList();
            var items = new JSONArray();
            foreach (var definition in itemList)
            {
                var obj = ToJsonObject(definition);
                var mods = definition.GetComponentsInChildren<ItemMod>(true);
                var modArray = new JSONArray();
                foreach (var itemMod in mods)
                {
                    if (itemMod.GetType() == typeof (ItemModMenuOption)) continue;
                    var mod = ToJsonObject(itemMod);
                    if (itemMod.GetType() == typeof(ItemModBurnable))
                    {
                        StripObject(mod["byproductItem"].Obj);
                    }
                    else if (itemMod.GetType() == typeof(ItemModCookable))
                    {
                        StripObject(mod["becomeOnCooked"].Obj);
                    }
                    else if (itemMod.GetType() == typeof(ItemModConsume))
                    {
                        mod["effects"] = ToJsonArray(itemMod.GetComponent<ItemModConsumable>().effects);
                    }
                    else if (itemMod.GetType() == typeof(ItemModSwap))
                    {
                        var becomeItems = mod["becomeItem"].Array;
                        foreach (var entry in becomeItems)
                        {
                            entry.Obj["itemDef"] = entry.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                        }
                    }
                    if (!mod.Any()) continue;
                    mod["type"] = itemMod.GetType().FullName;
                    modArray.Add(mod);
                }
                var modEntity = definition.GetComponent<ItemModEntity>();
                if (modEntity != null)
                {
                    var thrownWeapon = modEntity.entityPrefab.targetObject.GetComponent<ThrownWeapon>();
                    if (thrownWeapon != null)
                    {
                        var timedExplosive = thrownWeapon.prefabToThrow.GetComponent<TimedExplosive>();
                        if (timedExplosive != null)
                        {
                            var mod = ToJsonObject(timedExplosive);
                            mod["type"] = modEntity.GetType().FullName + timedExplosive.GetType().FullName;
                            modArray.Add(mod);
                        }
                    }
                }
                var modProjectile = definition.GetComponent<ItemModProjectile>();
                if (modProjectile != null)
                {
                    var projectile = modProjectile.projectileObject.targetObject.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        var mod = ToJsonObject(projectile);
                        mod.Remove("sourceWeapon");
                        mod.Remove("projectileID");
                        mod.Remove("seed");
                        mod.Remove("velocityScalar");
                        mod["type"] = modProjectile.GetType().FullName;
                        modArray.Add(mod);
                    }
                    /*var components = modProjectile.projectileObject.targetObject.GetComponents(typeof (Component));
                    foreach (var component in components)
                    {
                        LocalPuts("Name: " + component.name + " Type: " + component.GetType().Name);                            
                    }*/
                    var timedExplosive = modProjectile.projectileObject.targetObject.GetComponent<TimedExplosive>();
                    if (timedExplosive != null)
                    {
                        var mod = ToJsonObject(timedExplosive);
                        mod["type"] = modProjectile.GetType().FullName + timedExplosive.GetType().FullName;
                        modArray.Add(mod);
                    }
                    var serverProjectile = modProjectile.projectileObject.targetObject.GetComponent<ServerProjectile>();
                    if (serverProjectile != null)
                    {
                        var mod = ToJsonObject(serverProjectile);
                        mod["type"] = modProjectile.GetType().FullName + serverProjectile.GetType().FullName;
                        modArray.Add(mod);
                    }
                }
                obj["modules"] = modArray;

                items.Add(obj);
            }
            Config["Items"] = JsonObjectToObject(items);
            var bps = ToJsonArray(bpList);
            foreach (var bp in bps)
            {
                StripObject(bp.Obj["targetItem"].Obj);
                foreach (var ing in bp.Obj.GetArray("ingredients"))
                {
                    ing.Obj["itemDef"] = ing.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                }
            }
            Config["Blueprints"] = JsonObjectToObject(bps);
            
            try
            {
                Config.Save(_configpath);
            }
            catch (Exception e)
            {
                LocalPuts(e.Message);
                return false;
            }
            LocalPuts("Created new config");
            return LoadConfig();
        }

        private bool LoadConfig()
        {
            try
            {
                Config.Load(_configpath);
            }
            catch (FileNotFoundException)
            {
                return CreateDefaultConfig();
            }
            catch (Exception e)
            {
                LocalPuts(e.Message);
                return false;
            }
            return true;
        }

        private void CheckConfig()
        {
            if (Config["Version"] != null && (int) Config["Version"] == Protocol.network) return;
            LocalPuts("Incorrect config version(" + Config["Version"] + ") move to .old");
            Config.Save(string.Format("{0}.old", _configpath));
            CreateDefaultConfig();
        }

        void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateItems();
            UpdateBlueprints();
        }

        private void UpdateItems()
        {
            var items = Config["Items"] as List<object>;
            if (items == null)
            {
                LocalPuts("No items in config");
                return;
            }
            var manager = SingletonComponent<ItemManager>.Instance;
            foreach (var item in items)
            {
                var value = ObjectToJsonObject(item);
                if (value.Type != JSONValueType.Object)
                {
                    LocalPuts("Item is not object");
                    continue;
                }
                var obj = value.Obj;
                var itemid = obj.GetInt("itemid", 0);
                var definition = manager.itemList.Find(x => x.itemid == itemid);
                if (definition == null)
                {
                    LocalPuts("Item does not exist: " + obj.GetString("shortname", "") + "(" + itemid + ")");
                    continue;
                }
                UpdateItem(definition, obj);
            }
        }

        private void UpdateBlueprints()
        {
            var bps = Config["Blueprints"] as List<object>;
            if (bps == null)
            {
                LocalPuts("No blueprints in config");
                return;
            }
            var manager = SingletonComponent<ItemManager>.Instance;
            foreach (var blueprint in bps)
            {
                var value = ObjectToJsonObject(blueprint);
                if (value.Type != JSONValueType.Object)
                {
                    LocalPuts("Item is not object");
                    continue;
                }
                var obj = value.Obj;
                var itemid = obj.GetObject("targetItem").GetInt("itemid", 0);
                var bp = manager.bpList.Find(x => x.targetItem.itemid == itemid);
                if (bp == null)
                {
                    LocalPuts("Blueprint does not exist: " + obj.GetObject("targetItem").GetString("shortname", "") + "(" + itemid + ")");
                    continue;
                }
                UpdateBlueprint(bp, obj);
            }
            manager.defaultBlueprints = manager.bpList.Where(x => x.defaultBlueprint).Select(x => x.targetItem.itemid).ToArray();
        }

        private static void UpdateBlueprint(ItemBlueprint bp, JSONObject o)
        {
            bp.rarity = (Rarity)Enum.Parse(typeof(Rarity), o.GetString("rarity", "None"));
            bp.time = o.GetInt("time", 0);
            bp.amountToCreate = o.GetInt("amountToCreate", 1);
            bp.userCraftable = o.GetBoolean("userCraftable", true);
            bp.defaultBlueprint = o.GetBoolean("defaultBlueprint", false);
            var ingredients = o.GetArray("ingredients");
            var manager = SingletonComponent<ItemManager>.Instance;
            bp.ingredients.Clear();
            foreach (var ingredient in ingredients)
            {
                var itemid = ingredient.Obj.GetInt("itemid", 0);
                var definition = manager.itemList.Find(x => x.itemid == itemid);
                bp.ingredients.Add(new ItemAmount(definition, ingredient.Obj.GetFloat("amount", 0)));
            }
        }

        private static void UpdateItem(ItemDefinition definition, JSONObject item)
        {
            definition.shortname = item.GetString("shortname", "unnamed");
            definition.itemid = item.GetInt("itemid", 0);
            definition.stackable = item.GetInt("stackable", 1);
            definition.category = (ItemCategory)Enum.Parse(typeof(ItemCategory), item.GetString("category", "Weapon"));
            var condition = item.GetObject("condition");
            definition.condition.enabled = condition.GetBoolean("enabled", false);
            definition.condition.max = condition.GetInt("max", 0);
            definition.condition.repairable = condition.GetBoolean("repairable", false);
            definition.rarity = (Rarity)Enum.Parse(typeof(Rarity), item.GetString("rarity", "None"));
            var modules = item.GetArray("modules").Select(m => m.Obj);
            var cook = 0;
            foreach (var mod in modules)
            {
                var typeName = mod.GetString("type", "");
                if (typeName.Equals("ItemModConsume"))
                {
                    var itemMod = definition.GetComponent<ItemModConsume>();
                    var itemEffects = itemMod.GetComponent<ItemModConsumable>().effects;
                    var effects = mod.GetArray("effects");
                    itemEffects.Clear();
                    foreach (var effect in effects)
                    {
                        itemEffects.Add(new ItemModConsumable.ConsumableEffect
                        {
                            type = (MetabolismAttribute.Type)Enum.Parse(typeof (MetabolismAttribute.Type), effect.Obj.GetString("type", "")),
                            amount = effect.Obj.GetFloat("amount", 0),
                            time = effect.Obj.GetFloat("time", 0)
                        });
                    }
                } 
                else if (typeName.Equals("ItemModContainer"))
                {
                    var itemMod = definition.GetComponent<ItemModContainer>();
                    itemMod.capacity = mod.GetInt("capacity", 6);
                    itemMod.openInDeployed = mod.GetBoolean("openInDeployed", true);
                    itemMod.openInInventory = mod.GetBoolean("openInInventory", true);
                    itemMod.defaultContents.Clear();
                    var defaultContents = mod.GetArray("defaultContents");
                    var manager = SingletonComponent<ItemManager>.Instance;
                    foreach (var content in defaultContents)
                    {
                        var itemid = content.Obj.GetInt("itemid", 0);
                        var def = manager.itemList.Find(x => x.itemid == itemid);
                        itemMod.defaultContents.Add(new ItemAmount(def, content.Obj.GetFloat("amount", 0)));
                    }
                }
                else if (typeName.Equals("ItemModBurnable"))
                {
                    var itemMod = definition.GetComponent<ItemModBurnable>();
                    itemMod.fuelAmount = mod.GetFloat("fuelAmount", 10f);
                    itemMod.byproductAmount = mod.GetInt("byproductAmount", 1);
                    itemMod.byproductChance = mod.GetFloat("byproductChance", 0.5f);
                    var manager = SingletonComponent<ItemManager>.Instance;
                    var itemid = mod.GetObject("byproductItem").GetInt("itemid", 0);
                    itemMod.byproductItem = manager.itemList.Find(x => x.itemid == itemid);
                }
                else if (typeName.Equals("ItemModCookable"))
                {
                    var itemMods = definition.GetComponents<ItemModCookable>();
                    var itemMod = itemMods[cook++];
                    itemMod.cookTime = mod.GetFloat("cookTime", 30f);
                    itemMod.amountOfBecome = mod.GetInt("amountOfBecome", 1);
                    itemMod.lowTemp = mod.GetInt("lowTemp", 0);
                    itemMod.highTemp = mod.GetInt("highTemp", 0);
                    itemMod.setCookingFlag = mod.GetBoolean("setCookingFlag", false);
                    var become = mod.GetObject("becomeOnCooked");
                    if (become == null)
                    {
                        itemMod.becomeOnCooked = null;
                        continue;
                    }
                    var manager = SingletonComponent<ItemManager>.Instance;
                    var itemid = become.GetInt("itemid", 0);
                    itemMod.becomeOnCooked = manager.itemList.Find(x => x.itemid == itemid);
                }
                else if (typeName.Equals("ItemModSwap"))
                {
                    var itemMod = definition.GetComponent<ItemModSwap>();
                    itemMod.sendPlayerDropNotification = mod.GetBoolean("sendPlayerDropNotification", false);
                    itemMod.sendPlayerPickupNotification = mod.GetBoolean("sendPlayerPickupNotification", false);
                    var items = new List<ItemAmount>();
                    var becomeItems = mod.GetArray("becomeItem");
                    var manager = SingletonComponent<ItemManager>.Instance;
                    foreach (var content in becomeItems)
                    {
                        var itemid = content.Obj.GetInt("itemid", 0);
                        var def = manager.itemList.Find(x => x.itemid == itemid);
                        items.Add(new ItemAmount(def, content.Obj.GetFloat("amount", 0)));
                    }
                    itemMod.becomeItem = items.ToArray();
                }
                else if (typeName.Equals("ItemModProjectile"))
                {
                    var itemMod = definition.GetComponent<ItemModProjectile>();
                    var projectile = itemMod.projectileObject.targetObject.GetComponent<Projectile>();
                    projectile.drag = mod.GetFloat("drag", 0);
                    projectile.thickness = mod.GetFloat("thickness", 0);
                    projectile.remainInWorld = mod.GetBoolean("remainInWorld", false);
                    projectile.stickProbability = mod.GetFloat("stickProbability", 0);
                    projectile.breakProbability = mod.GetFloat("breakProbability", 0);
                    projectile.ricochetChance = mod.GetFloat("ricochetChance", 0);
                    projectile.fullDamageVelocity = mod.GetFloat("fullDamageVelocity", 200);
                    projectile.damageTypes.Clear();
                    var damageTypes = mod.GetArray("damageTypes");
                    foreach (var damageType in damageTypes)
                    {
                        projectile.damageTypes.Add(new DamageTypeEntry
                        {
                            amount = damageType.Obj.GetFloat("amount", 0),
                            type = (DamageType) Enum.Parse(typeof (DamageType), damageType.Obj.GetString("type", ""))
                        });
                    }
                }
                else if (typeName.EndsWith("TimedExplosive"))
                {
                    TimedExplosive timedExplosive;
                    if (typeName.StartsWith("ItemModProjectile"))
                    {
                        var itemMod = definition.GetComponent<ItemModProjectile>();
                        timedExplosive = itemMod.projectileObject.targetObject.GetComponent<TimedExplosive>();
                    }
                    else if (typeName.StartsWith("ItemModEntity"))
                    {
                        var itemMod = definition.GetComponent<ItemModEntity>();
                        timedExplosive = itemMod.entityPrefab.targetObject.GetComponent<ThrownWeapon>().prefabToThrow.GetComponent<TimedExplosive>();
                        
                    }
                    else
                        continue;
                    timedExplosive.canStick = mod.GetBoolean("canStick", false);
                    timedExplosive.explosionRadius = mod.GetFloat("explosionRadius", 10);
                    timedExplosive.timerAmountMax = mod.GetFloat("timerAmountMax", 20);
                    timedExplosive.timerAmountMin = mod.GetFloat("timerAmountMin", 10);
                    timedExplosive.damageTypes.Clear();
                    var damageTypes = mod.GetArray("damageTypes");
                    foreach (var damageType in damageTypes)
                    {
                        timedExplosive.damageTypes.Add(new DamageTypeEntry
                        {
                            amount = damageType.Obj.GetFloat("amount", 0),
                            type = (DamageType)Enum.Parse(typeof(DamageType), damageType.Obj.GetString("type", ""))
                        });
                    }
                }
                else if (typeName.Equals("ItemModProjectileServerProjectile"))
                {
                    var itemMod = definition.GetComponent<ItemModProjectile>();
                    var projectile = itemMod.projectileObject.targetObject.GetComponent<ServerProjectile>();
                    projectile.drag = mod.GetFloat("drag", 0);
                    projectile.gravityModifier = mod.GetFloat("gravityModifier", 0);
                    projectile.speed = mod.GetFloat("speed", 0);
                }
            }
        }

        private JSONValue ObjectToJsonObject(object obj)
        {
            if (obj == null)
            {
                return new JSONValue(JSONValueType.Null);
            }
            if (obj is string)
            {
                return new JSONValue((string) obj);
            }
            if (obj is double)
            {
                return new JSONValue((double) obj);
            }
            if (obj is int)
            {
                return new JSONValue((int)obj);
            }
            if (obj is bool)
            {
                return new JSONValue((bool)obj);
            }
            var dict = obj as Dictionary<string, object>;
            if (dict != null)
            {
                var newObj = new JSONObject();
                foreach (var prop in dict)
                {
                    newObj.Add(prop.Key, ObjectToJsonObject(prop.Value));
                }
                return newObj;
            }
            var list = obj as List<object>;
            if (list != null)
            {
                var arr = new JSONArray();
                foreach (var o in list)
                {
                    arr.Add(ObjectToJsonObject(o));
                }
                return arr;
            }
            LocalPuts("Unknown: " + obj.GetType().FullName + " Value: " + obj);
            return new JSONValue(JSONValueType.Null);
        }

        private object JsonObjectToObject(JSONValue obj)
        {
            switch (obj.Type)
            {
                case JSONValueType.String:
                    return obj.Str;
                case JSONValueType.Number:
                    return obj.Number;
                case JSONValueType.Boolean:
                    return obj.Boolean;
                case JSONValueType.Null:
                    return null;
                case JSONValueType.Array:
                    return obj.Array.Select(v => JsonObjectToObject(v.Obj)).ToList();
                case JSONValueType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in obj.Obj)
                    {
                        dict[prop.Key] = JsonObjectToObject(prop.Value);                        
                    }
                    return dict;
                default:
                    LocalPuts("Missing type: " + obj.Type);
                    break;
            }
            return null;
        }

        private void LocalPuts(string msg)
        {
            Puts("{0}: {1}", Title, msg);
        }

        [ConsoleCommand("item.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateItems();
            UpdateBlueprints();
            LocalPuts("Item config reloaded.");
        }

        [ConsoleCommand("item.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (!CreateDefaultConfig())
                return;
            UpdateItems();
            UpdateBlueprints();
        }

        class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive || property.PropertyType == typeof(List<ItemAmount>) ||
                             property.PropertyType == typeof(ItemAmount[]) ||
                             property.PropertyType == typeof(List<DamageTypeEntry>) ||
                             property.PropertyType == typeof(DamageType) ||
                             property.PropertyType == typeof(List<ItemModConsumable.ConsumableEffect>) ||
                             property.PropertyType == typeof(MetabolismAttribute.Type) ||
                             property.PropertyType == typeof(Rarity) ||
                             property.PropertyType == typeof(Translate.Phrase) ||
                             property.PropertyType == typeof(ItemCategory) ||
                             property.PropertyType == typeof(ItemDefinition) ||
                             property.PropertyType == typeof(ItemDefinition.Condition) ||
                             property.PropertyType == typeof(String);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => p.DeclaringType == type && IsAllowed(p)).ToList();
            }
        }
    }
}
