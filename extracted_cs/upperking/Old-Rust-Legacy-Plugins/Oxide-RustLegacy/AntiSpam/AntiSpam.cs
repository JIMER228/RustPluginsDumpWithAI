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
    public class AntiSpam : RustLegacyPlugin
    {
        public Dictionary<ulong, double> Cooldown = new Dictionary<ulong, double>();
        public Dictionary<ulong, int> Detections = new Dictionary<ulong, int>();
        public int Seconds = 2;
        public int MaxWarnings = 3;
        public string Message = "[color red]DO NOT SPAM!!!";

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
        void OnPlayerChat(NetUser netuser, string message)
        {
            if (!Cooldown.ContainsKey(netuser.userID))
            {
                Cooldown[netuser.userID] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            }
            else
            {
                double calc = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - Cooldown[netuser.userID];
                if (calc < Seconds)
                {
                    SendReply(netuser, Message);
                    if (!Detections.ContainsKey(netuser.userID))
                    {
                        Detections[netuser.userID] = 0;
                    }
                    Detections[netuser.userID] = Detections[netuser.userID] + 1;
                    if (Detections[netuser.userID] == MaxWarnings)
                    {
                        netuser.Kick(NetError.Facepunch_Kick_Violation, true);
                        foreach (NetUser all in rust.GetAllNetUsers())
                        {
                            rust.Notice(all, netuser.displayName + " has been auto kicked for spamming");
                        }
                    }
                }
                else
                {
                    Cooldown[netuser.userID] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    if(Detections.ContainsKey(netuser.userID))
                    {
                        Detections.Remove(netuser.userID);
                    }
                }
            }
        }
        void OnPlayerDisconnected(uLink.NetworkPlayer netuser)
        {
            NetUser net = netuser.GetLocalData<NetUser>();
            if(Detections.ContainsKey(net.userID))
            {
                Detections.Remove(net.userID);
            }
            if(Cooldown.ContainsKey(net.userID))
            {
                Cooldown.Remove(net.userID);
            }
        }
    }
}
