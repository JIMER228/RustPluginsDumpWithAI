using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealRates", "Kira", "1.0.0")]
    public class ZealRates : RustPlugin
    {
        #region [Vars] / [Переменные]

        private StoredData _dataBase = new StoredData();

        public enum Privileges
        {
            Arhont,
            Hero,
            Cock
        }

        #endregion

        #region [Classes] / [Классы]

        public class RateSettings
        {
            public float Resource;
            public float Career;
            public float Excavator;
            public float Animal;
        }

        #endregion

        #region [Dictionaries] / [Словари]

        public static Dictionary<Privileges, int> PrivelegeStatement = new Dictionary<Privileges, int>
        {
            [Privileges.Arhont] = 1,
            [Privileges.Hero] = 2,
            [Privileges.Cock] = 3
        };

        public static Dictionary<Privileges, RateSettings> PrivelegeSettings = new Dictionary<Privileges, RateSettings>
        {
            [Privileges.Arhont] = new RateSettings
            {
                Resource = 3f,
                Career = 3f,
                Excavator = 3f,
                Animal = 3f
            },
            [Privileges.Hero] = new RateSettings
            {
                Resource = 2f,
                Career = 2f,
                Excavator = 2f,
                Animal = 2f
            },
            [Privileges.Cock] = new RateSettings
            {
                Resource = 1.5f,
                Career = 1.5f,
                Excavator = 1.5f,
                Animal = 1.5f
            }
        };

        #endregion

        #region [MonoBehaviours]

        public class Arhont : MonoBehaviour
        {
            public BasePlayer player;
            public float resource;
            public float career;
            public float excavator;
            public float animal;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                var rates = PrivelegeSettings[Privileges.Arhont];
                resource = rates.Resource;
                career = rates.Career;
                excavator = rates.Excavator;
                animal = rates.Animal;
            }
        }

        public class Hero : MonoBehaviour
        {
            public BasePlayer player;
            public float resource;
            public float career;
            public float excavator;
            public float animal;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                var rates = PrivelegeSettings[Privileges.Hero];
                resource = rates.Resource;
                career = rates.Career;
                excavator = rates.Excavator;
                animal = rates.Animal;
            }
        }

        public class Cock : MonoBehaviour
        {
            public BasePlayer player;
            public float resource;
            public float career;
            public float excavator;
            public float animal;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                var rates = PrivelegeSettings[Privileges.Cock];
                resource = rates.Resource;
                career = rates.Career;
                excavator = rates.Excavator;
                animal = rates.Animal;
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null) return;
            if (!(entity is BasePlayer)) return;
            var player = (BasePlayer) entity;
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) =>
            OnDispenserGather(dispenser, player, item);

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public Dictionary<ulong, PlayerPriveleges> PlayerDB = new Dictionary<ulong, PlayerPriveleges>();

            public class PlayerPriveleges
            {
                public List<Privileges> List = new List<Privileges>();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private IEnumerator LoadPriveleges()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckFromDataBase(player.userID);
                var playerPriveleges = _dataBase.PlayerDB[player.userID];
                var id = 4;
                foreach (var privelege in playerPriveleges.List)
                {
                    if (PrivelegeStatement[privelege] < id) FindPrivelege(privelege, player);
                    else id = PrivelegeStatement[privelege];
                }
            }

            yield return 0;
        }

        private static void FindPrivelege(Privileges name, BasePlayer player)
        {
            switch (name)
            {
                case Privileges.Arhont:
                    SetArhont(player);
                    break;
                case Privileges.Hero:
                    SetHero(player);
                    break;
                case Privileges.Cock:
                    SetCock(player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, null);
            }
        }

        private void CheckFromDataBase(ulong userid)
        {
            if (!_dataBase.PlayerDB.ContainsKey(userid))
                _dataBase.PlayerDB.Add(userid, new StoredData.PlayerPriveleges());
        }

        private static void SetArhont(BasePlayer player)
        {
            if (player.GetComponent<Arhont>() != null)
            {
                UnityEngine.Object.Destroy(player.GetComponent<Arhont>());
                SetArhont(player);
            }

            var arhont = player.gameObject.AddComponent<Arhont>();
        }

        private static void SetHero(BasePlayer player)
        {
            if (player.GetComponent<Hero>() != null)
            {
                UnityEngine.Object.Destroy(player.GetComponent<Hero>());
                SetHero(player);
            }

            var hero = player.gameObject.AddComponent<Hero>();
        }

        private static void SetCock(BasePlayer player)
        {
            if (player.GetComponent<Cock>() != null)
            {
                UnityEngine.Object.Destroy(player.GetComponent<Cock>());
                SetCock(player);
            }

            var cock = player.gameObject.AddComponent<Cock>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion
    }
}