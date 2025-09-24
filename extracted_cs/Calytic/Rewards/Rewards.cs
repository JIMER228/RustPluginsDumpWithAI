using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info ("Rewards", "Tarek", "1.3.2")]
    [Description ("Rewards players for activities using Economic and/or ServerRewards")]
    public class Rewards : RustPlugin
    {
        [PluginReference]
        Plugin Economics, ServerRewards, Friends, Clans, HumanNPC;

        bool IsFriendsLoaded;
        bool IsEconomicsLoaded;
        bool IsServerRewardsLoaded;
        bool IsClansLoaded;

        bool HappyHourActive;
        TimeSpan hhstart; TimeSpan hhend;

        StoredData storedData;

        public List<string> Options_itemList = new List<string> { "NPCReward_Enabled", "Permission_Multiplier_Enabled", "ActivityReward_Enabled", "WelcomeMoney_Enabled", "WeaponMultiplier_Enabled", "DistanceMultiplier_Enabled", "UseEconomicsPlugin", "UseServerRewardsPlugin", "UseFriendsPlugin", "UseClansPlugin", "Economincs_TakeMoneyFromVictim", "ServerRewards_TakeMoneyFromVictim", "PrintToConsole", "HappyHour_Enabled" };
        public List<string> Multipliers_itemList = new List<string> { "LR300", "HuntingBow", "Crossbow", "AssaultRifle", "PumpShotgun", "SemiAutomaticRifle", "Thompson", "CustomSMG", "BoltActionRifle", "TimedExplosiveCharge", "M249", "EokaPistol", "Revolver", "WaterpipeShotgun", "SemiAutomaticPistol", "DoubleBarrelShotgun", "SatchelCharge", "distance_50", "distance_100", "distance_200", "distance_300", "distance_400", "HappyHourMultiplier", "M92Pistol", "MP5A4", "RocketLauncher", "BeancanGrenade", "F1Grenade", "Machete", "Longsword", "Mace", "SalvagedCleaver", "SalvagedSword", "StoneSpear", "WoodenSpear" };
        public List<string> Rewards_itemList = new List<string> { "human", "bear", "wolf", "chicken", "horse", "boar", "stag", "helicopter", "autoturret", "ActivityRewardRate_minutes", "ActivityReward", "WelcomeMoney", "HappyHour_BeginHour", "HappyHour_EndHour", "NPCKill_Reward" };
        RewardRates rewardrates = new RewardRates ();
        Options options = new Options ();
        Multipliers multipliers = new Multipliers ();

        Dictionary<BasePlayer, int> LastReward = new Dictionary<BasePlayer, int> ();

        void OnServerInitialized ()
        {
            if (options.UseEconomicsPlugin && Economics != null)
                IsEconomicsLoaded = true;
            else if (options.UseEconomicsPlugin && Economics == null)
                PrintWarning ("Economics plugin was not found! Can't reward players using Economics.");
            if (options.UseServerRewardsPlugin && ServerRewards != null)
                IsServerRewardsLoaded = true;
            else if (options.UseServerRewardsPlugin && ServerRewards == null)
                PrintWarning ("ServerRewards plugin was not found! Can't reward players using ServerRewards.");
            if (options.UseFriendsPlugin && Friends != null)
                IsFriendsLoaded = true;
            else if (options.UseFriendsPlugin && Friends == null)
                PrintWarning ("Friends plugin was not found! Can't check if victim is friend to killer.");
            if (options.UseClansPlugin && Clans != null)
                IsClansLoaded = true;
            else if (options.UseClansPlugin && Clans == null)
                PrintWarning ("Clans plugin was not found! Can't check if victim is in the same clan of killer.");
        }

        protected override void LoadDefaultConfig ()
        {
            PrintWarning ("Creating a new configuration file");
            Config ["Version"] = Version.ToString ();
            Config ["Rewards"] = GetDefaultRewardRate ();
            Config ["Multipliers"] = GetDefaultMultipliers ();
            Config ["Options"] = GetDefaultOptions ();
            SaveConfig ();
            LoadConfig ();
        }

        protected override void LoadDefaultMessages ()
        {
            lang.RegisterMessages (new Dictionary<string, string> {
                ["KillReward"] = "You received {0}. Reward for killing {1}",
                ["ActivityReward"] = "You received {0}. Reward for activity",
                ["WelcomeReward"] = "Welcome to server! You received {0} as a welcome reward",
                ["VictimNoMoney"] = "{0} doesn't have enough money.",
                ["SetRewards"] = "Variables you can set:",
                ["RewardSet"] = "Reward was set",
                ["stag"] = "a stag",
                ["boar"] = "a boar",
                ["horse"] = "a horse",
                ["bear"] = "a bear",
                ["wolf"] = "a wolf",
                ["chicken"] = "a chicken",
                ["autoturret"] = "an autoturret",
                ["helicopter"] = "a helicopter",
                ["Prefix"] = "Rewards",
                ["HappyHourStart"] = "Happy hour started",
                ["HappyHourEnd"] = "Happy hour ended"
            }, this);
        }

        void SaveData ()
        {
            Interface.Oxide.DataFileSystem.WriteObject ("Rewards", storedData);
            Puts ("Data saved");
        }

        RewardRates GetDefaultRewardRate ()
        {
            return new RewardRates {
                human = 50,
                bear = 35,
                wolf = 30,
                chicken = 15,
                horse = 15,
                boar = 15,
                stag = 10,
                helicopter = 250,
                autoturret = 150,
                ActivityRewardRate_minutes = 30,
                ActivityReward = 25,
                WelcomeMoney = 250,
                HappyHour_BeginHour = 20,
                HappyHour_EndHour = 23,
                NPCKill_Reward = 50
            };
        }

        Multipliers GetDefaultMultipliers ()
        {
            return new Multipliers {
                AssaultRifle = 1.5,
                BoltActionRifle = 1.5,
                HuntingBow = 1,
                PumpShotgun = 1,
                Thompson = 1.3,
                SemiAutomaticRifle = 1.3,
                Crossbow = 1.3,
                CustomSMG = 1.5,
                M249 = 1.5,
                SemiAutomaticPistol = 1,
                WaterpipeShotgun = 1.4,
                EokaPistol = 1.1,
                Revolver = 1.2,
                TimedExplosiveCharge = 2,
                SatchelCharge = 2,
                DoubleBarrelShotgun = 1.5,
                distance_50 = 1,
                distance_100 = 1.3,
                distance_200 = 1.5,
                distance_300 = 2,
                distance_400 = 3,
                HappyHourMultiplier = 2,
                LR300 = 1.5,
                M92Pistol = 1,
                MP5A4 = 1.5,
                RocketLauncher = 2,
                BeancanGrenade = 1.5,
                F1Grenade = 1.5,
                Machete = 2,
                Longsword = 1.5,
                Mace = 1,
                SalvagedCleaver = 1,
                SalvagedSword = 1,
                StoneSpear = 1,
                WoodenSpear = 1,
                Permissions = new Dictionary<string, int> {
                    {"rewards.vip",2}
                }
            };
        }

        Options GetDefaultOptions ()
        {
            return new Options {
                ActivityReward_Enabled = true,
                WelcomeMoney_Enabled = true,
                UseEconomicsPlugin = true,
                UseServerRewardsPlugin = false,
                UseFriendsPlugin = true,
                UseClansPlugin = true,
                Economincs_TakeMoneyFromVictim = true,
                ServerRewards_TakeMoneyFromVictim = false,
                WeaponMultiplier_Enabled = true,
                DistanceMultiplier_Enabled = true,
                PrintToConsole = true,
                HappyHour_Enabled = true,
                Permission_Multiplier_Enabled = false,
                NPCReward_Enabled = false
            };
        }

        void FixConfig ()
        {
            try {
                Dictionary<string, object> temp;
                Dictionary<string, object> temp2;
                Dictionary<string, object> temp3;

                var rr = GetDefaultRewardRate ();
                var o = GetDefaultOptions ();
                var m = GetDefaultMultipliers ();

                try { temp = (Dictionary<string, object>)Config ["Rewards"]; } catch { Config ["Rewards"] = rr; SaveConfig (); temp = (Dictionary<string, object>)Config ["Rewards"]; }
                try { temp2 = (Dictionary<string, object>)Config ["Options"]; } catch { Config ["Options"] = o; SaveConfig (); temp2 = (Dictionary<string, object>)Config ["Options"]; }
                try { temp3 = (Dictionary<string, object>)Config ["Multipliers"]; } catch { Config ["Multipliers"] = m; SaveConfig (); temp3 = (Dictionary<string, object>)Config ["Multipliers"]; }
                foreach (var s in Rewards_itemList) {
                    if (!temp.ContainsKey (s)) {
                        Config ["Rewards", s] = rr.GetItemByString (s);
                    }
                }
                foreach (var s in Options_itemList) {
                    if (!temp2.ContainsKey (s)) {
                        Config ["Options", s] = o.GetItemByString (s);
                    }
                }
                foreach (var s in Multipliers_itemList) {
                    if (!temp3.ContainsKey (s)) {
                        Config ["Multipliers", s] = m.GetItemByString (s);
                    }
                }

                if (!temp3.ContainsKey ("Permissions")) {
                    Config ["Multipliers", "Permissions"] = new Dictionary<string, object> {
                        {"rewards.vip", 2}
                    };
                }

                Config ["Version"] = Version.ToString ();
                SaveConfig ();
            } catch (Exception ex) {
                Puts (ex.Message);
                Puts ("Couldn't fix. Creating new config file");
                Config.Clear ();
                LoadDefaultConfig ();
                Loadcfg ();
            }
        }

        void Loadcfg ()
        {
            try {
                if (Version.ToString () != Config ["Version"].ToString ()) {
                    Puts ("Outdated config file. Fixing");
                    FixConfig ();
                }
            } catch (Exception e) {
                Puts ("Outdated config file. Fixing");
                FixConfig ();
            }
            try {
                Dictionary<string, object> temp = (Dictionary<string, object>)Config ["Rewards"];
                rewardrates.ActivityReward = Convert.ToDouble (temp ["ActivityReward"]);
                rewardrates.ActivityRewardRate_minutes = Convert.ToDouble (temp ["ActivityRewardRate_minutes"]);
                rewardrates.autoturret = Convert.ToDouble (temp ["autoturret"]);
                rewardrates.bear = Convert.ToDouble (temp ["bear"]);
                rewardrates.boar = Convert.ToDouble (temp ["boar"]);
                rewardrates.chicken = Convert.ToDouble (temp ["chicken"]);
                rewardrates.helicopter = Convert.ToDouble (temp ["helicopter"]);
                rewardrates.horse = Convert.ToDouble (temp ["horse"]);
                rewardrates.human = Convert.ToDouble (temp ["human"]);
                rewardrates.stag = Convert.ToDouble (temp ["stag"]);
                rewardrates.WelcomeMoney = Convert.ToDouble (temp ["WelcomeMoney"]);
                rewardrates.wolf = Convert.ToDouble (temp ["wolf"]);
                rewardrates.HappyHour_BeginHour = Convert.ToDouble (temp ["HappyHour_BeginHour"]);
                rewardrates.HappyHour_EndHour = Convert.ToDouble (temp ["HappyHour_EndHour"]);
                rewardrates.NPCKill_Reward = Convert.ToDouble (temp ["NPCKill_Reward"]);

                Dictionary<string, object> temp2 = (Dictionary<string, object>)Config ["Options"];
                options.ActivityReward_Enabled = (bool)temp2 ["ActivityReward_Enabled"];
                options.DistanceMultiplier_Enabled = (bool)temp2 ["DistanceMultiplier_Enabled"];
                options.Economincs_TakeMoneyFromVictim = (bool)temp2 ["Economincs_TakeMoneyFromVictim"];
                options.ServerRewards_TakeMoneyFromVictim = (bool)temp2 ["ServerRewards_TakeMoneyFromVictim"];
                options.UseClansPlugin = (bool)temp2 ["UseClansPlugin"];
                options.UseEconomicsPlugin = (bool)temp2 ["UseEconomicsPlugin"];
                options.UseFriendsPlugin = (bool)temp2 ["UseFriendsPlugin"];
                options.UseServerRewardsPlugin = (bool)temp2 ["UseServerRewardsPlugin"];
                options.WeaponMultiplier_Enabled = (bool)temp2 ["WeaponMultiplier_Enabled"];
                options.WelcomeMoney_Enabled = (bool)temp2 ["WelcomeMoney_Enabled"];
                options.PrintToConsole = (bool)temp2 ["PrintToConsole"];
                options.Permission_Multiplier_Enabled = (bool)temp2 ["Permission_Multiplier_Enabled"];
                options.NPCReward_Enabled = (bool)temp2 ["NPCReward_Enabled"];
                options.HappyHour_Enabled = (bool)temp2 ["HappyHour_Enabled"];

                Dictionary<string, object> temp3 = (Dictionary<string, object>)Config ["Multipliers"];
                multipliers.AssaultRifle = Convert.ToDouble (temp3 ["AssaultRifle"]);
                multipliers.BoltActionRifle = Convert.ToDouble (temp3 ["BoltActionRifle"]);
                multipliers.HuntingBow = Convert.ToDouble (temp3 ["HuntingBow"]);
                multipliers.PumpShotgun = Convert.ToDouble (temp3 ["PumpShotgun"]);
                multipliers.Thompson = Convert.ToDouble (temp3 ["Thompson"]);
                multipliers.SemiAutomaticRifle = Convert.ToDouble (temp3 ["SemiAutomaticRifle"]);
                multipliers.Crossbow = Convert.ToDouble (temp3 ["Crossbow"]);
                multipliers.CustomSMG = Convert.ToDouble (temp3 ["CustomSMG"]);
                multipliers.M249 = Convert.ToDouble (temp3 ["M249"]);
                multipliers.TimedExplosiveCharge = Convert.ToDouble (temp3 ["TimedExplosiveCharge"]);
                multipliers.EokaPistol = Convert.ToDouble (temp3 ["EokaPistol"]);
                multipliers.Revolver = Convert.ToDouble (temp3 ["Revolver"]);
                multipliers.SemiAutomaticPistol = Convert.ToDouble (temp3 ["SemiAutomaticPistol"]);
                multipliers.WaterpipeShotgun = Convert.ToDouble (temp3 ["WaterpipeShotgun"]);
                multipliers.DoubleBarrelShotgun = Convert.ToDouble (temp3 ["DoubleBarrelShotgun"]);
                multipliers.SatchelCharge = Convert.ToDouble (temp3 ["SatchelCharge"]);
                multipliers.distance_50 = Convert.ToDouble (temp3 ["distance_50"]);
                multipliers.distance_100 = Convert.ToDouble (temp3 ["distance_100"]);
                multipliers.distance_200 = Convert.ToDouble (temp3 ["distance_200"]);
                multipliers.distance_300 = Convert.ToDouble (temp3 ["distance_300"]);
                multipliers.distance_400 = Convert.ToDouble (temp3 ["distance_400"]);
                multipliers.HappyHourMultiplier = Convert.ToDouble (temp3 ["HappyHourMultiplier"]);
                multipliers.LR300 = Convert.ToDouble (temp3 ["LR300"]);
                //NEW
                multipliers.M92Pistol = Convert.ToDouble (temp3 ["M92Pistol"]);
                multipliers.MP5A4 = Convert.ToDouble (temp3 ["MP5A4"]);
                multipliers.RocketLauncher = Convert.ToDouble (temp3 ["RocketLauncher"]);
                multipliers.BeancanGrenade = Convert.ToDouble (temp3 ["BeancanGrenade"]);
                multipliers.F1Grenade = Convert.ToDouble (temp3 ["F1Grenade"]);
                multipliers.Machete = Convert.ToDouble (temp3 ["Machete"]);
                multipliers.Longsword = Convert.ToDouble (temp3 ["Longsword"]);
                multipliers.Mace = Convert.ToDouble (temp3 ["Mace"]);
                multipliers.SalvagedCleaver = Convert.ToDouble (temp3 ["SalvagedCleaver"]);
                multipliers.SalvagedSword = Convert.ToDouble (temp3 ["SalvagedSword"]);
                multipliers.StoneSpear = Convert.ToDouble (temp3 ["StoneSpear"]);
                multipliers.WoodenSpear = Convert.ToDouble (temp3 ["WoodenSpear"]);

                if (!temp3.ContainsKey ("Permissions")) {
                    multipliers.Permissions = new Dictionary<string, int> {
                        {"rewards.vip", 2}
                    };

                    permission.RegisterPermission ("rewards.vip", this);
                } else {
                    Dictionary<string, object> temp4 = (Dictionary<string, object>)temp3 ["Permissions"];

                    if (multipliers.Permissions == null) {
                        multipliers.Permissions = new Dictionary<string, int> ();
                    }

                    foreach (KeyValuePair<string, object> kvp in temp4) {
                        multipliers.Permissions.Add (kvp.Key, Convert.ToInt32 (kvp.Value));
                        permission.RegisterPermission (kvp.Key, this);
                    }
                }
            } catch (Exception ex) {
                Interface.Oxide.LogException ("Could not load config", ex);
            }
        }

        void Init ()
        {
            permission.RegisterPermission ("rewards.admin", this);
            permission.RegisterPermission ("rewards.showrewards", this);

            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData> (Name);

            Loadcfg ();

            if (options.HappyHour_Enabled) {
                hhstart = new TimeSpan (Convert.ToInt32 (rewardrates.HappyHour_BeginHour), 0, 0);
                hhend = new TimeSpan (Convert.ToInt32 (rewardrates.HappyHour_EndHour), 0, 0);
            }
            #region Activity Check
            if (options.ActivityReward_Enabled || options.HappyHour_Enabled) {
                timer.Repeat (60, 0, () => {
                    if (options.ActivityReward_Enabled) {
                        foreach (var p in BasePlayer.activePlayerList) {
                            if (Convert.ToDouble (p.secondsConnected) / 60 > rewardrates.ActivityRewardRate_minutes) {
                                if (LastReward.ContainsKey (p)) {
                                    if (Convert.ToDouble (p.secondsConnected - LastReward [p]) / 60 > rewardrates.ActivityRewardRate_minutes) {
                                        RewardPlayer (p, rewardrates.ActivityReward);
                                        LastReward [p] = p.secondsConnected;
                                    }
                                } else {
                                    RewardPlayer (p, rewardrates.ActivityReward);
                                    LastReward.Add (p, p.secondsConnected);
                                }
                            }
                        }
                    }
                    if (options.HappyHour_Enabled) {
                        var gameTime = GameTime ();
                        if (!HappyHourActive) {
                            if (gameTime >= rewardrates.HappyHour_BeginHour) {
                                HappyHourActive = true;
                                Puts ("Happy hour started. Ending at " + rewardrates.HappyHour_EndHour);
                                BroadcastMessage (Lang ("HappyHourStart"), Lang ("Prefix"));
                            }
                        } else {
                            if (gameTime > rewardrates.HappyHour_EndHour || gameTime < rewardrates.HappyHour_BeginHour) {
                                HappyHourActive = false;
                                Puts ("Happy hour ended");
                                BroadcastMessage (Lang ("HappyHourEnd"), Lang ("Prefix"));
                            }
                        }
                    }
                });
            }
            #endregion
        }

        bool checktime (float gtime, double cfgtime)
        {
            return false;
        }

        void OnPlayerInit (BasePlayer player)
        {
            if (options.WelcomeMoney_Enabled) {
                if (!storedData.Players.Contains (player.UserIDString)) {
                    RewardPlayer (player, rewardrates.WelcomeMoney, 1, null, true);
                    storedData.Players.Add (player.UserIDString);
                    SaveData ();
                }
            }
        }

        string Lang (string key, string id = null, params object [] args) => string.Format (lang.GetMessage (key, this, id), args);

        bool HasPerm (BasePlayer p, string pe) => permission.UserHasPermission (p.userID.ToString (), pe);

        void SendChatMessage (BasePlayer player, string msg, string prefix = null, object uid = null) => rust.SendChatMessage (player, prefix == null ? msg : "<color=#C4FF00>" + prefix + "</color>: ", msg, uid?.ToString () ?? "0");

        void BroadcastMessage (string msg, string prefix = null, object uid = null) => rust.BroadcastChat (prefix == null ? msg : "<color=#C4FF00>" + prefix + "</color>: ", msg);

        double GetMultiplier (BasePlayer attacker, BaseCombatEntity victim, string weapon)
        {
            double multiplier = 1;

            if (options.DistanceMultiplier_Enabled) {
                multiplier *= multipliers.GetDistanceM (victim.Distance2D (attacker));
            }

            if (options.WeaponMultiplier_Enabled) {
                multiplier *= multipliers.GetWeaponM (weapon);
            }

            if (HappyHourActive) {
                multiplier *= multipliers.HappyHourMultiplier;
            }

            if (options.Permission_Multiplier_Enabled) {
                var permissions = multipliers.Permissions.Where (x => permission.UserHasPermission (attacker.UserIDString, x.Key)).ToArray ();
                multiplier *= (permissions.Any ()
                        ? permissions.OrderByDescending (x => x.Value).First ().Value
                        : 1);
            }

            return multiplier;
        }

        void OnKillNPC (BasePlayer victim, HitInfo info)
        {
            if (options.NPCReward_Enabled) {
                if (info?.Initiator?.ToPlayer () == null)
                    return;
                double totalmultiplier = GetMultiplier (info?.Initiator?.ToPlayer (), victim, info?.Weapon?.GetItem ()?.info?.displayName?.english);

                RewardPlayer (info?.Initiator?.ToPlayer (), rewardrates.NPCKill_Reward, totalmultiplier, victim.displayName);
            }
        }

        void OnEntityDeath (BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;
            if (info?.Initiator?.ToPlayer () == null)
                return;

            double totalmultiplier = GetMultiplier (info?.Initiator?.ToPlayer (), victim, info?.Weapon?.GetItem ()?.info?.displayName?.english);

            if (options.NPCReward_Enabled && victim is NPCPlayer) {
                RewardPlayer (info?.Initiator?.ToPlayer (), rewardrates.NPCKill_Reward, totalmultiplier, (victim as NPCPlayer).displayName);
            } else if (victim.ToPlayer () != null) {
                if (victim.ToPlayer ().userID <= 2147483647)
                    return;
                if (info?.Initiator?.ToPlayer ().userID == victim.ToPlayer ().userID)
                    return;

                RewardForPlayerKill (info?.Initiator?.ToPlayer (), victim.ToPlayer (), totalmultiplier);
            } else if (victim.name.Contains ("assets/rust.ai/agents/")) {
                try {
                    var AnimalName = victim.name.Split (new [] { "assets/rust.ai/agents/" }, StringSplitOptions.None) [1].Split ('/') [0];
                    double rewardmoney = 0;
                    switch (AnimalName) {
                    case "stag":
                        rewardmoney = rewardrates.stag;
                        break;
                    case "boar":
                        rewardmoney = rewardrates.boar;
                        break;
                    case "horse":
                        rewardmoney = rewardrates.horse;
                        break;
                    case "bear":
                        rewardmoney = rewardrates.bear;
                        break;
                    case "wolf":
                        rewardmoney = rewardrates.wolf;
                        break;
                    case "chicken":
                        rewardmoney = rewardrates.chicken;
                        break;
                    default:
                        return;
                    }

                    RewardPlayer (info?.Initiator?.ToPlayer (), rewardmoney, totalmultiplier, Lang (AnimalName, info?.Initiator?.ToPlayer ().UserIDString));
                } catch { }
            } else if (victim.name.Contains ("helicopter/patrolhelicopter.prefab")) {
                RewardPlayer (info?.Initiator?.ToPlayer (), rewardrates.helicopter, totalmultiplier, Lang ("helicopter", info?.Initiator?.ToPlayer ().UserIDString));
            } else if (victim.name == "assets/prefabs/npc/autoturret/autoturret_deployed.prefab") {
                RewardPlayer (info?.Initiator?.ToPlayer (), rewardrates.autoturret, totalmultiplier, Lang ("autoturret", info?.Initiator?.ToPlayer ().UserIDString));
            }
        }

        void RewardPlayer (BasePlayer player, double amount, double multiplier = 1, string reason = null, bool isWelcomeReward = false)
        {
            if (amount > 0) {
                amount = amount * multiplier;

                if (options.UseEconomicsPlugin)
                    Economics?.Call ("Deposit", player.UserIDString, amount);
                if (options.UseServerRewardsPlugin)
                    ServerRewards?.Call ("AddPoints", player.userID, (int)amount);
                if (!isWelcomeReward) {
                    SendChatMessage (player, reason == null ? Lang ("ActivityReward", player.UserIDString, amount) : Lang ("KillReward", player.UserIDString, amount, reason), Lang ("Prefix"));
                    LogToFile (Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason), this);
                    if (options.PrintToConsole)
                        Puts (player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
                } else {
                    SendChatMessage (player, Lang ("WelcomeReward", player.UserIDString, amount), Lang ("Prefix"));
                    LogToFile (Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount + " as a welcome reward", this);
                    if (options.PrintToConsole)
                        Puts (player.displayName + " got " + amount + " as a welcome reward");
                }
            }
        }

        static float GameTime ()
        {
            return TOD_Sky.Instance.Cycle.Hour;
        }

        void RewardForPlayerKill (BasePlayer player, BasePlayer victim, double multiplier = 1)
        {
            if (rewardrates.human > 0) {
                bool success = true;
                bool isFriend = false;
                if (IsFriendsLoaded)
                    isFriend = (bool)Friends?.CallHook ("HasFriend", player.userID, victim.userID);
                if (!isFriend && IsClansLoaded) {
                    string pclan = (string)Clans?.CallHook ("GetClanOf", player); string vclan = (string)Clans?.CallHook ("GetClanOf", victim);
                    if (pclan == vclan)
                        isFriend = true;
                }
                if (!isFriend) {
                    var reward = rewardrates.human * multiplier;

                    if (IsEconomicsLoaded) //Eco
                    {
                        if (options.Economincs_TakeMoneyFromVictim) {
                            if (!(bool)Economics?.Call ("Transfer", victim.UserIDString, player.UserIDString, rewardrates.human * multiplier)) {
                                SendChatMessage (player, Lang ("VictimNoMoney", player.UserIDString, victim.displayName), Lang ("Prefix"));
                                success = false;
                            }
                        } else
                            Economics?.Call ("Deposit", player.UserIDString, reward);
                    }
                    if (IsServerRewardsLoaded) //ServerRewards
                    {
                        if (options.ServerRewards_TakeMoneyFromVictim)
                            ServerRewards?.Call ("TakePoints", new object [] { victim.userID, rewardrates.human * multiplier });
                        ServerRewards?.Call ("AddPoints", player.userID, (int)(reward));
                        success = true;
                    }
                    if (success) //Send message if transaction was successful
                    {
                        SendChatMessage (player, Lang ("KillReward", player.UserIDString, rewardrates.human * multiplier, victim.displayName), Lang ("Prefix"));
                        LogToFile (Name, $"[{DateTime.Now}] " + player.displayName + " got " + rewardrates.human * multiplier + " for killing " + victim.displayName, this);
                        if (options.PrintToConsole)
                            Puts (player.displayName + " got " + reward + " for killing " + victim.displayName);
                    }
                }
            }
        }

        [ConsoleCommand ("setreward")]
        void setreward (ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin) {
                try {
                    var args = arg.Args;
                    Config ["Rewards", args [0]] = Convert.ToDouble (args [1]);
                    SaveConfig ();
                    try {
                        Loadcfg ();
                    } catch {
                        FixConfig ();
                    }
                    arg.ReplyWith ("Reward set");
                } catch { arg.ReplyWith ("Variables you can set: 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'autoturret', 'ActivityReward' 'ActivityRewardRate_minutes', 'WelcomeMoney'"); }
            }
        }

        [ConsoleCommand ("showrewards")]
        void showrewards (ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin)
                arg.ReplyWith (string.Format ("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, autoturret = {8} Activity Reward Rate (minutes) = {9}, Activity Reward = {10}, WelcomeMoney = {11}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney));
        }

        [ChatCommand ("setreward")]
        void setrewardCommand (BasePlayer player, string command, string [] args)
        {
            if (HasPerm (player, "rewards.admin")) {
                try {
                    Config ["Rewards", args [0]] = Convert.ToDouble (args [1]);
                    SaveConfig ();
                    try {
                        Loadcfg ();
                    } catch {
                        FixConfig ();
                    }
                    SendChatMessage (player, Lang ("RewardSet", player.UserIDString), Lang ("Prefix"));
                } catch { SendChatMessage (player, Lang ("SetRewards", player.UserIDString) + " 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'autoturret', 'ActivityReward', 'ActivityRewardRate_minutes', 'WelcomeMoney'", Lang ("Prefix")); }
            }
        }

        [ChatCommand ("showrewards")]
        void showrewardsCommand (BasePlayer player, string command, string [] args)
        {
            if (HasPerm (player, "rewards.showrewards"))
                SendChatMessage (player, string.Format ("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, autoturret = {8} Activity Reward Rate (minutes) = {9}, Activity Reward = {10}, WelcomeMoney = {11}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney), Lang ("Prefix"));
        }

        class StoredData
        {
            public HashSet<string> Players = new HashSet<string> ();
        }

        class RewardRates
        {
            public double human { get; set; }
            public double bear { get; set; }
            public double wolf { get; set; }
            public double chicken { get; set; }
            public double horse { get; set; }
            public double boar { get; set; }
            public double stag { get; set; }
            public double helicopter { get; set; }
            public double autoturret { get; set; }
            public double ActivityRewardRate_minutes { get; set; }
            public double ActivityReward { get; set; }
            public double WelcomeMoney { get; set; }
            public double HappyHour_BeginHour { get; set; }
            public double HappyHour_EndHour { get; set; }
            public double NPCKill_Reward { get; set; }
            public double GetItemByString (string itemName)
            {
                switch (itemName) {
                case "human":
                    return human;
                case "bear":
                    return bear;
                case "wolf":
                    return wolf;
                case "chicken":
                    return chicken;
                case "horse":
                    return horse;
                case "boar":
                    return boar;
                case "stag":
                    return stag;
                case "helicopter":
                    return helicopter;
                case "autoturret":
                    return autoturret;
                case "ActivityRewardRate_minutes":
                    return ActivityRewardRate_minutes;
                case "ActivityReward":
                    return ActivityReward;
                case "WelcomeMoney":
                    return WelcomeMoney;
                case "HappyHour_BeginHour":
                    return HappyHour_BeginHour;
                case "HappyHour_EndHour":
                    return HappyHour_EndHour;
                case "NPCKill_Reward":
                    return NPCKill_Reward;
                default:
                    return 0;
                }
            }
        }

        class Multipliers
        {
            public double HuntingBow { get; set; }
            public double Crossbow { get; set; }
            public double AssaultRifle { get; set; }
            public double PumpShotgun { get; set; }
            public double SemiAutomaticRifle { get; set; }
            public double Thompson { get; set; }
            public double CustomSMG { get; set; }
            public double BoltActionRifle { get; set; }
            public double TimedExplosiveCharge { get; set; }
            public double M249 { get; set; }
            public double EokaPistol { get; set; }
            public double Revolver { get; set; }
            public double WaterpipeShotgun { get; set; }
            public double SemiAutomaticPistol { get; set; }
            public double DoubleBarrelShotgun { get; set; }
            public double SatchelCharge { get; set; }
            public double distance_50 { get; set; }
            public double distance_100 { get; set; }
            public double distance_200 { get; set; }
            public double distance_300 { get; set; }
            public double distance_400 { get; set; }
            public double HappyHourMultiplier { get; set; }
            public double LR300 { get; set; }
            public double M92Pistol { get; set; }
            public double MP5A4 { get; set; }
            public double RocketLauncher { get; set; }
            public double BeancanGrenade { get; set; }
            public double F1Grenade { get; set; }
            public double Machete { get; set; }
            public double Longsword { get; set; }
            public double Mace { get; set; }
            public double SalvagedCleaver { get; set; }
            public double SalvagedSword { get; set; }
            public double StoneSpear { get; set; }
            public double WoodenSpear { get; set; }

            public Dictionary<string, int> Permissions;

            public double GetWeaponM (string wn)
            {
                switch (wn) {
                case "Assault Rifle":
                    return AssaultRifle;
                case "Hunting Bow":
                    return HuntingBow;
                case "Bolt Action Rifle":
                    return BoltActionRifle;
                case "Crossbow":
                    return Crossbow;
                case "Thompson":
                    return Thompson;
                case "Eoka Pistol":
                    return EokaPistol;
                case "Revolver":
                    return Revolver;
                case "Custom SMG":
                    return CustomSMG;
                case "Semi-Automatic Rifle":
                    return SemiAutomaticRifle;
                case "Semi-Automatic Pistol":
                    return SemiAutomaticPistol;
                case "Pump Shotgun":
                    return PumpShotgun;
                case "Waterpipe Shotgun":
                    return WaterpipeShotgun;
                case "M249":
                    return M249;
                case "Explosivetimed":
                    return TimedExplosiveCharge;
                case "Explosivesatchel":
                    return SatchelCharge;
                case "Double Barrel Shotgun":
                    return DoubleBarrelShotgun;
                case "LR-300 Assault Rifle":
                    return LR300;
                case "M92 Pistol":
                    return M92Pistol;
                case "MP5A4":
                    return MP5A4;
                case "Rocket Launcher":
                    return RocketLauncher;
                case "Beancan Grenade":
                    return BeancanGrenade;
                case "F1 Grenade":
                    return F1Grenade;
                case "Machete":
                    return Machete;
                case "Longsword":
                    return Longsword;
                case "Mace":
                    return Mace;
                case "Salvaged Cleaver":
                    return SalvagedCleaver;
                case "Salvaged Sword":
                    return SalvagedSword;
                case "Stone Spear":
                    return StoneSpear;
                case "Wooden Spear":
                    return WoodenSpear;
                default:
                    return 1;
                }
            }

            public double GetDistanceM (float distance)
            {
                if (distance >= 400)
                    return distance_400;
                if (distance >= 300)
                    return distance_300;
                if (distance >= 200)
                    return distance_200;
                if (distance >= 100)
                    return distance_100;
                if (distance >= 50)
                    return distance_50;

                return 1;
            }

            public double GetItemByString (string itemName)
            {
                switch (itemName) {
                case "HuntingBow":
                    return HuntingBow;
                case "Crossbow":
                    return Crossbow;
                case "AssaultRifle":
                    return AssaultRifle;
                case "PumpShotgun":
                    return PumpShotgun;
                case "SemiAutomaticRifle":
                    return SemiAutomaticRifle;
                case "Thompson":
                    return Thompson;
                case "CustomSMG":
                    return CustomSMG;
                case "BoltActionRifle":
                    return BoltActionRifle;
                case "TimedExplosiveCharge":
                    return TimedExplosiveCharge;
                case "M249":
                    return M249;
                case "EokaPistol":
                    return EokaPistol;
                case "Revolver":
                    return Revolver;
                case "WaterpipeShotgun":
                    return WaterpipeShotgun;
                case "SemiAutomaticPistol":
                    return SemiAutomaticPistol;
                case "DoubleBarrelShotgun":
                    return DoubleBarrelShotgun;
                case "SatchelCharge":
                    return SatchelCharge;
                case "distance_50":
                    return distance_50;
                case "distance_100":
                    return distance_100;
                case "distance_200":
                    return distance_200;
                case "distance_300":
                    return distance_300;
                case "distance_400":
                    return distance_400;
                case "HappyHourMultiplier":
                    return HappyHourMultiplier;
                case "LR300":
                    return LR300;
                case "M92 Pistol":
                    return M92Pistol;
                case "MP5A4":
                    return MP5A4;
                case "Rocket Launcher":
                    return RocketLauncher;
                case "Beancan Grenade":
                    return BeancanGrenade;
                case "F1 Grenade":
                    return F1Grenade;
                case "Machete":
                    return Machete;
                case "Longsword":
                    return Longsword;
                case "Mace":
                    return Mace;
                case "Salvaged Cleaver":
                    return SalvagedCleaver;
                case "Salvaged Sword":
                    return SalvagedSword;
                case "Stone Spear":
                    return StoneSpear;
                case "Wooden Spear":
                    return WoodenSpear;
                default:
                    return 0;
                }
            }
        }

        class Options
        {
            public bool ActivityReward_Enabled { get; set; }
            public bool WelcomeMoney_Enabled { get; set; }
            public bool WeaponMultiplier_Enabled { get; set; }
            public bool DistanceMultiplier_Enabled { get; set; }
            public bool HappyHour_Enabled { get; set; }
            public bool Permission_Multiplier_Enabled { get; set; }
            public bool UseEconomicsPlugin { get; set; }
            public bool UseServerRewardsPlugin { get; set; }
            public bool UseFriendsPlugin { get; set; }
            public bool UseClansPlugin { get; set; }
            public bool Economincs_TakeMoneyFromVictim { get; set; }
            public bool ServerRewards_TakeMoneyFromVictim { get; set; }
            public bool PrintToConsole { get; set; }
            public bool NPCReward_Enabled { get; set; }
            public bool GetItemByString (string itemName)
            {
                switch (itemName) {
                case "ActivityReward_Enabled":
                    return ActivityReward_Enabled;
                case "WelcomeMoney_Enabled":
                    return WelcomeMoney_Enabled;
                case "WeaponMultiplier_Enabled":
                    return WeaponMultiplier_Enabled;
                case "DistanceMultiplier_Enabled":
                    return DistanceMultiplier_Enabled;
                case "UseEconomicsPlugin":
                    return UseEconomicsPlugin;
                case "UseServerRewardsPlugin":
                    return UseServerRewardsPlugin;
                case "UseFriendsPlugin":
                    return UseFriendsPlugin;
                case "UseClansPlugin":
                    return UseClansPlugin;
                case "Economincs_TakeMoneyFromVictim":
                    return Economincs_TakeMoneyFromVictim;
                case "ServerRewards_TakeMoneyFromVictim":
                    return ServerRewards_TakeMoneyFromVictim;
                case "PrintToConsole":
                    return PrintToConsole;
                case "HappyHour_Enabled":
                    return HappyHour_Enabled;
                case "Permission_Multiplier_Enabled":
                    return Permission_Multiplier_Enabled;
                case "NPCReward_Enabled":
                    return NPCReward_Enabled;
                default:
                    return false;
                }
            }
        }
    }
}
