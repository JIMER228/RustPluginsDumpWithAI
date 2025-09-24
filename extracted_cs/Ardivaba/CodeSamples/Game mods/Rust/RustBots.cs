using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using Rust;
using Oxide.Core;
using System.Collections;
using System.Globalization;
using System.IO;
using Oxide.Core.Plugins;
using ConVar;

/*
    TODO:
        Write some kind of reflection static class to make changing private stuff more clean

*/

namespace Oxide.Plugins
{
    #region oxide plugin
    [Info("AimTrain", "Ardivaba", 0.1)]
    [Description("Git gud.")]
    public class AimBots : RustPlugin
    {
        #region properties
        public static AimBots Instance;
        #endregion
        #region main methods
        public void Chat(string chat)
        {
            PrintToChat(chat);
        }

        void UnlimitedAmmo(BaseProjectile projectile, BasePlayer player)
        {
            projectile.GetItem().condition = projectile.GetItem().info.condition.max;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }
        #endregion

        #region oxide hooks
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            UnlimitedAmmo(projectile, player);
        }

        void OnTick()
        {
            BaseBot[] bots = GameObject.FindObjectsOfType<BaseBot>();
            if(bots.Length < 5)
            {
                PlayerCorpse[] corpses = GameObject.FindObjectsOfType<PlayerCorpse>();

                //TODO: Filter out non-bot corpses
                foreach (PlayerCorpse corpse in corpses)
                {
                    //corpse.Kill(BaseNetworkable.DestroyMode.Gib);
                }

                BotFactory.SpawnSavedBots();
            }
        }

        void Loaded()
        {
            AimBots.Instance = this;
            TitleResolver.InitializeTitleResolver();
            BotFactory.ReadBotsData();

            BotFactory.AddComposition("Naked", "Static", "Idle", "Naked", "Not Updated");
            BotFactory.AddComposition("Naked Strafer", "Strafing", "Idle", "Naked", "Slowly Updated");
        }
        #endregion

        #region console commands
        [ConsoleCommand("bots.data.clear")]
        void CmdClearData(ConsoleSystem.Arg arg)
        {
            BotFactory.ClearData();
        }

        [ConsoleCommand("bots.create")]
        void CmdCreateSavedBot(ConsoleSystem.Arg arg)
        {
            Assert.Test(arg.Player() != null, "AimBots.CmdCreateSavedBot: arg.Player() is null!");
            BotFactory.CreateBot(arg.Player().transform.position, "Naked");
        }

        [ConsoleCommand("bots.spawn.saved")]
        void CmdCreateBotsFromData(ConsoleSystem.Arg arg)
        {
            BotFactory.SpawnSavedBots();
        }

        [ConsoleCommand("bots.clearCorpses")]
        void CmdCreateBot(ConsoleSystem.Arg arg)
        {
            PlayerCorpse[] corpses = GameObject.FindObjectsOfType<PlayerCorpse>();

            //TODO: Filter out non-bot corpses
            foreach(PlayerCorpse corpse in corpses)
            {
                corpse.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }

        [ConsoleCommand("bots.titleResolver.debug")]
        void CmdDebugTitleResolver(ConsoleSystem.Arg arg)
        {
            TitleResolver.TitleToType("derp");
        }
        #endregion
    }
    #endregion

    #region bot factory
    public static class BotFactory
    {
        public static Dictionary<string, string[]> BotCompositions;

        public static void AddComposition(string name, params string[] classes)
        {
            if (BotCompositions.ContainsKey(name))
                return;

            BotCompositions.Add(name, classes);
            WriteBotsData();
        }

        public static string[] GetComposition(string name)
        {
            Assert.Test(BotCompositions.ContainsKey(name), "BotFactory.GetCompositions: Bot composition with name: " + name + " doesn't exist!");
            return BotCompositions[name];
        }

        public static List<BotData> BotDatas;

        public static void ReadBotsData()
        {
            BotCompositions = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string[]>>("BotCompositions");
            BotDatas = Interface.Oxide.DataFileSystem.ReadObject<List<BotData>>("Bots");
        }

        public static void WriteBotsData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BotCompositions", BotCompositions);
            Interface.Oxide.DataFileSystem.WriteObject("Bots", BotDatas);
        }

        public static void ClearData()
        {
            BotCompositions.Clear();
            BotDatas.Clear();
            Interface.Oxide.DataFileSystem.WriteObject("Bots", BotDatas);
            Interface.Oxide.DataFileSystem.WriteObject("BotCompositions", BotCompositions);
        }

