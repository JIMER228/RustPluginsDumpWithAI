using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("AntiSpam", "ice cold", "1.0")]
    [Description("Prevents spamming in chat")]
    public class AntiSpam : RustPlugin
    {
        public Dictionary<ulong, double> Cooldown = new Dictionary<ulong, double>();
        public Dictionary<ulong, int> Detections = new Dictionary<ulong, int>();

        public int Seconds = 2;
        public int MaxWarnings = 3;
        public string Message = "<color=#ff0000ff>DO NOT SPAM!!!</color>";

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        void Init()
        {
            CheckCfg<int>("Option: SpamSeconds", ref Seconds);
            CheckCfg<int>("Option: Max warnings before kick", ref MaxWarnings);
            CheckCfg<string>("Message: Warning message", ref Message);
            SaveConfig();
        }
        void Loaded()
        {
            Cooldown.Clear();
            Detections.Clear();
        }
        public void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer talker = BasePlayer.FindByID(arg.Connection.userid);
            if(!Cooldown.ContainsKey(talker.userID))
            {
                Cooldown[talker.userID] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            }
            else{
                double calc = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - Cooldown[talker.userID];
                if (calc < Seconds)
                {
                    SendReply(talker, Message);
                    if (!Detections.ContainsKey(talker.userID))
                    {
                        Detections[talker.userID] = 0;
                    }
                    Detections[talker.userID] = Detections[talker.userID] + 1;
                    if (Detections[talker.userID] == MaxWarnings)
                    {
                        talker.Kick("Spamming");
                        rust.BroadcastChat("AntiSpam", talker.displayName + " has been autokicked for spamming");
                    }
                }
                else
                {
                    Cooldown[talker.userID] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    if (Detections.ContainsKey(talker.userID))
                    {
                        Detections.Remove(talker.userID);
                    }
                }
            }
        }
        void OnPlayerDisconnected(BasePlayer user, string reason)
        {
            if(Detections.ContainsKey(user.userID))
            {
                Detections.Remove(user.userID);
            }
            if (Cooldown.ContainsKey(user.userID))
            {
                Cooldown.Remove(user.userID);
            }
        }
    }
}
