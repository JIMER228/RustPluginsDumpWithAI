using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("WeatherManager", "Kaidoz | vk.com/kaidoz", "1.0.1")]
    [Description("Позволяет управлять погодой")]

    class WeatherManager : CovalencePlugin
    {

		private const string perm = "WeatherManager.admin";
	
        float day => GetConfig("ПродолжительностьДня", 1200f); //в секундах
        float night => GetConfig("ПродолжительностьНочи", 420f); //в секундах
        bool rain => GetConfig("Дождь", true); //true - дождь включен, false -  дождь выключен
		float raintime => GetConfig("ЧастотаДождя", 3600f); //в секундах
        bool storm => GetConfig("Буря", true); //true - буря включена, false - буря выключена
		float stormtime => GetConfig("ЧастотаБури", 7200f); //в секундах

        protected override void LoadDefaultConfig()
        {
            Config["ПродолжительностьДня"] = day;
            Config["ПродолжительностьНочи"] = night;
            Config["Дождь"] = rain;
            Config["ЧастотаДождя"] = raintime;
            Config["Буря"] = storm;
            Config["ЧастотаБури"] = stormtime;
            SaveConfig();
        }
		
		private void Init()
        {
			TimeManager.Instance.DayLength = day;
			TimeManager.Instance.NightLength = night;
            permission.RegisterPermission(perm, this);
			if(rain == true){
			timer.Repeat(raintime, 0, () =>{
			server.Command("spawn RainstormServer");
			});
			}
			if(storm == true){
			timer.Repeat(stormtime, 0, () =>{
			server.Command("spawn SandstormServer");
			});
			}
			else{}
        }
        void Loaded()
        {
            LoadConfig();
        }
		
		
		public void SetDay() => server.Command("settime 0.4");
		
		public void SetNight() => server.Command("settime 1.5");
		
		public void SetTime() => server.Command("settime");
		
        [Command("night")]
        void NightSet(IPlayer player, string command, string[] args)
        {
			if (!player.HasPermission(perm))
			{
			player.Reply("У вас нет прав");
			}
			else{
			SetNight();	
			player.Reply("<color=189cff>|Сервер|</color> Погода убрана");
			}
        }

        [Command("day")]
        void DaySet(IPlayer player, string command, string[] args)
        {
			if (!player.HasPermission(perm))
			{
			player.Reply("У вас нет прав");
			}
			else{
			SetDay();
			player.Reply("<color=189cff>|Сервер|</color> Погода убрана");
			}
        }	
		
		
		
        [Command("weatherclear")]
        void ControlW(IPlayer player, string command, string[] args)
        {
			if (!player.HasPermission(perm))
			{
			player.Reply("У вас нет прав");
			}
			else{
            server.Command("destroyall RainstormServer");
            server.Command("destroyall SandstormServer");
			player.Reply("<color=189cff>|Сервер|</color> Погода убрана");
			}
        }		
			
        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
    }
}