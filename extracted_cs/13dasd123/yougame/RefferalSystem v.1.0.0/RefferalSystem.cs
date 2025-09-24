// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Refferal System", "A0001", "1.0.0")]
	[Description("Rewards for refers and refferals.")]
	
    class RefferalSystem : RustPlugin
    {	
	    #region Variables
	   
	    private List<ulong> refferals = new List<ulong>();
	    
	    private int rewardRefer;
	    private int rewardReferral;
	    private string shopId;
	    private string secretKey;
	    private bool gameStores;
	    
	    #endregion
	    
		#region Initialization
		
		void Init()
        {			
			LoadMessages();
			LoadData();
			LoadDefaultConfig();
        }
		
	    void LoadMessages()
	    {
		    lang.RegisterMessages(new Dictionary<string, string>
		    {
			    ["R.SYNTAX"] = "<size=16><color=#DC143C>[Ошибка]</color> Чтобы указать кого-то рефералом введите:\n<color=cyan>/refer steamID (Он состоит из 17 цифр)</color></size>",
			    ["R.SUCCESS"] = "<size=16><color=green>[Успех]</color> Вы успешно указали своего рефера как <color=cyan>{0}</color>.</size>",
			    ["R.ALREADY"] = "<size=16><color=#DC143C>[Ошибка]</color> У вас уже есть рефер.</size>",
			    ["R.NOPLAYER"] = "<size=16><color=#DC143C>[Ошибка]</color> Не удается найти данного игрока.</size>",
			    ["R.SELF"] = "<size=16><color=#DC143C>[Ошибка]</color> Вы не можете пригласить сами себя.</size>",
		    }, this);
	    }
		
	    protected override void LoadDefaultConfig()
	    {
		    Config["[GameStores] Использовать GameStores? (Если OVH, оставьте false)"] = gameStores = GetConfig("[GameStores] Использовать GameStores?", false);
		    Config["[GameStores] Секретный ключ"] = secretKey = GetConfig("[GameStores] Секретный ключ", "XXXX");
		    Config["[GameStores] SHOP.ID"] = shopId = GetConfig("[GameStores] SHOP.ID", "XXXX");
		    Config["Бонус тому кто пригласил (в рублях):"] = rewardRefer = GetConfig("Бонус тому кто пригласил (в рублях):", 20);
		    Config["Бонус тому кого пригласили (в рублях):"] = rewardReferral = GetConfig("Бонус тому кого пригласили (в рублях):", 10);
		    SaveConfig();
	    }
	    
	    void LoadData()
	    {
		    if (!Interface.Oxide.DataFileSystem.ExistsDatafile("RefferalSystem"))
		    {
			    refferals = new List<ulong>();
			    Interface.Oxide.DataFileSystem.WriteObject("RefferalSystem", refferals);
			    PrintWarning("Failed to load datafile. Creating new datafile...");
		    }
		    else
			    refferals = Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>("RefferalSystem");
	    }
	    
		#endregion
		
		#region Commands

		[ChatCommand("refer")]
        void CmdRefer(BasePlayer player, string command, string[] args)
        {
			if (args.Length != 1 || args[0].Length !=17) 
			{
				SendReply(player, lang.GetMessage("R.SYNTAX", this, player.UserIDString));
				return;
			}
			if (refferals.Contains(player.userID))
			{
				SendReply(player, lang.GetMessage("R.ALREADY", this, player.UserIDString));
				return;
			}
	        
	        BasePlayer refer = BasePlayer.FindByID(Convert.ToUInt64(args[0]));
	        
			if (refer == null)
			{	
				SendReply(player, lang.GetMessage("R.NOPLAYER", this, player.UserIDString));
				return;
			} 
			if (refer.userID == player.userID) 
			{
				SendReply(player, lang.GetMessage("R.SELF", this, player.UserIDString));
				return;
			} 
				
			if (gameStores)
			{
				MoneyPlus(player.userID, rewardReferral);
				MoneyPlus(refer.userID, rewardRefer);
			}
			else
			{
				plugins.Find("RustStore").CallHook("APIChangeUserBalance", player.userID, rewardReferral, null);
				plugins.Find("RustStore").CallHook("APIChangeUserBalance", refer.userID, rewardRefer, null);
			}
	        
			refferals.Add(player.userID);
			SendReply(player, lang.GetMessage("R.SUCCESS", this, player.UserIDString), refer);
        }

		#endregion
		
		#region Save
	    
	    void OnServerSave() => Interface.Oxide.DataFileSystem.WriteObject("RefferalSystem", refferals);

		#endregion
	    
	    #region Helper
	    
	    T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
	    
	    #endregion
		
		#region GameStores
		
		void MoneyPlus(ulong userId, int amount)
		{
			ExecuteApiRequest(new Dictionary<string, string>()
			{
				{ "action", "moneys" },
				{ "type", "plus" },
				{ "steam_id", userId.ToString() },
				{ "amount", amount.ToString() }
			});
		}

		void ExecuteApiRequest(Dictionary<string, string> args)
		{
			string url = $"http://panel.gamestores.ru/api?shop_id={shopId}&secret={secretKey}" +
			$"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
			webrequest.EnqueueGet(url, (i, s) =>
			{
				if (i != 200)
				{
					PrintError($"[ERROR]{url}\nCODE {i}: {s}");
				}
			}, this);
		}

		#endregion
    }
}