using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    [Info("Money Refund", "YaMang -w-", "1.0.1")]
    [Description("If you have this permission, this item will refund to buyer")]
    public class MoneyRefund : RustPlugin
    {
        [PluginReference] Plugin ServerRewards, Economics;

        #region Command
        [ConsoleCommand("mrefund")]
        private void MRefund(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon) return;

            //user&group id perm amount eco&sr
            if (arg.Args.Length == 5)
            {
                var target = BasePlayer.FindByID(Convert.ToUInt64(arg.Args[1]));

                if(arg.Args[0] == "user")
                {
                    if (permission.UserHasPermission(target.UserIDString, arg.Args[2]))
                    {
                        MRefund(target, arg.Args[4], arg.Args[3]);
                    }
                    else
                    {
                        Server.Command($"o.grant user {target.UserIDString} {arg.Args[2]}");
                    }
                }
                else if (arg.Args[0] == "group")
                {
                    if (permission.UserHasGroup(target.UserIDString, arg.Args[2]))
                    {
                        MRefund(target, arg.Args[4], arg.Args[3], false);
                    }
                    else
                    {
                        Server.Command($"o.usergroup add {target.UserIDString} {arg.Args[2]}");
                    }
                }
            }
        }
        #endregion
        
        #region Funtion
        private void Messages(BasePlayer player, string text)
        {
            player.SendConsoleCommand("chat.add", 2, _config.SteamID, $"{_config.Prefix} {text}");
        }

        private bool MRefund(BasePlayer target, string pay, string amount, bool perm = true)
        {
            switch (pay.ToLower().Trim())
            {
                case "eco":
                    Economics.Call("Deposit", target.UserIDString, Convert.ToUInt64(pay));
                    Messages(target, $"{(perm ? Lang("HasPerm", amount, Lang("ECO")) : Lang("HasGroup", amount, Lang("ECO")))}");
                    break;

                case "rp":
                    ServerRewards.Call("AddPoints", target.userID, Convert.ToInt32(amount));
                    Messages(target, $"{(perm ? Lang("HasPerm", amount, Lang("RP")) : Lang("HasGroup", amount, Lang("RP")))}");
                    break;
            }
            return false;
        }
        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {

                { "HasPerm", "<color=red>You already have this permission</color> <color=yellow>I got {0} {1} back</color>" },
                { "HasGroup", "<color=red>You already have this group</color> <color=yellow>I got {0} {1} back</color>" },
                { "RP", "<color=lime>RP</color>" },
                { "ECO", "<color=lime>ECO</color>" }

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "HasPerm", "<color=red>이미 보유한 권한 입니다.</color> <color=yellow>{0} {1} 를 돌려 받았습니다.</color>" },
                { "HasGroup", "<color=red>이미 보유한 그룹 입니다.</color> <color=yellow>{0} {1} 를 돌려 받았습니다.</color>" },
                { "RP", "<color=lime>RP</color>" },
                { "ECO", "<color=lime>ECO</color>" }

            }, this, "ko");
        }

        private string Lang(string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this), args);
        }

        #endregion

        #region Config        
        private ConfigData _config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Prefix")] public string Prefix { get; set; }
            [JsonProperty(PropertyName = "SteamID")] public ulong SteamID { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Prefix = "<color=#00ffff>[ Money Refund ] - </color>\n",
                SteamID = 0,
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");
            _config.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion
    }
}
