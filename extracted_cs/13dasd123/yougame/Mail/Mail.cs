using UnityEngine;
using System.Collections;
using Oxide.Core;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Mail", "Anonymuspro", "0.0.1")]
    public class Mail : RustPlugin
    {
        #region Classes
        public class MailType
        {
            [JsonProperty("Скин")]
            public ulong skinId;
            [JsonProperty("Название в инвентаре")]
            public string name;
            [JsonProperty("Использовать лут из списка?")]
            public bool mod;
            [JsonProperty("Список лута")]
            public Dictionary<string, int> itemlist;
            [JsonProperty("Сколько будет взято из каждого списка(ресурсы , оружие и тп)")]
            public int itemCount;
        }

        public class Data
        {
            public DateTime last;
            public int CurrentTime;
            public bool sended;
            public bool Notification;
        }

        public class Text
        {
            [JsonProperty("Отступ для 3д текста")]
            public Vector3 pos;

            [JsonProperty("Цвет 3д текста")]
            public string color;

            [JsonProperty("Размер 3д текста")]
            public string size;
			
			[JsonProperty("3д Текст")]
            public string text;
        }

        public class ConfigData
        {
            [JsonProperty("Настройки 3д текста")]
            public Text txt;
            [JsonProperty("Настройка писем")]
            public List<MailType> types;
            [JsonProperty("Время до прихода письма")]
            public int Time;
        }

        #endregion

        #region Vars
        public string mailBox = "mailbox";
        public string item = "xmas.present.large";

        public Dictionary<ulong, Data> data;

        public List<BaseEntity> currentMails;

        public ConfigData cfg;
        #endregion

        #region LoadConfig

        protected override void LoadConfig()
        {
            base.LoadConfig();
            cfg = Config.ReadObject<ConfigData>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            cfg = new ConfigData
            {
                txt = new Text
                {
                    pos = new Vector3(-0.2f, 1.5f,-0.1f),
                    color = "#DEE2E5",
                    size = "25",
					text = "До получения посылки \n  {0}",
                },
                types = new List<MailType>
                {
                    new MailType
                    {
                        skinId = 1817256181,
                        name = "БАНДЕРОЛЬ",
                        mod = true,
                        itemlist = new Dictionary<string, int>()
                        {

                            {"rifle.ak", 1},
                            {"pistol.semiauto", 1 },
                            {"hatchet", 1 },
                            {"jackhammer", 1 },
                            {"hazmatsuit", 1 },
                            {"pickaxe", 1 },
                        }
                    },
                    new MailType
                    {
                        skinId = 1713122270,
                        name = "ПИСЬМО",
                        mod = false,
                        itemlist = new Dictionary<string, int>()
                        {

                            {"rifle.ak", 1},
                            {"pistol.semiauto", 1 },
                            {"hatchet", 1 },
                            {"jackhammer", 1 },
                            {"hazmatsuit", 1 },
                            {"pickaxe", 1 },
                        },
                        itemCount = 2
                    }
                },
                Time = 1200
            };
        }

        #endregion

        #region OxideHooks

        void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Mail/Data"))
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>("Mail/Data");
                foreach(var d in data)
                {
                    if(d.Value.CurrentTime > cfg.Time)
                    {
                        d.Value.CurrentTime = cfg.Time;
                    }
                }
            }
            else data = new Dictionary<ulong, Data>();
            currentMails = new List<BaseEntity>();
            BaseEntity[] objects = GameObject.FindObjectsOfType<Mailbox>();
            if (objects != null && objects.Length > 0)
            {
                foreach (var e in objects)
                {
                    currentMails.Add(e);
                }
            }
            timer.Every(1f, MailTick);
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Mail/Data", data);
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;

            if (entity.name.Contains(mailBox))
            {
                if (data.ContainsKey(player.userID))
                {
                    Data d = data[player.userID];
                    if(d.CurrentTime == 0)
                    {
                        if(!d.sended)
                        {
                            d.sended = true;
                            int type = UnityEngine.Random.Range(0, cfg.types.Count);
                            Item x = ItemManager.CreateByName(item, 1, cfg.types[type].skinId);
                            x.name = cfg.types[type].name;
                            if(!player.inventory.GiveItem(x))
                            {
                                x.Drop(player.ServerPosition, Vector3.down * 3);
                            }
                        }
                    }
                }
                else return;
            }
            else return;
        }

        private object OnItemAction(Item item, string action)
        {
            if (item == null || action == null || action == "")
                return null;
            if (item.info.shortname != "xmas.present.large")
                return null;
            if (action != "unwrap")
                return null;
            BasePlayer player = item.GetRootContainer().GetOwnerPlayer();
            if (player == null)
                return null;
            GiveThink(player, item);
            ItemRemovalThink(item, player, 1);
            Effect.server.Run("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player.transform.position);
            return false;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            foreach(var m in currentMails)
            {
                if(m == entity)
                {
                    currentMails.Remove(m);
                    return;
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            BaseEntity m = entity as BaseEntity;
            if (m == null) return;
            if (m.name.Contains(mailBox))
            {
                currentMails.Add(m);
            }
        }

        #endregion

        #region MyMethods

        public void MailTick()
        {
            
            foreach(var player in BasePlayer.activePlayerList)
            {
                if (!data.ContainsKey(player.userID))
                {
                    data.Add(player.userID, new Data
                    {
                        last = DateTime.Now,
                        CurrentTime = cfg.Time,
                    });
                }
                else
                {
                    Data d = data[player.userID];
                    if (d.CurrentTime == 0)
                    {
                        if(d.Notification == false)
                        {
                            d.Notification = true;
                            SendNotification(player);
                        }
                        if (d.last.Day != DateTime.Now.Day)
                        {
                            d.last = DateTime.Now;
                            d.CurrentTime = cfg.Time;
                            d.sended = false;
                            d.Notification = false;
                        }
                    }
                    else d.CurrentTime -= 1;
                }

                if (!player.IsBuildingAuthed()) return;
                if (currentMails.Count == 0) return;
                foreach (var c in currentMails)
                {
                    if (Vector3.Distance(player.ServerPosition, c.ServerPosition) <= 2f)
                        ShowText(c, player);
                }
            }
        }

        #endregion

        #region Help

        public string TimeToString(double time)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(time);
            int hours = elapsedTime.Hours;
            int minutes = elapsedTime.Minutes;
            int seconds = elapsedTime.Seconds;
            int days = Mathf.FloorToInt((float)elapsedTime.TotalDays);
            string s = "";

            if (days > 0) s += $"{days} дн.";
            if (hours > 0) s += $"{hours} ч. ";
            if (minutes > 0) s += $"{minutes} мин. ";
            if (seconds > 0) s += $"{seconds} сек.";
            else s = s.TrimEnd(' ');
            return s;
        }

        public string GetText(Data d, StorageContainer l)
        {
            if (d == null || l == null) return "";
            string text = "";
            if (d.CurrentTime > 0)
                text = $"<color={cfg.txt.color}><size={cfg.txt.size}>{string.Format(cfg.txt.text, TimeToString(d.CurrentTime))}</size></color>";
            else
            {
                if(!d.sended)
                {
                    text = $"<size={cfg.txt.size}>Заберите посылку!</size>";
                }
                else
                {
                    text = $"<size={cfg.txt.size}>Вы уже получали письмо сегодня \n Приходите завтра!</size>";
                }
            }
            return text;
        }

        public void ShowText(BaseEntity entity, BasePlayer player)
        {
          
            if (entity == null || player == null) return;
            Data d = data[player.userID];
            
            player.SendConsoleCommand("ddraw.text", 1f, cfg.txt.color, entity.transform.position + cfg.txt.pos, GetText(d,entity.GetComponent<StorageContainer>()));
        }

        private void GiveThink(BasePlayer player, Item item)
        {
            foreach (var p in cfg.types)
            {
                if (p.itemlist == null || p.itemlist.Count == 0) return;
                if (item.skin == p.skinId)
                {
                    if (!p.mod) return;

                    Dictionary<string, int> items = new Dictionary<string, int>();
                    for (int i = 0; i < p.itemCount; i++)
                    {
                        int a = UnityEngine.Random.Range(0, p.itemlist.Count);
                        if (!items.Contains(p.itemlist.ElementAt(a)))
                        {
                            items.Add(p.itemlist.ElementAt(a).Key, p.itemlist.ElementAt(a).Value);
                        }
                        else
                            i--;
                    }
                    foreach (var i in items)
                    {
                        Item n = ItemManager.CreateByName(i.Key, i.Value);
                        if (!player.inventory.GiveItem(n))
                        {
                            n.Drop(player.ServerPosition, Vector3.down * 3f);
                        }
                    }
                }
            }
        }

        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }

        private void SendNotification(BasePlayer player)
        {
            if (player == null) return;
            string Layer = "notif.layer";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0.4"
                },
                Text =
                {
                    Text = "ВАМ ПОСЫЛКА ИЗ ДЕРЕВНИ",
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 17,    
                },
                RectTransform =
                {
                    AnchorMin = "0.3421876 0.1180556",
                    AnchorMax = "0.6414063 0.1444445"
                },
                FadeOut = 1f

            }, "Hud", Layer);

            CuiHelper.AddUi(player, container);

            timer.Once(2f, () =>
            {
                CuiHelper.DestroyUi(player, Layer);
            });
        }

        #endregion
    }
}
