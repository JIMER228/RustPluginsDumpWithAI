using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("IQGradeRemove", "https://discord.gg/dNGbxafuJn", "1.2.3")]
    [Description("Умный Grade Remove")]
    class IQGradeRemove : RustPlugin
    {


        public void GradeAll(BasePlayer player, BuildingBlock buildingBlock)
        {
            if (buildingBlock.GetBuildingPrivilege() == null)
            {
                Interface_Error(player, GetLang("GRADE_ALL_NO_AUTH",player.UserIDString));
                return;
            }
            if (!player.IsBuildingAuthed())
            {
                Interface_Error(player, GetLang("GRADE_NO_AUTH", player.UserIDString));
                return;
            }

            foreach (var Block in buildingBlock.GetBuildingPrivilege().GetBuilding().buildingBlocks.Where(x => x.grade != (BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel))
            {
                if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
                {
                    if (!Block.CanAffordUpgrade(((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel), player))
                    {
                        Interface_Error(player, GetLang("GRADE_NO_RESOURCE", player.UserIDString));
                        return;
                    }
                    else
                    {
                        Block.PayForUpgrade(buildingBlock.GetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel), player);
                        Block.SetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel);
                        Block.SetHealthToMax();
                        Block.UpdateSkin();
                    }
                }
                else
                {
                    Block.SetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel);
                    Block.SetHealthToMax();
                    Block.UpdateSkin();
                }
            }
        }

        
                public Dictionary<ulong, GradeRemove> DataPlayer = new Dictionary<ulong, GradeRemove>();

        [ChatCommand("grade")]
        void Interface_New_Main(BasePlayer player)
        {
            var Data = DataPlayer[player.userID];
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            Data.RebootTimer();

            if (player.HasFlag(BaseEntity.Flags.Reserved10))
            {
                Data.GradeLevel = 0;
                CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                player.SetFlag(BaseEntity.Flags.Reserved10, false);
                return;
            }
            player.SetFlag(BaseEntity.Flags.Reserved10, true);

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = $"0.7075472 0.703154 0.5640352 0" },
                RectTransform = { AnchorMin = config.InterfaceSetting.MainInterfaces.Positions.AnchorMin, AnchorMax = config.InterfaceSetting.MainInterfaces.Positions.AnchorMax, OffsetMin = config.InterfaceSetting.MainInterfaces.Positions.OffsetMin, OffsetMax = config.InterfaceSetting.MainInterfaces.Positions.OffsetMax }
            }, "Overlay", GradeRemoveOverlay);
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorPanel) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0.092 0.274", OffsetMax = "190.0 30.00487" } 
            }, GradeRemoveOverlay, "InformationPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "159.871 1.976", OffsetMax = "-29.360 -1.976487" } 
            }, "InformationPanel", "LinePanel");

            if (Interface.MainInterfaces.ShowCloseButton)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "chat.say /grade" },
                    Text = { Text = Interface.MainInterfaces.SymbolCloseButton, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "191.404 -29.453", OffsetMax = "210.07 0.273" }
                }, GradeRemoveOverlay, "CloseUI");
            }

            CuiHelper.DestroyUi(player, "GradeRemoveOverlay");
            CuiHelper.AddUi(player, container);

            GradeRemove_Status(player);
            GradeRemove_AllObject_Turned(player);
        }
   

        [ConsoleCommand("gr.func.turned")] 
        void UI_ConsoleCommandAdminMenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];

            if (Data.GradeAllObject)
                Data.GradeAllObject = false;
            else Data.GradeAllObject = true;

            GradeRemove_AllObject_Turned(player);
        }

        [ConsoleCommand("remove")]
        void RemoveCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];
            if (Data.GradeLevel == 5)
                Data.GradeUP(player, true, 0);
            else Data.GradeUP(player, true, 5);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);
        }
        ///0 - солома
        ///1 - дерево
        ///2 - камень
        ///3 - металл
        ///4 - мвк
        ///5 - ремув
        
        public class GradeRemove
        {
            public int ActiveTime;
            public int GradeLevel;
            public Boolean GradeAllObject;
            public Timer TimerEvent = null;
            public void GradeUP(BasePlayer player, bool UseMyGrade = false, int CustomGrade = 0)
            {
                if (config.UsePermission)
                    foreach (var Perm in PermissionsLevel.Where(p => inst.permission.UserHasPermission(player.UserIDString, p.Value)))
                    {
                        if (!UseMyGrade)
                        {
                            if (GradeLevel == 5) GradeLevel = 0;
                            if (GradeLevel >= Perm.Key) continue;
                            GradeLevel = Perm.Key;
                        }
                        else if (String.IsNullOrWhiteSpace(PermissionsLevel[CustomGrade]) || inst.permission.UserHasPermission(player.UserIDString, PermissionsLevel[CustomGrade]))
                        {
                            GradeLevel = CustomGrade;
                            return;
                        }
                        else inst.Interface_Error(player, inst.GetLang("NO_PERM_GRADE_REMOVE", player.UserIDString));
                        break;
                    }
                else
                {
                    if (!UseMyGrade)
                    {
                        if (GradeLevel > 4)
                            GradeLevel = 0;
                        else GradeLevel++;
                    }
                    else GradeLevel = CustomGrade;
                }

                RebootTimer();
                int TimeActive = CustomGrade != 0 & CustomGrade != 5 ? config.GradeSetting.GradeTime : config.RemoveSetting.RemoveTime;
                ActiveTime = Convert.ToInt32(TimeActive + CurrentTime());
            }
            public void RebootTimer()
            {
                if (TimerEvent != null)
                    TimerEvent.Destroy();
            }
        }

        
        
        private static Configuration config = new Configuration();
        public Boolean IsRaidBlocked(BasePlayer player)
        {
            var ret = Interface.Call("CanTeleport", player) as String;
            if (ret != null)
                return true;
            else return false;
        }
        /// <summary> 
        /// Обновление 1.2.3
        /// - Корректировка проверки на билду 

                public static String PermissionGRMenu = "iqgraderemove.gruse";
        [ChatCommand("remove")]
        void RemoveCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            if (Data.GradeLevel == 5)
                Data.GradeUP(player, true, 0);
            else Data.GradeUP(player, true, 5);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);
        }
        public readonly Dictionary<Int32, String> StatusLevels = new Dictionary<Int32, String>
        {
            [0] = "ОТКЛЮЧЕНО",
            [1] = "ДЕРЕВА",
            [2] = "КАМНЯ",
            [3] = "МЕТАЛЛА",
            [4] = "МВК",
            [5] = "УДАЛЕНИЕ",
            [6] = "УДАЛЕНИЕ ВСЕГО",
            [7] = "УЛУЧШЕНИЕ ВСЕГО",
        };
        public readonly Dictionary<Int32, String> SoundLevelsGrade = new Dictionary<Int32, String>
        {
            [0] = "ОТКЛЮЧЕНО",
            [1] = "assets/bundled/prefabs/fx/build/frame_place.prefab",
            [2] = "assets/bundled/prefabs/fx/build/promote_stone.prefab",
            [3] = "assets/bundled/prefabs/fx/build/promote_metal.prefab",
            [4] = "assets/bundled/prefabs/fx/build/promote_toptier.prefab",
            [5] = "УДАЛЕНИЕ"
        };
        
        [ChatCommand("bgrade")]
        void BGradeChatCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            switch (arg.Length)
            {
                case 0:
                    {
                        Data.GradeUP(player);
                        break;
                    }
                case 1:
                    {
                        int GradeLevel;
                        if (!int.TryParse(arg[0], out GradeLevel) || GradeLevel < 0 || GradeLevel > 4)
                            return;
                        
                        Data.GradeUP(player, true, GradeLevel);
                        break;
                    }
            }
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            String Alert = GradeErrorParse(player, entity.GetComponent<BuildingBlock>(), grade);
            if (!String.IsNullOrWhiteSpace(Alert)) // grade
            {
                Interface_Error(player, Alert);
                return false;
            }
            return null;
        }
        //void OnHammerHit(BasePlayer player, HitInfo info)
        void OnHammerHitMethod(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || info.HitEntity == null) return;
         //   PrintToChat("OnHammerHit");
            var Data = DataPlayer[player.userID];

            if (Data.GradeLevel == 5)
                if (info.HitEntity is BaseEntity)
                    RemoveBuilding(player, info.HitEntity);

            BuildingBlock buildingBlock = info.HitEntity as BuildingBlock;
            if (buildingBlock == null) return;

            if (Data.GradeAllObject && Data.GradeLevel > 0 && Data.GradeLevel != 5)
            {
                GradeAll(player, buildingBlock);
                return;
            }
            if(Data.GradeAllObject && Data.GradeLevel == 5)
            {
                RemoveAll(player, buildingBlock);
                return;
            }

            if (Data.GradeLevel != 0 && Data.GradeLevel != 5)
            {
                if (buildingBlock.grade == (BuildingGrade.Enum)Data.GradeLevel)
                    return;

                GradeBuilding(player, buildingBlock);
            }
        }

        public void GradeBuilding(BasePlayer player, BuildingBlock buildingBlock)
        {
            var Data = DataPlayer[player.userID];
            String AlertGrrade = GradeErrorParse(player, buildingBlock);
            if (!String.IsNullOrWhiteSpace(AlertGrrade))
            {
                Interface_Error(player, AlertGrrade);
                return;
            }
            if (!config.GradeSetting.UseBackUp)
                if (buildingBlock.grade > (BuildingGrade.Enum)Data.GradeLevel) return;

            if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
                buildingBlock.PayForUpgrade(buildingBlock.GetGrade((BuildingGrade.Enum)Data.GradeLevel), player);

            buildingBlock.SetGrade((BuildingGrade.Enum)Data.GradeLevel);
            buildingBlock.SetHealthToMax();
            buildingBlock.UpdateSkin();

            Effect.server.Run(SoundLevelsGrade[Data.GradeLevel], player.GetNetworkPosition());
            DataPlayer[player.userID].ActiveTime = (Int32)(config.GradeSetting.GradeTime + CurrentTime());
        }
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null) return;
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null || go == null) return;

            BaseEntity entity = go.ToBaseEntity();
            if (entity == null) return;
            if (entity is BuildingBlock || entity is BaseEntity)
            {
                if (entity.net == null) return;

                AddRemoveBlockBuild(entity.net.ID, player.userID);
                AddBlockBuild(entity.net.ID, player.userID);
            }
            
            BuildingBlock buildingBlock = entity.GetComponent<BuildingBlock>();
            if (buildingBlock == null) return;
            var Data = DataPlayer[player.userID];
            if (Data == null) return;
           // if (Data.GradeLevel == 6 || Data.GradeLevel == 7) return;

            if (Data.GradeLevel != 0 && Data.GradeLevel != 5)
                GradeBuilding(player, buildingBlock);
        }



                static Int32 CurrentTime() => Facepunch.Math.Epoch.Current;

        public void RemoveAll(BasePlayer player, BaseEntity buildingBlock)
        {
            if (buildingBlock.GetBuildingPrivilege() == null)
            {
                Interface_Error(player, GetLang("REMOVE_ALL_NO_AUTH", player.UserIDString));
                return;
            }
            ListHashSet<BuildingBlock> BlocksRemoveAll = new ListHashSet<BuildingBlock>();
            foreach (var Block in buildingBlock.GetBuildingPrivilege().GetBuilding().buildingBlocks)
                if (!BlocksRemoveAll.Contains(Block))
                     BlocksRemoveAll.Add(Block);


            NextTick(() =>
            {
                foreach (var Block in BlocksRemoveAll)
                    Block.Kill();
            });
        }
        object OnMeleeAttack(BasePlayer player, HitInfo info)
        {
            if (!Tools.Contains(info?.WeaponPrefab?.name)) return null;
            OnHammerHitMethod(player, info);
            return null;
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQGradeRemove/BlockBuilding", BuildingRemoveTimers);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQGradeRemove/BuildingRemoveBlock", BuildingRemoveBlock);
        }
        private Boolean IsBlockBuildPemanent(UInt32 netID)
        {
            if (IsBlockAvailablePermanent(netID))
                if (BuildingRemoveBlock[netID] <= CurrentTime())
                    return true;
                else return false;
            else return false;
        }
        
        
                [PluginReference] Plugin Friends, IQTurret;
        
                private void AddBlockBuild(UInt32 netID, UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            if (!RemoveTime.UseTimesBlock) return;
            if (IsBlockAvailable(netID)) return;
            Int32 Time = GetTimeBlock(userID);

            BuildingRemoveTimers.Add(netID, Time);
        }
        void RegisteredUser(BasePlayer player)
        {
            if (!DataPlayer.ContainsKey(player.userID))
                DataPlayer.Add(player.userID, new GradeRemove { ActiveTime = 0, GradeLevel = 0 });

            if (player.HasFlag(BaseEntity.Flags.Reserved10))
                player.SetFlag(BaseEntity.Flags.Reserved10, false);
        }
        Int32 API_GET_GRADE_TIME_PLAYER(BasePlayer player)
        {
            if (player == null)
                return 0;

            if (!DataPlayer.ContainsKey(player.userID))
                return 0;

            if (DataPlayer[player.userID].GradeLevel == 0)
                return 0;

            if (DataPlayer[player.userID].ActiveTime < CurrentTime())
                return 0;

            return DataPlayer[player.userID].ActiveTime - CurrentTime();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (config.InterfaceSetting.MainInterfaces.Positions.AnchorMin == null)
                    config.InterfaceSetting.MainInterfaces.Positions.AnchorMin = "0 0";
                if (config.InterfaceSetting.MainInterfaces.Positions.AnchorMax == null)
                    config.InterfaceSetting.MainInterfaces.Positions.AnchorMax = "0 0";
                if (config.InterfaceSetting.MainInterfaces.Positions.OffsetMin == null)
                    config.InterfaceSetting.MainInterfaces.Positions.OffsetMin = "16.19 19.273";
                if (config.InterfaceSetting.MainInterfaces.Positions.OffsetMax == null)
                    config.InterfaceSetting.MainInterfaces.Positions.OffsetMax = "396.002 78.727";
                if (config.InterfaceSetting.MainInterfaces.ColorPanelBlured == null)
                    config.InterfaceSetting.MainInterfaces.ColorPanelBlured = "#373737";     
                if (config.InterfaceSetting.MainInterfaces.ColorTextTwo == null)
                    config.InterfaceSetting.MainInterfaces.ColorTextTwo = "#F3F3F3";
            }
            catch
            {
                PrintWarning("Ошибка #132" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        public Dictionary<uint, int> BuildingRemoveBlock = new Dictionary<uint, int>();
        public Dictionary<uint, int> BuildingRemoveTimers = new Dictionary<uint, int>();
        private Int32 GetTimeBlockPermanent(UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            Int32 Time = Convert.ToInt32(CurrentTime() + RemoveTime.TimeAllBlock);
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            foreach (var Perms in RemoveTime.ItemsTimesAllPermissions)
                if (permission.UserHasPermission(userID.ToString(), Perms.Key))
                {
                    Time = Convert.ToInt32(CurrentTime() + Perms.Value);
                    return Time;
                }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            return Time;
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        public static StringBuilder sb = new StringBuilder();
        void Init() => ReadData();

        public string RemoveErrorParseBuilding(BasePlayer player, BaseEntity buildingBlock)
        {
            var Remove = config.RemoveSetting;
            var Block = Remove.SettingsBlock;
            var RemoveTimed = Remove.TimedSetting;

            if(buildingBlock.OwnerID == 0)
                return GetLang("REMOVE_ONLY_FRIENDS", player.UserIDString);

            if (Block.NoEscape && IsRaidBlocked(player))
                return GetLang("REMOVE_NO_ESCAPE", player.UserIDString);

            if(IQTurret)
            {
                if(buildingBlock is ElectricSwitch)
                    if ((Boolean)IQTurret.CallHook("API_IS_TURRETLIST", buildingBlock.skinID))
                        return GetLang("IQTURRET_NO_DELETE_TUMBLER", player.UserIDString);
            }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            BuildingBlock buildingBlocks = buildingBlock.GetComponent<BuildingBlock>();
            if (buildingBlocks != null)
            {
                if (buildingBlocks.SecondsSinceAttacked < 30)
                    return GetLang("REMOVE_ATTACKED_BLOCK", player.UserIDString, FormatTime(TimeSpan.FromSeconds(30 - (int)buildingBlocks.SecondsSinceAttacked)));
            }
            BuildingPrivlidge privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
            if (Block.Friends && player.userID != buildingBlock.OwnerID)
            {
                if (!IsFriends(player.userID, buildingBlock.OwnerID))
                    return GetLang("REMOVE_ONLY_FRIENDS", player.UserIDString);
                else if (player.IsBuildingBlocked())
                    return GetLang("REMOVE_NO_AUTH", player.UserIDString);
            }
            else if (privilege != null && !player.IsBuildingAuthed())
            {
                PrintToChat($"{player?.GetBuildingPrivilege()?.buildingID} - {buildingBlock?.GetBuildingPrivilege()?.buildingID}\n {player.IsBuildingAuthed()}\n");
                return GetLang("REMOVE_NO_AUTH", player.UserIDString);
            }

            if (RemoveTimed.UseTimesBlock && IsBlockBuild(buildingBlock.net.ID))
                return GetLang("REMOVE_TIME_EXECUTE", player.UserIDString, FormatTime(TimeSpan.FromSeconds(GetTimerBlock(buildingBlock.net.ID))));
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            if (RemoveTimed.UseAllBlock && IsBlockBuildPemanent(buildingBlock.net.ID))
                return GetLang("REMOVE_TIME_EXECUTE_UNREMOVE", player.UserIDString);

            if (Block.ShortnameNoteReturned.Contains(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", "")))
                return GetLang("REMOVE_UNREMOVE", player.UserIDString);

            return "";
        }
        
        
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_GR_ADMIN"] = "<b><size=12>CHOOSE AVAILABLE MODE</size></b>",
                ["TITLE_TAKE_UP"] = "<b><size=12>UPGRADE</size></b>",
                ["TITLE_GR_ADMIN_ALL_OBJ"] = "<b><size=12>ВСЕ ПРИВЯЗАННЫЕ ОБЪЕКТЫ</size></b>",
                ["TITLE_TAKE_REMOVE"] = "<b><size=12>REMOVE</size></b>",
                ["GR_REMOVE_ALL_USE"] = "<b><size=10>REMOVE ALL</size></b>",
                ["GR_UP_ALL_USE"] = "<b><size=10>UP ALL</size></b>",

                ["REMOVE_TITLE"] = "<b><size=10>REMOVE ITEMS : {0}</size></b>",
                ["REMOVE_NO_ESCAPE"] = "<b><size=10>DO NOT REMOVE BUILDINGS DURING THE RAID</size></b>",
                ["REMOVE_NO_AUTH"] = "<b><size=10>DO NOT REMOVE OTHER BUILDINGS</size></b>",
                ["REMOVE_ATTACKED_BLOCK"] = "<b><size=11>CAN BE DELETED THROUG {0}</size></b>",
                ["REMOVE_TIME_EXECUTE"] = "<b><size=10>YOU CAN REMOVE AN OBJECT THROUGH : {0}</size></b>",
                ["REMOVE_TIME_EXECUTE_UNREMOVE"] = "<b><size=10>YOU CAN'T REMOVE IT MORE</size></b>",
                ["REMOVE_UNREMOVE"] = "<b><size=12>YOU CAN'T REMOVE</size></b>",
                ["REMOVE_ALL_UNDO"] = "<b><size=10>UNDO</size></b>",
                ["REMOVE_ONLY_FRIENDS"] = "<b><size=10>YOU CAN REMOVE FRIENDS ONLY</size></b>",

                ["GRADE_TITLE"] = "<b><size=10>UPDATE {0}</size></b>",
                ["GRADE_NO_ESCAPE"] = "<b><size=10>CANNOT IMPROVE BUILDINGS DURING THE RAID</size></b>",
                ["GRADE_NO_AUTH"] = "<b><size=10>DO NOT IMPROVE DEVELOPMENTS IN ANOTHER'S TERRITORY</size></b>",
                ["GRADE_ATTACKED_BLOCK"] = "<b><size=11>YOU CAN IMPROVE THROUGH {0}</size></b>",
                ["GRADE_NO_RESOURCE"] = "<b><size=11>NOT ENOUGH RESOURCES FOR IMPROVEMENT</size></b>",
                ["GRADE_NO_THIS_USER"] = "<b><size=11>A PLAYER IS IN THE CONSTRUCTION</size></b>",
                ["GRADE_ALL_NO_AUTH"] = "<b><size=10>IT IS IMPOSSIBLE TO IMPROVE EVERYTHING WITHOUT A CABINET</size></b>",
                ["REMOVE_ALL_NO_AUTH"] = "<b><size=10>DO NOT REMOVE ALL WITHOUT CABINET</size></b>",

                ["NO_PERM_GRADE_REMOVE"] = "<b><size=10>YOU HAVE NO RIGHT TO DO THIS</size></b>",
                ["IQTURRET_NO_DELETE_TUMBLER"] = "<b><size=10>YOU CANNOT DELETE THIS ITEM!</size></b>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_GR_ADMIN"] = "<b><size=12>ВЫБЕРИТЕ ДОСТУПНЫЙ РЕЖИМ</size></b>",
                ["TITLE_GR_ADMIN_ALL_OBJ"] = "<b><size=12>ВСЕ ПРИВЯЗАННЫЕ ОБЪЕКТЫ</size></b>",
                ["TITLE_TAKE_UP"] = "<b><size=12>УЛУЧШЕНИЕ</size></b>",
                ["TITLE_TAKE_REMOVE"] = "<b><size=12>УДАЛЕНИЕ</size></b>",
                ["GR_UP_ALL_USE"] = "<b><size=10>УЛУЧШЕНИЯ ВСЕХ ОБЪЕКТОВ</size></b>",

                ["REMOVE_TITLE"] = "<b><size=10>УДАЛЕНИЕ ПОСТРОЕК : {0}</size></b>", 
                ["REMOVE_NO_ESCAPE"] = "<b><size=10>НЕЛЬЗЯ УДАЛЯТЬ ПОСТРОЙКИ ВО ВРЕМЯ РЕЙДА</size></b>",
                ["REMOVE_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УДАЛЯТЬ ЧУЖИЕ ПОСТРОЙКИ</size></b>",
                ["REMOVE_ATTACKED_BLOCK"] = "<b><size=11>УДАЛИТЬ МОЖНО БУДЕТ ЧЕРЕЗ {0}</size></b>",
                ["REMOVE_TIME_EXECUTE"] = "<b><size=10>ВЫ СМОЖЕТЕ УДАЛИТЬ ОБЪЕКТ ЧЕРЕЗ : {0}</size></b>",
                ["REMOVE_TIME_EXECUTE_UNREMOVE"] = "<b><size=10>ВЫ БОЛЬШЕ НЕ МОЖЕТЕ УДАЛИТЬ ЭТОТ ОБЪЕКТ</size></b>",
                ["REMOVE_UNREMOVE"] = "<b><size=12>ВЫ НЕ МОЖЕТЕ УДАЛИТЬ ЭТОТ ОБЪЕКТ</size></b>",
                ["REMOVE_ALL"] = "<b><size=10>ВКЛЮЧЕНО УДАЛЕНИЯ ВСЕХ ОБЪЕКТОВ</size></b>",
                ["REMOVE_ALL_UNDO"] = "<b><size=10>ВЕРНУТЬ</size></b>",
                ["REMOVE_ALL_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УДАЛИТЬ ВСЕ БЕЗ ШКАФА</size></b>",
                ["REMOVE_ONLY_FRIENDS"] = "<b><size=10>ВЫ МОЖЕТЕ УДАЛЯТЬ ТОЛЬКО ПОСТРОЙКИ ДРУЗЕЙ</size></b>",

                ["GRADE_TITLE"] = "<b><size=10>УЛУЧШЕНИЕ ДО {0} : {1}</size></b>",
                ["GRADE_NO_ESCAPE"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШАТЬ ПОСТРОЙКИ ВО ВРЕМЯ РЕЙДА</size></b>",
                ["GRADE_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШАТЬ ПОСТРОЙКИ НА ЧУЖОЙ ТЕРРИТОРИИ</size></b>",
                ["GRADE_ATTACKED_BLOCK"] = "<b><size=11>УЛУЧШИТЬ МОЖНО БУДЕТ ЧЕРЕЗ {0}</size></b>",
                ["GRADE_NO_RESOURCE"] = "<b><size=11>НЕДОСТАТОЧНО РЕСУРСОВ ДЛЯ УЛУЧШЕНИЯ</size></b>",
                ["GRADE_NO_THIS_USER"] = "<b><size=11>В ПОСТРОЙКЕ НАХОДИТСЯ ПРЕДМЕТ</size></b>",
                ["GRADE_ALL_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШИТЬ ВСЕ БЕЗ ШКАФА</size></b>",

                ["NO_PERM_GRADE_REMOVE"] = "<b><size=10>У ВАС НЕТ ПРАВ ДЛЯ ЭТОГО</size></b>",
                ["IQTURRET_NO_DELETE_TUMBLER"] = "<b><size=10>НЕЛЬЗЯ УДАЛИТЬ ЭТОТ ПРЕДМЕТ!</size></b>",

            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }

        void GradeRemove_AllObject_Turned(BasePlayer player)
        {
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];

            CuiHelper.DestroyUi(player, $"AdminButtonDeleteAll");
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            CuiElementContainer container = new CuiElementContainer();

            if (permission.UserHasPermission(player.UserIDString, PermissionGRMenu))
            {
                String ColorButton = Data.GradeAllObject ? MainInterface.ColorPanelBlured : MainInterface.ColorPanel;

                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(ColorButton), Command = "gr.func.turned" },
                    Text = { Text = GetLang("TITLE_GR_ADMIN_ALL_OBJ", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "193.415 31.363", OffsetMax = "379.905 59.727" }
                }, "GradeRemoveOverlay", "AdminButtonDeleteAll");
            }

            CuiHelper.AddUi(player, container);
        }
        public static String FormatTime(TimeSpan time)
        {
            String result = String.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "д", "д", "д")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "ч", "ч", "ч")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "м", "м", "м")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "с", "с", "с")} ";

            return result;
        }

        [ConsoleCommand("building.upgrade")]
        void BUpgradeCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];

            Int32 GradeLevel;
            if (arg?.Args != null && Int32.TryParse(arg.Args[0], out GradeLevel))
            {
                if (GradeLevel < 0 || GradeLevel > 4)
                    return;

                Data.GradeUP(player, true, GradeLevel);
            }
            else Data.GradeUP(player);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        void OnPlayerConnected(BasePlayer player) => RegisteredUser(player);

        public void RemoveBuilding(BasePlayer player, BaseEntity buildingBlock)
        {
            String Alert = RemoveErrorParseBuilding(player, buildingBlock);
            if (!String.IsNullOrWhiteSpace(Alert))
            {
                Interface_Error(player, Alert);
                return;
            }

            ReturnedRemoveItems(player, buildingBlock);

            NextTick(() => {
                {
                    StorageContainer container = buildingBlock.GetComponent<StorageContainer>();
                    if (container != null) 
                        container.DropItems();

                    buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            });
            DataPlayer[player.userID].ActiveTime = (int)(config.RemoveSetting.RemoveTime + CurrentTime());
        }
        
        
        [ConsoleCommand("up")]
        void UPCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var Data = DataPlayer[player.userID];

            Int32 GradeLevel;
            if (arg?.Args != null && Int32.TryParse(arg.Args[0], out GradeLevel))
            {
                if (GradeLevel < 0 || GradeLevel > 4)
                    return;

                Data.GradeUP(player, true, GradeLevel);
            }
            else Data.GradeUP(player);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        void Update_Take_Button_UP(BasePlayer player)
        {
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;

            CuiHelper.DestroyUi(player, "ButtonUpgrade");
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var container = new CuiElementContainer();
            var Data = DataPlayer[player.userID];

            String ColorButton = Data.GradeLevel != 0 && Data.GradeLevel <= 4 ? MainInterface.ColorPanelBlured : MainInterface.ColorPanel;

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(ColorButton), Command = "chat.say /up" },
                Text = { Text = GetLang("TITLE_TAKE_UP", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "193.415 0.273", OffsetMax = "284.785 29.999" }
            }, "GradeRemoveOverlay", "ButtonUpgrade");

            CuiHelper.AddUi(player, container);
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private Boolean IsBlockAvailable(UInt32 netID)
        {
            if (BuildingRemoveTimers.ContainsKey(netID))
                return true;
            else return false;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        void UpdateButton_Upgrade(BasePlayer player)
        {
            if (!config.InterfaceSetting.MainInterfaces.ShowLevelUp) return;

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;

            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];
            
            CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2196079 0.2196079 0.2196079 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0.091 31.363", OffsetMax = "190.001 59.727" }
            }, GradeRemoveOverlay, "UpgradeButtonStatus");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 1" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-145.386 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelOne");

            String HexGradeWood = Data.GradeLevel == 1 ? MainInterface.ColorTextTwo : MainInterface.ColorPanelBlured;
            String SpriteCheckWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "assets/icons/level_wood.png" : "assets/icons/occupied.png" : "assets/icons/level_wood.png";
            String AnchorMinWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9"; 

            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeWood), Sprite = SpriteCheckWood },
                RectTransform = { AnchorMin = AnchorMinWood, AnchorMax = AnchorMaxWood }
            }, "ButtonUpgradeLevelOne", "SpriteLevelOne");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 2" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "48.993 0", OffsetMax = "-96.393 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLeveTwo");

            String HexGradeStone = Data.GradeLevel == 2 ? MainInterface.ColorTextTwo : MainInterface.ColorPanelBlured;
            String SpriteCheckStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "assets/icons/level_stone.png" : "assets/icons/occupied.png" : "assets/icons/level_stone.png";
            String AnchorMinStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeStone), Sprite = SpriteCheckStone },
                RectTransform = { AnchorMin = AnchorMinStone, AnchorMax = AnchorMaxStone }
            }, "ButtonUpgradeLeveTwo", "SpriteLevelTwo");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 3" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "96.893 0", OffsetMax = "-48.493 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelThree");

            String HexGradeMetal = Data.GradeLevel == 3 ? MainInterface.ColorTextTwo : MainInterface.ColorPanelBlured;
            String SpriteCheckMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "assets/icons/level_metal.png" : "assets/icons/occupied.png" : "assets/icons/level_metal.png";
            String AnchorMinMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeMetal), Sprite = SpriteCheckMetal },
                RectTransform = { AnchorMin = AnchorMinMetal, AnchorMax = AnchorMaxMetal }
            }, "ButtonUpgradeLevelThree", "SpriteLevelThree");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 4" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "145.386 0", OffsetMax = "0 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelFo");

            String HexGradeTop = Data.GradeLevel == 4 ? MainInterface.ColorTextTwo : MainInterface.ColorPanelBlured;
            String SpriteCheckTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "assets/icons/level_top.png" : "assets /icons/occupied.png" : "assets/icons/level_top.png";
            String AnchorMinTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeTop), Sprite = SpriteCheckTop },
                RectTransform = { AnchorMin = AnchorMinTop, AnchorMax = AnchorMaxTop }
            }, "ButtonUpgradeLevelFo", "SpriteLevelFo");

            CuiHelper.AddUi(player, container);
        }

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        void OnNewSave(string filename)
        {
            BuildingRemoveTimers.Clear();
            BuildingRemoveBlock.Clear();
            WriteData();
        }
        private class Configuration
        {
            [JsonProperty("Использовать пермишенсы для включения определенного UP или ремува(смотрите в описании плагина, там указаны права)")]
            public bool UsePermission;
            [JsonProperty("Настройка улучшения объектов")]
            public GradeSettings GradeSetting = new GradeSettings();
            [JsonProperty("Настройка удаления объектов")]
            public RemoveSettings RemoveSetting = new RemoveSettings();
            
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UsePermission = false,

                                        RemoveSetting = new RemoveSettings
                    {
                        RemoveTime = 30,
                        ReturnedSetting = new RemoveSettings.ReturnedSettings
                        {
                            UseReturnedResource = true,
                            PercentReturnRecource = 50,
                            UseDamageReturned = true,
                            UseAllowedReturned = true,
                            ShortnameNoteReturned = new List<string>
                            {
                                "campfire",
                            }
                        },
                        TimedSetting = new RemoveSettings.TimedSettings
                        {
                            UseAllBlock = false,
                            TimeAllBlock = 60,
                            ItemsTimesAllPermissions = new Dictionary<string, int>
                            {
                                ["iqgraderemove.vip"] = 200,
                                ["iqgraderemove.prem"] = 250,
                                ["iqgraderemove.gold"] = 300,
                            },
                            UseTimesBlock = false,
                            TimesBlock = 100,
                            ItemsTimesPermissions = new Dictionary<string, int>
                            {
                                ["iqgraderemove.vip"] = 150,
                                ["iqgraderemove.prem"] = 200,
                                ["iqgraderemove.gold"] = 300,
                            },
                        },
                        SettingsBlock = new RemoveSettings.SettingsBlocks
                        {
                            Friends = true,
                            NoEscape = true,
                            ShortnameNoteReturned = new List<string>
                            {
                                "campfire",
                            }
                        }
                    },
                    
                                        GradeSetting = new GradeSettings
                    {
                        GradeTime = 30,
                        UseBackUp = false,
                        SettingsBlock = new GradeSettings.SettingsBlocks
                        {
                            NoEscape = true,
                        }
                    },
                    
                                        InterfaceSetting = new InterfaceSettings
                    {
                        MainInterfaces = new InterfaceSettings.MainInterface
                        {
                            ColorPanel = "#525252",
                            ColorPanelBlured = "#373737",
                            ColorText = "#C9C0B9FF",
                            ColorTextTwo = "#F3F3F3",
                            SymbolCloseButton = "<",
                            ShowCloseButton = true,
                            ShowLevelUp = true,
                            HideMenuTimer = true,
                            Positions = new InterfaceSettings.MainInterface.InterfacePosition
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "16.19 19.273",
                                OffsetMax = "396.002 78.727"
                            }
                        },
                    },
                                    };
            }

                        internal class RemoveSettings
            {
                [JsonProperty("Настройка запретов")]
                public SettingsBlocks SettingsBlock = new SettingsBlocks();

                internal class ReturnedSettings
                {
                    [JsonProperty("Снижать состояние предмета при возврате?Эффект будто он поднял его через RUST систему")]
                    public bool UseDamageReturned;
                    [JsonProperty("Возвращать все предметы при удалении?(true- да/false - нет)")]
                    public bool UseAllowedReturned;
                    [JsonProperty("Возвращать ресурсы за удаление строений?(true- да/false - нет)")]
                    public bool UseReturnedResource;
                    [JsonProperty("Предметы,которые не возвращаются при удалении(Shortname)")]
                    public List<string> ShortnameNoteReturned = new List<string>();
                    [JsonProperty("Процент возврата ресурсов за удаление строений?(true- да/false - нет)")]
                    public int PercentReturnRecource;
                }
                internal class SettingsBlocks
                {
                    [JsonProperty("[NoEscape] Запретить удаление во время рейдблока(true - да/false - нет)")]
                    public bool NoEscape;
                    [JsonProperty("[Friends] Удалять постройки могут только друзья(Иначе все,кто есть в шкафу)(true - да/false - нет)")]
                    public bool Friends;
                    [JsonProperty("Предметы,которые нельзя удалить(Shortname)")]
                    public List<string> ShortnameNoteReturned = new List<string>();
                }
                internal class TimedSettings
                {
                    [JsonProperty("Включить полный запрет на удаление объекта через N время(Пример : Через 3 часа после постройки,его нельзя будет удалить вообще)")]
                    public bool UseAllBlock;
                    [JsonProperty("Через сколько нельзя будет удалять постройку вообще")]
                    public int TimeAllBlock;
                    [JsonProperty("Кастомный список предметов,которые нельзя будет удалить через время по правам. [[IQGradeRemove.NAME]] - Время(в сек)")]
                    public Dictionary<string, int> ItemsTimesAllPermissions = new Dictionary<string, int>();
                    [JsonProperty("Использовать запрет на удаление постройки на время(После постройки объекта,его N количество времени нельзя будет удалить)")]
                    public bool UseTimesBlock;
                    [JsonProperty("Через сколько можно будет удалять постройку(если включено)")]
                    public int TimesBlock;
                    [JsonProperty("Кастомный список предметов,которые можно будет удалить через время по правам. [[IQGradeRemove.NAME]] - Время(в сек)")]
                    public Dictionary<string, int> ItemsTimesPermissions = new Dictionary<string, int>();
                }
                [JsonProperty("Настройка возврата предметов после удаления")]
                public ReturnedSettings ReturnedSetting = new ReturnedSettings();
                [JsonProperty("Время действия удаления")]
                public int RemoveTime;
                [JsonProperty("Настройка удаления через время")]
                public TimedSettings TimedSetting = new TimedSettings();
            }
            
                        internal class GradeSettings
            {
                [JsonProperty("Время действия улучшения")]
                public int GradeTime;
                [JsonProperty("Разрешить обратное улучшение?(Пример : МВК стенку откатить в деревянную)(true - да/false - нет)")]
                public bool UseBackUp;
                [JsonProperty("Настройка запретов")]
                public SettingsBlocks SettingsBlock = new SettingsBlocks();
                internal class SettingsBlocks
                {
                    [JsonProperty("[NoEscape] Запретить улучшение во время рейдблока(true - да/false - нет)")]
                    public bool NoEscape;
                }
            }
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSettings InterfaceSetting = new InterfaceSettings();
            
                        internal class InterfaceSettings
            {
                [JsonProperty("Настройка интерфейса")]
                public MainInterface MainInterfaces = new MainInterface();

                internal class MainInterface
                {
                    [JsonProperty("Скрывать интерфейс по истечению таймера")]
                    public Boolean HideMenuTimer;
                    [JsonProperty("Отображать уровни улучшений в интерфейсе")]
                    public Boolean ShowLevelUp;
                    [JsonProperty("Отображать кнопку закрыть в меню")]
                    public Boolean ShowCloseButton;
                    [JsonProperty("Символ для кнопки закрыть")]
                    public String SymbolCloseButton;
                    [JsonProperty("Цвет панели(HEX)")]
                    public string ColorPanel;   
                    [JsonProperty("Приглушенный цвет панели(HEX)")]
                    public string ColorPanelBlured;
                    [JsonProperty("Цвет текста(HEX)")]
                    public string ColorText;    
                    [JsonProperty("Цвет текста(HEX) #2")]
                    public string ColorTextTwo;

                    [JsonProperty("Настройка позиции интерфейса")]
                    public InterfacePosition Positions = new InterfacePosition();
                    internal class InterfacePosition
                    {
                        public String AnchorMin;
                        public String AnchorMax;
                        public String OffsetMin;
                        public String OffsetMax;
                    }
                }
            }
        }

        [ChatCommand("up")]
        void UPCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            switch (arg.Length)
            {
                case 0:
                    {
                        Data.GradeUP(player);
                        break;
                    }
                case 1:
                    {
                        Int32 GradeLevel;
                        if (!int.TryParse(arg[0], out GradeLevel) || GradeLevel < 0 || GradeLevel > 4)
                            return;
                        
                        Data.GradeUP(player, true, GradeLevel);
                        break;
                    }
            }
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        void ReadData() {
            BuildingRemoveTimers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, int>>("IQGradeRemove/BlockBuilding");
            BuildingRemoveBlock = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, int>>("IQGradeRemove/BuildingRemoveBlock");
        }
        private Int32 GetTimeBlock(UInt64 userID = 0)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            Int32 Time = Convert.ToInt32(CurrentTime() + RemoveTime.TimesBlock);
            if (!userID.IsSteamId()) return Time;

            foreach (var Perms in RemoveTime.ItemsTimesPermissions)
                if (permission.UserHasPermission(userID.ToString(), Perms.Key))
                {
                    Time = Convert.ToInt32(CurrentTime() + Perms.Value);
                    return Time;
                }

            return Time;
        }
        void Unload()
        {
            WriteData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                if (player.HasFlag(BaseEntity.Flags.Reserved10))
                    player.SetFlag(BaseEntity.Flags.Reserved10, false);
            }
        }
        private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            "     Author - Sempai#3239\n" +
            "     VK - https://vk.com/rustnastroika/n" +
            "     Discord - https://discord.gg/5DPTsRmd3G/n" 
            inst = this;
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);
            RegisteredPermissions();
        }
        private static String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        void Update_Take_Button_REMOVE(BasePlayer player)
        {
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;
            CuiHelper.DestroyUi(player, "ButtonRemove");
            CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var container = new CuiElementContainer();
            var Data = DataPlayer[player.userID];
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            String ColorButton = Data.GradeLevel > 4 ? MainInterface.ColorPanelBlured : MainInterface.ColorPanel;

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(ColorButton), Command = "chat.say /remove" },
                Text = { Text = GetLang("TITLE_TAKE_REMOVE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "288.535 0.273", OffsetMax = "379.905 29.999" }
            }, "GradeRemoveOverlay", "ButtonRemove");

            CuiHelper.AddUi(player, container);
        }
        public Boolean IsFriends(UInt64 userID, UInt64 targetID)
        {
            if (Friends)
                return (Boolean)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        
        
        Int32 API_GET_GRADE_LEVEL_PLAYER(BasePlayer player)
        {
            if (player == null)
                return 0;

            if (!DataPlayer.ContainsKey(player.userID))
                return 0;

            return DataPlayer[player.userID].GradeLevel;
        }
                public static IQGradeRemove inst;
        private Int32 GetTimerBlock(UInt32 netID)
        {
            Int32 Time = 0;
            if (IsBlockAvailable(netID))
                Time = Convert.ToInt32(BuildingRemoveTimers[netID] - CurrentTime());
            return Time;
        }
        
        
                public static String GradeRemoveOverlay = "GradeRemoveOverlay";
        
        
                private void AddRemoveBlockBuild(UInt32 netID, UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            if (!RemoveTime.UseAllBlock) return;
            if (IsBlockAvailablePermanent(netID)) return;
            Int32 Time = GetTimeBlockPermanent(userID);
            BuildingRemoveBlock.Add(netID, Time);
        }

        public readonly Dictionary<Int32, String> StatusLevelsAllGrades = new Dictionary<Int32, String>
        {
            [0] = "В СОЛОМУ",
            [1] = "В ДЕРЕВО",
            [2] = "В КАМЕНЬ",
            [3] = "В МЕТАЛЛ",
            [4] = "В МВК",
        };
        private void OnServerShutdown() => Unload();

        void RegisteredPermissions()
        {
            var RemoveTimed = config.RemoveSetting.TimedSetting;

            foreach (var PermsBlockTimed in RemoveTimed.ItemsTimesAllPermissions)
                if (!permission.PermissionExists(PermsBlockTimed.Key, this))
                    permission.RegisterPermission(PermsBlockTimed.Key, this);

            foreach (var PermsBlockTimed in RemoveTimed.ItemsTimesPermissions)
                if (!permission.PermissionExists(PermsBlockTimed.Key, this))
                    permission.RegisterPermission(PermsBlockTimed.Key, this);

            if (!permission.PermissionExists(PermissionGRMenu, this))
                permission.RegisterPermission(PermissionGRMenu, this);

            if (!permission.PermissionExists(PermissionGRNoResource, this))
                permission.RegisterPermission(PermissionGRNoResource, this);

            foreach (string Permissions in PermissionsLevel.Values)
                if (!permission.PermissionExists(Permissions, this))
                    permission.RegisterPermission(Permissions, this);
        }

        void Interface_Error(BasePlayer player, String Message)
        {
            player.SendConsoleCommand("gametip.showtoast", new object[] 
            {
                "1",
                Message
            });
            Effect.server.Run("assets/bundled/prefabs/fx/invite_notice.prefab", player.GetNetworkPosition());
        }
        
                public String GradeErrorParse(BasePlayer player, BuildingBlock buildingBlock, BuildingGrade.Enum grade = BuildingGrade.Enum.None)
        {
            var Data = DataPlayer[player.userID];
            var Block = config.GradeSetting.SettingsBlock;
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            if (buildingBlock.name.Contains("foundation") && DeployVolume.Check(buildingBlock.transform.position, buildingBlock.transform.rotation, PrefabAttribute.server.FindAll<DeployVolume>(buildingBlock.prefabID), ~(5 << buildingBlock.gameObject.layer)))
                return GetLang("GRADE_NO_THIS_USER", player.UserIDString);

            if (Block.NoEscape && IsRaidBlocked(player))
                return GetLang("GRADE_NO_ESCAPE", player.UserIDString);

            if (buildingBlock.SecondsSinceAttacked < 30)
                return GetLang("GRADE_ATTACKED_BLOCK", player.UserIDString, FormatTime(TimeSpan.FromSeconds(30 - (int)buildingBlock.SecondsSinceAttacked)));
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            if (!player.CanBuild())
                return GetLang("GRADE_NO_AUTH", player.UserIDString);

            if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
            {
                Int32 Grade = grade == BuildingGrade.Enum.None ? Data.GradeLevel : (Int32)grade;
                if (!buildingBlock.CanAffordUpgrade((BuildingGrade.Enum)Grade, player))
                    return GetLang("GRADE_NO_RESOURCE", player.UserIDString);
            }

            return "";
        }
        public static String PermissionGRNoResource = "iqgraderemove.grusenorecource";
        private Boolean IsBlockAvailablePermanent(UInt32 netID)
        {
            if (BuildingRemoveBlock.ContainsKey(netID))
                return true;
            else return false;
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        void UpdateLabelStatus(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SpriteStatus");
            CuiHelper.DestroyUi(player, "LabelStatus");
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];

            if (Data.ActiveTime - CurrentTime() <= 1 && Data.GradeLevel != 0)
            {
                Data.GradeLevel = 0;
                if (Interface.MainInterfaces.HideMenuTimer)
                {
                    Data.RebootTimer();
                    CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                    player.SetFlag(BaseEntity.Flags.Reserved10, false);
                }
                else
                {
                    CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
                    GradeRemove_Status(player);
                }
                return;
            }

            CuiElementContainer container = new CuiElementContainer();
            String SpriteStatus = Data.GradeLevel == 0 ? "assets/icons/loading.png" : Data.GradeLevel != 0 && Data.GradeLevel <= 4 ? "assets/icons/upgrade.png" : "assets/icons/level_stone.png";

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorText), Sprite = SpriteStatus },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "162.344 3.414", OffsetMax = "-4.744 -3.414" }
            }, "InformationPanel", "SpriteStatus");
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
            String LangStatus = Data.GradeLevel == 0 ? GetLang("TITLE_GR_ADMIN", player.UserIDString) : Data.GradeLevel != 0 && Data.GradeLevel <= 4 ?
            GetLang("GRADE_TITLE", player.UserIDString, StatusLevels[Data.GradeLevel], FormatTime(TimeSpan.FromSeconds(Data.ActiveTime - CurrentTime()))) :
            GetLang("REMOVE_TITLE", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Data.ActiveTime - CurrentTime())));

            container.Add(new CuiElement
            {
                Name = "LabelStatus",
                Parent = "InformationPanel",
                Components = {
                    new CuiTextComponent { Text = LangStatus, Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0.044 0.137", OffsetMax = "-32.266 -0.136" }
                }
            });

            CuiHelper.AddUi(player, container);

            Data.RebootTimer();
            if (Data.GradeLevel == 0)
                return;
            Data.TimerEvent = timer.Once(1f, () => UpdateLabelStatus(player));
        }
        private Boolean IsBlockBuild(UInt32 netID)
        {
            if (IsBlockAvailable(netID))
                if (BuildingRemoveTimers[netID] > CurrentTime())
                    return true;
                else return false;
            else return false;
        }
        public static readonly Dictionary<Int32, String> PermissionsLevel = new Dictionary<Int32, String>
        {
            [0] = "",
            [1] = "iqgraderemove.upwood",
            [2] = "iqgraderemove.upstones",
            [3] = "iqgraderemove.upmetal",
            [4] = "iqgraderemove.uphmetal",
            [5] = "iqgraderemove.removeuse",
        };

        void GradeRemove_Status(BasePlayer player)
        {
            UpdateLabelStatus(player);
            Update_Take_Button_UP(player);
            Update_Take_Button_REMOVE(player);
        }
        
        void ReturnedRemoveItems(BasePlayer player, BaseEntity buildingBlock)
        {
            var Remove = config.RemoveSetting.ReturnedSetting;
            if(Remove.ShortnameNoteReturned.Contains(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", ""))) return;

            if (Remove.UseAllowedReturned)
            {
                Item ItemReturned;

                if (buildingBlock is ModularCarGarage && buildingBlock.ShortPrefabName.Contains("electrical.modularcarlift"))
                    ItemReturned = ItemManager.CreateByName("modularcarlift", 1);
                else ItemReturned = buildingBlock is BaseOven || buildingBlock is MiningQuarry || buildingBlock is BaseLadder || buildingBlock.GetComponent<BaseCombatEntity>().pickup.itemTarget == null ?
                                    ItemManager.CreateByName(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", ""), 1) : ItemManager.Create(buildingBlock.GetComponent<BaseCombatEntity>().pickup.itemTarget, 1);
               
                if (ItemReturned != null)
                {
                    if (Remove.UseDamageReturned)
                    {
                        Single healthFraction = buildingBlock.GetComponent<BaseCombatEntity>().Health() / buildingBlock.GetComponent<BaseCombatEntity>().MaxHealth();
                        ItemReturned.conditionNormalized = Mathf.Clamp01(healthFraction - buildingBlock.GetComponent<BaseCombatEntity>().pickup.subtractCondition);
                    }
                    player.GiveItem(ItemReturned);
                    return;
                }
            }
            if (Remove.UseReturnedResource)
                if (buildingBlock is StabilityEntity)
                {
                    Single PercentResource = (Single)((Single)(Remove.PercentReturnRecource) / (Single)(100));
                    foreach (var CostReturned in (buildingBlock as StabilityEntity).BuildCost())
                        player.GiveItem(ItemManager.Create(CostReturned.itemDef, Mathf.FloorToInt((Single)CostReturned.amount * PercentResource)));
                    return;
                }
        }
        
                private List<String> Tools = new List<String>
        {
            "assets/prefabs/weapons/hammer/hammer.entity.prefab",
            "assets/prefabs/weapons/toolgun/toolgun.entity.prefab"
        };
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || player.userID < 2147483647) return;
            CuiHelper.DestroyUi(player, GradeRemoveOverlay);
        }
            }
}
