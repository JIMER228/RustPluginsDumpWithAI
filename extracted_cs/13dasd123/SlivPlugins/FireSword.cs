// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Facepunch;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("FireSword", "Colon Blow", "2.0.0")]
    public class FireSword : CovalencePlugin
    {

        //changed lang coding
        //changed permissions
        //changed config
        //changed to Covalence Plugin
        //fixed RPC for throwning weapon

        #region Load and Data

        private bool initComplete = false;
        private const string permGive = "firesword.give";
        private const string permCraft = "firesword.craft";
        private const string permUse = "firesword.use";
        private Dictionary<ulong, ToggleFireData> FireOn = new Dictionary<ulong, ToggleFireData>();

        public class ToggleFireData
        {
            public BasePlayer player;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permGive, this);
            permission.RegisterPermission(permCraft, this);
            permission.RegisterPermission(permUse, this);
            initComplete = true;
        }

        private void Loaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyFireOnData(player);
            }
        }

        #endregion

        #region Commands

        [Command("firesword")]
        private void cmdFireSword(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.HasPermission(permUse) || iplayer.HasPermission(permGive))
            {
                ToggleFireSword(iplayer);
            }
            else iplayer.Message(lang.GetMessage("notauthorizedtouse", this, iplayer.Id));
        }

        [Command("craftfiresword")]
        private void cmdCraftFireSword(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.HasPermission(permCraft) || iplayer.HasPermission(permGive))
            {
                CraftFireSword(iplayer);
            }
            else iplayer.Message(lang.GetMessage("notauthorizedtocraft", this, iplayer.Id));
        }

        [Command("givefiresword")]
        private void cmdGiveFireSword(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.HasPermission(permGive))
            {
                var player = iplayer.Object as BasePlayer;
                GiveFireSword(iplayer);
            }
            else iplayer.Message(lang.GetMessage("notauthorizedtogive", this, iplayer.Id));
        }

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            public FireSwordSettings fireswordSettings { get; set; }

            public class FireSwordSettings
            {
                [JsonProperty(PropertyName = "Craft - Sword Item - Item ID of Sword : (default Salvaged Sword) : ")] public int ReqBuildItemID { get; set; }
                [JsonProperty(PropertyName = "Craft - Require player be next to something to craft ? ")] public bool requireCraftBench { get; set; }
                [JsonProperty(PropertyName = "Craft - if Enabled, player must be near this prefab to craft FireSword (default Workbench 3) : ")] public string prefabCraftBench { get; set; }
                [JsonProperty(PropertyName = "Craft - Material 1 - Amount of crafting material 1 needed : ")] public int craft1Amount { get; set; }
                [JsonProperty(PropertyName = "Craft - Material 1 - Item ID of crafting material 1 : (default low grade) : ")] public int craft1ItemID { get; set; }
                [JsonProperty(PropertyName = "Craft - Material 2 - Amount of crafting material 2 needed : ")] public int craft2Amount { get; set; }
                [JsonProperty(PropertyName = "Craft - Material 2 - Item ID of crafting material 2 : (default gunpowder) : ")] public int craft2ItemID { get; set; }

                [JsonProperty(PropertyName = "Skin - Fire Weapon custom steam skin ID : (Set to 0 for default) : ")] public ulong CustomSkinID { get; set; }

                [JsonProperty(PropertyName = "Materials - Flame - Amount PER TICK needed for flames : ")] public int mat1Amount { get; set; }
                [JsonProperty(PropertyName = "Materials - Flame - Material ID needed to fuel flames (default low grade) : ")] public int mat1ItemID { get; set; }
                [JsonProperty(PropertyName = "Materials - Explosion - Amount needed for Explosion : ")] public int mat2Amount { get; set; }
                [JsonProperty(PropertyName = "Materials - Explosion - Material ID needed for Explosion on Weapon Throw (default gunpowder) : ")] public int mat2ItemID { get; set; }
                [JsonProperty(PropertyName = "Materials - Use Materials to make flames : ")] public bool useMats { get; set; }

                [JsonProperty(PropertyName = "Damage - Chance - Likelyhood of Fireball spawn on Melee Fire Weapon Strike : (percentage) : ")] public float FSChance { get; set; }
                [JsonProperty(PropertyName = "Damage - Explosion - amount to add when Throwing FireSword : (Mats needed) ")] public float FSExplosionDamage { get; set; }
                [JsonProperty(PropertyName = "Damage - Strike - amount to add when swinging FireSword : (Mats needed) ")] public float FSStrikeDamage { get; set; }
                [JsonProperty(PropertyName = "Damage - Radius - Strike/Throw damage radius : ")] public float DamageRadius { get; set; }

                [JsonProperty(PropertyName = "Damage - Reduction - Use Victims Protection Values when damaging : ")] public bool UseProt { get; set; }
                [JsonProperty(PropertyName = "Usage - Found/Looted Fire Weapons can be used by Anyone : (no perms needed) : ")] public bool LootAndUse { get; set; }
                [JsonProperty(PropertyName = "Durability - Deal random condition loss when Fire Weapon is Thrown : ")] public bool DamageConditionOnThrow { get; set; }

            }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                fireswordSettings = new PluginConfig.FireSwordSettings
                {
                    requireCraftBench = false,
                    prefabCraftBench = "assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab",
                    craft1Amount = 5,
                    craft1ItemID = -946369541,  //Default ID is low grade fuel
                    craft2Amount = 20,
                    craft2ItemID = -265876753, //Default ID is gunpowder

                    ReqBuildItemID = 1326180354,
                    CustomSkinID = 813766930,

                    mat1Amount = 5,
                    mat1ItemID = -946369541,  //Default ID is low grade fuel
                    mat2Amount = 20,
                    mat2ItemID = -265876753, //Default ID is gunpowder
                    useMats = true,

                    FSChance = 50f,
                    FSStrikeDamage = 35f,
                    FSExplosionDamage = 100f,
                    DamageRadius = 1f,

                    UseProt = true,
                    LootAndUse = true,
                    DamageConditionOnThrow = true,
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorizedtouse"] = "You are not authorized to use this weapon !!",
                ["notauthorizedtocraft"] = "You are not authorized to craft that weapon !!",
                ["notauthorizedtogive"] = "You are not authorized to give that weapon !!",
                ["notcorrectitem"] = "That in not the correct weapon to do that !!",
                ["fireweapondestroyed"] = "You have destroyed your Fire Weapon !!",
                ["noroom"] = "No room in your inventory for Fire Weapon !!",
                ["fireweaponnomats1"] = "You need Low Grade Fuel (" + config.fireswordSettings.mat1Amount.ToString() + ") to Toggle Fire Weapon !!",
                ["fireweaponnomats2"] = "You need Low Grade Fuel (" + config.fireswordSettings.mat1Amount.ToString() + ") and GunPowder (" + config.fireswordSettings.mat2Amount.ToString() + ") to have Fire Weapon Throw Explosion !!",
                ["fireweaponcreated"] = "You have created a Fire Weapon !!",
                ["crafted"] = "A FireSword was added to your inventory !!",
                ["needmats"] = "You need the required Materials to craft that!",
                ["needcraftbench"] = "You must be near specified crafting area !!",
                ["notauthorized"] = "You are NOT Authorized to do that !!",
            }, this);
        }

        #endregion

        #region Rust Hooks

        private void OnMeleeThrown(BasePlayer player, Item item)
        {
            if (item == null || player == null) return;
            if (!item.HasFlag(global::Item.Flag.OnFire)) { RemoveFireOn(player); return; }
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                var flameweapon = player.GetComponent<FlameWeapon>();
                if (flameweapon == null) return;
                AddFireOn(player);
                ThrowWeaponCondition(player, item);
            }
        }

        private void OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null) return;
            var flameweapon = player.GetComponent<FlameWeapon>() ?? null;
            if (flameweapon != null)
            {
                Vector3 pos = hitInfo.HitPositionWorld;
                if (ThrowWeaponHasFireOn(player))
                {
                    WeaponThrowFX(player, pos);
                    RemoveFireOn(player);
                    GameObject.Destroy(flameweapon);
                    return;
                }
                else if (UsingFireWeapon(player))
                {
                    WeaponStrikeFX(player, pos);
                    RemoveFireOn(player);
                    return;
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var flameweapon = player.GetComponent<FlameWeapon>();
            if (flameweapon == null) return;
            if (flameweapon != null)
            {
                GameObject.Destroy(flameweapon);
            }
            DestroyFireOnData(player);
            return;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            var flameweapon = player.GetComponent<FlameWeapon>();
            if (flameweapon == null) return;
            if (flameweapon != null)
            {
                GameObject.Destroy(flameweapon);
            }
            DestroyFireOnData(player);
            return;
        }

        private void Unload()
        {
            DestroyAll<FlameWeapon>();
        }

        #endregion

        #region Methods

        private void CraftFireSword(IPlayer iplayer)
        {
            var player = iplayer.Object as BasePlayer;
            if (config.fireswordSettings.requireCraftBench && !IsNearCraftBench(player))
            {
                iplayer.Message(lang.GetMessage("needcraftbench", this, iplayer.Id));
                return;
            }
            if (!HoldingCorrectWeapon(player))
            {
                iplayer.Message(lang.GetMessage("notcorrectitem", this, iplayer.Id));
                return;
            }
            Item playerweaponitem = player.GetActiveItem();
            if (playerweaponitem.skin == config.fireswordSettings.CustomSkinID)
            {
                iplayer.Message(lang.GetMessage("notcorrectitem", this, iplayer.Id));
                return;
            }
            if (player.inventory.GetAmount(config.fireswordSettings.craft1ItemID) >= config.fireswordSettings.craft1Amount)
            {
                if (player.inventory.GetAmount(config.fireswordSettings.craft2ItemID) >= config.fireswordSettings.craft2Amount)
                {
                    playerweaponitem.Remove(0f);
                    player.inventory.Take(null, config.fireswordSettings.craft1ItemID, config.fireswordSettings.craft1Amount);
                    player.Command("note.inv", config.fireswordSettings.craft1ItemID, -config.fireswordSettings.craft1Amount);
                    player.inventory.Take(null, config.fireswordSettings.craft2ItemID, config.fireswordSettings.craft2Amount);
                    player.Command("note.inv", config.fireswordSettings.craft2ItemID, -config.fireswordSettings.craft2Amount);
                    GiveFireSword(iplayer);
                    return;
                }
            }
            iplayer.Message(lang.GetMessage("needmats", this, iplayer.Id));
            ItemDefinition item0 = ItemManager.FindItemDefinition(config.fireswordSettings.ReqBuildItemID);
            iplayer.Message("You need 1 " + item0.shortname);
            ItemDefinition item1 = ItemManager.FindItemDefinition(config.fireswordSettings.craft1ItemID);
            iplayer.Message("You need " + config.fireswordSettings.craft1Amount + " " + item1.shortname);
            ItemDefinition item2 = ItemManager.FindItemDefinition(config.fireswordSettings.craft2ItemID);
            iplayer.Message("You need " + config.fireswordSettings.craft2Amount + " " + item2.shortname);
        }

        private bool IsNearCraftBench(BasePlayer player)
        {
            bool canCraft = false;
            List<BaseEntity> benchList = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, 2f, benchList);

            foreach (BaseEntity foundent in benchList)
            {
                if (foundent.name.ToString() == config.fireswordSettings.prefabCraftBench) return true;
            }
            return canCraft;
        }

        private void GiveFireSword(IPlayer iplayer)
        {
            var player = iplayer.Object as BasePlayer;
            Item newFireSword = ItemManager.CreateByItemID(1326180354, 1, config.fireswordSettings.CustomSkinID);
            newFireSword.name = "FireSword";
            newFireSword.text = "FireSword";
            newFireSword.SetFlag(global::Item.Flag.OnFire, true);
            newFireSword.MarkDirty();
            if (!player.inventory.GiveItem(newFireSword, null))
            {
                newFireSword.Drop(player.eyes.position, Vector3.forward, new Quaternion());
                return;
            }
            player.Command("note.inv", new object[] { -388967316, 1, "FireSword", 0 });
        }

        public void SpawnFireSword(ItemContainer itemContainer)
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll >= 75) return;
            Item sword = ItemManager.CreateByItemID(1326180354, 1, config.fireswordSettings.CustomSkinID);
            sword.MoveToContainer(itemContainer, -1, false);
            sword.SetFlag(global::Item.Flag.OnFire, true);
            sword.MarkDirty();
        }

        private void ToggleFireSword(IPlayer iplayer)
        {
            var player = iplayer.Object as BasePlayer;
            if (!HoldingCorrectWeapon(player))
            {
                iplayer.Message(lang.GetMessage("notcorrectitem", this, iplayer.Id));
                return;
            }

            var addFlame = player.GetComponent<FlameWeapon>();
            if (addFlame != null)
            {
                GameObject.Destroy(addFlame);
                RemoveFireOn(player);
                return;
            }
            else
            {
                addFlame = player.gameObject.AddComponent<FlameWeapon>();
                return;
            }
        }

        private void AddFireOn(BasePlayer player)
        {
            if (ThrowWeaponHasFireOn(player)) return;
            FireOn.Add(player.userID, new ToggleFireData { player = player, });
        }

        private void RemoveFireOn(BasePlayer player)
        {
            if (!ThrowWeaponHasFireOn(player)) return;
            FireOn.Remove(player.userID);
        }

        private bool ThrowWeaponHasFireOn(BasePlayer player)
        {
            if (FireOn.ContainsKey(player.userID)) return true;
            return false;
        }

        private bool HoldingCorrectWeapon(BasePlayer player)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem != null && activeItem.info.shortname == "salvaged.sword") return true;
            return false;
        }
        static bool UsingFireWeapon(BasePlayer player)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem != null && activeItem.HasFlag(global::Item.Flag.OnFire)) return true;
            return false;
        }

        private void WeaponThrowFX(BasePlayer player, Vector3 pos)
        {
            if (HasItem2Mats(player))
            {
                Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_01.prefab", pos);
                AddSwordDamage(player, pos, true);
                return;
            }
            else
            {
                player.IPlayer.Message(lang.GetMessage("fireweaponnomats2", this, player.IPlayer.Id));
                return;
            }
        }

        private void WeaponStrikeFX(BasePlayer player, Vector3 pos)
        {
            AddSwordDamage(player, pos);
            float chanceforstrike = UnityEngine.Random.Range(0f, 99f);
            if (chanceforstrike <= config.fireswordSettings.FSChance)
            {
                Quaternion rot = new Quaternion();
                string prefab = "assets/bundled/prefabs/fireball.prefab";
                BaseEntity fball = GameManager.server.CreateEntity(prefab, pos, rot, true);
                FireBall fireball = fball.GetComponent<FireBall>();
                fireball.damagePerSecond = 1f;
                fireball.radius = 1f;
                fireball.lifeTimeMin = 5f;
                fireball.lifeTimeMin = 5f;
                fireball.generation = 10f;
                fireball.Spawn();
                return;
            }
        }

        private void AddSwordDamage(BasePlayer player, Vector3 location, bool isThrow = false)
        {
            var damageamount = 0f;
            if (!isThrow) damageamount = config.fireswordSettings.FSStrikeDamage;
            if (isThrow) damageamount = config.fireswordSettings.FSExplosionDamage;

            List<BaseCombatEntity> entityList = Pool.GetList<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(location, config.fireswordSettings.DamageRadius, entityList);

            foreach (BaseCombatEntity combatentity in entityList)
            {
                if (combatentity is BasePlayer)
                {
                    var attacker = (BasePlayer)combatentity;
                    if (attacker.userID == player.userID) break;
                }
                if (!(combatentity is BuildingPrivlidge))
                {
                    combatentity.health = combatentity.health - damageamount;
                }
                if (!isThrow) break;
            }
            Pool.FreeList<BaseCombatEntity>(ref entityList);
        }

        private void ThrowWeaponCondition(BasePlayer player, Item item)
        {
            if (item == null) return;
            if (config.fireswordSettings.DamageConditionOnThrow)
            {
                float currentcond = item.condition;
                float randomcond = UnityEngine.Random.Range(10f, currentcond + 10f);
                item.condition = item.condition - randomcond;
                if (item.condition <= 0)
                {
                    player.IPlayer.Message(lang.GetMessage("fireweapondestroyed", this, player.IPlayer.Id));
                }
                return;
            }
            else
                return;
        }

        private bool HasItem1Mats(BasePlayer player)
        {
            int HasReq1 = player.inventory.GetAmount(config.fireswordSettings.mat1Amount);
            if (HasReq1 >= config.fireswordSettings.mat1Amount) return true;
            return false;
        }

        private bool HasItem2Mats(BasePlayer player)
        {
            int HasReq2 = player.inventory.GetAmount(config.fireswordSettings.mat2ItemID);
            if (HasReq2 >= config.fireswordSettings.mat2Amount)
            {
                player.inventory.Take(null, config.fireswordSettings.mat2ItemID, config.fireswordSettings.mat2Amount);
                player.Command("note.inv", config.fireswordSettings.mat2ItemID, -config.fireswordSettings.mat2Amount);
                return true;
            }
            return false;
        }

        private void DestroyFireOnData(BasePlayer player)
        {
            if (FireOn.ContainsKey(player.userID))
            {
                FireOn.Remove(player.userID);
            }
            else
                return;
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region Flame Weapon

        private class FlameWeapon : MonoBehaviour
        {
            private BasePlayer player;
            private BaseEntity flame;
            private BaseEntity playerweapon;
            private FireBall fireball;
            private Vector3 pos;
            private Quaternion rot;
            private string prefab;
            private int Req1ItemID = config.fireswordSettings.mat1ItemID;
            private int AmountReq1 = config.fireswordSettings.mat1Amount;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                playerweapon = player.GetHeldEntity();
                pos = new Vector3(-0.1f, -0.1f, -0.6f);
                rot = Quaternion.identity;
                prefab = "assets/bundled/prefabs/fireball_small.prefab";
            }

            private void SpawnFireEffects()
            {
                if (playerweapon == null) { OnDestroy(); return; }
                if (config.fireswordSettings.useMats)
                {
                    if (!HasItem1Mats(player))
                    {
                        GameObject.Destroy(this);
                        return;
                    }
                    else TakeItem1Mats(player);
                }

                flame = GameManager.server.CreateEntity(prefab, pos, rot, true);
                fireball = flame.GetComponent<FireBall>();
                fireball.generation = 0.1f;
                fireball.radius = 0.1f;
                fireball.tickRate = 0.1f;
                if (playerweapon != null) fireball.SetParent(playerweapon, 0);
                flame?.Spawn();
            }

            private bool HasItem1Mats(BasePlayer player)
            {
                int HasReq1 = player.inventory.GetAmount(Req1ItemID);
                if (HasReq1 >= AmountReq1) return true;
                return false;
            }


            private void TakeItem1Mats(BasePlayer player)
            {
                int HasReq1 = player.inventory.GetAmount(Req1ItemID);

                if (HasReq1 >= AmountReq1)
                {
                    player.inventory.Take(null, Req1ItemID, AmountReq1);
                    player.Command("note.inv", Req1ItemID, -AmountReq1);
                }
                else OnDestroy();
            }


            private void FixedUpdate()
            {
                if (playerweapon == null) { OnDestroy(); return; }
                if (!UsingFireWeapon(player))
                {
                    if (fireball == null) return;
                    fireball.Kill(BaseNetworkable.DestroyMode.None);
                    return;
                }
                if (fireball != null)
                {
                    fireball.transform.localPosition = new Vector3(-0.1f, -0.1f, -0.6f);
                    fireball.transform.hasChanged = true;
                    fireball.SendNetworkUpdateImmediate();
                    return;

                }
                if (fireball == null)
                {
                    SpawnFireEffects();
                }
            }

            public void OnDestroy()
            {
                if (fireball == null) return;
                fireball.Kill(BaseNetworkable.DestroyMode.None);
            }
        }

        #endregion

    }
}