        public static void CreateBot(Vector3 position, string compositionName)
        {
            Assert.Test(position != null, "BotFactory.CreateBot: position is null!");

            BotData data = new BotData();
            data.PosX = position.x;
            data.PosY = position.y;
            data.PosZ = position.z;

            Assert.Test(position != null, "BotFactory.CreateBot: BotDatas is null!");
            data.ID = (ulong)(BotDatas.Count + 1);
            data.CompositionTitle = "Naked Strafer";
            BotDatas.Add(data);

            WriteBotsData();

            SpawnBot(position, data);
        }

        public static BasePlayer SpawnBot(Vector3 position, BotData data)
        {
            var newPlayer = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", position, Quaternion.identity);

            //Set flag Reserved1 so that we can identify BasePlayers that are bots.
            newPlayer.SetFlag(BaseEntity.Flags.Reserved1, true);

            var bot = newPlayer.gameObject.AddComponent<BaseBot>();
            bot.Data = data;

            string name = "";
            foreach(string classTitle in BotFactory.GetComposition(data.CompositionTitle))
            {
                var obj = newPlayer.gameObject.AddComponent(TitleResolver.TitleToType(classTitle));
                name += (string)obj.GetType().GetField("Title").GetValue(obj) + " ";
            }

            //var movement = (BaseBotMovement)newPlayer.gameObject.AddComponent(TitleResolver.TitleToType("Static"));
            //var animation = (BaseBotAnimation)newPlayer.gameObject.AddComponent(TitleResolver.TitleToType("Idle"));
            //var equipment = (BaseBotEquipment)newPlayer.gameObject.AddComponent(TitleResolver.TitleToType("Naked"));
            //var networking = (BaseBotNetworking)newPlayer.gameObject.AddComponent(TitleResolver.TitleToType("Updated"));

            SetName(newPlayer.GetComponent<BasePlayer>(), name);

            newPlayer.Spawn(true);

            newPlayer.SendNetworkUpdate();

            BasePlayer basePlayer = (BasePlayer)newPlayer;
            basePlayer.inventory.GiveItem(ItemManager.CreateByItemID(340009023), basePlayer.inventory.containerWear);
            basePlayer.inventory.GiveItem(ItemManager.CreateByItemID(1554697726), basePlayer.inventory.containerWear);
            basePlayer.inventory.GiveItem(ItemManager.CreateByItemID(-1883959124), basePlayer.inventory.containerWear);

            return newPlayer.gameObject.GetComponent<BasePlayer>();
        }

        public static void SpawnSavedBots()
        {
            ReadBotsData();

            List<ulong> existingIDs = new List<ulong>();

            BaseBot[] bots = GameObject.FindObjectsOfType<BaseBot>();
            foreach(BaseBot bot in bots)
            {
                if(bot.BasePlayer.IsAlive() && !bot.BasePlayer.IsWounded())
                    existingIDs.Add(bot.Data.ID);

                if (bot.BasePlayer.IsWounded())
                    bot.BasePlayer.Kill();
            }

            foreach(BotData data in BotDatas)
            {
                if (!existingIDs.Contains(data.ID))
                {
                    SpawnBot(new Vector3(data.PosX, data.PosY, data.PosZ), data);
                }
            }
        }

