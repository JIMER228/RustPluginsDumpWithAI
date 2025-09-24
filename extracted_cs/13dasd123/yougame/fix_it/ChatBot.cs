using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ChatBot", "OxideBro", "1.1.0")]
    [Description("ЧатБот для вашего сервера")]
    class ChatBot : RustPlugin
    {
        #region Classes
	 	public List<BotList> Chat = new List<BotList>();
        public class BotList
        {
            public BotList(string[] Specifying, string[] Excluding, string Return)
            {
                this.Specifying = Specifying;
                this.Excluding = Excluding;
				this.Return = Return; 
            }
 
            public string[] Specifying { get; set;} 
            public string[] Excluding { get; set;}
			public string Return { get; set;}
        }
	    #endregion

		#region Hooks
		void Loaded()
        { 
            Chat = Interface.Oxide.DataFileSystem.ReadObject<List<BotList>>("ChatBot/BotList");
			int checkChat = (from x in Chat select x).Count();
			if(checkChat==0)
			{
				Chat.Add(new BotList(new string[] {"Друзья"}, new string[] {"Удалить"}, "/addfriend НИК"));	
			    Interface.Oxide.DataFileSystem.WriteObject("ChatBot/BotList", Chat);
			    Puts("Вопросы и ответы добавлять в /Data/ChatBot/BotList.json");
			}
        }

		void OnPlayerChat(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
                return;
			BasePlayer player = arg.Player();
		    foreach (var Otvet in Chat)
            {
			    foreach (var spec in Otvet.Specifying)
                {
			        if(arg.Args[0].ToLower().Contains(spec))
					{
						bool exclud = true;
						foreach (var exc in Otvet.Excluding)
                        {
							if(arg.Args[0].ToLower().Contains(exc))exclud = false;
			            }
						if(exclud)
						{
							ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { "0", $"<size=15><color=#FFA500>ЧатБот:</color></size> {Otvet.Return}" });
			                Puts($"ЧатБот: {Otvet.Return}");
			                return;
			            }
			        }
			    }
		    }
		    return;
        }
		#endregion
    }
}
