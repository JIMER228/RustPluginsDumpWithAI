// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fractions System", "Lomarine", "1.0.0")] 
    public class FractionsSystem : RustPlugin /* Разработал https://vk.com/lomarine.space для https://server-rust.ru */
    {
        [PluginReference] private Plugin Friends;
        
        #region Language and Config

        private string name1, name2, desc1, desc2, color1, color2, url1, url2;

        private const string
            config1 = "Название 1 фракции",
            config2 = "Название 2 фракции",
            config3 = "Описание 1 фракции",
            config4 = "Описание 2 фракции",
            config5 = "Цвет 1 фракции",
            config6 = "Цвет 2 фракции",
            config7 = "Картинка 1 фракции",
            config8 = "Картинка 2 фракции";

	    private readonly Dictionary<string,string> Messages = new Dictionary<string, string>()
	    {
		    // Ally
		    ["A.STRUCTURE"] = "<color=red><size=20>Это структура игрока вашей фракции, вы не можете нанести ей урон!</size></color>",
	        ["A.PLAYER"] = "<color=red><size=20> Вы атакуете игрока своей фракции!</size></color>",
	        ["A.LOOT"] = "<color=red><size=20>Вы не можете лутать ящики своих соклановцев, если они не находятся в списке ваших друзей.</size></color>"
	    };

	    protected override void LoadDefaultConfig()
	    {
		    // Pickup
		    Config[config1] = name1 = GetConfig(config1, "Империя");
	        Config[config2] = name2 = GetConfig(config2, "Республика");
	        Config[config3] = desc1 = GetConfig(config3, "В далёкой далёкой галактике...");
	        Config[config4] = desc2 = GetConfig(config4, "В далёкой далёкой галактике...");
	        Config[config5] = color1 = GetConfig(config5, "1 0 0 1");
	        Config[config6] = color2 = GetConfig(config6, "0 0 1 1");
	        Config[config7] = url1 = GetConfig(config7, "https://imgur.com/KYe8vNT.png");
	        Config[config8] = url2 = GetConfig(config8, "https://imgur.com/MlVVPy3.png");
		    
		    SaveConfig();
	    }

	    private T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
	    
	    #endregion

        #region Oxide Hooks

         private void Init()
        {
            if(!plugins.Exists("Friends")) PrintError("Плагин друзей не обнаружен!");
            LoadData();

            if (!permission.GroupExists("rp_one")) permission.CreateGroup("rp_one", "", 0);
            if (!permission.GroupExists("rp_two")) permission.CreateGroup("rp_two", "", 0);
            
            LoadDefaultConfig();
            lang.RegisterMessages(Messages, this);
            
            timer.Every(150f, () => { SaveData(); });

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerSleepEnded(player);
            }
        }
        
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyGUI(player);
            }
            SaveData();
        }
        
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if(!playerData.ContainsKey(player.userID)) CreateGUI(player);
        }
        
        private void OnLootEntity(BasePlayer player, BaseEntity entity) // Лутание ящиков
        {
            if (entity.OwnerID == player.userID || playerData[entity.OwnerID] != playerData[player.userID]) return;

            if ((bool)(Friends?.CallHook("IsFriend", player.userID, entity.OwnerID) ?? false)) return;

            player.ChatMessage(lang.GetMessage("A.LOOT", this));
            timer.Once(0.01f, player.EndLooting);
        }
        
        private object OnEntityTakeDamage(BaseEntity entity, HitInfo info) // Механика нанесения урона
        {
            // Buldings
            if (entity.OwnerID != 0)
            {
                BasePlayer Attacker = info.InitiatorPlayer;

                if (Attacker == null) return null;

                if (Attacker.userID == entity.OwnerID) return null; // Хомячок

                if (playerData[entity.OwnerID] == playerData[Attacker.userID])
                {
                    Attacker.ChatMessage(lang.GetMessage("A.STRUCTURE", this));
                    return false;
                }
            }
            
            // Players
            if (entity is BasePlayer)
            {
                BasePlayer Victim = entity.ToPlayer(); 
                BasePlayer Attacker = info.InitiatorPlayer; 

                if (Victim == null || Attacker == null || Victim == Attacker) return null;

                if (playerData[Victim.userID] == playerData[Attacker.userID]) 
                {
                    Attacker.ChatMessage(lang.GetMessage("A.PLAYER", this));
                    return false;
                }
            }
            
            return null;
        }

        private object OnTurretTarget(BaseEntity turret, BaseEntity target) // Механика агра туррели
        {
            BasePlayer Owner = BasePlayer.FindByID(turret.OwnerID);
            BasePlayer Target = target.ToPlayer();
            
            if (Target == null || Owner == null) return null;
            
            if (playerData[Target.userID] == playerData[Owner.userID])
            {
                return false;
            }

            return null;
        }

        #endregion

        #region GUI

        private void CreateGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "MainUI",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 0.35"
                    },
                    
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    
                    new CuiNeedsCursorComponent()
                }
            });
            
            // Подложки
            container.Add(new CuiElement
            {
                Name = "FRACTION_ONE",
                Parent = "MainUI",
                Components = 
                { 
                    new CuiButtonComponent { Command = "UI_CHOOSE_FRACTION 1", Color = color1},
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.1",AnchorMax = "0.4 0.9" }
                }
            });
            
            container.Add(new CuiElement
            {
                Name = "FRACTION_TWO",
                Parent = "MainUI",
                Components =
                {
                    new CuiButtonComponent { Command = "UI_CHOOSE_FRACTION 2", Color = color2},
                    new CuiRectTransformComponent{ AnchorMin = "0.6 0.1",AnchorMax = "0.9 0.9" }
                }
            });
            // Иконки
            container.Add(new CuiElement
            {
                Parent = "FRACTION_ONE",
                Components = 
                { 
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = url1}, // Image Library
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.4",AnchorMax = "0.9 0.9" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "FRACTION_TWO",
                Components = 
                { 
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = url2}, // Image Library
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.4",AnchorMax = "0.9 0.9" }
                }
            });
            // Описание
            container.Add(new CuiElement
            {
                Parent = "FRACTION_ONE",
                Components = 
                { 
                    new CuiTextComponent() { Color = "1 1 1 1", Text = desc1,
                        Align = TextAnchor.MiddleCenter}, // Image Library
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.1",AnchorMax = "0.9 0.4" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "FRACTION_TWO",
                Components = 
                { 
                    new CuiTextComponent()
                    {
                        Color = "1 1 1 1", Text = desc2,
                        Align = TextAnchor.MiddleCenter
                    }, // Image Library
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.1",AnchorMax = "0.9 0.4" }
                }
            });
            // Прочее
            container.Add(new CuiElement
            {
                Parent = "MainUI",
                Components = 
                { 
                    new CuiTextComponent()
                    {
                        Color = "0 0 0 1", Text = "Выбери на чьей ты стороне:",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 40
                    }, // Image Library
                    new CuiRectTransformComponent{ AnchorMin = "0.1 0.9",AnchorMax = "0.9 1" }
                }
            });
            

            CuiHelper.AddUi(player, container);
        }

        private void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "MainUI");
        }
        
        private void DestroyGUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "MainUI");
            }
        }
        
        #endregion

        #region Command

        [ConsoleCommand("UI_CHOOSE_FRACTION")]
        private void CmdChoose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var group = 0;
            
            var frchat = "";

            switch (arg.Args[0])
            {
                case "1":
                    group = 1;
                    frchat = name1;
                    if(permission.UserHasGroup(player.UserIDString ,"rp_two")) permission.RemoveUserGroup(player.UserIDString , "rp_two");
                    permission.AddUserGroup(player.UserIDString ,"rp_one");
                    break;
                case "2":
                    group = 2;
                    frchat = name2;
                    if(permission.UserHasGroup(player.UserIDString ,"rp_one")) permission.RemoveUserGroup(player.UserIDString , "rp_one");
                    permission.AddUserGroup(player.UserIDString ,"rp_two");
                    break;
            }
            
            if(!playerData.ContainsKey(player.userID)) playerData.Add(player.userID, 0);
            
            playerData[player.userID] = group;
            
            DestroyGUI(player);
            Server.Broadcast($"Игрок {player.displayName} выбрал фракцию {frchat}");
        }

        #endregion

        #region Data

        private const string filename = "FractionsSystem_Data";
        
        private Dictionary<ulong, int>  playerData = new Dictionary<ulong, int>();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(filename, playerData);
        }

        private void LoadData()
        {
            playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>(filename);
        }

        #endregion
    }
}