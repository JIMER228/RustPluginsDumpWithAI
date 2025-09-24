// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    [Info("Advanced Loading Messages", "Dana", "1.0.8")]
    [Description("An advanced version of the plugin LoadingMessages by VVoid.")]
    public class AdvancedLoadingMessages : RustPlugin
    {
        #region PluginReference

        [PluginReference]
        private Plugin ServerRewards, PlaytimeTracker, Economics;

        #endregion


        #region Variables

        private static MsgConfig _config;
        private Timer _timer;
        private List<Connection> _queueConnections;
        private static MsgCollection _messages, _messagesQueue;
        private readonly Dictionary<ulong, Connection> _clients = new Dictionary<ulong, Connection>();
        private readonly List<ulong> _disconnectedClients = new List<ulong>();
        #endregion

        #region Classes

        private class MsgCollection
        {
            public List<MsgEntry> MessagesList;
            public MsgEntry CurrentMessage;
            private int _messageIndex;
            private float _nextMessageChange;

            public void AdvanceMessage()
            {
                if (!_config.EnableCyclicity || Time.realtimeSinceStartup < _nextMessageChange)
                {
                    return;
                }

                _nextMessageChange = Time.realtimeSinceStartup + _config.CyclicityFreq;
                if (_config.EnableRandomCyclicity)
                {
                    CurrentMessage = PickRandom(MessagesList);
                }
                else
                {
                    CurrentMessage = MessagesList[_messageIndex++];
                    if (_messageIndex >= MessagesList.Count)
                    {
                        _messageIndex = 0;
                    }
                }
            }

            public void SelectFirst()
            {
                CurrentMessage = MessagesList.First();
            }
        }

        #endregion

        #region Config

        private class MsgConfig
        {
            [JsonProperty("Message - Display Frequency In Seconds")]
            public float TimerFreq;

            [JsonProperty("Message - Randomized")]
            public bool EnableRandomCyclicity;

            [JsonProperty("Message - Cycle Messages")]
            public bool EnableCyclicity;

            [JsonProperty("Message - Cycle Messages Interval In Seconds")]
            public float CyclicityFreq;

            [JsonProperty("Message - Queue - Enabled")]
            public bool EnableQueueMessages;

            [JsonProperty("Connecting Messages")]
            public List<MsgEntry> Msgs;

            [JsonProperty("Queue Messages")]
            public List<MsgEntry> QueueMsgs;

            [JsonProperty("Entering Game Message")]
            public MsgEntry LastMessage;

        }

        private class MsgEntry
        {
            [JsonProperty("Top Status")]
            public string TopString;
            [JsonProperty("Bottom Status")]
            public string BottomString;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<MsgConfig>();
            _messages = new MsgCollection { MessagesList = _config.Msgs };
            _messagesQueue = new MsgCollection { MessagesList = _config.QueueMsgs };
            if (_config.EnableQueueMessages || _config.QueueMsgs != null)
            {
                return;
            }

            _config.QueueMsgs = new List<MsgEntry>
            {
                new MsgEntry{TopString = "<color=#ffff00>You're in queue...</color>", BottomString = "<color=#add8e6>{AHEAD} players ahead of you.</color>"},
                new MsgEntry{TopString = "<color=#add8e6>You're in queue...</color>", BottomString = "<color=#ffff00>{BEHIND} players behind you.</color>"}
            };
            SaveConfig();
            PrintWarning("Detected probably outdated config. New entries added. Check your config.");
        }


        protected override void LoadDefaultConfig()
        {

            _config = new MsgConfig
            {
                TimerFreq = 0.3f,
                EnableRandomCyclicity = false,
                EnableCyclicity = true,
                CyclicityFreq = 3.0f,
                EnableQueueMessages = true,
                Msgs = new List<MsgEntry>
                {
                    new MsgEntry{TopString = "{PlayerName}", BottomString = "{SteamID}"},
                    new MsgEntry{TopString = "Balance", BottomString = "{RewardsPoint}"},
                    new MsgEntry{TopString = "Balance", BottomString = "{Economics}"},
                    new MsgEntry{TopString = "Playtime", BottomString = "{PlayTime}"}
                },
                QueueMsgs = new List<MsgEntry>
                {
                    new MsgEntry{TopString = "Queue", BottomString = "{AHEAD} players ahead of you and {BEHIND} behind"}
                },
                LastMessage = new MsgEntry { TopString = "{PlayerName}", BottomString = "{SteamID}" }
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region Hooks

        private void Unload()
        {
            _messages = null;
            _messagesQueue = null;
            if (_config.EnableQueueMessages)
                ServerMgr.Instance.connectionQueue.nextMessageTime = 0f;

            _config = null;
        }

        private void Loaded()
        {
            if (_config?.Msgs == null || _config.Msgs.Count == 0)
            {
                Unsubscribe(nameof(OnUserApprove));
                Unsubscribe(nameof(OnPlayerConnected));
                PrintWarning("No loading messages defined! Check your config.");
                return;
            }
            if (_config.EnableCyclicity && _config.Msgs.Count <= 1)
            {
                _config.EnableCyclicity = false;
                PrintWarning("You have message cyclicity enabled, but only 1 message is defined. Check your config.");
            }

            if (_config.EnableQueueMessages && _config.QueueMsgs == null || _config.QueueMsgs.Count == 0)
            {
                _config.EnableQueueMessages = false;
                PrintWarning("You have queue messages enabled, but no queue messages is defined. Check your config.");
            }

            _messages.SelectFirst();
            if (_config.EnableQueueMessages)
            {
                _messagesQueue.SelectFirst();
            }
        }

        private void OnServerInitialized()
        {
            _queueConnections = ServerMgr.Instance.connectionQueue.queue;
        }

        private void OnUserApprove(Connection connection)
        {
            _clients[connection.userid] = connection;
            if (_timer == null)
            {
                _timer = timer.Every(_config.TimerFreq, HandleClients);
            }

            DisplayMessage(connection, GetCurrentMessage(connection));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            _clients.Remove(player.userID);
            DisplayMessage(player.Connection, GetLastMessage(player.Connection) ?? GetCurrentMessage(player.Connection));
        }

        #endregion

        #region Logic

        private void HandleClients()
        {
            if (_clients.Count == 0)
            {
                _timer.Destroy();
                _timer = null;
                return;
            }
            UpdateCurrentMessages();
            if (_config.EnableQueueMessages && ServerMgr.Instance.connectionQueue.Queued > 0)
            {
                SuppressDefaultQueueMessage();
            }

            foreach (Connection client in _clients.Values)
            {
                if (!client.active)
                {
                    _disconnectedClients.Add(client.userid);
                    continue;
                }

                if (client.state == Connection.State.InQueue)
                {
                    if (!_config.EnableQueueMessages)
                    {
                        continue;
                    }

                    DisplayQueueMessage(client, GetCurrentQueueMessage());
                    continue;
                }
                DisplayMessage(client, GetCurrentMessage(client));
            }

            if (_disconnectedClients.Count == 0)
            {
                return;
            }

            _disconnectedClients.ForEach(uid => _clients.Remove(uid));
            _disconnectedClients.Clear();
        }

        private static void DisplayMessage(Connection con, MsgEntry msgEntry)
        {
            //if (!Net.sv.write.Start()) return;

            var net = Net.sv.StartWrite();

            net.PacketID(Message.Type.Message);
            net.String(msgEntry.TopString);
            net.String(msgEntry.BottomString);
            net.Send(new SendInfo(con));
        }

        private void DisplayQueueMessage(Connection con, MsgEntry msgEntry)
        {
            //if (!Net.sv.wr.Start()) return;

            var net = Net.sv.StartWrite();

            int ahead = GetQueuePosition(con);
            int behind = (ServerMgr.Instance.connectionQueue.Queued - ahead) - 1;
            net.PacketID(Message.Type.Message);
            net.String(msgEntry.TopString);
            net.String(msgEntry.BottomString.Replace("{AHEAD}", ahead.ToString()).Replace("{BEHIND}", behind.ToString()));
            net.Send(new SendInfo(con));

            /*
             *             var net = Net.sv.StartWrite();
            net.PacketID(Message.Type.Message);
            net.String(top);
            net.String(bottom);
            net.Send(new SendInfo(conn));
            */
        }

        #endregion

        #region Utils

        private static T PickRandom<T>(IReadOnlyList<T> list)
        {
            return list[Random.Range(0, list.Count - 1)];
        }

        private MsgEntry GetMessage(MsgEntry message, Connection player)
        {
            if (player != null)
            {
                var playTime = PlaytimeTracker?.Call("GetPlayTime", player.userid)?.ToString() ?? "";
                var rewardsPoint = ServerRewards?.Call("CheckPoints", player.userid)?.ToString() ?? "0";
                var balance = Economics?.Call("Balance", player.userid)?.ToString() ?? "";

                message.TopString = message?.TopString
                    ?.Replace("{PlayerName}", player.username ?? "")
                    .Replace("{SteamID}", player.userid.ToString())
                    .Replace("{RewardsPoint}", rewardsPoint)
                    .Replace("{Economics}", balance)
                    .Replace("{PlayTime}", playTime);

                message.BottomString = message?.BottomString
                    ?.Replace("{PlayerName}", player.username ?? "")
                    .Replace("{SteamID}", player.userid.ToString())
                    .Replace("{RewardsPoint}", rewardsPoint)
                    .Replace("{Economics}", balance)
                    .Replace("{PlayTime}", playTime);
            }
            else
            {
                message.TopString = message?.TopString
                    ?.Replace("{PlayerName}", "")
                    .Replace("{SteamID}", "")
                    .Replace("{RewardsPoint}", "0")
                    .Replace("{Economics}", "")
                    .Replace("{PlayTime}", "");

                message.BottomString = message?.BottomString
                    ?.Replace("{PlayerName}", "")
                    .Replace("{SteamID}", "")
                    .Replace("{RewardsPoint}", "0")
                    .Replace("{Economics}", "")
                    .Replace("{PlayTime}", "");


            }

            return message;
        }

        private MsgEntry GetCurrentMessage(Connection player)
        {
            return GetMessage(_messages.CurrentMessage, player);
        }

        private MsgEntry GetLastMessage(Connection player)
        {
            return GetMessage(_config.LastMessage, player);
        }
        private static MsgEntry GetCurrentQueueMessage()
        {
            return _messagesQueue.CurrentMessage;
        }

        private static MsgCollection GetMessagesCollection()
        {
            return _messages;
        }

        private static MsgCollection GetQueueMessagesCollection()
        {
            return _messagesQueue;
        }

        private static void UpdateCurrentMessages()
        {
            GetMessagesCollection().AdvanceMessage(); ;
            GetQueueMessagesCollection().AdvanceMessage();
        }
        private int GetQueuePosition(Connection con)
        {
            return _queueConnections.IndexOf(con);
        }

        private static void SuppressDefaultQueueMessage()
        {
            ServerMgr.Instance.connectionQueue.nextMessageTime = float.MaxValue;
        }

        #endregion
    }
}