        private static void SetName(BasePlayer player, string name)
        {
            player.PrivateField("_displayName").Value = name;
        }
    }
    #endregion

    #region bot data
    public class BotData
    {
        public ulong ID;
        public float PosX;
        public float PosY;
        public float PosZ;
        public string CompositionTitle;
    }
    #endregion

    #region base bot behaviour
    /*
        TODO: Add privates so that we have to GetComponent only once
    */
    public class BaseBotBehaviour : BaseMonoBehaviour
    {
        public bool IsDirty = false;
        public BaseBotEquipment Equipment { get { return GetComponent<BaseBotEquipment>(); } }
        public BaseBotMovement Movement { get { return GetComponent<BaseBotMovement>(); } }
        public BaseBot Bot { get { return GetComponent<BaseBot>(); } }
        public BasePlayer BasePlayer { get { return GetComponent<BasePlayer>(); } }
    }
    #endregion

    #region bot classes
    public class BaseBot : BaseBotBehaviour
    {
        public BotData Data;
        void Start()
        {
            //BasePlayer.OwnerID = Data.ID;
            Equipment.Give();
        }

        void FixedUpdate()
        {
            Movement.Move();
        }
    }
    #endregion

    #region bot equipment classes
    public class BaseBotEquipment : BaseBotBehaviour
    {
        public string Title = "Naked";
        public void Give()
        {

        }
    }
    #endregion

    #region bot movement classes
    public class BaseBotMovement : BaseBotBehaviour
    {
        public string Title = "Static";

        void Start()
        {
        }

        public virtual void Move()
        {
        }

        public Vector3 FixHeight(Vector3 position)
        {
            position.y = TerrainMeta.HeightMap.GetHeight(position);

            return position;
        }
    }

    public class StrafingBotMovement : BaseBotMovement
    {
        public new string Title = "Strafing";

        Vector3 targetPosition;

        void Start()
        {
            targetPosition = transform.position;
        }

        public void FixedUpdate()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2.5f * UnityEngine.Time.fixedDeltaTime);

            if(Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                //targetPosition = transform.position + transform.right * UnityEngine.Random.Range(-5.0f, 5.0f);
            }

            IsDirty = true;

            transform.position = FixHeight(transform.position);
        }
    }
    #endregion

    #region bot networking classes
    public class BaseBotNetworking : BaseBotBehaviour
    {
        public string Title = "Not Updated";

        public virtual void FixedUpdate()
        {
        }
    }

    public class UpdatedBotNetworking : BaseBotNetworking
    {
        public new string Title = "Updated";
        public override void FixedUpdate()
        {
            if (IsDirty)
            {
                BasePlayer.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                IsDirty = false;
            }
        }
    }

    public class SlowlyUpdatedBotNetworking : BaseBotNetworking
    {
        public new string Title = "Slowly Updated";

        float TimeLeft = 0.0f;
        public override void FixedUpdate()
        {
            if (TimeLeft <= 0.0f)
            {
                BasePlayer.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                IsDirty = false;
                TimeLeft = 0.5f;
            }

            TimeLeft -= UnityEngine.Time.fixedDeltaTime;
        }
    }
    #endregion

    #region bot animation classes
    public class BaseBotAnimation : BaseBotBehaviour
    {
        public string Title = "Idle";

        ModelState modelState;
        void Start()
        {
            modelState = GetModelState();
            modelState.sprinting = true;

            SetDefaultAnimationState();
        }

        ModelState GetModelState()
        {
            return (ModelState)BasePlayer.PrivateField("modelState").Value;
        }

        void SetDefaultAnimationState()
        {
            modelState.onground = true;
        }
    }
    #endregion

    #region helper classes
    #region reflection helpers
    public static class ReflectionEx
    {
        public class PrivateFieldData
        {
            public object ObjectInstance;
            public FieldInfo FieldInfo;
            public object Value
            {
                get
                {
                    return FieldInfo.GetValue(ObjectInstance);
                }

                set
                {
                    FieldInfo.SetValue(ObjectInstance, value);
                }
            }
        }

        public static PrivateFieldData PrivateField(this object obj, string name)
        {
            PrivateFieldData data = new PrivateFieldData
            {
                ObjectInstance = obj,
                FieldInfo = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            };

            return data;
        }
    }
    #endregion

    #region title resolver
    public static class TitleResolver
    {
        public static Dictionary<string, Type> StringsToType;
        public static Dictionary<Type, string> TypesToString;

        public static void InitializeTitleResolver()
        {
            StringsToType = new Dictionary<string, Type>();
            TypesToString = new Dictionary<Type, string>();

            List<string> titles = new List<string>();

            var listOfBotBehaviours = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                       from assemblyType in domainAssembly.GetTypes()
                                       where typeof(BaseBotBehaviour).IsAssignableFrom(assemblyType)
                                       select assemblyType).ToArray();

            foreach (Type behaviour in listOfBotBehaviours)
            {
                var titleField = behaviour.GetField("Title");
                if (titleField != null)
                {
                    string title = (string)titleField.GetValue(Activator.CreateInstance(behaviour));

                    StringsToType.Add(title, behaviour);
                    TypesToString.Add(behaviour, title);
                }
            }
        }

        public static Type TitleToType(string title)
        {
            return StringsToType[title];
        }

        public static string TypeToTitle(Type type)
        {
            return TypesToString[type];
        }
    }
    #endregion
    #endregion
}
