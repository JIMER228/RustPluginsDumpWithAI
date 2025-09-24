using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Globalization;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;


namespace Oxide.Plugins
{
    [Info("ChatBot", "Enigma", "1.0.0")]
    [Description("ChatBot")]
    class ChatBot : RustPlugin
    {
		 	void OnPlayerChat(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
                return;
			BasePlayer player = arg.Player();
		foreach (var Otvet in Chat)
         {	  
			foreach (var spec in Otvet.Specifying)
            {
			if(arg.Args[0].ToLower().Contains(spec)){
			bool exclud = true;
			foreach (var exc in Otvet.Excluding)
            {   
			if(arg.Args[0].ToLower().Contains(exc))exclud = false;
			}
			if(exclud){
			ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { "76561198323227442", $"<size=15><color=#1E90FF>БОТ:</color></size> {Otvet.Return}" });
			Puts($"Бот: {Otvet.Return}");
			return;
			}
			}
			}
		}
		return;
        } 
		 		
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

        #region [HELPERS]
		void Loaded()
        { 
            Chat = Interface.Oxide.DataFileSystem.ReadObject<List<BotList>>("BotList");
			int checkChat = (from x in Chat select x).Count();
			if(checkChat==0){
			Chat.Add(new BotList(new string[] {"как открыть карту", "как забиндить карту", "как пользоваться картой"}, new string[] {"удалить", "закрыть"}, "Нажми F1 на клавиатуре и веди: bind m LMUI_Control map"));	
			Interface.Oxide.DataFileSystem.WriteObject("BotList", Chat);
			Puts("Вопросы и ответы добавлять в /Data/BotList.json");
			}
        }

	   #endregion
		
    }
}
