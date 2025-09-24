using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("LeaderosConnect", "LeaderOS", "1.0.0")]
    [Description("Leaderos WebSocket connection for server communication")]
    public class LeaderosConnect : CovalencePlugin
    {
        #region Static Connection Settings
        private const string APP_KEY = "leaderos-connect";
        private const string HOST = "connect-socket.leaderos.net:6001";
        private const string AUTH_ENDPOINT = "https://connect-api.leaderos.net/broadcasting/auth";
        private const int PING_INTERVAL = 30;
        private const int PONG_TIMEOUT = 10;
        private const int RECONNECT_DELAY = 5;
        private const int MAX_RECONNECT_ATTEMPTS = 10;
        private const string QUEUE_FILE = "queue.json";
        #endregion

        #region Configuration Validation
        private bool ValidateConfiguration()
        {
            bool isValid = true;

            // Website URL validation
            if (string.IsNullOrWhiteSpace(_config.WebsiteUrl))
            {
                PrintError("❌ Website URL is not set in configuration.");
                isValid = false;
            }
            else if (_config.WebsiteUrl.StartsWith("http://"))
            {
                PrintError("❌ Website URL must use HTTPS protocol.");
                isValid = false;
            }

            // Validate API key
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                PrintError("❌ API key is not set in configuration.");
                isValid = false;
            }

            // Validate server token
            if (string.IsNullOrWhiteSpace(_config.ServerToken))
            {
                PrintError("❌ Server token is not set in configuration.");
                isValid = false;
            }

            return isValid;
        }
        #endregion

        #region Queue Management
        private Dictionary<string, List<string>> _commandQueue = new Dictionary<string, List<string>>();
        private readonly object _queueLock = new object();

        private void LoadQueue()
        {
            try
            {
                string queuePath = Path.Combine(Interface.Oxide.DataDirectory, Name, QUEUE_FILE);

                if (File.Exists(queuePath))
                {
                    string json = File.ReadAllText(queuePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _commandQueue = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
                        PrintToConsole($"📋 Loaded {_commandQueue.Count} queued command entries");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error loading command queue: {ex.Message}");
                _commandQueue = new Dictionary<string, List<string>>();
            }
        }

        private void SaveQueue()
        {
            try
            {
                lock (_queueLock)
                {
                    string dataPath = Path.Combine(Interface.Oxide.DataDirectory, Name);
                    if (!Directory.Exists(dataPath))
                        Directory.CreateDirectory(dataPath);

                    string queuePath = Path.Combine(dataPath, QUEUE_FILE);
                    string json = JsonConvert.SerializeObject(_commandQueue, Formatting.Indented);
                    File.WriteAllText(queuePath, json);
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error saving command queue: {ex.Message}");
            }
        }

        private void AddToQueue(string steamId, List<string> commands)
        {
            try
            {
                lock (_queueLock)
                {
                    if (_commandQueue.ContainsKey(steamId))
                    {
                        _commandQueue[steamId].AddRange(commands);
                    }
                    else
                    {
                        _commandQueue[steamId] = new List<string>(commands);
                    }

                    SaveQueue();
                    PrintToConsole($"📝 Added {commands.Count} commands to queue for Steam ID: {steamId}");
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error adding commands to queue: {ex.Message}");
            }
        }

        private void ProcessQueueForPlayer(string steamId)
        {
            try
            {
                lock (_queueLock)
                {
                    if (_commandQueue.ContainsKey(steamId))
                    {
                        var commands = _commandQueue[steamId];
                        var player = players.FindPlayerById(steamId);
                        string username = player?.Name ?? "Unknown";

                        PrintToConsole($"🎯 Processing {commands.Count} queued commands for {username} ({steamId})");

                        ExecuteCommands(commands, username);

                        _commandQueue.Remove(steamId);
                        SaveQueue();

                        PrintToConsole($"✅ Removed {username} from command queue");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error processing queue for player {steamId}: {ex.Message}");
            }
        }

        private bool IsPlayerOnline(string steamId)
        {
            try
            {
                var player = players.FindPlayerById(steamId);
                return player != null && player.IsConnected;
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error checking player online status: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Command Processing
        private void HandleSendCommandsEvent(object data)
        {
            try
            {
                if (_config.DebugMode)
                    PrintToConsole($"📋 Processing send-commands event: {data}");

                // Security: data null check
                if (data == null)
                {
                    PrintWarning("⚠️ Received null data in send-commands event");
                    return;
                }

                var commandData = JsonConvert.DeserializeObject<CommandEventData>(data.ToString());

                if (commandData?.commands == null || commandData.commands.Length == 0)
                {
                    PrintWarning("⚠️ No commands received in send-commands event");
                    return;
                }

                // Validate commands with website
                ValidateAndExecuteCommands(commandData.commands);
            }
            catch (JsonException ex)
            {
                PrintError($"❌ JSON parsing error in send-commands event: {ex.Message}");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error processing send-commands event: {ex.Message}");
            }
        }

        private void ValidateAndExecuteCommands(string[] commandIds)
        {
            try
            {
                for (int i = 0; i < commandIds.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(commandIds[i]))
                    {
                        PrintWarning($"⚠️ Empty command ID at index {i}, skipping");
                        return;
                    }
                }

                var formValues = new List<string>
                {
                    $"token={Uri.EscapeDataString(_config.ServerToken)}"
                };

                for (int i = 0; i < commandIds.Length; i++)
                {
                    string key = $"commands[{i}]";
                    string value = Uri.EscapeDataString(commandIds[i]);
                    formValues.Add($"{Uri.EscapeDataString(key)}={value}");
                }

                string formData = string.Join("&", formValues);

                var headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = _config.ApiKey,
                    ["Content-Type"] = "application/x-www-form-urlencoded",
                    ["Accept"] = "application/json"
                };

                string validateUrl = $"{_config.WebsiteUrl}/api/command-logs/validate";

                if (_config.DebugMode)
                    PrintToConsole($"🔍 Validating commands at: {validateUrl}");

                webrequest.Enqueue(validateUrl, formData, (code, response) =>
                {
                    try
                    {
                        if (code == 200)
                        {
                            HandleValidationResponse(response);
                        }
                        else
                        {
                            PrintError($"❌ Command validation failed. Code: {code}, Response: {response}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError($"❌ Error in validation callback: {ex.Message}");
                    }
                }, this, Core.Libraries.RequestMethod.POST, headers);
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error validating commands: {ex.Message}");
            }
        }


        private void HandleValidationResponse(string response)
        {
            try
            {
                if (_config.DebugMode)
                    PrintToConsole($"✅ Validation response: {response}");

                // Security: Response null check
                if (string.IsNullOrWhiteSpace(response))
                {
                    PrintWarning("⚠️ Empty validation response received");
                    return;
                }

                var validationResponse = JsonConvert.DeserializeObject<ValidationResponse>(response);

                if (validationResponse?.commands == null || validationResponse.commands.Length == 0)
                {
                    PrintWarning("⚠️ No valid commands received from validation");
                    return;
                }

                string steamId = "";
                var commandsList = new List<string>();

                foreach (var commandItem in validationResponse.commands)
                {
                    if (!string.IsNullOrWhiteSpace(commandItem.command))
                    {
                        commandsList.Add(commandItem.command);

                        // Get Steam ID from first command if not set
                        if (string.IsNullOrEmpty(steamId) && !string.IsNullOrEmpty(commandItem.username))
                        {
                            steamId = commandItem.username; // Username is Steam ID
                        }
                    }
                }

                if (commandsList.Count > 0 && !string.IsNullOrEmpty(steamId))
                {
                    // Check if player online checking is enabled
                    if (_config.CheckPlayerOnline)
                    {
                        if (IsPlayerOnline(steamId))
                        {
                            // Player is online, execute commands immediately
                            var player = players.FindPlayerById(steamId);
                            string playerName = player?.Name ?? "Unknown";
                            PrintToConsole($"👤 Player {playerName} ({steamId}) is online, executing commands immediately");
                            ExecuteCommands(commandsList, playerName);
                        }
                        else
                        {
                            // Player is offline, add to queue
                            PrintToConsole($"💤 Player with Steam ID {steamId} is offline, adding commands to queue");
                            AddToQueue(steamId, commandsList);
                        }
                    }
                    else
                    {
                        // Player online check is disabled, execute commands immediately
                        var player = players.FindPlayerById(steamId);
                        string playerName = player?.Name ?? "Unknown";
                        ExecuteCommands(commandsList, playerName);
                    }
                }
                else
                {
                    PrintWarning("⚠️ No executable commands found after validation or missing Steam ID");
                }
            }
            catch (JsonException ex)
            {
                PrintError($"❌ JSON parsing error in validation response: {ex.Message}");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error processing validation response: {ex.Message}");
            }
        }

        private void ExecuteCommands(List<string> commands, string username)
        {
            try
            {
                PrintToConsole($"⚡ Executing {commands.Count} command(s) for user: {username}");

                float delay = 0f;
                foreach (string command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    try
                    {
                        if (_config.DebugMode)
                            PrintToConsole($"🔨 Executing command: {command}");

                        timer.Once(delay, () => // Wait between each command
                        {
                            try
                            {
                                server.Command(command);
                                PrintToConsole($"✅ Command executed: {command}");
                            }
                            catch (Exception ex)
                            {
                                PrintError($"❌ Failed to execute command '{command}': {ex.Message}");
                            }
                        });

                        delay += 1f; // Increase delay for next command
                    }
                    catch (Exception ex)
                    {
                        PrintError($"❌ Failed to execute command '{command}': {ex.Message}");
                    }
                }

                PrintToConsole($"🎉 Command execution completed for user: {username}");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error during command execution: {ex.Message}");
            }
        }

        #endregion

        #region Command Data Classes
        public class CommandEventData
        {
            public string[] commands { get; set; }
        }

        public class ValidationResponse
        {
            public ValidatedCommand[] commands { get; set; }
        }

        public class ValidatedCommand
        {
            public string command { get; set; }
            public string username { get; set; }
        }
        #endregion

        #region Configuration
        private Configuration _config;

        public class Configuration
        {
            [JsonProperty("Website URL")]
            public string WebsiteUrl { get; set; } = "";

            [JsonProperty("API Key")]
            public string ApiKey { get; set; } = "";

            [JsonProperty("Server Token")]
            public string ServerToken { get; set; } = "";

            [JsonProperty("Debug Mode")]
            public bool DebugMode { get; set; } = false;

            [JsonProperty("Check Player Online")]
            public bool CheckPlayerOnline { get; set; } = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                PrintWarning("⚠️ Configuration file is corrupt, creating new one...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }
        #endregion

        #region Fields
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private string _socketId;
        private bool _isConnected = false;
        private bool _isAuthenticated = false;
        private Timer _pingTimer;
        private Timer _pongTimeoutTimer;
        private Timer _reconnectTimer;
        private Task _receiveTask;
        private int _reconnectAttempts = 0;
        private bool _shouldReconnect = true;
        private DateTime _lastPongReceived = DateTime.Now;
        private bool _waitingForPong = false;
        private string _channelName;
        private string _socketUrl;
        #endregion

        #region Oxide Hooks
        void Init()
        {
            // Validate configuration
            if (!ValidateConfiguration())
            {
                PrintError("❌ Configuration validation failed. Plugin disabled.");
                return;
            }

            _channelName = $"private-servers.{_config.ServerToken}";
            _socketUrl = $"ws://{HOST}/app/{APP_KEY}?protocol=7&client=rust-oxide&version=1.0";

            // Load command queue
            LoadQueue();

            PrintToConsole("🚀 LeaderosConnect plugin initialized successfully!");
        }

        void OnServerInitialized()
        {
            // Added delay
            timer.Once(2f, () => ConnectToWebSocket());
        }

        void OnUserConnected(IPlayer player)
        {
            try
            {
                if (player == null || string.IsNullOrEmpty(player.Id))
                    return;

                string steamId = player.Id;

                if (_config.DebugMode)
                    PrintToConsole($"👋 Player connected: {player.Name} ({steamId})");

                // Check if player has queued commands
                if (_commandQueue.ContainsKey(steamId))
                {
                    // Process queued commands after a small delay to ensure player is fully connected
                    timer.Once(2f, () => ProcessQueueForPlayer(steamId));
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error in OnUserConnected: {ex.Message}");
            }
        }

        void Unload()
        {
            _shouldReconnect = false;
            DisconnectWebSocket();
            SaveQueue(); // Save queue on unload
        }
        #endregion

        #region WebSocket Connection
        private async void ConnectToWebSocket()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                PrintError($"❌ Max reconnection attempts ({MAX_RECONNECT_ATTEMPTS}) reached. Stopping reconnection attempts.");
                return;
            }

            try
            {
                if (_webSocket != null && _webSocket.State != WebSocketState.Closed)
                {
                    await CleanupWebSocket();
                }

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                var uri = new Uri(_socketUrl);

                PrintToConsole($"🔌 Connecting to WebSocket... (Attempt {_reconnectAttempts + 1}/{MAX_RECONNECT_ATTEMPTS})");

                // Added timeout
                var connectTask = _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                var timeoutTask = Task.Delay(10000, _cancellationTokenSource.Token); // 10 seconds timeout

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("WebSocket connection timeout");
                }

                await connectTask; // Check actual result

                _isConnected = true;
                _reconnectAttempts = 0;
                PrintToConsole("✅ WebSocket connection established successfully!");

                // Start receiving messages
                _receiveTask = Task.Run(() => ReceiveMessages(_cancellationTokenSource.Token));

                // Start keep-alive mechanism
                StartKeepAlive();
            }
            catch (Exception ex)
            {
                _reconnectAttempts++;
                PrintError($"❌ WebSocket connection failed: {ex.Message}");

                if (_shouldReconnect && _reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
                {
                    var delay = Math.Min(RECONNECT_DELAY * _reconnectAttempts, 60);
                    PrintToConsole($"⏳ Retrying connection in {delay} seconds...");
                    _reconnectTimer = timer.Once(delay, () => ConnectToWebSocket());
                }
            }
        }

        private async Task CleanupWebSocket()
        {
            try
            {
                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        var closeTask = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                        var timeoutTask = Task.Delay(3000);
                        await Task.WhenAny(closeTask, timeoutTask);
                    }
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }
            catch (Exception ex)
            {
                if (_config.DebugMode)
                    PrintError($"🔧 Cleanup error: {ex.Message}");
            }
        }

        private void DisconnectWebSocket()
        {
            try
            {
                StopKeepAlive();
                _reconnectTimer?.Destroy();

                _isConnected = false;
                _isAuthenticated = false;

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        var closeTask = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Plugin unloaded", CancellationToken.None);
                        var timeoutTask = Task.Delay(3000);
                        Task.WhenAny(closeTask, timeoutTask).Wait(3000);
                    }
                    _webSocket.Dispose();
                    _webSocket = null;
                }

                PrintToConsole("🔌 WebSocket connection closed.");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error during disconnect: {ex.Message}");
            }
        }
        #endregion

        #region Keep-Alive Mechanism
        private void StartKeepAlive()
        {
            _lastPongReceived = DateTime.Now;
            _waitingForPong = false;

            // Start ping timer
            _pingTimer = timer.Repeat(PING_INTERVAL, 0, () => SendKeepAlivePing());
        }

        private void StopKeepAlive()
        {
            _pingTimer?.Destroy();
            _pongTimeoutTimer?.Destroy();
            _waitingForPong = false;
        }

        private void SendKeepAlivePing()
        {
            if (!_isConnected || _webSocket?.State != WebSocketState.Open)
                return;

            // Check if we're waiting for a pong and it's been too long
            if (_waitingForPong && (DateTime.Now - _lastPongReceived).TotalSeconds > PONG_TIMEOUT)
            {
                PrintWarning("⚠️ Pong timeout detected, connection may be stale. Reconnecting...");
                HandleConnectionLost();
                return;
            }

            _waitingForPong = true;
            SendPing();

            // Set timeout for pong response
            _pongTimeoutTimer?.Destroy();
            _pongTimeoutTimer = timer.Once(PONG_TIMEOUT, () =>
            {
                if (_waitingForPong)
                {
                    PrintWarning("⚠️ No pong response received, connection appears dead. Reconnecting...");
                    HandleConnectionLost();
                }
            });
        }

        private void HandlePongReceived()
        {
            _waitingForPong = false;
            _lastPongReceived = DateTime.Now;
            _pongTimeoutTimer?.Destroy();

            if (_config.DebugMode)
                PrintToConsole("💓 Pong received - connection alive");
        }

        private void HandleConnectionLost()
        {
            _isConnected = false;
            _isAuthenticated = false;

            PrintWarning("🔄 Connection lost, attempting to reconnect...");

            if (_shouldReconnect)
            {
                _reconnectTimer = timer.Once(RECONNECT_DELAY, () => ConnectToWebSocket());
            }
        }
        #endregion

        #region Message Receiving
        private async Task ReceiveMessages(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(chunk);

                        if (result.EndOfMessage)
                        {
                            var completeMessage = messageBuilder.ToString();
                            messageBuilder.Clear();

                            // Process on main thread
                            NextTick(() => HandleMessage(completeMessage));
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        PrintToConsole("🔌 WebSocket closed by server.");
                        NextTick(() => HandleConnectionLost());
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (_config.DebugMode)
                    PrintToConsole("🔧 Message receiving cancelled normally.");
            }
            catch (WebSocketException ex)
            {
                PrintWarning($"⚠️ WebSocket error: {ex.Message}");
                NextTick(() => HandleConnectionLost());
            }
            catch (Exception ex)
            {
                PrintError($"❌ Unexpected error in message receiving: {ex.Message}");
                NextTick(() => HandleConnectionLost());
            }
        }

        private void HandleMessage(string data)
        {
            try
            {
                if (_config.DebugMode)
                    PrintToConsole($"📨 Message received: {data}");

                if (string.IsNullOrWhiteSpace(data))
                    return;

                var wsMessage = JsonConvert.DeserializeObject<WebSocketMessage>(data);

                if (wsMessage == null)
                    return;

                switch (wsMessage.@event)
                {
                    case "pusher:connection_established":
                        HandleConnectionEstablished(wsMessage.data);
                        break;
                    case "pusher:subscription_succeeded":
                        HandleSubscriptionSucceeded(wsMessage);
                        break;
                    case "pusher:subscription_error":
                        HandleSubscriptionError(wsMessage);
                        break;
                    case "pusher:pong":
                        HandlePongReceived();
                        break;
                    case "ping":
                        HandlePingEvent(wsMessage);
                        break;
                    case "send-commands":
                        HandleSendCommandsEvent(wsMessage.data);
                        break;
                    default:
                        HandleCustomEvent(wsMessage);
                        break;
                }
            }
            catch (JsonException ex)
            {
                PrintError($"❌ JSON parsing error in message handling: {ex.Message}");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error processing message: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers
        private void HandleConnectionEstablished(object data)
        {
            try
            {
                if (data == null)
                    return;

                var connectionData = JsonConvert.DeserializeObject<ConnectionData>(data.ToString());
                _socketId = connectionData?.socket_id;

                if (!string.IsNullOrEmpty(_socketId))
                {
                    PrintToConsole($"🆔 Connection established! Socket ID: {_socketId}");
                    // Start authentication
                    AuthenticateAndSubscribe();
                }
                else
                {
                    PrintError("❌ No socket ID received in connection established event");
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error handling connection established: {ex.Message}");
            }
        }

        private void HandleSubscriptionSucceeded(WebSocketMessage message)
        {
            PrintToConsole($"🎉 Successfully subscribed to channel: {message.channel}");
            _isAuthenticated = true;
        }

        private void HandleSubscriptionError(WebSocketMessage message)
        {
            PrintError($"❌ Subscription error for channel: {message.channel}");
        }

        private void HandlePingEvent(WebSocketMessage message)
        {
            if (_config.DebugMode)
                PrintToConsole($"🏓 Ping event received: {message.data}");

            PrintToConsole("✅ Ping received from the server!");
        }

        private void HandleCustomEvent(WebSocketMessage message)
        {
            if (_config.DebugMode)
                PrintToConsole($"🎯 Custom event received - Event: {message.@event}, Channel: {message.channel}");
        }
        #endregion

        #region Authentication
        private void AuthenticateAndSubscribe()
        {
            if (string.IsNullOrEmpty(_socketId))
            {
                PrintError("❌ Socket ID not found, cannot authenticate.");
                return;
            }

            var authData = new
            {
                socket_id = _socketId,
                channel_name = _channelName
            };

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-API-Key"] = _config.ApiKey,
                ["Accept"] = "application/json"
            };

            string jsonData = JsonConvert.SerializeObject(authData);

            if (_config.DebugMode)
                PrintToConsole($"🔐 Attempting authentication");

            webrequest.Enqueue(AUTH_ENDPOINT, jsonData, (code, response) =>
            {
                try
                {
                    if (code == 200)
                    {
                        PrintToConsole($"✅ Authentication successful!");
                        if (_config.DebugMode)
                            PrintToConsole($"🔐 Auth response: {response}");
                        HandleAuthResponse(response);
                    }
                    else
                    {
                        PrintError($"❌ Authentication failed. Code: {code}");
                        // Try direct subscription as fallback
                        TryDirectSubscription();
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"❌ Error in auth callback: {ex.Message}");
                    TryDirectSubscription();
                }
            }, this, Core.Libraries.RequestMethod.POST, headers, 10f);
        }

        private void HandleAuthResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                {
                    TryDirectSubscription();
                    return;
                }

                var authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);

                if (authResponse != null && !string.IsNullOrEmpty(authResponse.auth))
                {
                    // Subscribe to channel with auth
                    SubscribeToChannel(authResponse.auth, authResponse.channel_data);
                }
                else
                {
                    TryDirectSubscription();
                }
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error processing auth response: {ex.Message}");
                TryDirectSubscription();
            }
        }

        private void SubscribeToChannel(string auth, string channelData = null)
        {
            var subscribeMessage = new
            {
                @event = "pusher:subscribe",
                data = new
                {
                    auth = auth,
                    channel = _channelName,
                    channel_data = channelData
                }
            };

            SendMessage(subscribeMessage);
        }

        private void TryDirectSubscription()
        {
            PrintToConsole("🔄 Attempting direct subscription...");

            var subscribeMessage = new
            {
                @event = "pusher:subscribe",
                data = new
                {
                    channel = _channelName
                }
            };

            SendMessage(subscribeMessage);
        }
        #endregion

        #region Message Sending
        private async void SendMessage(object message)
        {
            try
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    PrintError("❌ WebSocket not connected, cannot send message.");
                    return;
                }

                string jsonMessage = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);

                var sendTask = _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(sendTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    PrintError("❌ Message send timeout");
                    HandleConnectionLost();
                    return;
                }

                await sendTask;

                if (_config.DebugMode)
                    PrintToConsole($"📤 Message sent successfully");
            }
            catch (Exception ex)
            {
                PrintError($"❌ Error sending message: {ex.Message}");
                HandleConnectionLost();
            }
        }

        private void SendPing()
        {
            var pingMessage = new
            {
                @event = "pusher:ping",
                data = new { }
            };

            SendMessage(pingMessage);
        }
        #endregion

        #region Utility Methods
        private void PrintToConsole(string message)
        {
            Puts($"[LeaderosConnect] {message}");
        }

        private void PrintWarning(string message)
        {
            LogWarning($"[LeaderosConnect] {message}");
        }

        private void PrintError(string message)
        {
            LogError($"[LeaderosConnect] {message}");
        }
        #endregion

        #region Data Classes
        public class WebSocketMessage
        {
            public string @event { get; set; }
            public string channel { get; set; }
            public object data { get; set; }
        }

        public class ConnectionData
        {
            public string socket_id { get; set; }
            public int activity_timeout { get; set; }
        }

        public class AuthResponse
        {
            public string auth { get; set; }
            public string channel_data { get; set; }
        }
        #endregion

        #region Commands
        [Command("leaderos.status")]
        private void StatusCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            var uptime = _isConnected ? "Connected" : "Disconnected";
            var lastPong = _isConnected ? $"{(DateTime.Now - _lastPongReceived).TotalSeconds:F1}s ago" : "N/A";

            var status = $"📊 WebSocket Status:\n" +
                        $"🔌 Connection: {uptime}\n" +
                        $"🔐 Authenticated: {(_isAuthenticated ? "Yes" : "No")}\n" +
                        $"🆔 Socket ID: {_socketId ?? "None"}\n" +
                        $"📺 Channel: {_channelName}\n" +
                        $"💓 Last Pong: {lastPong}\n" +
                        $"🔄 Reconnect Attempts: {_reconnectAttempts}";

            player.Reply(status);
        }

        [Command("leaderos.reconnect")]
        private void ReconnectCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            _reconnectAttempts = 0; // Reset attempts
            DisconnectWebSocket();
            timer.Once(2f, () => ConnectToWebSocket());
            player.Reply("🔄 WebSocket reconnecting...");
        }

        [Command("leaderos.debug")]
        private void DebugCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            _config.DebugMode = !_config.DebugMode;
            SaveConfig();
            player.Reply($"🔧 Debug mode: {(_config.DebugMode ? "Enabled" : "Disabled")}");
        }
        #endregion
    }
}