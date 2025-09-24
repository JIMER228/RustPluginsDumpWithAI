using UnityEngine;
using Oxide.Core;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{

    [Info("MagiсTree", "OxideBro", "0.0.3")]
    public class MagicTree : RustPlugin
    {
        #region Class

        public class Seed
        {
            public string shortname;
            public string name;
            public ulong skinId;
        }

        Seed seed = new Seed()
        {
            shortname = "seed.hemp",
            name = "Семена Необычного дерева",
            skinId = 1787823357
        };

        public class Wood
        {
            [JsonProperty("UID Дерева")]
            public uint woodId;
            [JsonProperty("Осталось времени")]
            public int NeedTime;
            [JsonProperty("Этап")]
            public int CurrentEtap;
            [JsonProperty("Позиция")]
            public Vector3 woodPos;
            [JsonProperty("Список боксов")]
            public Dictionary<uint, BoxItemsList> BoxListed = new Dictionary<uint, BoxItemsList>();
        }

        public class BoxItemsList
        {
            [JsonProperty("Shortname предмета")]
            public string ShortName;
            [JsonProperty("Минимальное количество")]
            public int MinAmount;
            [JsonProperty("Максимальное количество")]
            public int MaxAmount;
            [JsonProperty("Шанс что предмет будет добавлен (максимально 100%)")]
            public int Change;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID;
            [JsonProperty("Имя предмета при создании (Оставте поле постым чтобы использовать стандартное название итема)")]
            public string Name;
            [JsonProperty("Это чертеж")]
            public bool IsBlueprnt;
        }


        public Dictionary<ulong, Dictionary<uint, Wood>> WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>();

        public Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"CmdError", "Неправильно ввели команду." },
            {"CountError", "Неверное кол-во!" },
            {"Permission", "У вас нет прав!" },
            {"SeedGived", "Вам выпала семечка волшебного дерева!\nПосадите ее и у вас выростет необычное дерево на каком растут ящики с ценными предметами!" },
            {"Wood", "Вы посадили волшебное дерево\nСкоро оно вырастет, и даст плоды!" },
            {"Matured",  "Ваше волшебное дерево созрело!\nСкоро на нем начнут рости ящики"},
            {"Box", "На дереве еще нет ящиков,\nВы не можете его срубить!" },
            {"Seed", "Ваш лайтинг саженец сломали!"},
            {"Destroy", "Это волшебное дерево\nЕго можно только добыть когда оно созреет!" }
        };

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
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

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Время роста дерева в секундах")]
            public int Time;

            [JsonProperty("Количество вещей в ящике")]
            public int ItemsCount;

            [JsonProperty("Кол-во ящиков на дереве")]
            public int BoxCount;

            [JsonProperty("Время появления ящиков в секундах")]
            public int BoxTime;


            [JsonProperty("Список префабов этапов дерева")]
            public List<string> etaps;

            [JsonProperty("Права на выдачу")]
            public string Permission = "seed.perm";

            [JsonProperty("Тип ящика")]
            public string CrateBasic = "assets/bundled/prefabs/radtown/crate_basic.prefab";

            [JsonProperty("Шанс выпдаения с дерева (макс-100)")]
            public int Chance;

            [JsonProperty("Настройка лута в ящиках")]
            public List<BoxItemsList> casesItems;

            [JsonProperty("Ссылка на удачный эффект")]
            public string SucEffect;
            [JsonProperty("Ссылка на эффект ошибки")]
            public string ErrorEffect;

            [JsonProperty("Размер шрифта")]
            public int FontSize;


            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    BoxTime = 10,
                    ItemsCount = 2,
                    Permission = "MagicTree.perm",
                    CrateBasic = "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
                    BoxCount = 4,
                    Chance = 5,
                    Time = 10,
                    casesItems = new List<BoxItemsList>()
                {
                new BoxItemsList
                {
                ShortName = "stones",
                MinAmount = 300,
                MaxAmount = 1000,
                Change = 100,
                Name = "",
                SkinID = 0,
                IsBlueprnt = false
                },
                },
                    SucEffect = "assets/prefabs/misc/xmas/candy cane club/effects/hit.prefab",
                    ErrorEffect = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                    FontSize = 30,
                    etaps = new List<string>()
                {
                  "assets/bundled/prefabs/autospawn/resource/v2_temp_forest/birch_tiny_temp.prefab",
                  "assets/bundled/prefabs/autospawn/resource/v2_temp_forest_small/douglas_fir_d.prefab",
                  "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest/oak_b_tundra.prefab"
                },
                };
            }
        }


        #endregion

        #region DefaultMethods

        #endregion

        #region OxideHooks

        void LoadData()
        {
            try
            {
                WoodsList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<uint, Wood>>>($"{Title}_Players");
                if (WoodsList == null)
                    WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>();
            }
            catch
            {
                WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>();
            }
        }

        void SaveData()
        {
            if (WoodsList != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Title}_Players", WoodsList);
        }

        public static MagicTree ins;

        void OnEntityKill(BaseNetworkable entity)
        {
            try
            {
                if (entity == null || entity?.net.ID == null) return;
                if (entity.GetComponent<TreeEntity>() != null && entity.GetComponent<TreeConponent>() != null)
                {
                    var tree = entity.GetComponent<TreeEntity>();
                    if (WoodsList.ContainsKey(tree.OwnerID) && WoodsList[tree.OwnerID].ContainsKey(tree.net.ID))
                        WoodsList[tree.OwnerID].Remove(tree.net.ID);
                }
            }
            catch (NullReferenceException)
            {
            }
           
        }


        private void OnServerInitialized()
        {
            ins = this;
            permission.RegisterPermission(config.Permission, this);
            LoadData();
            var treeList = GameObject.FindObjectsOfType<TreeEntity>();
            if (treeList != null)
                treeList.ToList().ForEach(tree =>
                {
                    if (tree.OwnerID != 0 && tree.GetComponent<TreeConponent>() == null)
                    {
                        AddOrRemoveComponent("add", tree.OwnerID, tree.net.ID);
                    }
                });
        }

        void AddOrRemoveComponent(string type, ulong playedID, uint treeID)
        {
            if (!WoodsList.ContainsKey(playedID) || !WoodsList[playedID].ContainsKey(treeID)) return;
            switch (type)
            {
                case "add":
                    var Tree = BaseEntity.serverEntities.Find(treeID);
                    var data = WoodsList[playedID][treeID];
                    if (Tree != null && data != null)
                    {
                        if (WoodsList[playedID][treeID].CurrentEtap > 2 && WoodsList[playedID][treeID].BoxListed.Count > 0)
                        {
                            data.BoxListed.Clear();
                            data.CurrentEtap = 2;
                            if (Tree.GetComponent<TreeConponent>() == null)
                            {
                                Tree.gameObject.GetOrAddComponent<TreeConponent>().Init(WoodsList[playedID][treeID]);
                            }
                            return;
                        }
                        else
                            if (Tree.GetComponent<TreeConponent>() == null)
                        {
                            Tree.gameObject.GetOrAddComponent<TreeConponent>().Init(WoodsList[playedID][treeID]);
                        }
                    }
                    break;
                case "remove":
                    if (WoodsList[playedID][treeID].CurrentEtap == 3 && WoodsList[playedID][treeID].BoxListed.Count > 0)
                    {
                        foreach (var ent in WoodsList[playedID][treeID].BoxListed)
                        {
                            var entity = BaseEntity.serverEntities.Find(ent.Key);
                            if (entity != null && !entity.IsDestroyed)
                            {
                                entity.Kill();

                            }
                        }
                        var wood = BaseEntity.serverEntities.Find(treeID);
                        if (wood == null) return;

                        var component = wood.GetComponent<TreeConponent>();
                        if (component != null)
                            component.DestroyComponent();
                    }
                    else
                    {
                        var wood = BaseEntity.serverEntities.Find(treeID);
                        if (wood == null) return;
                        var component = wood.GetComponent<TreeConponent>();

                        if (component != null)
                            component.DestroyComponent();
                    }
                    break;
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            var entity = go?.GetComponent<BaseEntity>();
            Vector3 ePos = entity.transform.position;
            if (entity == null || ePos == null || player == null) return;
            if (entity.skinID == seed.skinId)
            {
                entity.Kill();
                SpawnWood(player.userID, entity.transform.position, null);
                SendReply(player, string.Format(Messages["Wood"]));
                return;
            }
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return null;
            switch (item.info.shortname)
            {
                case "wood":
                    NextTick(() => TreeEntity wood1 = dispenser.GetComponentInParent<TreeEntity>());
                    if (wood1 != null && wood1.GetComponent<TreeConponent>() != null)
                    {
                        item.amount = item.amount * 2;
                    }
                    break;
            }
            return null;
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || item == null) return null;

            BasePlayer player = entity?.ToPlayer();
            if (player == null) return null;

            switch (item.info.shortname)
            {
                case "wood":
                    if (UnityEngine.Random.Range(0f, 100f) < config.Chance)
                    {
                        var activeitem = player.GetActiveItem();
                        if (activeitem != null && !activeitem.info.shortname.Contains("chainsaw"))
                            AddSeed(player, 1);
                    }
                    TreeEntity wood1 = dispenser.GetComponentInParent<TreeEntity>();
                    if (wood1 != null && wood1.GetComponent<TreeConponent>() != null)
                    {
                        var component = wood1.GetComponent<TreeConponent>();
                        if (component.data.BoxListed.Count > 0)
                        {
                            var box = component.data.BoxListed.ToList().GetRandom();

                            var boxEntity = BaseEntity.serverEntities.Find(box.Key);
                            if (boxEntity != null && component.data.CurrentEtap == 3)
                            {
                                if (boxEntity.GetParentEntity() != null)
                                {
                                    Vector3 pos = boxEntity.GetParentEntity().transform.position + boxEntity.GetNetworkPosition();

                                    boxEntity.GetComponent<BaseEntity>().SetParent(null);
                                    boxEntity.transform.position = pos;
                                    boxEntity.SendNetworkUpdate();

                                }
                                FreeableLootContainer l = boxEntity.GetComponent<FreeableLootContainer>();
                                l.SetFlag(BaseEntity.Flags.Reserved8, false, true);
                                l.SendNetworkUpdate();
                                boxEntity.GetComponent<BaseEntity>().SetFlag(BaseEntity.Flags.Busy, false, true);
                                boxEntity.SendNetworkUpdate();
                                Rigidbody rb = boxEntity.GetComponent<Rigidbody>();
                                rb.useGravity = true;
                                rb.mass = 4f;
                                rb.isKinematic = false;
                                boxEntity.SendNetworkUpdate();
                                component.data.BoxListed.Remove(box.Key);
                                if (component.data.BoxListed.Count <= 0)
                                    component.data.CurrentEtap = 4;
                            }
                        }
                        else
                        {
                            if (component.data.CurrentEtap == 4)
                            {
                                dispenser.AssignFinishBonus(player, 500);
                                HitInfo hitInfo = new global::HitInfo(player, wood1, Rust.DamageType.Generic, 9999999999999, wood1.transform.position);
                                hitInfo.gatherScale = 0f;
                                wood1.GetComponent<BaseEntity>().OnAttacked(hitInfo);
                            }

                        }
                    }
                    break;
            }
            return null;
        }

        void Unload()
        {
            var AllTree = GameObject.FindObjectsOfType<TreeEntity>();
            if (AllTree != null)
                AllTree.ToList().ForEach(tree =>
                {
                    if (tree.GetComponent<TreeConponent>() != null)
                        AddOrRemoveComponent("remove", tree.OwnerID, tree.net.ID);
                });
            SaveData();


        }

        #endregion

        #region MyMethods

        #region Wood


        public void SpawnWood(ulong player, Vector3 pos, TreeEntity tree)
        {
            if (tree == null)
            {
                TreeEntity Wood = GameManager.server.CreateEntity(config.etaps[0], pos) as TreeEntity;
                Wood.Spawn();
                Wood.OwnerID = player;
                Wood.SendNetworkUpdate();
                if (!WoodsList.ContainsKey(player))

                    WoodsList.Add(player, new Dictionary<uint, Wood>()
                    {
                        [Wood.net.ID] = new Wood() { woodId = Wood.net.ID, CurrentEtap = 0, NeedTime = config.Time / 3, woodPos = Wood.transform.position }

                    });
                else
                    WoodsList[player].Add(Wood.net.ID, new Wood() { woodId = Wood.net.ID, CurrentEtap = 0, NeedTime = config.Time / 3, woodPos = Wood.transform.position });
                Wood.GetOrAddComponent<TreeConponent>().Init(WoodsList[player][Wood.net.ID]);
            }

            else
            {
                if (tree == null) return;
                var old = WoodsList[player][tree.net.ID];
                var current = ++old.CurrentEtap;
                TreeEntity Wood = GameManager.server.CreateEntity(config.etaps[current], pos) as TreeEntity;
                WoodsList[player].Remove(tree.net.ID);
                tree.Kill();
                Wood.Spawn();
                Wood.GetComponent<TreeEntity>().OwnerID = player;
                Wood.SendNetworkUpdate();
                WoodsList[player].Add(Wood.net.ID, new Wood() { woodId = Wood.net.ID, CurrentEtap = current, NeedTime = config.Time / 3, woodPos = Wood.transform.position });
                Wood.GetOrAddComponent<TreeConponent>().Init(WoodsList[player][Wood.net.ID]);
            }
        }


        #endregion

        #region Seed

        [ChatCommand("seed")]
        void GiveSeed(BasePlayer player, string command, string[] args)
        {

            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, config.Permission))
            {
                if (args.Length == 1)
                {
                    int amount;
                    if (!int.TryParse(args[0], out amount))
                    {
                        SendReply(player, "Вы не указали количество, используйте /seed AMOUNT");

                        return;
                    }
                    AddSeed(player, amount);
                    return;
                }
                if (args.Length > 0 && args.Length == 2)
                {
                    var target = BasePlayer.Find(args[0]);
                    if (target == null)
                    {
                        SendReply(player, "Данный игрок не найден, попробуйте уточнить имя или SteamID, используйте /seed TARGETNAME/ID AMOUNT");
                        return;
                    }

                    int amount;
                    if (!int.TryParse(args[1], out amount))
                    {
                        SendReply(player, "Вы не указали количество, используйте /seed TARGETNAME/ID AMOUNT");
                        return;
                    }

                    AddSeed(target, amount);

                }
              
            }
            else
            {
                SendReply(player, string.Format(Messages["Permission"]));
                Effect.server.Run(config.ErrorEffect, player, 0, Vector3.zero, Vector3.forward);
            }
        }

        void AddSeed(BasePlayer player, int amount)
        {
            if (player == null) return;
            Item sd = ItemManager.CreateByName(seed.shortname, amount, seed.skinId);
            sd.name = seed.name;
            player.GiveItem(sd, BaseEntity.GiveItemReason.Crafted);
            SendReply(player, string.Format(Messages["SeedGived"]));
            Effect.server.Run(config.SucEffect, player, 0, Vector3.zero, Vector3.forward);
        }

        #endregion

        #region Boxs

        public void SpawnBox(Wood wood, int i, TreeEntity tree, ulong ownerID)
        {
            if (wood == null) return;
            if (wood != null)
            {
                for (int count = 0; count < config.BoxCount; count++)
                {
                    Vector3 pos = new Vector3();
                    pos.y += UnityEngine.Random.Range(5.5f, 6.0f);//высота
                    pos.x += UnityEngine.Random.Range(-2.0f, 2.0f);//
                    pos.z += UnityEngine.Random.Range(-2.0f, 2.0f);//
                    BaseEntity box = GameManager.server.CreateEntity(config.CrateBasic, pos);
                    box.Spawn();
                    box.SetParent(tree);
                    box.transform.localPosition = pos;
                    AddLoot(box);
                    box.SetFlag(BaseEntity.Flags.Reserved8, false, true);
                    box.SetFlag(BaseEntity.Flags.Busy, true, true);
                    box.SendNetworkUpdate();
                    wood.BoxListed.Add(box.net.ID, new BoxItemsList());
                }
                CreateInfo(ownerID);
            }
        }

        public void AddLoot(BaseEntity box)
        {
            if (box == null) return;
            int count = 0;
            LootContainer container = box.GetComponent<LootContainer>();
            if (container == null) return;
            container.inventory.itemList.Clear();
            
            foreach (var item in config.casesItems)
            {
                if (UnityEngine.Random.Range(0, 100) > item.Change) continue;
                if (count >= config.ItemsCount) break;
                var amount = UnityEngine.Random.Range(item.MinAmount, item.MaxAmount);

                var newItem = item.IsBlueprnt ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, amount, item.SkinID);
                if (newItem == null)
                {
                    PrintError($"Предмет {item.ShortName} не найден!");
                    return;
                }

                if (item.IsBlueprnt)
                {
                    var bpItemDef = ItemManager.FindItemDefinition(ItemManager.CreateByName(item.ShortName, amount, item.SkinID).info.itemid);
                    if (bpItemDef == null)
                    {
                        PrintError($"Предмет {item.ShortName} для создания чертежа не найден!");
                        return;
                    }

                    newItem.blueprintTarget = bpItemDef.itemid;
                }

                if (!string.IsNullOrEmpty(item.Name))
                    newItem.name = item.Name;


                if (container.inventory.IsFull())
                    container.inventory.capacity++;
                newItem.MoveToContainer(container.inventory, -1);
                count++;
            }
        }

        #endregion

        #region Other


        class TreeConponent : BaseEntity
        {
            public Dictionary<BasePlayer, bool> ColliderPlayersList = new Dictionary<BasePlayer, bool>();
            private TreeEntity tree;
            SphereCollider sphereCollider;

            public Wood data;

            void Awake()
            {
                tree = gameObject.GetComponent<TreeEntity>();
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Rust.Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 3f;
                InvokeRepeating(DrawInfo, 1f, 1);
            }

            public void Init(Wood wood)
            {
                data = wood;
            }

            private void OnTriggerEnter(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && !ColliderPlayersList.ContainsKey(target))
                    ColliderPlayersList.Add(target, !target.IsAdmin);
            }

            private void OnTriggerExit(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && ColliderPlayersList.ContainsKey(target))
                    ColliderPlayersList.Remove(target);
            }

            bool IsVisibled(BasePlayer player, Vector3 source, Vector3 dest) => player.IsVisible(source, dest);

            void DrawInfo()
            {
                if (data == null) return;
                foreach (var player in ColliderPlayersList)
                {
                    if (CanVisible(tree.transform.position + Vector3.up, player.Key))
                    {
                        if (data.NeedTime <= 0 && data.CurrentEtap == 2 && data.BoxListed.ToList().Count <= 0)
                        {
                            if (player.Value) SetPlayerFlag(player.Key, BasePlayer.PlayerFlags.IsAdmin, true);
                            player.Key.SendConsoleCommand("ddraw.text", 1.01f, Color.white, tree.transform.position + Vector3.up, $"<size=25><b>Волшебное дерево</b></size>\n<size=17>\nПЛОДЫ НА ПОДХОДЕ</size>");
                        }
                        if (data.CurrentEtap == 3 && data.BoxListed.ToList().Count > 0)
                        {
                            if (player.Value) SetPlayerFlag(player.Key, BasePlayer.PlayerFlags.IsAdmin, true);
                            player.Key.SendConsoleCommand("ddraw.text", 1.01f, Color.white, tree.transform.position + Vector3.up, $"<size=25><b>Волшебное дерево</b></size>\n<size=17>\nПЛОДЫ ДОЗРЕЛИ, ВЫ МОЖЕТЕ ИХ СОБРАТЬ</size>");
                        }

                        if (data.NeedTime > 0)
                        {
                            if (player.Value) SetPlayerFlag(player.Key, BasePlayer.PlayerFlags.IsAdmin, true);
                            player.Key.SendConsoleCommand("ddraw.text", 1.01f, Color.white, tree.transform.position + Vector3.up, $"<size=25><b>Волшебное дерево</b></size>\n<size=17>Этап созревания дерева: {data.CurrentEtap}/3\n\nВремя до полного созревания: {FormatShortTime(TimeSpan.FromSeconds(data.NeedTime))}</size>");
                        }
                    }
                    if (player.Value) SetPlayerFlag(player.Key, BasePlayer.PlayerFlags.IsAdmin, false);
                }

                if (data.NeedTime <= 0 && data.CurrentEtap == 2 && data.BoxListed.ToList().Count <= 0)
                {
                    ins.SpawnBox(data, 3, tree, tree.OwnerID);
                    data.CurrentEtap = 3;
                }
                if (data.NeedTime <= 0 && data.CurrentEtap < 2)
                {
                    ins.SpawnWood(tree.OwnerID, tree.transform.position, tree);
                }
                data.NeedTime--;
            }

            void SetPlayerFlag(BasePlayer player, BasePlayer.PlayerFlags f, bool b)
            {
                if (b)
                {
                    if (player.HasPlayerFlag(f)) return;
                    player.playerFlags |= f;
                }
                else
                {
                    if (!player.HasPlayerFlag(f)) return;
                    player.playerFlags &= ~f;
                }
                player.SendNetworkUpdateImmediate(false);
            }

            public bool ContainsAny(string value, params string[] args)
            {
                return args.Any(value.Contains);
            }

            bool CanVisible(Vector3 pos, BasePlayer player)
            {
                if (player == null) return false;
                RaycastHit[] hits = new RaycastHit[50];
                Vector3 pos1 = pos;
                if (player.eyes == null) return false;
                Vector3 pos2 = player.eyes.position;
                var length = Physics.RaycastNonAlloc(new Ray(pos1, (pos2 - pos1)), hits, 10f, LayerMask.GetMask("Construction", "World", "Default", "Deployed", "Terrain", "Player (Server)"), QueryTriggerInteraction.Collide);
                var objhits = new RaycastHit[length];
                for (int i = 0;
                i < length;
                i++) objhits[i] = hits[i];
                var results = objhits.OrderBy(h => h.distance).Select(p => p.GetEntity()).Where(p => p).ToList();
                results.RemoveAll(p => p.ShortPrefabName != "wall" && !ContainsAny(p.ShortPrefabName, "foundation", "door", "player", "floor", "wall.half", "wall.low"));
                if (results.Count > 0)
                {
                    var result = results[0];
                    if (result == player)
                        return true;
                }
                return false;
            }

            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;

                result += $"{time.Hours.ToString("00")}:";

                result += $"{time.Minutes.ToString("00")}:";

                result += $"{time.Seconds.ToString("00")}";

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units} {form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units} {form2}";

                return $"{units} {form3}";
            }

            public void DestroyComponent() => Destroy(this);
            void OnDestroy()
           => Destroy(this);
        }


        void CreateInfo(ulong playeId)
        {
            var player = BasePlayer.FindByID(playeId);

            if (player != null)
            {
                CuiHelper.DestroyUi(player, "SocialHelp");

                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.3447913 0.112037", AnchorMax = "0.640625 0.15", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 0.2" }
                }, "Hud", "SocialHelp");
                container.Add(new CuiLabel
                {
                    FadeOut = 2,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "ВАШЕ ДЕРЕВО СОЗРЕЛО, И ДАЛО ПЛОДЫ!", FontSize = 17, Align = TextAnchor.MiddleCenter, FadeIn = 2, Color = "1 1 1 0.8", Font = "robotocondensed-regular.ttf" }
                }, "SocialHelp");

                CuiHelper.AddUi(player, container);

                timer.Once(5f, () => { if (player != null) CuiHelper.DestroyUi(player, "SocialHelp"); });
            }
        }

        #endregion

        #endregion

    }
}