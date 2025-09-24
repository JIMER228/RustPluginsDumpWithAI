// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using Facepunch.Extend;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EMPRocket","Baks","1.2.10")]
    public class EMPRocket : RustPlugin
    {
        #region var

        string rocket;
        private AutoTurret[] autoturrets;
        private GunTrap[] guntraps;
        private SamSite[] samsites;
        private List<GunTrap> gtData = new List<GunTrap>();
        private float time;
        private float radius;
        private bool usePermission;
        private ulong rocketId = 2556236982;
        private Dictionary<string, int> craftItems;

        #endregion

        #region data
        protected override void LoadDefaultConfig()
        {
            Config["На сколько будут отключаться турели(секунды)"] = time = GetConfig("На сколько будут отключаться турели(секунды)", 35f);
            Config["Радиус действия"] = radius = GetConfig("Радиус действия", 40f);
            Config["Использовать пермишен (emprocket.craft)"] = usePermission = GetConfig("Использовать пермишен (emprocket.craft)", false);
        }
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        void OnServerInitialized()
        {
            permission.RegisterPermission("emprocket.craft",this);
            LoadDefaultConfig();
            PrintWarning("Config Loaded");
            UpdateList();
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("EMP Rocket/craft"))
            {
                craftItems = new Dictionary<string, int>
                {
                    ["sulfur"] = 1000,
                    ["techparts"] = 5
                };
                Interface.Oxide.DataFileSystem.WriteObject("EMP Rocket/craft",craftItems);
            }
            else
            {
                craftItems = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>("EMP Rocket/craft");
            }
        }

        #endregion

        #region hooks

        void UpdateList()
        {
            autoturrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
            guntraps = UnityEngine.Object.FindObjectsOfType<GunTrap>();
            samsites = UnityEngine.Object.FindObjectsOfType<SamSite>();
            
        }
        
        bool CanBeTargeted(BaseCombatEntity player, GunTrap behaviour)
        {
            if (gtData.Contains(behaviour))
            {
                return false;
            }
            return true;
        }
        

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {

            entity.OwnerID = player.userID;

        }
        void OnEntityKill(BaseNetworkable entity)
        {
            
            if (entity.ShortPrefabName == "rocket_smoke")
            {
                
                
                Vector3 pos = entity.transform.position;
                float minx = pos.x - radius;
                float miny = pos.y - radius;
                float minz = pos.z - radius;
                float maxx = pos.x + radius;
                float maxy = pos.y + radius;
                float maxz = pos.z + radius;
                UpdateList();
                int count = 0;
                foreach (var var in autoturrets)
                {
                    if (var.transform.position.x>minx && var.transform.position.x<maxx && var.transform.position.y>miny && var.transform.position.y<maxy && var.transform.position.z>minz && var.transform.position.z<maxz)
                    {
                        
                        
                        if (var.IsOnline())
                        {
                            var.InitiateShutdown();
                            var.UpdateHasPower(0,0);
                            timer.Once(time, (() =>
                            {
                                var.InitiateStartup();
                                var.UpdateHasPower(25,0);
                                var.SendNetworkUpdate();
                                var.UpdateFacingToTarget(1);
                            }));
                            count++;
                        }
                    }
                    
                }

                foreach (var var in samsites)
                {
                    if (var.transform.position.x>minx && var.transform.position.x<maxx && var.transform.position.y>miny && var.transform.position.y<maxy && var.transform.position.z>minz && var.transform.position.z<maxz)
                    {
                        
                        if (var.IsOn())
                        {
                            count++;
                            var.UpdateHasPower(0,0);
                    
                            timer.Once(time, (() =>
                            {
                                
                                var.UpdateHasPower(25, 0);
                            }));
                        }
                    }
                    
                    
                }
                
                foreach (var var in guntraps)
                {
                    if (var.transform.position.x>minx && var.transform.position.x<maxx && var.transform.position.y>miny && var.transform.position.y<maxy && var.transform.position.z>minz && var.transform.position.z<maxz)
                    {
                        gtData.Add(var);
                        count++;
                        timer.Once(time, (() =>
                        {
                            gtData.Clear();
                        }));
                    }
                }

                BaseEntity ent = entity as BaseEntity;
                BasePlayer player = Player.FindById(ent.OwnerID);
                SendReply(player,$"Устройств отключено:<color=#FF0000>{count}</color> на <color=#3af01e>{time}</color> секунд");
                timer.Once(time, (() =>
                {
                    
                   SendReply(player,$"Включено <color=#FF0000>{count}</color> устройств"); 
                }));
            }
        }

        bool GiveEmpRocket(ulong userid, int amount)
        {
            BasePlayer target = BasePlayer.FindByID(userid);
            if (target == null)
            {
                LogToFile(Name,$"Игрок {userid} не найден",this,true,true);
                return false;
            }
            Item item = ItemManager.CreateByPartialName("ammo.rocket.smoke", amount, rocketId);
            item.name = "EMP ракета";
            if (target.inventory.containerBelt.capacity>target.inventory.containerBelt.itemList.Count || target.inventory.containerMain.capacity>target.inventory.containerMain.itemList.Count)
                target.GiveItem(item);
            else
                item.DropAndTossUpwards(target.transform.position, 5);

            return true;
        }
        
        void OnPlayerCraftEmp(BasePlayer player)
        {
            if (usePermission)
                if (!permission.UserHasPermission(player.UserIDString, "emprocket.craft")) SendReply(player,$"У вас нет доступа к этой команде");

            bool craft = false;
            bool precraft = false;
            Dictionary<string, int> back = new Dictionary<string, int>();
            foreach (var var in craftItems)
            {
                foreach (var vara in player.inventory.AllItems())
                {
                    if (vara.info.shortname == var.Key)
                    {
                        precraft = true;
                    }
                }

                if (!precraft)
                {
                    SendReply(player,$"<color=#FF0000>Недостаточно ресурсов</color>");
                    SendReply(player,$"Для крафта необходимо иметь:\n");
                    foreach (var item in craftItems)
                    {
                        SendReply(player,$"{item.Value} x {item.Key}\n ");
                    }
                    return;
                }

                if (player.inventory.FindItemByItemID(var.Key).amount >= var.Value)
                {
                    if (player.inventory.FindItemByItemID(var.Key).amount - var.Value == 0)
                    {
                        player.inventory.FindItemByItemID(var.Key).Remove();
                        back.Add(player.inventory.FindItemByItemID(var.Key).info.shortname,player.inventory.FindItemByItemID(var.Key).amount);
                    }
                    else
                    {
                        player.inventory.FindItemByItemID(var.Key).amount -= var.Value;
                        back.Add(player.inventory.FindItemByItemID(var.Key).info.shortname,player.inventory.FindItemByItemID(var.Key).amount);
                    }  

                    craft = true;
                }
                /*if (player.inventory.FindItemID(var.Key).amount >= var.Value)
                {
                    if (player.inventory.FindItemID(var.Key).amount - var.Value == 0)
                    {
                        player.inventory.FindItemID(var.Key).Remove();
                        back.Add(player.inventory.FindItemID(var.Key).info.shortname,player.inventory.FindItemID(var.Key).amount);
                    }
                    else
                    {
                        player.inventory.FindItemID(var.Key).amount -= var.Value;
                        back.Add(player.inventory.FindItemID(var.Key).info.shortname,player.inventory.FindItemID(var.Key).amount);
                    }

                    craft = true;
                }*/
                else
                {
                    craft = false;
                }
            }

            if (!craft)
            {
                if (back.Count > 0)
                {
                    foreach (var var in back)
                    {
                        ItemManager.CreateByPartialName(var.Key, var.Value)
                            .MoveToContainer(player.inventory.containerMain);
                    }
                }
                
                SendReply(player,$"<color=#a30101>Недостаточно ресурсов</color>");
                return;
            }
            
            Item rocket = ItemManager.CreateByPartialName("ammo.rocket.smoke", 1, rocketId);
            rocket.name = "EMP ракета";
            rocket.MoveToContainer(player.inventory.containerMain);
            SendReply(player,$"<color=#3af01e>Вы спешно скрафтили EMP Rocket</color>");
        }

        #endregion

        #region commands

        

        [ChatCommand("emp")]
        void GivePlayerEMP(BasePlayer player,string command, string[] args)
        {
            if (args.Length<1 && player.IsAdmin)
            {
                Item rocket = ItemManager.CreateByPartialName("ammo.rocket.smoke", 1, rocketId);
                rocket.name = "EMP ракета";
                rocket.MoveToContainer(player.inventory.containerMain);
            }

            if (args.Length == 1)
            {
                if (args[0] == "craft")
                {
                    OnPlayerCraftEmp(player);
                }
            }
            
        }

        [ConsoleCommand("gemp")]
        void ConsoleGiveEMP(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length<2) return;
            if (arg.Player() != null)
            {
                if (arg.Player().IsAdmin) if (GiveEmpRocket(Convert.ToUInt64(arg.Args[0]), arg.Args[1].ToInt()))
                    PrintToConsole(arg.Player(),$"{arg.Args[1]} EMP Rocket gived to {arg.Args[0]}");
            }
            else
            {
                if (GiveEmpRocket(Convert.ToUInt64(arg.Args[0]), arg.Args[1].ToInt()))
                {
                    PrintWarning($"{arg.Args[1]} EMP Rocket gived to {arg.Args[0]}");
                }
            }
        }

        #endregion
    }
}