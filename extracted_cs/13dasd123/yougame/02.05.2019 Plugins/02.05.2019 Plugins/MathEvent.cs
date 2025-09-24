using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Oxide.Core;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Math Event","Baks","1.1.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class MathEvent : RustPlugin
    {
        private float time;
        private float lc;
        private int ans;
        private bool eventon;
        private int minPlayers;
        private List<Rewards> reward = new List<Rewards>();


        private class Rewards
        {
            public string shortname;

            public int amount;
        }

        void Loaded()
        {
            LoadDefaultConfig();
            PrintWarning("MATH EVENT LOADED");
            timer.Every(time, EventTryStart);

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("MathEvent/Rewards"))
            {
                reward = new List<Rewards>
                {
                    new Rewards
                    {
                        shortname = "sulfur",
                        amount = 100
                    },
                    new Rewards
                    {
                        shortname = "sulfur",
                        amount = 500
                    }
                };
                Interface.Oxide.DataFileSystem.WriteObject("MathEvent/Rewards",reward);
            }
            else
            {
                reward = Interface.Oxide.DataFileSystem.ReadObject<List<Rewards>>("MathEvent/Rewards");
            }
        }
        
        protected override void LoadDefaultConfig()
        {
            Config["Частота срабатывания (в секундах)"] = time = GetConfig("Частота срабатывания (в секундах)", 3600f);
            Config["Време действия ивента(в секундах)"] = lc = GetConfig("Време действия ивента(в секундах)", 120f);
            Config["Минимальное количество человек"] = minPlayers = GetConfig("Минимальное количество человек", 10);
        }
        
        
        
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
        

        
        void EventStart()
        {

            Random r = new Random();
            ans = r.Next(1, 100);
            int a = r.Next(0,3);
            eventon = true;
            timer.Once(lc, EventEnd);
            
            
            switch (a)
           {
               case 0:
                    PlusEvent(ans);
                   break;
                case 1:
                    MinusEvent(ans);
                    break;
                case 2:
                    DivEvent(ans);
                    break;
                case 3:
                    MultipEvent(ans);
                    break;
                    
                
            }
        }

        void EventEnd()
        {
            eventon = false;
            Server.Broadcast("Математический ивент кончился");
        }

        void PlusEvent(int arg)
        {
            Random rndy = new Random();
            int y = rndy.Next(3, 80);
            int c = arg - y;
            foreach (var var in Player.Players) 
            {
                SendReply(var,$"{c.ToString()} + {y.ToString()} = ???");
                SendReply(var,"Чтобы ответь напишите /ans 'ваш ответ' ");
            }
        }
        
        
        void MinusEvent(int arg)
        {
            Random rndy = new Random();
            int y = rndy.Next(3, 80);
            int c = arg + y;
            foreach (var var in Player.Players)
            {
                SendReply(var, $"{c.ToString()} - {y.ToString()} = ???");
                SendReply(var, "Чтобы ответь напишите /ans 'ваш ответ' ");
            }
        }

        void MultipEvent(int arg)
        {
            var flag = 271;
            Random rndy = new Random();
            int y = rndy.Next(5, 50);
            int x = arg / y;
            foreach (var var in Player.Players)
            {
                SendReply(var, $"{x.ToString()} x {y.ToString()} = ???");
                SendReply(var, "Чтобы ответь напишите /ans 'ваш ответ' ");
            }
        }

        void DivEvent(int arg)
        {
            Random rndy = new Random();
            int y = rndy.Next(3, 90);
            
            int x = arg * y;
            foreach (var var in Player.Players)
            {
                SendReply(var, $"{x.ToString()}/{y.ToString()} = ???");
                SendReply(var, "Чтобы ответь напишите /ans 'ваш ответ' ");
            }
        }

        void Reward(BasePlayer player)
        {
            var recieve = reward.ToList().GetRandom();
            ItemManager.CreateByPartialName(recieve.shortname, recieve.amount).MoveToContainer(player.inventory.containerMain);
            
            SendReply(player,$"Вы получили  {recieve.amount} x {recieve.shortname}");
        }

        [ChatCommand("ma")]
        void maon(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            EventStart();
            //SendReply(player, ans.ToString());
            
        }

        [ChatCommand("ans")]
        void AnsResiever(BasePlayer player, string command, string[] args)
        {
            if(eventon != true) return;
            SendReply(player,$"Ваш ответ:{args}");
            //int answ = Convert.ToInt32(args.ToString());
            //SendReply(player,$"_____{args[0]}____");
            if (args[0] != ans.ToString())
            {
                SendReply(player,"Неверный ответ");
            }
            else
            {
                SendReply(player,"Правильный ответ");
                Reward(player);
                EventEnd();
            }
        }
        

        void EventTryStart()
        {
            if (Player.Players.Count < minPlayers)
            {
                foreach (var var in Player.Players) 
                {
                    SendReply(var,"Недостаточно людей для старта");
                }
                
            }
            else
            {
                EventStart();
            }
        }

    }
}
