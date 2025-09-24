using System;
using System.Linq;


namespace Oxide.Plugins
{
    //Define:FileOrder=5
    public partial class GatherRewards
    {
        private float CheckPoints(string animal, float defaultAmount = 0)
        {
            foreach (var configValue in _config.Rewards.Where(configValue =>
                         string.Equals(animal, configValue.Key, StringComparison.CurrentCultureIgnoreCase)))
            {
                animal = configValue.Key;
                return _config.Rewards[configValue.Key];
            }

            if (defaultAmount == 0) return defaultAmount;

            if (_config.Settings.AddMissingRewards)
            {
                _config.Rewards.Add(animal, defaultAmount);
                Config.WriteObject(_config);
            }

            return defaultAmount;
        }

        private void GiveCredit(BasePlayer player, string type, float amount, string gathered)
        {
            if(amount==0) return;
            foreach (var groupModifier in _config.Settings.GroupModifiers.OrderByDescending(x=>x.Value))
            {
                if (permission.UserHasPermission(player.UserIDString, groupModifier.Key))
                {
                    amount *= float.Parse(groupModifier.Value.ToString());
                    break;
                }
            }

            if (amount > 0)
            {
                if (_config.Settings.UseEconomics && Economics)
                {
                    Economics.Call("Deposit", player.UserIDString, (double)amount);
                }

                if (_config.Settings.UseServerRewards && ServerRewards)
                {
                    ServerRewards.Call("AddPoints", new object[] { player.userID, (int)amount });
                }

                if (type == "gather" && _config.Settings.ShowMessagesOnGather)
                {
                    var message = _config.Settings.PluginPrefix + " " + string.Format(
                        Lang("ReceivedForGather", player.UserIDString), amount,
                        gathered.ToLower());
                    PrintToChat(player, message);
                    if (_config.Settings.UseUINotify)
                    {
                        UINotify?.Call("SendNotify", player.userID, _config.Settings.UINotifyMessageType, message);
                    }
                }
                else if (type == "kill" && _config.Settings.ShowMessagesOnKill)
                {
                    var message = _config.Settings.PluginPrefix + " " +
                                  string.Format(Lang("ReceivedForKill", player.UserIDString), amount, gathered.ToLower());
                    PrintToChat(player, message);
                    if (_config.Settings.UseUINotify)
                    {
                        UINotify?.Call("SendNotify", player.userID, _config.Settings.UINotifyMessageType, message);
                    }
                }
            }
            else
            {
                amount = Math.Abs(amount);

                if (_config.Settings.UseEconomics && Economics)
                {
                    Economics.Call("Withdraw", player.UserIDString, (double)amount);
                }

                if (_config.Settings.UseServerRewards && ServerRewards)
                {
                    var points = ServerRewards.Call<int>("CheckPoints", player.userID);
                    if (points < amount && points > 0)
                    {
                        amount = points;
                    }
                    ServerRewards.Call("TakePoints", new object[] { player.userID, (int)amount });
                }

                if (type == "gather" && _config.Settings.ShowMessagesOnGather)
                {
                    var message = _config.Settings.PluginPrefix + " " +
                                  string.Format(Lang("LostForGather", player.UserIDString), amount, gathered.ToLower());
                    PrintToChat(player,message);
                    if (_config.Settings.UseUINotify)
                    {
                        UINotify?.Call("SendNotify", player.userID, _config.Settings.UINotifyMessageType, message);
                    }
                }
                else if (type == "kill" && _config.Settings.ShowMessagesOnKill)
                {
                    var message = _config.Settings.PluginPrefix + " " +
                                  string.Format(Lang("LostForKill", player.UserIDString), amount, gathered.ToLower());
                    PrintToChat(player,message);
                    if (_config.Settings.UseUINotify)
                    {
                        UINotify?.Call("SendNotify", player.userID, _config.Settings.UINotifyMessageType, message);
                    }
                }
            }
        }

        private static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            var a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

    }
}