//Reference: WishInfrastructure
#define DEBUG
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WishInfrastructure;
using WishInfrastructure.Models;


//WishStatistics created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("WishStatistics", "Latvish&Zurius", "0.0.2")]
    [Description("WishStatistics")]
    public partial class WishStatistics : RustPlugin
    {
        #region ChatCommands.cs
        [ChatCommand("statistics")]
        private void stats(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }
        [ChatCommand("stats")]
        private void stats1(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }
        
        [ConsoleCommand("stats_close")]
        private void stats_close(ConsoleSystem.Arg arg)
        {
            _guiService.DestroyGui(BasePlayer.FindByID(ulong.Parse(arg.Args[0])));
        }
        [ConsoleCommand("remove_points_clan")]
        private void remove_points_clan(ConsoleSystem.Arg arg)
        {
            Database.SetClanData(arg.Args[0], arg.Args[1], Database.GetClanDataRaw<int>(arg.Args[0], arg.Args[1]) - int.Parse(arg.Args[2]));
        }
        #endregion

        #region WishStatistics.cs
        private ConfigSetup _config;
        private GuiService _guiService;
        
        public static DatabaseClient Database { get; set; }
        public ClanGuiService _clanGuiService { get; set; }
        void Init()
        {
            Interface.Oxide.LogDebug("Starting statistics plugin");
            _config = new ConfigSetup(this);
            
            SubscribeToEvents();
            InitInfrastructure();
            _guiService = new GuiService(Database);
            _clanGuiService = new ClanGuiService(_config, Database);
            timer.Every(30, () =>
            {
                Interface.Oxide.LogDebug("Updating clan scores");
                _clanGuiService.UpdateClanUI();
            });
            
        }
        
        private void InitInfrastructure()
        {
            Subscribe("OnServerSave");
            Subscribe("OnUserConnected");
            Database = new DatabaseClient("WishStats", "WishStatsClans", this, _config.ConfigFile.DatabaseConfig);
            Database.SetupDatabase();
        }
        
        private void SubscribeToEvents()
        {
            Subscribe("OnBigWheelWin");
            Subscribe("OnBigWheelLoss");
            Subscribe("CanMoveItem");
            Subscribe("CanLootEntity");
            Subscribe("OnItemSplit");
        }
        
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }
        #endregion

        #region EventListeners\DatabaseSaveEvents.cs
        void OnServerSave()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Performing database save WishStatistics");
            
            Database.SaveDatabase();
            
            stopwatch.Stop();
            Interface.Oxide.LogDebug($"END Performing database save WishStatistics - {stopwatch.ElapsedMilliseconds}ms");
        }
        
        void OnUserConnected(IPlayer player)
        {
            if (!Database.IsKnownPlayer(player.Id))
            {
                Database.LoadPlayer(player.Id);
            }
            
            Database.SetPlayerData(player.Id, "name", player.Name);
            
        }
        #endregion

        #region EventListeners\PlayerEvents.cs
        //Scrap Gambling
        void OnBigWheelLoss(BigWheelGame wheel, Item scrap)
        {
            Database.SetPlayerData(scrap.text.ToString(), "ScrapLost", Database.GetPlayerDataRaw<int>(scrap.text, "ScrapLost") + scrap.amount);
        }
        object OnBigWheelWin(BigWheelGame wheel, Item scrap, BigWheelBettingTerminal terminal, int multiplier)
        {
            Database.SetPlayerData(scrap.text.ToString(), "ScrapWon", Database.GetPlayerDataRaw<int>(scrap.text, "ScrapWon") + scrap.amount);
            return null;
        }
        
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item.info.shortname == "scrap")
            {
                item.text = playerLoot.baseEntity.UserIDString;
            }
            return null;
        }
        Item OnItemSplit(Item item, int amount)
        {
            if (item.info.shortname == "scrap")
            {
                Item newItem = ItemManager.CreateByItemID(item.info.itemid, amount, 0uL);
                item.amount -= amount;
                newItem.text = item.text;
                return newItem;
            }
            return null;
        }
        //Scrap Gaming End
        
        
        //Kills & Deaths
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            // Check for null or NPC
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            // If Suicide Tracking Enabled Ingnore Death
            if (player == info?.Initiator) return null;
            
            Database.SetPlayerData(player.UserIDString.ToString(), "Deaths", Database.GetPlayerDataRaw<int>(player.UserIDString, "Deaths") + 1);
            
            var attacker = info.InitiatorPlayer;
            if (attacker != null || attacker.userID.IsSteamId())
            {
                Database.SetPlayerData(attacker.UserIDString.ToString(), "Kills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Kills") + 1);
                if (attacker.Team != null && attacker?.Team?.teamID != player?.Team?.teamID)
                {
                    
                    Database.SetClanData(attacker.Team.teamID.ToString(), "Kills", Database.GetClanDataRaw<int>(attacker.Team.teamID.ToString(), "Kills") + 1);
                }
                
            }
            return null;
        }
        //End Kills & Deaths
        
        
        //Npc & animal kills
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (info == null) return;
            if (info.Initiator == null) return;
            if (entity == info.Initiator) return;
            
            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId()) return;
            
            if (entity is BaseAnimalNPC)
            {
                Database.SetPlayerData(attacker.UserIDString.ToString(), "AnimalKills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "AnimalKills") + 1);
            }
            else
            {
                
                if (entity is ScientistNPC)
                {
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "NpcKills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "NpcKills") + 1);
                }
            }
        }
        //End Npc & animal kills
        
        //Damage done
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null || info?.HitEntity == null || entity.IsNpc)
            return null;
            
            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId())
            return null;
            
            Database.SetPlayerData(attacker.UserIDString.ToString(), "Hits", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Hits") + 1);
            
            {
                NextTick(() =>
                {
                    var damages = info?.damageTypes?.Total() ?? 0f;
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "Damage", (int)(Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Damage") + damages));
                });
                
                if (info.isHeadshot)
                {
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "Headshots", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Headshots") + 1);
                }
                return null;
            }
            //End Damage done
        }
        
        //Accuracy
        //Shots:hits
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (projectile == null || player == null || !player.userID.IsSteamId())
            return;
            
            Database.SetPlayerData(player.UserIDString.ToString(), "Shots", Database.GetPlayerDataRaw<int>(player.UserIDString, "Shots") + 1);
        }
        //End Accuracy
        
        //Revives
        object OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            if (reviver == null || player == null) return null;
            
            Database.SetPlayerData(reviver.UserIDString.ToString(), "Revives", Database.GetPlayerDataRaw<int>(reviver.UserIDString, "Revives") + 1);
            return null;
        }
        //End Revives
        
        //Crafts
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            Database.SetPlayerData(task.owner.UserIDString.ToString(), "ItemsCrafted", Database.GetPlayerDataRaw<int>(task.owner.UserIDString, "ItemsCrafted") + 1);
        }
        //End Crafting
        
        
        
        //wood & ores mined
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null || !player.userID.IsSteamId() || dispenser == null || item == null) return;
            var gatherType = dispenser.gatherType;
            if (gatherType == ResourceDispenser.GatherType.Tree)
            {
                Database.SetPlayerData(player.UserIDString.ToString(), "TreesFarmed", Database.GetPlayerDataRaw<int>(player.UserIDString, "TreesFarmed") + 1);
            }
            else if (gatherType == ResourceDispenser.GatherType.Ore)
            {
                Database.SetPlayerData(player.UserIDString.ToString(), "OresFarmed", Database.GetPlayerDataRaw<int>(player.UserIDString, "OresFarmed") + 1);
            }
        }
        //end woood & ores mined
        #endregion

        #region ClanGuiService.cs
        public class ClanGuiService
        {
            static List<BasePlayer> _players = new List<BasePlayer>();
            private readonly ConfigSetup _configSetup;
            private readonly DatabaseClient _databaseClient;
            
            public ClanGuiService(ConfigSetup configSetup, DatabaseClient databaseClient)
            {
                _configSetup = configSetup;
                _databaseClient = databaseClient;
            }
            
            internal void UpdateClanUI()
            {
                
                //foreach (var player in _players)
                //{
                    //    if (IsOnline(player))
                    //    {
                        //        CuiHelper.DestroyUi(player, "mainclan");
                    //    }
                //}
                string generatedGui = GenerateClanGui();
                
                var activePlayers = BasePlayer.activePlayerList;
                
                foreach (var player in activePlayers)
                {
                    CuiHelper.DestroyUi(player, "mainclan");
                    
                    CuiHelper.AddUi(player, generatedGui);
                    _players.Add(player);
                };
            }
            
            private string GenerateClanGui()
            {
                var clans = _configSetup.ConfigFile.lb_clans;
                int i = 1;
                StringBuilder stringBuilder = new StringBuilder(CLANGUI);
                foreach (var clan in clans)
                {
                    var clanId = RelationshipManager.ServerInstance.teams.FirstOrDefault(x => x.Value.teamName == clan.Id).Value?.teamID;
                    if (clanId == null)
                    {
                        Interface.Oxide.LogDebug($"Could not find clan {clanId}");
                        continue;
                    }
                    Interface.Oxide.LogDebug($"Found clan {clanId}");
                    
                    stringBuilder = stringBuilder.Replace($"%team_{i}%", clan.Name + ":");
                    stringBuilder = stringBuilder.Replace($"%team_{i}kills%", _databaseClient.GetClanDataRaw<int>(clanId.ToString(), "Kills").ToString());
                    
                    i++;
                }
                return stringBuilder.ToString();
            }
            
            private static bool IsOnline(BasePlayer player)
            {
                return BasePlayer.activePlayerList.Any(x => x.userID == player.userID);
            }
            public static readonly string CLANGUI = @"[
            {
                ""name"": ""mainclan"",
                ""parent"": ""Hud"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.000520848 0.8703703"",
                    ""anchormax"": ""0.23125 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""main_2"",
                ""parent"": ""mainclan"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.002257336 0.5035715"",
                    ""anchormax"": ""0.9977427 0.9928572"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_1"",
                ""parent"": ""main_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.004535148 0.02919707"",
                    ""anchormax"": ""0.4977324 0.9708029"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_1_text"",
                ""parent"": ""team_1"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%team_1% %team_1kills%"",
                    ""fontSize"": 12,
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_2"",
                ""parent"": ""main_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.5022676 0.02919707"",
                    ""anchormax"": ""0.9954649 0.9708029"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_2_text"",
                ""parent"": ""team_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%team_2% %team_2kills%"",
                    ""fontSize"": 12,
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""main_1"",
                ""parent"": ""mainclan"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.002257336 0.007142901"",
                    ""anchormax"": ""0.9977427 0.4964286"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_3"",
                ""parent"": ""main_1"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.004535148 0.02919707"",
                    ""anchormax"": ""0.4977324 0.9708029"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_3_text"",
                ""parent"": ""team_3"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%team_3% %team_3kills%"",
                    ""fontSize"": 12,
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_4"",
                ""parent"": ""main_1"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.5022676 0.02919707"",
                    ""anchormax"": ""0.9954649 0.9708029"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""team_4_text"",
                ""parent"": ""team_4"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%team_4% %team_4kills%"",
                    ""fontSize"": 12,
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            }
            ]";
        }
        #endregion

        #region GuiService.cs
        public class GuiService
        {
            public static readonly string STATSGUI = @"[
            {
                ""name"": ""stats"",
                ""parent"": ""Hud"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.1614583 0.3055556"",
                    ""anchormax"": ""0.8385417 0.6944444"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_frame"",
                ""parent"": ""stats"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 0.345098""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_close_frame"",
                ""parent"": ""stats_frame"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.9292307 0.8880954"",
                    ""anchormax"": ""1.00125 1.002381"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_close_text"",
                ""parent"": ""stats_close_frame"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Close"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_close_button"",
                ""parent"": ""stats_close_text"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Button"",
                    ""command"": ""%stats_close_command%"",
                    ""sprite"": """",
                    ""material"": """",
                    ""color"": ""0 0 0 0""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""-0.007632971 0.04255176"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_frame_2"",
                ""parent"": ""stats_frame"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""-0.001250005 0.00238095"",
                    ""anchormax"": ""0.9999999 0.919048"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_combat"",
                ""parent"": ""stats_frame_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.6 0.6 0.6 0.3999993""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.01152406 0.03896103"",
                    ""anchormax"": ""0.329492 0.9610389"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_combat_title"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5411765 0.1372549 0.1372549 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.8403756"",
                    ""anchormax"": ""0.9637572 0.9577465"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_combat_textstats_combat_text"",
                ""parent"": ""stats_combat_title"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Combat"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_kills"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.6807512"",
                    ""anchormax"": ""0.9637572 0.798122"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""kills_bg"",
                ""parent"": ""stats_kills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""kills_text"",
                ""parent"": ""kills_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Total Kills"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""kills_amount_bg"",
                ""parent"": ""stats_kills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""kills_amount"",
                ""parent"": ""kills_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Kills%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_hits"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.5211267"",
                    ""anchormax"": ""0.9637572 0.6384977"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""hits_bg"",
                ""parent"": ""stats_hits"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""hits_text"",
                ""parent"": ""hits_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Total Hits"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""hits_amount_bg"",
                ""parent"": ""stats_hits"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""hits_amount"",
                ""parent"": ""hits_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Hits%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_deaths"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.3615023"",
                    ""anchormax"": ""0.9637572 0.4788733"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""deaths_bg"",
                ""parent"": ""stats_deaths"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""deaths_text"",
                ""parent"": ""deaths_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Total Deaths"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""deaths_amount_bg"",
                ""parent"": ""stats_deaths"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5411765 0.1333333 0.1333333 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""deaths_amount"",
                ""parent"": ""deaths_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Deaths%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_damage"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.201878"",
                    ""anchormax"": ""0.9637572 0.3192489"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""damage_bg"",
                ""parent"": ""stats_damage"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""damage_text"",
                ""parent"": ""damage_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Damage"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""damage_amount_bg"",
                ""parent"": ""stats_damage"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""damage_amount"",
                ""parent"": ""damage_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Damage%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_headshots"",
                ""parent"": ""stats_combat"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.04225355"",
                    ""anchormax"": ""0.9637572 0.1596245"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""headshots_bg"",
                ""parent"": ""stats_headshots"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""headshots_text"",
                ""parent"": ""headshots_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Headshots"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""headshots_amount_bg"",
                ""parent"": ""stats_headshots"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""headshots_amount"",
                ""parent"": ""headshots_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Headshots%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_farm"",
                ""parent"": ""stats_frame_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.6 0.6 0.6 0.3960784""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.341016 0.03896103"",
                    ""anchormax"": ""0.6589839 0.9610389"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""farm_title"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.8403756"",
                    ""anchormax"": ""0.9637572 0.9577465"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""farm_text"",
                ""parent"": ""farm_title"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Farm"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_ItemsCrafted"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.6807512"",
                    ""anchormax"": ""0.9637572 0.798122"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ItemsCrafted_bg"",
                ""parent"": ""stats_ItemsCrafted"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ItemsCrafted_text"",
                ""parent"": ""ItemsCrafted_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Items Crafted"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ItemsCrafted_amount_bg"",
                ""parent"": ""stats_ItemsCrafted"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ItemsCrafted_amount"",
                ""parent"": ""ItemsCrafted_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%ItemsCrafted%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_TreesFarmed"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.5211267"",
                    ""anchormax"": ""0.9637572 0.6384977"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""TreesFarmed_bg"",
                ""parent"": ""stats_TreesFarmed"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""TreesFarmed_text"",
                ""parent"": ""TreesFarmed_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Trees Farmed"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""TreesFarmed_amount_bg"",
                ""parent"": ""stats_TreesFarmed"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""TreesFarmed_amount"",
                ""parent"": ""TreesFarmed_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%TreesFarmed%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_OresFarmed"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.3615023"",
                    ""anchormax"": ""0.9637572 0.4788733"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""OresFarmed_bg"",
                ""parent"": ""stats_OresFarmed"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""OresFarmed_text"",
                ""parent"": ""OresFarmed_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Ores Farmed"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""OresFarmed_amount_bg"",
                ""parent"": ""stats_OresFarmed"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""OresFarmed_amount"",
                ""parent"": ""OresFarmed_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%OresFarmed%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_NpcKills"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.201878"",
                    ""anchormax"": ""0.9637572 0.3192489"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""NpcKills_bg"",
                ""parent"": ""stats_NpcKills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""NpcKills_text"",
                ""parent"": ""NpcKills_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Npc Kills"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""NpcKills_amount_bg"",
                ""parent"": ""stats_NpcKills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""NpcKills_amount"",
                ""parent"": ""NpcKills_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%NpcKills%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_AnimalKills"",
                ""parent"": ""stats_farm"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624282 0.04225355"",
                    ""anchormax"": ""0.9637572 0.1596245"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""AnimalKills_bg"",
                ""parent"": ""stats_AnimalKills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""AnimalKills_text"",
                ""parent"": ""AnimalKills_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Animal Kills"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""AnimalKills_amount_bg"",
                ""parent"": ""stats_AnimalKills"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.2470588 0.454902 0.06666667 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""AnimalKills_amount"",
                ""parent"": ""AnimalKills_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%AnimalKills%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_other"",
                ""parent"": ""stats_frame_2"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.6 0.6 0.6 0.3921569""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.670508 0.03896103"",
                    ""anchormax"": ""0.9884759 0.9610389"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""other_title"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5215687 0 1 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.8403756"",
                    ""anchormax"": ""0.9637572 0.9577465"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""other_title_text"",
                ""parent"": ""other_title"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Other"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_Shots"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.6807512"",
                    ""anchormax"": ""0.9637572 0.798122"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""Shots_bg"",
                ""parent"": ""stats_Shots"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""Shots_text"",
                ""parent"": ""Shots_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Shots"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""Shots_amount_bg"",
                ""parent"": ""stats_Shots"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5411765 0.1372549 0.1372549 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""Shots_amount"",
                ""parent"": ""Shots_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%Shots%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_ScrapWon"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.5211267"",
                    ""anchormax"": ""0.9637572 0.6384977"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapWon_bg"",
                ""parent"": ""stats_ScrapWon"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapWon_text"",
                ""parent"": ""ScrapWon_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Scrap Won"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapWon_amount_bg"",
                ""parent"": ""stats_ScrapWon"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5215687 0 1 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapWon_amount"",
                ""parent"": ""ScrapWon_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%ScrapWon%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""stats_ScrapLost"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.3615023"",
                    ""anchormax"": ""0.9637572 0.4788733"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapLost_bg"",
                ""parent"": ""stats_ScrapLost"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0 0 0 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""0.6 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapLost_text"",
                ""parent"": ""ScrapLost_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Scrap Lost"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapLost_amount_bg"",
                ""parent"": ""stats_ScrapLost"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""material"": """",
                    ""color"": ""0.5411765 0.1372549 0.1372549 1""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.6 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""ScrapLost_amount"",
                ""parent"": ""ScrapLost_amount_bg"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""%ScrapLost%"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0 0"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""other_p1"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.201878"",
                    ""anchormax"": ""0.9637572 0.3192489"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""other_p2"",
                ""parent"": ""stats_other"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.03624283 0.04225355"",
                    ""anchormax"": ""0.9637572 0.1596245"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""CuiElement"",
                ""parent"": ""Overlay"",
                ""components"": [
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.05208334 0.09259259"",
                    ""anchormax"": ""0.1041667 0.1851852"",
                    ""offsetmin"": ""0 0"",
                    ""offsetmax"": ""0 0""
                }
                ]
            }
            ]";
            private readonly DatabaseClient _database;
            
            public GuiService(DatabaseClient database)
            {
                _database = database;
            }
            
            public void ActivateGui(BasePlayer player)
            {
                
                Interface.Oxide.LogDebug($"Enabling statisctics ui for {player.displayName}");
                string readyGui = PrepareUi(player);
                CuiHelper.AddUi(player, readyGui);
            }
            
            private string PrepareUi(BasePlayer player)
            {
                string gui = STATSGUI.Replace("%stats_close_command%", "stats_close " + player.userID);
                
                gui = ReplaceIntPlaceholder(player, gui, "Kills");
                gui = ReplaceIntPlaceholder(player, gui, "Deaths");
                gui = ReplaceIntPlaceholder(player, gui, "Hits");
                gui = ReplaceIntPlaceholder(player, gui, "Damage");
                gui = ReplaceIntPlaceholder(player, gui, "Headshots");
                
                gui = ReplaceIntPlaceholder(player, gui, "ItemsCrafted");
                gui = ReplaceIntPlaceholder(player, gui, "TreesFarmed");
                gui = ReplaceIntPlaceholder(player, gui, "OresFarmed");
                gui = ReplaceIntPlaceholder(player, gui, "NpcKills");
                gui = ReplaceIntPlaceholder(player, gui, "AnimalKills");
                
                gui = ReplaceIntPlaceholder(player, gui, "ScrapLost");
                gui = ReplaceIntPlaceholder(player, gui, "ScrapWon");
                gui = ReplaceIntPlaceholder(player, gui, "Shots");
                
                
                return gui;
                
            }
            
            private string ReplaceIntPlaceholder(BasePlayer player, string gui, string statistic)
            {
                return gui.Replace($"%{statistic}%", _database.GetPlayerDataRaw<int>(player.userID.ToString(), statistic).ToString());
            }
            
            public void DestroyGui(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "stats_frame");
            }
        }
        #endregion

        #region Models\ConfigFile.cs
        public class ConfigFile
        {
            public LbClan[] lb_clans { get; set; }
            public DatabaseConfig DatabaseConfig { get; set; }
            
        }
        #endregion

        #region Models\LbClan.cs
        public class LbClan
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }
        #endregion

        #region Startup\ConfigSetup.cs
        public class ConfigSetup
        {
            
            
            private readonly RustPlugin _plugin;
            public ConfigFile ConfigFile { get; set; }
            
            
            internal ConfigSetup(RustPlugin plugin)
            {
                _plugin = plugin;
                Init();
            }
            
            private void Init()
            {
                ConfigFile = _plugin.Config.ReadObject<ConfigFile>();
                
            }
            
            internal static object GetDefaultConfig()
            {
                return new ConfigFile()
                {
                    DatabaseConfig = DatabaseConfig.GetDefaultDatabaseConfig(),
                    lb_clans = new LbClan[] { new LbClan() { Id = "1", Name = "Clan1" }, new LbClan() { Id = "2", Name = "Clan2" }, new LbClan() { Id = "3", Name = "Clan3" }, new LbClan() { Id = "4", Name = "Clan4" } }
                };
            }
        }
        #endregion

        #region Utils\PlayerUtil.cs
        internal class PlayerUtil
        {
            
            // Uz teams pl
            // Vjg chekot hoodie skin pec skina
            public bool checkHoodie(BasePlayer player)
            {
                var hoodie = (Item)ItemManager.CreateByItemID(1751045826, 1);
                
                if (isNaked(player))
                return false;
                
                if (player.inventory.containerWear.FindItemByItemID(1751045826) != null)
                return false;
                else
                
                return true;
            }
            
            //Kip uzvelk hoodie un locko slot.
            public void addHoodie(BasePlayer player, String team)
            {
                player.inventory.containerWear.GetSlot(1).LockUnlock(true);
                Item hoodie = (Item)ItemManager.CreateByItemID(1751045826, 1);
                hoodie.MoveToContainer(player.inventory.containerWear, 1);
                
            }
            public bool isNaked(BasePlayer player)
            {
                return player.inventory.containerWear.IsEmpty();
            }
        }
        #endregion

    }

}
