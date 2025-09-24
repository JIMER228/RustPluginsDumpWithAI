using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Horse Control", "https://discord.gg/dNGbxafuJn", "0.0.4")]
    public class HorseControl : RustPlugin
    {
        #region Classes

        private class Configuration
        {
            [JsonProperty("Cooldown на команду 'horse'")]
            public int Cooldown;
            
            [JsonProperty("Количество лошадей на карте")]
            public int HorseAmount;
            [JsonProperty("Бонус скорости при беге по дороге")]
            public float RoadSpeedBonus;
            
            [JsonProperty("Скорость лошади (ходьба)")]
            public float WalkSpeed;
            [JsonProperty("Скорость лошади (бег)")]
            public float RunSpeed;
            [JsonProperty("Скорость лошади (спринт)")]
            public float TrotSpeed;

            [JsonProperty("Кол-во секунд базовой стамины")]
            public float StaminaSeconds;
            [JsonProperty("Кол-во секунд макс. стамины")]
            public float MaxStaminaSeconds;
            [JsonProperty("Максимальная скорость")]
            public float MaxSpeed;
            [JsonProperty("Скорость поворота")]
            public float TurnSpeed;

            [JsonProperty("СкинИД предмета для установки")]
            public ulong SkinID;

            public static Configuration Generate()
            {
                var info = new BaseRidableAnimal();
                
                return new Configuration
                {
                    Cooldown = 10,
                    HorseAmount    = 15,
                    RoadSpeedBonus = info.roadSpeedBonus,
                    WalkSpeed      = info.walkSpeed,
                    RunSpeed       = info.runSpeed,
                    TrotSpeed      = info.trotSpeed,

                    StaminaSeconds    = info.staminaSeconds,
                    MaxStaminaSeconds = info.maxStaminaSeconds,

                    MaxSpeed  = info.maxSpeed,
                    TurnSpeed = info.turnSpeed,
                    
                    SkinID = 1766455225 
                };
            }
        }
        
        #endregion

        #region Variables
        
        private static Coroutine HorseCoroutine;
        private static string GivePermission = "HorseControl.Spawn";
        private static Configuration Settings = Configuration.Generate();

        #endregion

        #region Hooks
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => Settings = Configuration.Generate();
        protected override void SaveConfig()        => Config.WriteObject(Settings);

        private void OnServerInitialized()
        {
            permission.RegisterPermission(GivePermission, this); 
            var prefab = GameManager.server.FindPrefab("assets/rust.ai/nextai/testridablehorse.prefab");
            
            var set = prefab.GetComponent<BaseRidableAnimal>();
            Settings.RoadSpeedBonus = set.roadSpeedBonus;
            Settings.WalkSpeed = set.walkSpeed;
            Settings.RunSpeed = set.runSpeed;
            Settings.TrotSpeed = set.trotSpeed;

            Settings.StaminaSeconds = set.staminaSeconds;
            Settings.MaxStaminaSeconds = set.maxStaminaSeconds;

            Settings.MaxSpeed = set.maxSpeed;
            Settings.TurnSpeed = set.turnSpeed;
            
            PrintWarning($"Horse settings setted up!");
            
            HorseCoroutine = ServerMgr.Instance.StartCoroutine(ControlHorses());
        }
        
        private void Unload()
        {
            if (HorseCoroutine != null)
                ServerMgr.Instance.StopCoroutine(HorseCoroutine);
        }
        
        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            var entity = obj.GetComponent<BaseEntity>();
            if (entity != null && entity.ShortPrefabName == "box.wooden.large" && entity.skinID == Settings.SkinID)
            {
                var ent = GameManager.server.CreateEntity("assets/rust.ai/nextai/testridablehorse.prefab", entity.transform.position);
                ent.Spawn();
                
                NextTick(() =>
                {
                    if (entity != null) entity.Kill();
                });
            }
        }

        #endregion
        
        #region Commands
        
        [ChatCommand("horse")]
        private void CmdChatHorse(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, GivePermission)) return;

            if (IsCd(player.userID))
            {
                player.ChatMessage($"Вы недавно использовали эту команду! Подождите ещё {GetCd(player.userID)} секунд.");
                return;
            }
            
            var item = ItemManager.CreateByName("box.wooden.large", 1, Settings.SkinID);
            if (!item.MoveToContainer(player.inventory.containerMain))
                item.Drop(player.transform.position, Vector3.zero);
            
            player.ChatMessage($"Вы получили ручного коняку!");
            SetCd(player.userID);
        }

        #region Cooldown
        
        private readonly Dictionary<ulong, float> _cooldown = new Dictionary<ulong, float>();

        private bool IsCd(ulong user)
        {
            return GetCd(user) >= 0;
        }

        private int GetCd(ulong user)
        {
            return _cooldown.ContainsKey(user) ? (int) (_cooldown[user] - Time.realtimeSinceStartup) : -1;
        }

        private void SetCd(ulong user)
        {
            var time = Time.realtimeSinceStartup + Settings.Cooldown;
            if (_cooldown.ContainsKey(user))
                _cooldown[user] = time;
            else
                _cooldown.Add(user, time);
        }
        
        #endregion
        
        [ConsoleCommand("givehorse")]
        private void CmdGiveHorse(ConsoleSystem.Arg args)
        {
            if (args.Player() != null || !args.HasArgs(1)) return;

            ulong id = 0;
            if (!ulong.TryParse(args.Args[0], out id))
            {
                args.ReplyWithObject("Bad syntax! givehorst <steamId>");
                return;
            }

            var player = BasePlayer.FindByID(id);
            if (player == null) return;
            
            var item = ItemManager.CreateByName("box.wooden.large", 1, Settings.SkinID);
            if (!item.MoveToContainer(player.inventory.containerMain))
                item.Drop(player.transform.position, Vector3.zero);
            
            player.ChatMessage($"Вам выдана лошадь, вы можете установить её в любом удобном для вас месте!");
        }

        #endregion

        #region Methods

        private void SpawnHorse()
        {
            var pos = GetRandomPosition();
            var ent = GameManager.server.CreateEntity("assets/rust.ai/nextai/testridablehorse.prefab", pos);
            ent.Spawn();
            
            PrintWarning($"Horse spawned at: {pos}");
        }
        
        public Vector3 GetRandomPosition()
        {
            var originPosition = Vector3.zero;
            
            for (int i = 0; i < 15; i++)
            {
                var newPosition = Vector3.zero;

                newPosition.x += Oxide.Core.Random.Range(-World.Size / 2f, World.Size / 2f);
                newPosition.z += Oxide.Core.Random.Range(-World.Size / 2f, World.Size / 2f);
                newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);

                if (newPosition.y < 0)
                    continue; 
                
                RaycastHit hitInfo;
                if (Physics.Raycast(newPosition, Vector3.up, out hitInfo) || Physics.Raycast(newPosition, Vector3.down, out hitInfo))
                {
                    if (hitInfo.GetEntity() == null)
                    {
                        return newPosition;
                    }
                }
                else continue;
            }
             
            return originPosition;
        }

        private IEnumerator ControlHorses()
        {
            while (true)
            {
                var amount = GetHorseAmount();
                
      //          PrintWarning($"Current amount: {amount}"); 
                if (amount < Settings.HorseAmount)
                {
                    for (var i = amount; i < Settings.HorseAmount; i++)
                    {
                        SpawnHorse();
                    }    
                }
                
                yield return new WaitForSeconds(15f);
            }

            yield return 0;
        }
        private int GetHorseAmount() => UnityEngine.Object.FindObjectsOfType<BaseRidableAnimal>().Length;

        #endregion
    }
}