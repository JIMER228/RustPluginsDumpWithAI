using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("CCases", "Hougan", "0.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Ежедневные кейсы для игроков")]
    public class CCases : RustPlugin
    {
        #region Classes

        private class SingleCase
        {
            internal class CaseItem
            {
                internal class Execute
                {
                    internal class ExecuteCommand
                    {
                        [JsonProperty("Выполняемая команда")] public List<string> Commands;
                        [JsonProperty("Уникальное название")] public string ImagePNG;
                        [JsonProperty("Внешнее изображение")] public string ImageURL;
                    }

                    internal class ExecuteItem
                    {
                        [JsonProperty("Короткое название предмета")] public string ShortName;
                        [JsonProperty("Количество предмета")] public int Amount;
                        [JsonProperty("ID скина предмета")] public ulong SkinID;
                        
                        [JsonProperty("Минимальное количество")] public int MinAmount;
                        [JsonProperty("Максимальное количество")] public int MaxAmount;
                    }

                    [JsonProperty("Выпадающая команда")]
                    public ExecuteCommand Command;
                    [JsonProperty("Выпадающий предмет")]
                    public ExecuteItem Item;
                }

                [JsonProperty("Выпавший предмет")] public Execute CaseInternal;
                [JsonProperty("Шанс выпадения предметов")] public int DropChance;
            }

            [JsonProperty("Ссылка на изображение кейса")]
            public string ImagePNG;
            [JsonProperty("Время для открытия кейса")]
            public int OpenTime;
            
            [JsonProperty("Список выпадающих предметов")]
            public List<CaseItem> DropItems = new List<CaseItem>(); 
        }

        private class PlayerInfo
      //  Слив плагинов server-rust by Apolo YouGame
        {
            internal class Inventory
            {
                [JsonProperty("Список выпавших предметов")]
                public List<SingleCase.CaseItem.Execute> WonItems = new List<SingleCase.CaseItem.Execute>();
            }

            internal class Complete
            {
                internal class Join
                {
                    [JsonProperty("День входа")]
                    public int DayNumber;
                    [JsonProperty("Месяц входа")]
                    public int MonthNumber;

                    public Join(int day, int month)
                    {
                        DayNumber = day;
                        MonthNumber = month;
                    }
                }
                internal class Left
                {
                    [JsonProperty("Оставшееся время")]
                    public int LeftTime;
                    [JsonProperty("Полное время")]
                    public int AllTime;

                    public Left(int day, int month)
                    {
                        LeftTime = day;
                        AllTime = month;
                    }
                }
                [JsonProperty("Дата в которую игрок зашёл")]
                public Join JoinDate = new Join(0, 0);
                [JsonProperty("Оставшееся время для получения")]
                public Left LeftTime = new Left(0, 0);
            }

            [JsonProperty("Текущий сбор кейсов игрока")]
            public List<Complete> Completes = new List<Complete>();
            [JsonProperty("Собранные игроком вещи")]
            public Inventory PlayerInventory;

            /// <summary>
            /// Получает текущий день
            /// </summary>
            /// <returns>Номер дня</returns>
            public int CurrentIndex() => Completes.Count;

            /// <summary>
            /// Возвращает оставшееся время за день
            /// </summary>
            /// <returns>Секунды до получения награды</returns>
            public int LeftTime() => Completes.Last().LeftTime.LeftTime;

            /// <summary>
            /// Закончил ли игрок получать награду
            /// </summary>
            /// <returns>True/False</returns>
            public bool Finished() => Completes.Last().LeftTime.LeftTime <= 0;

            /// <summary>
            /// Получил ли игрок награду
            /// </summary>
            /// <returns>True/Flase</returns>
            public bool GotPrize() => LeftTime() == -999;
        }

        #endregion
        
        #region Variables

        [PluginReference] 
        private Plugin ImageLibrary;

        [JsonProperty("Системное название слоя")]
        private string Layer = "UI_CaseInterface";
        [JsonProperty("Системное названия слоя с наградой")]
        private string PrizeLayer = "UI_PrizeInterface";

        [JsonProperty("Список открытых интерфейсов")]
        private List<ulong> OpenMenu = new List<ulong>();
        [JsonProperty("Загружен ли плагин?")]
        private bool PluginInitialized = false;
        [JsonProperty("Информация об игроках")]
        private Dictionary<ulong, PlayerInfo> PlayerInformation = new Dictionary<ulong, PlayerInfo>();
      //  Слив плагинов server-rust by Apolo YouGame
        [JsonProperty("Список доступных для игрока кейсов")]
        private List<SingleCase> CaseList = new List<SingleCase>
        {
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/xQYmxMo.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingAdmin",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/mrIEwoV.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingAdmin21",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/zYuuvIW.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingAdmin213",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/lEcEuYL.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingAdmi214e2n",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/ZfIYHpV.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingA2132112dmin",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/loq7wMH.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "GivingAasdsaddmin",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
            new SingleCase
            {
                OpenTime = 60 * 60,
                ImagePNG = "https://i.imgur.com/q1hq0Ku.png",
                DropItems = new List<SingleCase.CaseItem>
                {
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = new SingleCase.CaseItem.Execute.ExecuteItem
                            {
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0UL,
                                
                                MinAmount = 1,
                                MaxAmount = 1
                            },
                            Command = null
                        }
                    },
                    new SingleCase.CaseItem
                    {
                        DropChance = 50,
                        CaseInternal = new SingleCase.CaseItem.Execute
                        {
                            Item = null,
                            Command = new SingleCase.CaseItem.Execute.ExecuteCommand
                            {
                                Commands = new List<string>
                                {
                                    "o.grant user %STEAMID% admin",
                                    "rain 100"
                                },
                                ImagePNG = "Givsaf12ingAdmin",
                                ImageURL = "https://lh3.googleusercontent.com/eS496-RroU7sJwkojrcqCQoz8OuZb3E_qZRz8YttwQPdaJNaHld3D2ekxKPZHgrTuA"
                            }
                        }
                    },
                }
            },
        };
        
        [JsonProperty("Изображения плагина")]
        private Dictionary<string, string> PluginImages = new Dictionary<string, string>
        {
            ["BTN.RETURN"] = "https://i.imgur.com/PFaWY5i.png",
            ["BTN.CLOSE"] = "https://i.imgur.com/3znHdxL.png",
            
            ["CASE.DEFAULT"] = "https://i.imgur.com/fsGv9iK.png",
            ["CASE.FINISH"] = "https://i.imgur.com/OGf3rpj.png"
        };
        
        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("CCases/Players"))
            {
                PlayerInformation = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("CCases/Players");
      //  Слив плагинов server-rust by Apolo YouGame
               // Server.Broadcast(PlayerInformation[76561198121100397].Completes.Last().LeftTime.Item1.ToString());
      //  Слив плагинов server-rust by Apolo YouGame
            }
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("CCases/Cases"))
            {
                CaseList = Interface.Oxide.DataFileSystem.ReadObject<List<SingleCase>>("CCases/Cases");
            } 
            else
            {
                SaveData();
            }

            ValidateCases();
            
            timer.Every(59, () =>
            {
                foreach (var check in BasePlayer.activePlayerList)
                {
                    if (!PlayerInformation.ContainsKey(check.userID))
      //  Слив плагинов server-rust by Apolo YouGame
                    {
                        OnPlayerInit(check);
                    }
                    var playerInfo = PlayerInformation[check.userID];
      //  Слив плагинов server-rust by Apolo YouGame
                    
                    ValidatePlayer(check);
                    
                    if (playerInfo.Completes.Last().LeftTime.LeftTime > 0 )
      //  Слив плагинов server-rust by Apolo YouGame
                    {
                        playerInfo.Completes.Last().LeftTime.LeftTime -= 60;
      //  Слив плагинов server-rust by Apolo YouGame
                    }
                    if (playerInfo.Completes.Last().LeftTime.LeftTime < 0 && playerInfo.Completes.Last().LeftTime.LeftTime != -999)
      //  Слив плагинов server-rust by Apolo YouGame
                    {
                        check.ChatMessage("У вас появилась возможность открыть кейс!\n" +
                                          "Подробнее: /case");
                        playerInfo.Completes.Last().LeftTime.LeftTime = 0;
      //  Слив плагинов server-rust by Apolo YouGame
                    }

                    if (OpenMenu.Contains(check.userID))
                    {
                        BasePlayer target = BasePlayer.FindByID(check.userID);
                        if (target != null && target.IsConnected)
                        {
                            UI_MainInterface(target);
                        }
                    }
                }
            }).Callback();

            foreach (var check in PluginImages)
                ImageLibrary.Call("AddImage", check.Value, check.Key);
        }

        #endregion

        #region Hooks

        private void Unload() => SaveData();

        private void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerInformation.ContainsKey(player.userID))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                PlayerInfo playerInfo = new PlayerInfo();
      //  Слив плагинов server-rust by Apolo YouGame
                playerInfo.PlayerInventory = new PlayerInfo.Inventory();
      //  Слив плагинов server-rust by Apolo YouGame
                playerInfo.Completes = new List<PlayerInfo.Complete>();
      //  Слив плагинов server-rust by Apolo YouGame
                PlayerInformation.Add(player.userID, playerInfo);
      //  Слив плагинов server-rust by Apolo YouGame
            }

            ValidatePlayer(player);
        }

        #endregion

        #region Functions

        private void GetPrize(BasePlayer player, SingleCase.CaseItem.Execute execute)
        {
            if (execute.Command != null)
            {
                foreach (var check in execute.Command.Commands)
                    Server.Command(check.Replace("%STEAMID%", player.userID.ToString()));
            }
            else
            {
                if (player.inventory.containerMain.itemList.Count == 24)
                {
                    player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                       $"У вас не хватает места в основном инвентаре!");
                    return;
                }
                Item x = ItemManager.CreateByPartialName(execute.Item.ShortName, execute.Item.Amount);
                x.skin = execute.Item.SkinID;

                x.MoveToContainer(player.inventory.containerMain);
            }
            player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                               $"Вы успешно получили награду!");
            PlayerInformation[player.userID].PlayerInventory.WonItems.Remove(execute);
      //  Слив плагинов server-rust by Apolo YouGame
        }
        
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CCases/Cases", CaseList);
            Interface.Oxide.DataFileSystem.WriteObject("CCases/Players", PlayerInformation);
      //  Слив плагинов server-rust by Apolo YouGame
        }

        private void OpenCase(BasePlayer player, int caseNumber)
        {
            PlayerInfo playerInfo = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
            if (playerInfo.Completes.Count < caseNumber)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Вы не можете открывать недоступные кейсы, откройте предыдущий!");
                return;
            }

            var different = DifferentWithToday(playerInfo.Completes.Last().JoinDate);
      //  Слив плагинов server-rust by Apolo YouGame
            if (different != 0)
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Возможно ошибка в плагине, сообщите разработчикам #1!");
                ValidatePlayer(player);
                return;
            }

            if (playerInfo.Completes.Last().LeftTime.LeftTime < 0)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Вы уже открыли сегодня плагин, ожидайте начала нового дня!");
                return;
            }

           //PrintWarning($"Генерируем приз для: {CaseList.ElementAt(caseNumber).DropItems[0].CaseInternal.Item.ShortName}");
            var wonItem = GeneratePrize(CaseList.ElementAt(caseNumber));
            if (wonItem == null)
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Возможно ошибка в плагине, сообщите разработчикам #2!");
                return;
            }
            playerInfo.PlayerInventory.WonItems.Add(wonItem);
      //  Слив плагинов server-rust by Apolo YouGame
            playerInfo.Completes.Last().LeftTime.LeftTime = -999;
      //  Слив плагинов server-rust by Apolo YouGame

            UI_DrawReward(player, wonItem);
            timer.Once(0.5f, () => { UI_DrawReward(player, wonItem); });
        }

        private SingleCase.CaseItem.Execute GeneratePrize(SingleCase gCase)
        {
            var totalPercents = 0;
            gCase.DropItems.ForEach(p => { totalPercents += p.DropChance; });
            var generateNumber = Random.Range(0, totalPercents);
            totalPercents = 0;

            foreach (var check in gCase.DropItems)
            {
                if (totalPercents + check.DropChance >= generateNumber)
                {
                    SingleCase.CaseItem.Execute newCase = check.CaseInternal;
                    if (newCase.Item != null)
                    {
                        newCase.Item.Amount = Oxide.Core.Random.Range(newCase.Item.MinAmount, newCase.Item.MaxAmount);
                    }
                    return check.CaseInternal;
                }

                totalPercents += check.DropChance;
            }

            return null;
        }

        private void StartOpen(BasePlayer player)
        {
            PlayerInfo playerInfo = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
            if (playerInfo.Completes.Count == CaseList.Count)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Вы открыли все кейсы, как на счёт того, чтобы начать всё по новой?");
                playerInfo.Completes.Clear();
      //  Слив плагинов server-rust by Apolo YouGame
                ValidatePlayer(player);
                return;
            }
            var nCase = CaseList.ElementAt(playerInfo.Completes.Count);
      //  Слив плагинов server-rust by Apolo YouGame
            playerInfo.Completes.Add(new PlayerInfo.Complete()
      //  Слив плагинов server-rust by Apolo YouGame
            {
                LeftTime = new PlayerInfo.Complete.Left(nCase.OpenTime, nCase.OpenTime), 
      //  Слив плагинов server-rust by Apolo YouGame
                JoinDate = new PlayerInfo.Complete.Join(DateTime.Now.Day, DateTime.Now.Month)
      //  Слив плагинов server-rust by Apolo YouGame
            });
            
            UI_MainInterface(player);
        }

        private void ValidatePlayer(BasePlayer player)
        {
            PlayerInfo playerInfo = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
            if (playerInfo.Completes.Count == 0)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                StartOpen(player);
                return;
            }

            var different = DifferentWithToday(playerInfo.Completes.Last().JoinDate);
      //  Слив плагинов server-rust by Apolo YouGame
            if (different == 1 && playerInfo.Finished())
      //  Слив плагинов server-rust by Apolo YouGame
            {
                StartOpen(player);
            }
            else if (different == 1 && (!playerInfo.Finished() || !playerInfo.GotPrize()))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                player.ChatMessage($"<size=16>Система открытия <color=#4286f4>кейсов</color>:</size>\n" +
                                   $"Вы не успели открыть последний кейс, вы начинаете заново!");
                playerInfo.Completes.Clear();
      //  Слив плагинов server-rust by Apolo YouGame
                ValidatePlayer(player);
                return;
            }
        }

        private void ValidateCases()
        {
            if (CaseList.Count != 7)
            {
                PrintError("Недостаточное количество кейсов, вы не можете использовать плагин!");
                return;
            }

            for (int i = 0; i < CaseList.Count; i++)
            {
                SingleCase currentCase = CaseList.ElementAt(i);
                
                if (string.IsNullOrEmpty(currentCase.ImagePNG))
                {
                    PrintError($"Не установлено изображение для кейса #{i}, вы не можете использовать плагин!");
                    return;
                }
                else
                {
                    ImageLibrary.Call("AddImage", currentCase.ImagePNG, $"Case.{i}");
                    
                }

                if (currentCase.OpenTime <= 0)
                {
                    PrintError($"Время для открытия кейса #{i} меньше или равно нулю, вы не можете использовать плагин!");
                    return;
                }

                for (int t = 0; t < currentCase.DropItems.Count; t++)
                {
                    SingleCase.CaseItem currentPrize = currentCase.DropItems.ElementAt(t);
                    if (currentPrize.DropChance == 0)
                    {
                        PrintError($"CASE #{i} | У приза #{t}, шанс выпадения равен нулю, вы не можете использовать плагин!");
                        return;
                    }

                    if (currentPrize.CaseInternal.Item != null && currentPrize.CaseInternal.Item.MaxAmount < currentPrize.CaseInternal.Item.MinAmount)
                    {
                        PrintError($"CASE #{i} | У приза #{t}, максимальное количество меньше минимального, вы не можете использовать плагин!");
                        return;
                    }

                    if (currentPrize.DropChance == 0)
                    {
                        PrintError($"CASE #{i} | У приза #{t}, минимальное количество предметов равно 0, вы не можете использовать плагин!");
                        return;
                    }

                    SingleCase.CaseItem.Execute executePrize = currentPrize.CaseInternal;
                    if (executePrize.Command == null && executePrize.Item == null)
                    {
                        PrintError($"CASE #{i}, Приз #{t} | Награда пуста, вы не можете использовать плагин!");
                        return;
                    }

                    if (executePrize.Command != null && executePrize.Item != null)
                    {
                        PrintWarning($"CASE #{i}, Приз #{t} | Оба варианты награды заполнены, игрок получит обе награды!");
                    }

                    if (executePrize.Command != null && string.IsNullOrEmpty(executePrize.Command.ImagePNG))
                    {
                        PrintError($"CASE #{i}, Приз #{t} | Не установлено изображение для команды, вы не можете использовать плагин!");
                        return;
                    }

                    if (executePrize.Command != null)
                    {
                        ImageLibrary.Call("AddImage", executePrize.Command.ImageURL, executePrize.Command.ImagePNG);
                    }
                }
                
            }

            PluginInitialized = true;
            PrintWarning($"Плагин успешно проверен, всё хорошо!");
        }

        private int DifferentWithToday(PlayerInfo.Complete.Join currentDate)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            DateTime nowTime = DateTime.Now;
            var different = Math.Abs(nowTime.Day - currentDate.DayNumber);
            if (different > 1)
                return 999;
            if (Math.Abs(nowTime.Month - currentDate.MonthNumber) > 1)
                return 999;

            return different;
        }

        #endregion

        #region Commands

        [ChatCommand("case")]
        private void cmdChatCases(BasePlayer player, string command, string[] args)
        {
            if (OpenMenu.Contains(player.userID))
            {
                OpenMenu.Remove(player.userID);
            }
            
            OpenMenu.Add(player.userID);
            UI_MainInterface(player);
        }

        [ConsoleCommand("UI_CaseHandler")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            if (!args.HasArgs(1))
                return;

            BasePlayer player = args.Player();
            if (player == null)
                return;

            switch (args.Args[0].ToLower())
            {
                case "hide":
                {
                    if (OpenMenu.Contains(player.userID))
                        OpenMenu.Remove(player.userID);
                    
                    break;
                }
                case "open":
                {
                    if (OpenMenu.Contains(player.userID))
                        OpenMenu.Remove(player.userID);
                    
                    PlayerInfo playerInfo = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
                    OpenCase(player, playerInfo.CurrentIndex() - 1);
      //  Слив плагинов server-rust by Apolo YouGame
                    break;
                }
                case "inventory":
                {
                    if (OpenMenu.Contains(player.userID))
                        OpenMenu.Remove(player.userID);
                    
                    if (args.HasArgs(2))
                        UI_InventoryInterface(player, Convert.ToInt32(args.Args[1]));
                    else
                        UI_InventoryInterface(player);
                    break;
                }
                case "take":
                {
                    if (OpenMenu.Contains(player.userID))
                        OpenMenu.Remove(player.userID);
                    
                    GetPrize(player, PlayerInformation[player.userID].PlayerInventory.WonItems.ElementAt(Convert.ToInt32(args.Args[1])));
      //  Слив плагинов server-rust by Apolo YouGame
                    UI_InventoryInterface(player, Convert.ToInt32(args.Args[2]));
                    break;
                }
            }
        }

        #endregion

        #region UI
        
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private void UI_DrawReward(BasePlayer player, SingleCase.CaseItem.Execute prize)
        {
            CuiHelper.DestroyUi(player, PrizeLayer);
            CuiHelper.DestroyUi(player, Layer + ".Open");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = PrizeLayer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#383E3838") },
                    new CuiRectTransformComponent { AnchorMin = "0.4079431 0.1378999", AnchorMax = "0.5920569 0.500001", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = PrizeLayer,
                Name = Layer + ".Icon",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#5E82605C") },
                    new CuiRectTransformComponent { AnchorMin = "0.02857167 0.1857783", AnchorMax = "0.9714283 0.9752498", OffsetMax = "0 0" }
                }
            });

            if (prize.Command != null)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Icon",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", prize.Command.ImagePNG) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Icon",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", prize.Item.ShortName) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });
            }
            
            
            container.Add(new CuiElement
            {
                Name = PrizeLayer + ".Get",
                Parent = PrizeLayer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#b2b2b24A") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.17", OffsetMax = "-5 -3", OffsetMin = "5 3"}
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = "Принять", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#afafafFF") }
            }, PrizeLayer + ".Get");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Command = "chat.say /case", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, PrizeLayer + ".Get");

            CuiHelper.AddUi(player, container);
        }

        private void UI_InventoryInterface(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            PlayerInfo playerCases = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.128711 0.1692131", AnchorMax = "0.871289 0.9363427", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Color = HexToRustFormat("#3B3E3DD7"), Command = "UI_CloseCases" },
                Text = { Text = "" }
            }, Layer);
            
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.9" },
                    new CuiRectTransformComponent { AnchorMin = "-10 -10", AnchorMax = "10 10", OffsetMax = "0 0" }
                }
            });



            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#7C7D7A52") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8934819", AnchorMax = "1 0.9815931", OffsetMax = "0 0" },
                Text = { Text = $"ИНВЕНТАРЬ {page + 1} / {(PlayerInformation[player.userID].PlayerInventory.WonItems.Count / 15) + 1}", Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#B4B4B4FF") }
      //  Слив плагинов server-rust by Apolo YouGame
            }, Layer);

            int i = 0;
            foreach (var currentCase in PlayerInformation[player.userID].PlayerInventory.WonItems.Skip(page * 15).Take(15))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = PrizeLayer,
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#383E3838") },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.13312219 + i * 0.16 - Math.Floor((double) i / 5) * 5 * 0.16} {0.6513031 - Math.Floor((double) i / 5) * 0.26}", 
                            AnchorMax = $"{0.2383299 + i * 0.16 - Math.Floor((double) i / 5)* 5 * 0.16} {0.8776157 - Math.Floor((double) i / 5) * 0.26}", 
                            OffsetMax = "0 0"
                        }
                    }
                });
        
                container.Add(new CuiElement
                {
                    Parent = PrizeLayer,
                    Name = Layer + ".Icon",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#b6b6b662") },
                        new CuiRectTransformComponent { AnchorMin = "0.02857167 0.1857783", AnchorMax = "0.9714283 0.9752498", OffsetMax = "0 0" }
                    }
                });
        
                if (currentCase.Command != null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Icon",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", currentCase.Command.ImagePNG) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Icon",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", currentCase.Item.ShortName) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                        }
                    });
                    
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Icon",
                        Components =
                        {
                            new CuiTextComponent { Text = currentCase.Item.Amount + " шт.", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.LowerRight },
                            new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-100 0", OffsetMax = "0 50" }
                        }
                    });
                } 
                
                container.Add(new CuiElement
                {
                    Name = PrizeLayer + ".Get",
                    Parent = PrizeLayer,
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#b2b2b24A") },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.17", OffsetMax = "-3 -1", OffsetMin = "3 1"}
                    }
                });
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = "ЗАБРАТЬ", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#afafafFF") }
                }, PrizeLayer + ".Get");
        
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Command = $"UI_CaseHandler take {(page * 15) + i} {page}", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, PrizeLayer + ".Get");

                i++;
            }
            
            #region Отрисовываем кнопки

            #region Вернуться в меню

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BTN",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "BTN.RETURN") },
                    new CuiRectTransformComponent { AnchorMin = "0.005531466 0.9502114", AnchorMax = "0.03989122 0.9924564" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Command = "chat.say /case", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".BTN");

            #endregion

            #region Закрыть панель

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BTN",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "BTN.CLOSE") },
                    new CuiRectTransformComponent { AnchorMin = "0.9699283 0.9502114", AnchorMax = "0.9944767 0.991" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Command = "UI_CaseHandler hide", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".BTN");

            #endregion

            #endregion

            #region Отрисовываем кнопки влево/вправо

            if (page != 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.02761696 0.0328906", AnchorMax = "0.1286165 0.1035001", OffsetMax = "0 0" },
                    Button = { Command = $"UI_CaseHandler inventory {page-1}", Color = HexToRustFormat("#b2b2b252") },
                    Text = { Text = "НАЗАД", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#afafafFF") }
                }, Layer);
            }
            
            if (page + 1 < (int) Math.Ceiling((double) PlayerInformation[player.userID].PlayerInventory.WonItems.Count / 15))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.88 0.0328906", AnchorMax = "0.98 0.1035001", OffsetMax = "0 0" },
                    Button = { Command = $"UI_CaseHandler inventory {page+1}", Color = HexToRustFormat("#b2b2b252") },
                    Text = { Text = "ВПЕРЕД", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#afafafFF") }
                }, Layer);
            }

            #endregion

            CuiHelper.AddUi(player, container);
        }

        private void UI_MainInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            PlayerInfo playerCases = PlayerInformation[player.userID];
      //  Слив плагинов server-rust by Apolo YouGame
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.128711 0.1692131", AnchorMax = "0.871289 0.9363427", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Color = HexToRustFormat("#3B3E3DD7"), Command = "UI_CloseCases" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.9" },
                    new CuiRectTransformComponent { AnchorMin = "-10 -10", AnchorMax = "10 10", OffsetMax = "0 0" }
                }
            });
            

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#7C7D7A52") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8934819", AnchorMax = "1 0.9815931", OffsetMax = "0 0" },
                Text = { Text = "ИГРАЙТЕ НА НАШЕМ СЕРВЕРЕ КАЖДЫЙ ДЕНЬ И ПОЛУЧАЙТЕ НАГРАДЫ", Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#B4B4B4FF") }
            }, Layer);
            
            
            

           container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8934819", AnchorMax = "1 0.9815931", OffsetMax = "0 0" },
                Text = { Text = "ИГРАЙТЕ НА НАШЕМ СЕРВЕРЕ КАЖДЫЙ ДЕНЬ И ПОЛУЧАЙТЕ НАГРАДЫ", Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#B4B4B4FF") }
            }, Layer);

            #region Отрисовываем кейсы

            for (int i = 0; i < 7; i++)
            {
                SingleCase currentCase = CaseList.ElementAt(i);
                string BGCase = i != 6 ? (string) ImageLibrary.Call("GetImage", "CASE.DEFAULT") : (string) ImageLibrary.Call("GetImage", "CASE.FINISH");
                string BGColor = i + 1 == playerCases.CurrentIndex() ? HexToRustFormat("#838383CB") : HexToRustFormat("#6A6A6ACB");
                string dText = $"День {i + 1}";
                if (i == 6)
                {
                    dText = "Особая награда";
                }
                else if (i + 1 == playerCases.CurrentIndex() && playerCases.LeftTime() != -999)
                {
                    if (playerCases.Finished())
                    {
                        dText = "ОТКРЫТЬ";
                    }
                    else
                    {
                        dText = $"Осталось:\n" +
                                $"{TimeSpan.FromSeconds(playerCases.LeftTime()).Minutes} минут";
                    }
                }
                else
                {
                    BGColor = HexToRustFormat("#6A6A6ACB");
                }

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $".{i}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = BGCase, Color = BGColor },
                        new CuiRectTransformComponent { AnchorMin = $"{0.0844302 + i * 0.12} 0.6054314", AnchorMax = $"{0.2064708 + i * 0.12} 0.7366505", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + $".{i}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", $"Case.{i}") },
                        new CuiRectTransformComponent { AnchorMin = $"0.2672338 0.1025643", AnchorMax = $"0.7844768 0.8717952", OffsetMax = "0 0" }
                    }
                });

                if (playerCases.Finished() && playerCases.CurrentIndex() == i + 1 && playerCases.LeftTime() != -999)
                {
                    string anchorMin = $"0 -0.3589745";
                    string anchorMax = $"0.7844768 -0.05128074";

                    if (i == 6)
                    {
                        anchorMin = $"0 -0.3589745";
                        anchorMax = $"1 -0.05128074";
                    }
                    
                    container.Add(new CuiElement
                    {
                        Name = Layer + ".Open",
                        Parent = Layer + $".{i}",
                        Components =
                        {
                            new CuiImageComponent { Color = HexToRustFormat("#838383CB") },
                            new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMax = "0 0" }
                        }
                    });
                    
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = dText, Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#B4B4B4FF") }
                    }, Layer + ".Open");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Command = "UI_CaseHandler open", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, Layer + ".Open");
                }
                else
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 -1", AnchorMax = "1 -0.1", OffsetMax = "0 0" },
                        Text = { Text = dText, Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#B4B4B4FF") }
                    }, Layer + $".{i}");
                }
            }

            #endregion

            #region Отрисовываем оставшееся время

            if (playerCases.GotPrize())
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 0.55", OffsetMax = "0 0" },
                    Text = 
                    { 
                        Text = $"<size=20>ДО СЛЕДУЮЩЕГО ДНЯ ОСТАЛОСЬ:</size>\n" +
                               $"<size=18>{23 - DateTime.Now.Hour} ЧАСОВ {59 - DateTime.Now.Minute} МИНУТ</size>\n" +
                               $"\n" +
                               $"ПОЛУЧАЙТЕ НАГРАДУ КАЖДЫЙ ДЕНЬ, ЧТОБЫ ПОЛУЧИТЬ ДОСТУП К БОЛЕЕ ЦЕННЫМ КЕЙСАМ",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#afafafFF")
                    }
                }, Layer);        
            }

            #endregion

            #region Отрисовываем инвентарь

            container.Add(new CuiElement
            {
                Name = Layer + ".Inventory",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#b2b2b24A") },
                    new CuiRectTransformComponent { AnchorMin = "0.3900579 0.03108118", AnchorMax = "0.6099421 0.1035019", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"ИНВЕНТАРЬ", Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#afafafFF") }
            }, Layer + ".Inventory");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Command = "UI_CaseHandler inventory", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".Inventory");
            

            #endregion
            
            #region Отрисовываем кнопки

            #region Вернуться в меню

          /*  container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BTN",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "BTN.RETURN") },
                    new CuiRectTransformComponent { AnchorMin = "0.005531466 0.9502114", AnchorMax = "0.03989122 0.9924564" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".BTN");*/

            #endregion

            #region Закрыть панель

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BTN",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "BTN.CLOSE") },
                    new CuiRectTransformComponent { AnchorMin = "0.9699283 0.9502114", AnchorMax = "0.9944767 0.991" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Command = "UI_CaseHandler hide", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".BTN");

            #endregion

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Utils



        #endregion
    }
}
