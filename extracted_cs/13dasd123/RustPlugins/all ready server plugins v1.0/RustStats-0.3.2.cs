using System.Collections.Generic;
using UnityEngine;
using Rust;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System.Reflection;
using System;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("RustStats", "Own3r", "0.3.2")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("GUI Top and Stats")]

    class RustStats : RustPlugin
    {
        #region OtherStaff
        [PluginReference]
        Plugin ImageLibrary, AspectRatio;

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public string GetUserAspectRatio(ulong userId) => (string)AspectRatio.Call("GetUserAspectRatio", userId);

        Dictionary<ulong, int> PVPKill = new Dictionary<ulong, int>();
        Dictionary<ulong, int> PVPDeath = new Dictionary<ulong, int>();
        Dictionary<ulong, int> Gather = new Dictionary<ulong, int>();
        Dictionary<ulong, int> QuarryGather = new Dictionary<ulong, int>();
        Dictionary<ulong, int> Time = new Dictionary<ulong, int>();

        public ulong playeruserID;

        public string KillMsg, DeathMsg, GatherMsg, QuarryGatherMsg, TimeMsg, KillValue, DeathValue, GatherValue, QuarryGatherValue, TimeValue, playerdisplayName;

        public int Hours, Min, iTop;

        class DataStorage
        {
            public Dictionary<ulong, STATSDATA> RustStatsData = new Dictionary<ulong, STATSDATA>();
            public DataStorage() { }
        }

        class STATSDATA
        {
            public string Name;
            public int Time;
            public int Coins;
            public int Kill;
            public int Death;
            public int PVEDeath;
            public int Suicide;

            public int Wood;
            public int Stone;
            public int SulfurOre;
            public int MetalOre;
            public int HighMetalOre;

            public int QuarryStone;
            public int QuarrySulfurOre;
            public int QuarryMetalOre;
            public int QuarryHighMetalOre;
        }
        DataStorage data;
        private DynamicConfigFile StatsData;
        void OnServerInitialized()
        {
            StatsData = Interface.Oxide.DataFileSystem.GetFile("RustStats");
            LoadData();

            try
            {
                ImageLibrary.Call("isLoaded", null);
            }
            catch (Exception)
            {
                PrintWarning("No Image Library.. load ImageLibrary to use this Plugin", Name);
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            AddImage("https://i.imgur.com/FK4ku3e.png", "pickaxe");
            AddImage("https://i.imgur.com/voHQmCL.png", "quarry");
            AddImage("https://i.imgur.com/ghWa8OZ.png", "kill");
            AddImage("https://i.imgur.com/2Lr7EBf.png", "death");
            AddImage("https://i.imgur.com/UvFR1Fp.png", "time");


            timer.Repeat(60f, 0, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    data.RustStatsData[player.userID].Time = data.RustStatsData[player.userID].Time + 1;
                }
                SaveData();
            });
        }

        void SaveData()
        {
            StatsData.WriteObject(data);
        }

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("RustStats");
            }

            catch
            {
                data = new DataStorage();
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!data.RustStatsData.ContainsKey(player.userID))
            {
                data.RustStatsData.Add(player.userID, new STATSDATA()
                {
                    Name = player.displayName,
                    Time = 0,
                    Coins = 0,
                    Wood = 0,
                    Stone = 0,
                    SulfurOre = 0,
                    MetalOre = 0,
                    HighMetalOre = 0,

                    Kill = 0,
                    Death = 0,
                    PVEDeath = 0,
                    Suicide = 0,

                    QuarryStone = 0,
                    QuarryMetalOre = 0,
                    QuarrySulfurOre = 0,
                    QuarryHighMetalOre = 0
                });
            }
            else { data.RustStatsData[player.userID].Name = player.displayName.ToString(); }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                ChatDestroyGui(player);
            }
            SaveData();
        }

        #endregion

        #region Resource Stats
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            if (item.info.shortname == "wood")
            {
                data.RustStatsData[player.userID].Wood = data.RustStatsData[player.userID].Wood + item.amount;
            }
            if (item.info.shortname == "stones")
            {
                data.RustStatsData[player.userID].Stone = data.RustStatsData[player.userID].Stone + item.amount;
            }
            if (item.info.shortname == "sulfur.ore")
            {
                data.RustStatsData[player.userID].SulfurOre = data.RustStatsData[player.userID].SulfurOre + item.amount;
            }
            if (item.info.shortname == "metal.ore")
            {
                data.RustStatsData[player.userID].MetalOre = data.RustStatsData[player.userID].MetalOre + item.amount;
            }
            if (item.info.shortname == "hq.metal.ore")
            {
                data.RustStatsData[player.userID].HighMetalOre = data.RustStatsData[player.userID].HighMetalOre + item.amount;
            }
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (item.info.shortname == "wood")
            {
                data.RustStatsData[player.userID].Wood = data.RustStatsData[player.userID].Wood + item.amount;
            }
            if (item.info.shortname == "stones")
            {
                data.RustStatsData[player.userID].Stone = data.RustStatsData[player.userID].Stone + item.amount;
            }
            if (item.info.shortname == "sulfur.ore")
            {
                data.RustStatsData[player.userID].SulfurOre = data.RustStatsData[player.userID].SulfurOre + item.amount;
            }
            if (item.info.shortname == "metal.ore")
            {
                data.RustStatsData[player.userID].MetalOre = data.RustStatsData[player.userID].MetalOre + item.amount;
            }
            if (item.info.shortname == "hq.metal.ore")
            {
                data.RustStatsData[player.userID].HighMetalOre = data.RustStatsData[player.userID].HighMetalOre + item.amount;
            }
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {

            if (item.info.shortname == "wood")
            {
                data.RustStatsData[player.userID].Wood = data.RustStatsData[player.userID].Wood + item.amount;
            }
            if (item.info.shortname == "stones")
            {
                data.RustStatsData[player.userID].Stone = data.RustStatsData[player.userID].Stone + item.amount;
            }
            if (item.info.shortname == "sulfur.ore")
            {
                data.RustStatsData[player.userID].SulfurOre = data.RustStatsData[player.userID].SulfurOre + item.amount;
            }
            if (item.info.shortname == "metal.ore")
            {
                data.RustStatsData[player.userID].MetalOre = data.RustStatsData[player.userID].MetalOre + item.amount;
            }
        }
        #endregion

        #region Quarry Stats
        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (quarry.OwnerID.ToString() == "0") return;
            playeruserID = quarry.OwnerID;

            if (item.info.shortname == "stones")
            {
                data.RustStatsData[playeruserID].QuarryStone = data.RustStatsData[playeruserID].QuarryStone + item.amount;
            }
            if (item.info.shortname == "sulfur.ore")
            {
                data.RustStatsData[playeruserID].QuarrySulfurOre = data.RustStatsData[playeruserID].QuarrySulfurOre + item.amount;
            }
            if (item.info.shortname == "metal.ore")
            {
                data.RustStatsData[playeruserID].QuarryMetalOre = data.RustStatsData[playeruserID].QuarryMetalOre + item.amount;
            }
            if (item.info.shortname == "hq.metal.ore")
            {
                data.RustStatsData[playeruserID].QuarryHighMetalOre = data.RustStatsData[playeruserID].QuarryHighMetalOre + item.amount;
            }

        }
        #endregion

        #region PVE PVP SUICIDE Stats
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)                
      //  Слив плагинов server-rust by Apolo YouGame
		{            
            if (entity == null || info?.Initiator == null || entity is NPCPlayerApex) return;	
			if (entity is BasePlayer && info?.Initiator is BasePlayer)					
            {
                var victim = entity.ToPlayer();
                var attacker = info.Initiator.ToPlayer();

                if (victim.userID == attacker.userID)
                {
                    data.RustStatsData[victim.userID].Suicide++;
                    return;
                }
                else
                {
                    data.RustStatsData[attacker.userID].Coins++;
                    data.RustStatsData[attacker.userID].Kill++;
                    data.RustStatsData[victim.userID].Death++;
                }
            }

            if (entity is BasePlayer && !(info?.Initiator is BasePlayer))
            {
                var victim = entity.ToPlayer();
                data.RustStatsData[victim.userID].PVEDeath++;
            }
        }
        #endregion

        #region GUI TOP AND STATS
        [ChatCommand("top")]
        void CreateUI(BasePlayer player)
        {
            CuiElementContainer Container = new CuiElementContainer();
            string UserAspectRatio = (string)(GetUserAspectRatio(player.userID) ?? string.Empty);
            if (UserAspectRatio == "16x9") { CreateGui(player, Container, "0 0", "1 1"); }
            else if(UserAspectRatio == "16x10") { CreateGui(player, Container, "0 0.02", "1 1"); }
            else if (UserAspectRatio == "4x3") { CreateGui(player, Container, "0 0.2", "1 1"); }
            else if (UserAspectRatio == "5x4") { CreateGui(player, Container, "0 0.22", "1 1"); }
        }


        void CreateGui(BasePlayer player, CuiElementContainer Container, string AnchorMin, string AnchorMax)
        {
            ChatDestroyGui(player);
            GetKillTop();
            GetDeathTop();
            GetGatherTop();
            GetQuarryGatherTop();
            GetPlayTime();

            Hours = data.RustStatsData[player.userID].Time / 60;
            Min = data.RustStatsData[player.userID].Time % 60;

            Container.Add(new CuiElement
            {
                Name = "AspectRatioMain",
                Parent = "Hud",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = AnchorMin,
                            AnchorMax = AnchorMax
                        },
                        new CuiNeedsCursorComponent()
                    }
            });
            string TitlePanelName = CuiHelper.GetGuid();
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.5"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.075 0.15",
                            AnchorMax = "0.325 0.89"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<size=24>{player.displayName}</size>\n<color=#ECBE13>{Hours} час(а) {Min} минут(у)</color>",
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.135 0.15",
                            AnchorMax = "0.265 0.87"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"\nPVP Убийств: \nPVP Смертей: \nPVE Смертей: \nСуицидов: \n\n\n\n\nДерево: \nКамень: \nЖелезная руда: \nСерная руда: \nHQM Руда: \n\n\n\n\nКамень: \nЖелезная руда: \nСерная руда: \nHQM Руда: \n\n\n\nМонет: ",
                            Align = TextAnchor.UpperLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.135 0.15",
                            AnchorMax = "0.265 0.77"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"\n<color=#ECBE13>{data.RustStatsData[player.userID].Kill}\n{data.RustStatsData[player.userID].Death}\n{data.RustStatsData[player.userID].PVEDeath}\n{data.RustStatsData[player.userID].Suicide}\n\n\n\n\n{data.RustStatsData[player.userID].Wood}\n{data.RustStatsData[player.userID].Stone}\n{data.RustStatsData[player.userID].MetalOre}\n{data.RustStatsData[player.userID].SulfurOre}\n{data.RustStatsData[player.userID].HighMetalOre}\n\n\n\n\n{data.RustStatsData[player.userID].QuarryStone}\n{data.RustStatsData[player.userID].QuarryMetalOre}\n{data.RustStatsData[player.userID].QuarrySulfurOre}\n{data.RustStatsData[player.userID].QuarryHighMetalOre}\n\n\n\n{data.RustStatsData[player.userID].Coins}</color>",
                            Align = TextAnchor.UpperRight
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.135 0.15",
                            AnchorMax = "0.265 0.77"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("pickaxe")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.145 0.59",
                        AnchorMax = "0.175 0.64"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ДОБЫТО РУКАМИ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.165 0.59",
                        AnchorMax = "0.265 0.64"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("quarry")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.140 0.39",
                        AnchorMax = "0.170 0.44"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ДОБЫТО КАРЬЕРОМ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.165 0.39",
                        AnchorMax = "0.265 0.44"
                        }
                    }
            });



            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.5"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.375 0.15",
                            AnchorMax = "0.625 0.9"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<size=24>ТОП ПО PVP</size>",
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.375 0.15",
                            AnchorMax = "0.625 0.875"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"\n\n{KillMsg}\n\n\n\n{DeathMsg}",
                            Align = TextAnchor.UpperLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.415 0.15",
                            AnchorMax = "0.585 0.81"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<color=#ECBE13>\n\n{KillValue}\n\n\n\n{DeathValue}</color>",
                            Align = TextAnchor.UpperRight
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.415 0.15",
                            AnchorMax = "0.585 0.81"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"{TimeMsg}",
                            Align = TextAnchor.UpperLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.4 0.15",
                            AnchorMax = "0.6 0.3"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<color=#ECBE13>{TimeValue}</color>",
                            Align = TextAnchor.UpperRight
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.4 0.15",
                            AnchorMax = "0.6 0.3"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("kill")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.455 0.78",
                        AnchorMax = "0.490 0.835"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"УБИЙЦЫ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.45 0.75",
                        AnchorMax = "0.585 0.85"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("death")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.460 0.590",
                        AnchorMax = "0.485 0.640"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ЖЕРТВЫ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.445 0.575",
                        AnchorMax = "0.58 0.65"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("time")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.44 0.3325",
                        AnchorMax = "0.465 0.3825"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ТОП ПО ВРЕМЕНИ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.440 0.15",
                        AnchorMax = "0.590 0.565"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.5"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.675 0.35",
                            AnchorMax = "0.925 0.9"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<size=24>ТОП ПО РЕСУРСАМ</size>",
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.7 0.80",
                            AnchorMax = "0.9 0.875"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"\n\n{GatherMsg}\n\n\n\n{QuarryGatherMsg}",
                            Align = TextAnchor.UpperLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.715 0.15",
                            AnchorMax = "0.885 0.81"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"<color=#00C2FF>\n\n{GatherValue}\n\n\n\n{QuarryGatherValue}</color>",
                            Align = TextAnchor.UpperRight
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.715 0.15",
                            AnchorMax = "0.885 0.81"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("pickaxe")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.745 0.780",
                        AnchorMax = "0.775 0.830"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ДОБЫТО РУКАМИ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.725 0.75",
                        AnchorMax = "0.905 0.85"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("quarry")
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.740 0.585",
                        AnchorMax = "0.77 0.635"
                        }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components = {
                        new CuiTextComponent {
                            Text = $"ДОБЫТО КАРЬЕРОМ",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.725 0.575",
                        AnchorMax = "0.905 0.65"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=18>ЗАКРЫТЬ</size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.675 0.15",
                        AnchorMax = "0.925 0.325"
                    }
                }
            });
            Container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "AspectRatioMain",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = $"statsclose {player}",
                        Color = "0 0 0 0.5",
                        
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.675 0.15",
                        AnchorMax = "0.925 0.325"
                    },
                }
            });

            CuiHelper.AddUi(player, Container);
        }

        [ConsoleCommand("statsclose")]
        void CmdDestroyGui(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "AspectRatioMain");
        }

        [ChatCommand("statsclose")]
        void ChatDestroyGui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "AspectRatioMain");
        }
        #endregion

        #region TOP
        public string GetKillTop()
        {
            KillMsg = "";
            KillValue = "";
            PVPKill.Clear();
            foreach (var player in data.RustStatsData)
            {
                PVPKill[player.Key] = player.Value.Kill;
            }
            iTop = 0;
            foreach (var rank in PVPKill.OrderByDescending(rank => rank.Value))
            {
                playeruserID = rank.Key;
                playerdisplayName = data.RustStatsData[playeruserID].Name;
                iTop++;
                KillMsg += ($"{iTop}. {playerdisplayName}:\n");
                KillValue += ($"{rank.Value}\n");
                if (iTop == 5) break;
            }
            return KillMsg;
        }
        public string GetDeathTop()
        {
            DeathMsg = "";
            DeathValue = "";
            PVPDeath.Clear();
            foreach (var player in data.RustStatsData)
            {
                PVPDeath[player.Key] = player.Value.Death;
            }
            iTop = 0;
            foreach (var rank in PVPDeath.OrderByDescending(rank => rank.Value))
            {
                playeruserID = rank.Key;
                playerdisplayName = data.RustStatsData[playeruserID].Name;
                iTop++;
                DeathMsg += ($"{iTop}. {playerdisplayName}:\n");
                DeathValue += ($"{rank.Value}\n");
                if (iTop == 5) break;
            }
            return DeathMsg;
        }
        public string GetGatherTop()
        {
            GatherMsg = "";
            GatherValue = "";
            Gather.Clear();
            foreach (var player in data.RustStatsData)
            {
                Gather[player.Key] = player.Value.Stone + player.Value.Wood + player.Value.SulfurOre + player.Value.MetalOre + player.Value.HighMetalOre;
            }
            iTop = 0;
            foreach (var rank in Gather.OrderByDescending(rank => rank.Value))
            {
                playeruserID = rank.Key;
                playerdisplayName = data.RustStatsData[playeruserID].Name;
                iTop++;
                GatherMsg += ($"{iTop}. {playerdisplayName}:\n");
                GatherValue += ($"{rank.Value}\n");
                if (iTop == 5) break;
            }
            return GatherMsg;
        }
        public string GetQuarryGatherTop()
        {
            QuarryGatherMsg = "";
            QuarryGatherValue = "";
            QuarryGather.Clear();
            foreach (var player in data.RustStatsData)
            {
                QuarryGather[player.Key] = player.Value.QuarryStone + player.Value.QuarrySulfurOre + player.Value.QuarryMetalOre + player.Value.QuarryHighMetalOre;
            }
            iTop = 0;
            foreach (var rank in QuarryGather.OrderByDescending(rank => rank.Value))
            {
                playeruserID = rank.Key;
                playerdisplayName = data.RustStatsData[playeruserID].Name;
                iTop++;
                QuarryGatherMsg += ($"{iTop}. {playerdisplayName}:\n");
                QuarryGatherValue += ($"{rank.Value}\n");
                if (iTop == 5) break;
            }
            return QuarryGatherMsg;
        }

        public string GetPlayTime()
        {
            TimeMsg = "";
            TimeValue = "";
            Time.Clear();
            foreach (var player in data.RustStatsData)
            {
                Time[player.Key] = player.Value.Time;
            }
            iTop = 0;
            foreach (var rank in Time.OrderByDescending(rank => rank.Value))
            {
                playeruserID = rank.Key;
                playerdisplayName = data.RustStatsData[playeruserID].Name;
                iTop++;
                if (rank.Value <= 60)
                {
                    TimeMsg += ($"{iTop}. {playerdisplayName}:\n");
                    TimeValue += ($"{rank.Value} минут(у)\n");
                }
                else
                {
                    Hours = rank.Value / 60;
                    Min = rank.Value % 60;
                    TimeMsg += ($"{iTop}. {playerdisplayName}: \n");
                    TimeValue += ($"{Hours} час(а) {Min} минут(у)\n");
                }
                if (iTop == 5) break;
            }
            return TimeMsg;
        }
        #endregion
    }
}
