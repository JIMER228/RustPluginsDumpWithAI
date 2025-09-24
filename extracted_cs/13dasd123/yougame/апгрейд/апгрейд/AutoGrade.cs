// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Reference: System.Drawing
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info( "AutoGrade", "Server-Rust.Ru", "1.0.2" )]
    class AutoGrade : RustPlugin
    {
		
        /*private MethodInfo payForUpgrade =
            typeof( BuildingBlock ).GetMethod( "PayForUpgrade",
                BindingFlags.Instance | BindingFlags.NonPublic );*/

        /*private MethodInfo getGrade =
            typeof( BuildingBlock ).GetMethod( "GetGrade",
                BindingFlags.Instance | BindingFlags.NonPublic );*/

        /*private MethodInfo canUpgrade =
            typeof( BuildingBlock ).GetMethod( "CanAffordUpgrade",
                BindingFlags.Instance | BindingFlags.NonPublic );*/

        #region Fields

        Dictionary<BuildingGrade.Enum, string> gradesString = new Dictionary<BuildingGrade.Enum, string>()
        {
            {BuildingGrade.Enum.Wood, "Дерево"},
            {BuildingGrade.Enum.Stone, "Камень"},
            {BuildingGrade.Enum.Metal, "Металл"},
            {BuildingGrade.Enum.TopTier, "Армор"}
        };

        Dictionary<BasePlayer, BuildingGrade.Enum> grades = new Dictionary<BasePlayer, BuildingGrade.Enum>();
        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();

        Dictionary<string, string> gradeImages = new Dictionary<string, string>()
        {
            { "building.upgrade.1", "http://i.imgur.com/FGdWRHx.png"},
            { "building.upgrade.2", "http://i.imgur.com/NOYW4HT.png"},
            { "building.upgrade.3", "http://i.imgur.com/NU0FXry.png"},
            { "building.upgrade.4", "http://i.imgur.com/9rH9qzh.png"},
        };
        bool loaded = false;
        #endregion

		#region NOREFLECTION
		
		private void PayForUpgrade(ConstructionGrade g, BasePlayer player)
		{
			List<Item> items = new List<Item>();
			foreach (ItemAmount itemAmount in g.costToBuild)
			{
				player.inventory.Take(items, itemAmount.itemid, (int)itemAmount.amount);
				player.Command(string.Concat(new object[] { "note.inv ", itemAmount.itemid, " ", itemAmount.amount * -1f }), new object[0]);
			}
			foreach (Item item in items)
			{
				item.Remove(0f);
			}
		}
		
		private ConstructionGrade GetGrade(BuildingBlock block, BuildingGrade.Enum iGrade)
		{
			if ((int)block.grade < (int)block.blockDefinition.grades.Length)
			{
				return block.blockDefinition.grades[(int)iGrade];
			}
			Debug.LogWarning(string.Concat(new object[] { "Grade out of range ", block.gameObject, " ", block.grade, " / ", (int)block.blockDefinition.grades.Length }));
			return block.blockDefinition.defaultGrade;
		}
		
		private bool CanAffordUpgrade(BuildingBlock block, BuildingGrade.Enum iGrade, BasePlayer player)
		{
			bool flag;
			object[] objArray = new object[] { player, block, iGrade };
			object obj = Interface.CallHook("CanAffordUpgrade", objArray);
			if (obj is bool)
			{
				return (bool)obj;
			}
			List<ItemAmount>.Enumerator enumerator = GetGrade(block, iGrade).costToBuild.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					ItemAmount current = enumerator.Current;
					if ((float)player.inventory.GetAmount(current.itemid) >= current.amount)
					{
						continue;
					}
					flag = false;
					return flag;
				}
				return true;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}
		
		#endregion
		
        #region CONFIGURATION

        int resetTime;
		bool getBuild;

        protected override void LoadDefaultConfig()
        {
            GetVariable( Config, "Через сколько секунд автоматически выключать улучшение строений", out resetTime, 40 );
			GetVariable( Config, "Разрешить Upgrade в Building Block?", out getBuild, true);
            SaveConfig();
        }
        public static void GetVariable<T>( DynamicConfigFile config, string name, out T value, T defaultValue )
        {
            config[ name ] = value = config[ name ] == null ? defaultValue : (T) Convert.ChangeType( config[ name ], typeof( T ) );
        }

        #endregion


        #region COMMANDS

        [ChatCommand( "up" )]
        void cmdAutoGrade(BasePlayer player, string command, string[] args)
        {
			// if (getBuild && !player.CanBuild())
			// {
				// player.ChatMessage( "<color=ffcc00><size=16><color=#EC402C>Upgrade</color> запрещен в билдинг блоке!!!</size></color>" );
				// return;
			// }
			 if (!permission.UserHasPermission(player.UserIDString, "autograde.use"))
             {
                 PrintToChat(player, "У вас нету прав на использование этой команды!", this, player.UserIDString);
                 return;
             }
			int grade;
			timers[ player ] = resetTime;
			
            if (player == null) return;
			if(args.Count() >= 1 && args[0] == "1")
				
                 {
					 grade = (int) ( grades[ player ] = BuildingGrade.Enum.Wood );
					 DrawUI( player, BuildingGrade.Enum.Wood, resetTime );
				 }
				 if(args.Count() >= 1 && args[0] == "2")
                 {
					 grade = (int) ( grades[ player ] = BuildingGrade.Enum.Stone );
					 DrawUI( player, BuildingGrade.Enum.Stone, resetTime );
				 }
				 if(args.Count() >= 1 && args[0] == "3")
                 {
					 grade = (int) ( grades[ player ] = BuildingGrade.Enum.Metal );
					 DrawUI( player, BuildingGrade.Enum.Metal, resetTime );
				 }
				 if(args.Count() >= 1 && args[0] == "4")
                 {
					 grade = (int) ( grades[ player ] = BuildingGrade.Enum.TopTier );
					  DrawUI( player, BuildingGrade.Enum.TopTier, resetTime );
				 }
				 if(args.Count() >= 1 && args[0] == "0")
                 {
					 grades.Remove( player );
                 timers.Remove( player );
                 DestroyUI( player );
				 player.ChatMessage( "<color=ffcc00><size=14>Вы отключили <color=#EC402C>Upgrade!</color></size></color>" );
                 return;
				 }
				 
			if (args == null || args.Length <= 0)
			{
				if (!grades.ContainsKey( player ))
				{
                grade = (int) ( grades[ player ] = BuildingGrade.Enum.Wood );
				player.ChatMessage( "<size=14><color=#EC402C>Upgrade включен!</color> \nДля быстрого переключения используйте: <color=#EC402C>/up 0-4</color></size>" );
				
				}
				else
				{
					grade = (int) grades[ player ];
					grade++;
					grades[ player ] = (BuildingGrade.Enum) Mathf.Clamp( grade, 1, 5 );
				}
				
				if (grade > 4)
				{
					grades.Remove( player );
					timers.Remove( player );
					DestroyUI( player );
					player.ChatMessage( "<color=ffcc00><size=14>Вы отключили <color=#EC402C>Upgrade!</color></size></color>" );
					return;
				}
				timers[ player ] = resetTime;
				DrawUI( player, (BuildingGrade.Enum) grade, resetTime );
			}
        }

        #endregion

        #region OXIDE HOOKS

		void Loaded()
        {
			
			PermissionService.RegisterPermissions(this, permisions);
        }
		
		
		public List<string> permisions = new List<string>()
        {
			"autograde.use"
        };
        void OnServerInitialized(BasePlayer player)
        {
            LoadDefaultConfig();
            timer.Every( 1f, GradeTimerHandler );
            InitFileManager();
            CommunityEntity.ServerInstance.StartCoroutine( StoreImages() );
        }
		
        IEnumerator StoreImages()
        {
            foreach (var img in gradeImages)
            {
                yield return m_FileManager.LoadFile( img.Key, img.Value );

            }
            var keys = gradeImages.Keys.ToList();
            foreach (string t in keys)
            {
                gradeImages[ t ] = m_FileManager.GetPng( t );
            }

            loaded = true;
        }
		
        void OnEntityBuilt( Planner planner, GameObject gameobject )
        {
            if (planner == null || gameobject == null) return;
            var player = planner.GetOwnerPlayer();
            BuildingBlock entity = gameobject.ToBaseEntity() as BuildingBlock;
            if (entity == null || entity.IsDestroyed) return;
            if (player == null) return;
            Grade( entity, player );
			
			
        }

        void Grade( BuildingBlock block, BasePlayer player, bool build = false )
        {
			if (getBuild && !player.CanBuild())
			{
				return;
			}
            BuildingGrade.Enum grade;
            if (!grades.TryGetValue( player, out grade ) || grade == BuildingGrade.Enum.Count)
                return;
            if (block == null) return;
            if (!( (int) grade >= 1 && (int) grade <= 4 )) return;
            //if ((bool) canUpgrade.Invoke( block, new object[] { grade, player } ))			
			if (CanAffordUpgrade(block, grade, player))            
			{
                var ret = Interface.Call( "CanUpgrade", player ) as string;
                if (ret != null)
                {
                    SendReply( player, ret );
                    return;
                }
                //payForUpgrade.Invoke( block, new object[] { getGrade.Invoke( block, new object[] { grade } ), player } );
				PayForUpgrade(GetGrade(block, grade), player);
                block.SetGrade( grade );
                block.SetHealthToMax();
                block.UpdateSkin( false );
				
                Effect.server.Run(
                    string.Concat( "assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab" ),
                    block,
                    0, Vector3.zero, Vector3.zero, null, false );
                timers[ player ] = resetTime;
                DrawUI( player, grade, resetTime );
timers[ player ] = resetTime;
				DrawUI( player, (BuildingGrade.Enum) grade, resetTime );
            }
            else
            {
                player.ChatMessage( "<color=#EC402C><size=16>Для улучшения нехватает ресурсов!!!</size></color>" );
            }
        }

        #endregion

        #region CORE

        int NextGrade( int grade ) => ++grade;

        void GradeTimerHandler()
        {
            foreach (var player in timers.Keys.ToList())
            {
                var seconds = --timers[ player ];
                if (seconds <= 0)
                {
                    BuildingGrade.Enum mode;
                    grades.Remove( player );
                    timers.Remove( player );
                    DestroyUI( player );
                    continue;
                }
                DrawUI( player, grades[ player ], seconds );
            }
        }

        #endregion


        #region UI

        void DrawUI( BasePlayer player, BuildingGrade.Enum grade, int seconds )
        {
            if (!loaded) return;
            DestroyUI( player );
            CuiHelper.AddUi( player,
                GUI.Replace( "{0}", gradesString[ grade ] ).Replace( "{1}", seconds.ToString() )
                    .Replace( "{2}", gradeImages[ $"building.upgrade.{(int) grade}" ] ) );
        }

        void DestroyUI( BasePlayer player )
        {
            CuiHelper.DestroyUi( player, "autograde" );
            CuiHelper.DestroyUi( player, "autogradetext" );
        }


        private string GUI = @"[{
	""name"": ""autograde"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.RawImage"",
        ""sprite"":""assets/content/textures/generic/fulltransparent.tga"",
		""png"": ""{2}""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.6463542 0.02685203"",
		""anchormax"": ""0.7166667 0.151852"",
		""offsetmin"": ""0.5 0"",
		""offsetmax"": ""-0.5 0""
	}]
}, {
	""name"": ""autogradetext"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{0}" + Environment.NewLine + @"{1} сек."",
		""fontSize"": 14,
		""align"": ""MiddleCenter""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0 0 0 1"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.6463542 0.02685185"",
		""anchormax"": ""0.7166667 0.1518518"",
		""offsetmin"": ""0.5 0"",
		""offsetmax"": ""-0.5 0""
	}]
}]";


        #endregion

        #region API

        void UpdateTimer( BasePlayer player )
        {
            timers[ player ] = resetTime;
            DrawUI( player, grades[ player ], timers[ player ] );
        }

        #endregion
		 public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if(player == null || string.IsNullOrEmpty(permissionName))
                    return false;

                var uid = player.UserIDString;
                if(permission.UserHasPermission(uid, permissionName))
                    return true;

                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if(owner == null) throw new ArgumentNullException("owner");
                if(permissions == null) throw new ArgumentNullException("commands");

                foreach(var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject( "MAP_FileManagerObject" );
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }
        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile( "AutoGradeImages" );

            private class FileInfo
            {
                public string Url;
                public string Png;
            }

            public void SaveData()
            {
                dataFile.WriteObject( files );
            }

            public string GetPng( string name ) => files[ name ].Png;

            private void Awake()
            {
                files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>();
            }
			
            public IEnumerator LoadFile( string name, string url, int size = -1 )
            {
                if (files.ContainsKey( name ) && files[ name ].Url == url && !string.IsNullOrEmpty( files[ name ].Png )) yield break;
                files[ name ] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine( LoadImageCoroutine( name, url, size ) );
            }

            IEnumerator LoadImageCoroutine( string name, string url, int size = -1 )
            {
                using (WWW www = new WWW( url ))
                {
                    yield return www;
                    {
                        if (string.IsNullOrEmpty( www.error ))
                        {
                            var bytes = size == -1 ? www.bytes : Resize( www.bytes, size );
                            var entityId = CommunityEntity.ServerInstance.net.ID;
                            var crc32 = FileStorage.server.Store( www.bytes, FileStorage.Type.png, entityId ).ToString();
                            files[ name ].Png = crc32;
                        }
                    }
                }
                loaded++;
            }

            static byte[] Resize( byte[] bytes, int size )
            {
                Image img = (Bitmap) ( new ImageConverter().ConvertFrom( bytes ) );
                Bitmap cutPiece = new Bitmap( size, size );
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage( cutPiece );
                graphic.DrawImage( img, new Rectangle( 0, 0, size, size ), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel );
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save( ms, ImageFormat.Jpeg );
                return ms.ToArray();
            }
			
        }

    }
}
