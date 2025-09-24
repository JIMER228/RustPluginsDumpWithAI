// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
/*
*  < ----- End-User License Agreement ----->
*
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of This Software without the Developer’s consent
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Developer: Rainey (rightasrainey@gmail.com)
*
*  Copyright © 2023 Rainey
*/
// V1.8.0
// - Added TunnelTussle Event - Kill tunnel dwellers to win!
// - Added RoadsignRun Event - Destroy roadsigns to win!
// - Added UnderwaterWar Event - Kill underwater lab scientists to win!
// - Added PlayerBattle Event - Kill players to win!
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Random=UnityEngine.Random;
using ProtoBuf;
namespace Oxide.Plugins
{
  [Info("ExtraEvents", "Rainey", "1.8.0")]
  [Description("Run mulitple competitive events for players to win awards.")]
  public class ExtraEvents : RustPlugin
  {
    [PluginReference]
    private Plugin ImageLibrary, Kits;
    private static ExtraEvents _instance;
    private ConfigData _configData;
    private PluginData _pluginData;
    private string adminPermission = "extraevents.admin";
    private string allEventsPermission = "extraevents.all";
    private const string OreWar = "orewar";
    private const string TreeTrimmers = "treetrimmers";
    private const string BarrelBreakers = "barrelbreakers";
    private const string FishingFrenzy = "fishingfrenzy";
    private const string BotBash = "botbash";
    private const string CrateClash = "crateclash";
    private const string AnimalAnnihilation = "animalannihilation";
    private const string ResourceRumble = "resourcerumble";
    private const string ResourceRun = "resourcerun";
    private const string TunnelTussle = "tunneltussle";
    private const string RoadsignRun = "roadsignrun";
    private const string UnderwaterWar = "underwaterwar";
    private const string PlayerBattle = "playerbattle";
    private const string UI_MAIN = "UI_MAIN";
    private const string UI_IMAGE = "UI_IMAGE";
    private Timer UIUpdateTimer, autoEventTimer, gameTipTimer, eventTimer;
    private string[] barrelShortPrefabNames = { "loot-barrel-1", "loot-barrel-2", "loot_barrel_1", "loot_barrel_2", "oil_barrel" };
    private string[] roadsignShortPrefabNames = { "roadsign1", "roadsign2", "roadsign3", "roadsign4", "roadsign5", "roadsign6", "roadsign7", "roadsign8", "roadsign9" };
    private string[] animalPrefabShortnames = { "chicken","wolf","bear","boar","polarbear","stag" };
    private string[] containerPrefabShortNames = { "trash-pile-1","crate_basic","crate_elite","crate_mine","crate_normal","crate_normal_2","crate_normal_2_food","crate_normal_2_medical","crate_tools","crate_underwater_advanced","crate_underwater_basic","dm ammo","dm c4","dm construction resources","dm construction tools","dm food","dm medical","dm res","dm tier1 lootbox","dm tier2 lootbox","dm tier3 lootbox","foodbox","loot_barrel_1","loot_barrel_2","loot_component_test","loot_trash","minecart","oil_barrel","vehicle_parts","codelockedhackablecrate","codelockedhackablecrate_oilrig","supply_drop","giftbox_loot","presentdrop","bradley_crate","heli_crate","diesel_barrel_world","visualshelvestest","crate_ammunition","crate_food_1","crate_food_2","crate_fuel","crate_medical","tech_parts_1","tech_parts_2","hiddenhackablecrate","wagon_crate_normal","wagon_crate_normal_2","wagon_crate_normal_2_food","wagon_crate_normal_2_medical","xmastunnellootbox" };
    private Dictionary<ulong, int> playerList = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> finalPlayerList = new Dictionary<ulong, int>();
    private List<string> rewardsList = new List<string>();
    private float eventStartTime, eventImageTransparency, eventGameTipDuration, eventMultipleWinnersDelay, eventMultiplier;
    private string message, debugMessage, currentEvent, eventName, eventDesc, eventChatPrefix, eventPermission, eventImage, eventImageMinAnchors, eventImageMaxAnchors, eventUIBackgroundColor, eventUIMinAnchors, eventUIMaxAnchors, eventUITextColor, eventUITextOutlineColor, eventUITextAlignment, eventUIPlayerListTextAlignment, eventPendingParticipationString, eventUpcomingString, eventStartingString, eventEndingString, eventNoParticipantsString, eventWinnerAnnouncementsString, eventGameTipStyle;
    private bool activeEvent, eventEnablePermission, eventShowUIAndNotificationsWithoutPermission, eventEnableImage, eventEnableUI, eventEnableChatNotifications, eventEnableGameTipNotifications, eventEnableUpcomingNotification, isAdditionalEvent;
    private ulong eventChatIconID = 0;
    private int eventLength = 600;
    private int eventUpcomingDelay = 0;
    private string withAdminPermissionConsoleCommandUsageReply;
    private void Init()
    {
      _instance = this;
      Unsubscribe(nameof(OnDispenserBonus));
      Unsubscribe(nameof(OnDispenserGather));
      Unsubscribe(nameof(OnCollectiblePickup));
      Unsubscribe(nameof(OnEntityDeath));
      Unsubscribe(nameof(OnLootEntityEnd));
      Subscribe(nameof(OnPlayerConnected));
      Subscribe(nameof(OnPlayerDisconnected));
      RegisterPermissions();
    }
    private void Unload()
    {
      if (activeEvent) EndEvent();
      SaveProtoData();
      Unsubscribe(nameof(OnDispenserBonus));
      Unsubscribe(nameof(OnDispenserGather));
      Unsubscribe(nameof(OnCollectiblePickup));
      Unsubscribe(nameof(OnEntityDeath));
      Unsubscribe(nameof(OnLootEntityEnd));
      Unsubscribe(nameof(OnPlayerConnected));
      Unsubscribe(nameof(OnPlayerDisconnected));
      if (BasePlayer.activePlayerList.Count > 0)
      {
        foreach (var player in BasePlayer.activePlayerList)
        {
          DestroyUI(player);
          DestroyEventImages(player);
          HideGameTip(player);
        }
      }
      eventTimer?.Destroy();
      autoEventTimer?.Destroy();
      UIUpdateTimer?.Destroy();
      gameTipTimer?.Destroy();
      _instance = null;
    }
    private void OnServerInitialized()
    {
      RegisterChatCommands();
      LoadProtoData();
      CacheImages();
      StartAutoEventTimer();
    }
    private void RegisterChatCommands()
    {
      string command = "extraevents";
      if (_configData.GeneralOptions.chatCommand != "")
        command = _configData.GeneralOptions.chatCommand;
      cmd.AddChatCommand(command, this, "ExtraEventsChatCommand");
      cmd.AddConsoleCommand(command, this, "ExtraEventsConsoleCommand");
      withAdminPermissionConsoleCommandUsageReply = $"Command Usage:\n/{command} start - Start random event\n/{command} start EventName - Start specific event (case sensitive) (default EventName)\n/{command} end - End current event";
    }
    private void RegisterPermissions()
    {
      if (_configData.GeneralOptions.adminPermission != "")
        adminPermission = _configData.GeneralOptions.adminPermission;
      permission.RegisterPermission(adminPermission, this);
      debugMessage = $"{adminPermission} permission registered";
      PrintDebug(debugMessage);
      if (_configData.GeneralOptions.allEventsPermission != "")
        allEventsPermission = _configData.GeneralOptions.allEventsPermission;
      permission.RegisterPermission(allEventsPermission, this);
      debugMessage = $"(optional) {allEventsPermission} permission registered (overrides individual event permissions if enabled)";
      PrintDebug(debugMessage);
      if (_configData.EventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.EventType> listItem in _configData.EventTypes)
        {
          if (listItem.Value.eventTypeEnabled && listItem.Value.eventTypePermissionEnabled && listItem.Value.eventTypePermission != "")
          {
            permission.RegisterPermission(listItem.Value.eventTypePermission, this);
            debugMessage = $"{listItem.Value.eventTypePermission} permission registered";
            PrintDebug(debugMessage);
          }
        }
      }
      if (_configData.AdditionalEventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.AdditionalEventType> listItem in _configData.AdditionalEventTypes)
        {
          if (listItem.Value.additionalEventTypeEnabled && listItem.Value.additionalEventTypePermissionEnabled && listItem.Value.additionalEventTypePermission != "")
          {
            permission.RegisterPermission(listItem.Value.additionalEventTypePermission, this);
            debugMessage = $"{listItem.Value.additionalEventTypePermission} permission registered";
            PrintDebug(debugMessage);
          }
        }
      }
    }
    private bool CheckPlayerEventPermission(BasePlayer player, string eventPermission)
    {
      if (eventPermission == "" || player == null)
        return false;
      else if (permission.UserHasPermission(player.UserIDString, eventPermission) || permission.UserHasPermission(player.UserIDString, allEventsPermission))
        return true;
      else
        return false;
    }
    void OnUserPermissionGranted(string id, string permName)
    {
      BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(id));
      if (activeEvent)
      {
        if (eventEnablePermission)
        {
          if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
          {
            if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
            {
              CreateUI(player);
              if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
                CreateImages(player, currentEvent);
            }
          }
        }
        else
        {
          if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
          {
            CreateUI(player);
            if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
              CreateImages(player, currentEvent);
          }
        }
      }
    }
    void OnUserPermissionRevoked(string id, string permName)
    {
      BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(id));
      if (activeEvent)
      {
        if (playerList.ContainsKey(Convert.ToUInt64(id)))
          playerList.Remove(Convert.ToUInt64(id));
        DestroyUI(player);
        DestroyEventImages(player);
      }
    }
    private void OnPlayerConnected(BasePlayer player)
    {
      if (activeEvent)
      {
        if (eventEnablePermission)
        {
          if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
          {
            if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
            {
              CreateUI(player);
              if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
                CreateImages(player, currentEvent);
            }
          }
        }
        else
        {
          if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
          {
            CreateUI(player);
            if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
              CreateImages(player, currentEvent);
          }
        }
      }
    }
    private void OnPlayerDisconnected(BasePlayer player, string reason)
    {
      if (activeEvent)
      {
        DestroyUI(player);
        DestroyEventImages(player);
      }
    }
    private void StartAutoEventTimer()
    {
      autoEventTimer?.Destroy();
      var randomTime = UnityEngine.Random.Range(_configData.GeneralOptions.minInitiateTime, _configData.GeneralOptions.maxInitiateTime);
      TimeSpan initiateTime = TimeSpan.FromSeconds(randomTime);
      var englishTime = $"{initiateTime.Hours}h {initiateTime.Minutes}m {initiateTime.Seconds}s ";
      debugMessage = $"Auto Event: Random event attempt in {englishTime} seconds.";
      PrintDebug(debugMessage);
      autoEventTimer = timer.In(randomTime, () =>
      {
        if (activeEvent)
        {
          debugMessage = "Auto Event: Attempted while active event running";
          PrintDebug(debugMessage);
          StartAutoEventTimer();
          return;
        }
        if (!CheckPlayerCount())
        {
          debugMessage = "Auto Event: Not enough players to initiate Auto Event";
          PrintDebug(debugMessage);
          StartAutoEventTimer();
          return;
        }
        debugMessage = "Auto Event: Initiated";
        PrintDebug(debugMessage);
        StartEvent();
      });
    }
    private bool CheckPlayerCount()
    {
      var playerCount = BasePlayer.activePlayerList.Count;
      if (playerCount >= _configData.GeneralOptions.minPlayers) return true;
      else return false;
    }
    private void ExtraEventsChatCommand(BasePlayer player, string command, string[] args)
    {
      if (player == null ) return;
      if (args.Length == 0 || args[0] == null || args[0] == "")
      {
        if (!permission.UserHasPermission(player.UserIDString, adminPermission))
        {
          message = lang.GetMessage("Chat Command Reply No Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
          if (player != null)
            Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
          return;
        }
        else
        {
          message = lang.GetMessage("Chat Command Reply With Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
          if (player != null)
            Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
          return;
        }
      }
      else
      {
        string requestedAction = args[0].ToString();
        switch (requestedAction)
        {
          case "start":
            if (!permission.UserHasPermission(player.UserIDString, adminPermission))
            {
              message = lang.GetMessage("Chat Command Reply No Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
              if (player != null)
                Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
              return;
            }
            debugMessage = "Event Start Request: Chat Command";
            PrintDebug(debugMessage);
            if (args.Length == 1 || args[1] == null || args[1] == "")
            {
              if (activeEvent)
              {
                message = lang.GetMessage("Active Event", this, player.UserIDString);
                if (player != null)
                  Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
                return;
              }
              StartEvent();
            }
            else
            {
              string requestedEvent = args[1].ToString();
              if (activeEvent)
              {
                message = lang.GetMessage("Active Event", this, player.UserIDString);
                if (player != null)
                  Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
                return;
              }
              if (!FindEvent(requestedEvent))
              {
                debugMessage = $"Cannot find the requested event: {requestedEvent}";
                PrintDebug(debugMessage);
                if (player != null)
                  Player.Message(player, debugMessage, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
                return;
              }
              else
                StartEvent(requestedEvent);
            }
            break;
          case "end":
            if (!permission.UserHasPermission(player.UserIDString, adminPermission))
            {
              message = lang.GetMessage("Chat Command Reply No Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
              if (player != null)
                Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
              return;
            }
            debugMessage = "Event End Request: Chat Command";
            PrintDebug(debugMessage);
            if (!activeEvent)
            {
              message = lang.GetMessage("No Active Event", this, player.UserIDString);
              if (player != null)
                Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
              return;
            }
            EndEvent();
            break;
          case "ui":
            ToggleExtraEventsUI(player);
            break;
          case "image":
            ToggleExtraEventsImage(player);
            break;
          default:
            if (!permission.UserHasPermission(player.UserIDString, adminPermission))
            {
              message = lang.GetMessage("Chat Command Reply No Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
              if (player != null)
                Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
              return;
            }
            else
            {
              message = lang.GetMessage("Chat Command Reply With Admin Permission", this, player.UserIDString).Replace("{ChatCommand}", _configData.GeneralOptions.chatCommand);
              if (player != null)
                Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
              return;
            }
            break;
        }
      }
    }
    private void ExtraEventsConsoleCommand(ConsoleSystem.Arg args)
    {
      var player = args.Player();
      if (player != null && !permission.UserHasPermission(player.UserIDString, adminPermission))
      {
        message = lang.GetMessage("No Permission", this, player.UserIDString);
        SendReply(args, message);
        return;
      }
      if (args.Args == null || args.Args.Length == 0)
      {
        message = withAdminPermissionConsoleCommandUsageReply;
        if (player != null)
          SendReply(args, message);
        else
          PrintDebug(message);
        return;
      }
      else
      {
        string requestedAction = args.Args[0].ToString();
        switch (requestedAction)
        {
          case "start":
            debugMessage = "Event Start Request: Console Command";
            PrintDebug(debugMessage);
            if (args.Args.Length == 1 || args.Args[1] == null || args.Args[1] == "")
            {
              if (activeEvent)
              {
                message = "An event is already running.";
                if (player != null)
                  SendReply(args, message);
                else
                  PrintDebug(message);
                return;
              }
              StartEvent();
            }
            else
            {
              string requestedEvent = args.Args[1].ToString();
              if (activeEvent)
              {
                message = "An event is already running.";
                if (player != null)
                  SendReply(args, message);
                else
                  PrintDebug(message);
                return;
              }
              if (!FindEvent(requestedEvent))
              {
                message = $"Cannot find the requested event: {requestedEvent}";
                if (player != null)
                  SendReply(args, message);
                else
                  PrintDebug(message);
                return;
              }
              else
                StartEvent(requestedEvent);
            }
            break;
          case "end":
            debugMessage = "Event End Request: Console Command";
            PrintDebug(debugMessage);
            if (!activeEvent)
            {
              message = "There is no event currently running.";
              if (player != null) SendReply(args, message);
              PrintDebug(message);
              return;
            }
            EndEvent();
            break;
          default:
            message = withAdminPermissionConsoleCommandUsageReply;
            if (player != null) SendReply(args, message);
            else PrintDebug(message);
            break;
        }
      }
    }
    private string GetRandomEvent()
    {
      debugMessage = "Finding Random Event...";
      PrintDebug(debugMessage);
      var listWithKeys = Facepunch.Pool.GetList<string>();
      if (_configData.EventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.EventType> listItem in _configData.EventTypes)
        {
          if (listItem.Value.eventTypeEnabled)
            listWithKeys.Add(listItem.Key);
        }
      }
      if (_configData.AdditionalEventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.AdditionalEventType> listItem in _configData.AdditionalEventTypes)
        {
          if (listItem.Value.additionalEventTypeEnabled)
            listWithKeys.Add(listItem.Key);
        }
      }
      try
      {
        var value = listWithKeys[Random.Range(0, listWithKeys.Count)];
        Facepunch.Pool.FreeList(ref listWithKeys);
        return value;
      }
      catch
      {
        debugMessage = "No events enabled in ExtraEvents.json config file.";
        PrintDebug(debugMessage);
        Facepunch.Pool.FreeList(ref listWithKeys);
        return "";
      }
    }
    private bool FindEvent(string eventName)
    {
      if (_configData.EventTypes.ContainsKey(eventName))
        return true;
      else
      {
        if (_configData.AdditionalEventTypes.ContainsKey(eventName))
          return true;
        else
          return false;
      }
    }
    private void StartEvent(string selectedEvent)
    {
      if (activeEvent)
      {
        debugMessage = "CRITICAL WARNING: An event is already running. Aborting StartEvent()";
        PrintDebug(debugMessage);
        return;
      }
      else
      {
        Unsubscribe(nameof(OnDispenserBonus));
        Unsubscribe(nameof(OnDispenserGather));
        Unsubscribe(nameof(OnCollectiblePickup));
        Unsubscribe(nameof(OnEntityDeath));
        Unsubscribe(nameof(OnLootEntityEnd));
      }
      if (selectedEvent == "random")
        currentEvent = GetRandomEvent();
      else
        currentEvent = selectedEvent;
      if (!GetEventInfo(currentEvent))
      {
        debugMessage = "CRITICAL WARNING: Cannot get the event info. Aborting StartEvent()";
        PrintDebug(debugMessage);
        return;
      }
      if (eventEnableUpcomingNotification)
      {
        var delay = eventUpcomingDelay.ToString();
        string eventUpcomingAnnouncement = eventUpcomingString.Replace("{event_name}", eventName).Replace("{upcoming_announcement_delay}", delay).Replace("{event_desc}", eventDesc).Replace("{event_description}", eventDesc);
        NotifyPlayers(eventUpcomingAnnouncement, eventChatPrefix, eventChatIconID);
        PrintDebug(eventUpcomingAnnouncement);
        timer.Once(eventUpcomingDelay, () =>
        {
          CreateEvent(currentEvent);
        });
      }
      else {
        CreateEvent(currentEvent);
      }
    }
    private void CreateEvent(string currentEvent)
    {
      if (currentEvent.ToLower() == OreWar)
      {
        Subscribe(nameof(OnDispenserBonus));
      }
      else if (currentEvent.ToLower() == TreeTrimmers)
      {
        Subscribe(nameof(OnDispenserBonus));
      }
      else if (currentEvent.ToLower() == BarrelBreakers)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == FishingFrenzy)
      {
        Subscribe(nameof(OnFishCaught));
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == BotBash)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == CrateClash)
      {
        Subscribe(nameof(OnLootEntityEnd));
      }
      else if (currentEvent.ToLower() == AnimalAnnihilation)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == ResourceRumble)
      {
        Subscribe(nameof(OnDispenserGather));
        Subscribe(nameof(OnDispenserBonus));
        Subscribe(nameof(OnCollectiblePickup));
      }
      else if (currentEvent.ToLower() == ResourceRun)
      {
        Subscribe(nameof(OnDispenserGather));
        Subscribe(nameof(OnDispenserBonus));
        Subscribe(nameof(OnCollectiblePickup));
      }
      else if (currentEvent.ToLower() == TunnelTussle)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == RoadsignRun)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == UnderwaterWar)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else if (currentEvent.ToLower() == PlayerBattle)
      {
        Subscribe(nameof(OnEntityDeath));
      }
      else
      {
        debugMessage = $"CRITICAL WARNING: Cannot find and subscribe to the requested event hooks. ({currentEvent}) - Aborting StartEvent()";
        PrintDebug(debugMessage);
        return;
      }
      playerList.Clear();
      rewardsList.Clear();
      eventTimer?.Destroy();
      autoEventTimer?.Destroy();
      eventStartTime = Time.realtimeSinceStartup;
      CreateEventUIs();
      CreateEventImages();
      UIUpdateTimer = timer.Every(1f, () => CreateEventUIs());
      eventTimer = timer.Once(eventLength, () => EndEvent());
      activeEvent = true;
      string eventStartingAnnouncement = eventStartingString.Replace("{event_name}", eventName).Replace("{event_desc}", eventDesc).Replace("{event_description}", eventDesc);
      NotifyPlayers(eventStartingAnnouncement, eventChatPrefix, eventChatIconID);
      PrintDebug(eventStartingAnnouncement);
    }
    private void StartEvent()
    {
      StartEvent("random");
    }
    private bool GetEventInfo(string requestedEvent)
    {
      if (!FindEvent(requestedEvent))
      {
        debugMessage = $"CRITICAL WARNING: Cannot find the requested event: {requestedEvent} - Aborting GetEventInfo()";
        PrintDebug(debugMessage);
        return false;
      }
      eventName = eventDesc = eventPermission = eventUIBackgroundColor = eventUIMinAnchors = eventUIMaxAnchors = eventUITextColor = eventUITextOutlineColor = eventUpcomingString = eventStartingString = eventEndingString = eventNoParticipantsString = eventWinnerAnnouncementsString = eventPendingParticipationString = eventGameTipStyle = "";
      isAdditionalEvent = false;
      if (requestedEvent != "")
      {
        ConfigData.EventType eventType;
        if (_configData.EventTypes.TryGetValue(requestedEvent, out eventType))
        {
          if (eventType.eventTypeEnabled)
          {
            eventName = eventType.eventTypeName;
            eventDesc = eventType.eventTypeDesc;
            eventLength = eventType.eventTypeLength;
            eventEnablePermission = eventType.eventTypePermissionEnabled;
            eventPermission = eventType.eventTypePermission;
            eventShowUIAndNotificationsWithoutPermission = eventType.eventTypeShowUIAndNotificationsWithoutPermission;
            eventEnableUI = eventType.EventTypeLeaderboards.eventTypeEnableLeaderboardUI;
            eventUIBackgroundColor = eventType.EventTypeLeaderboards.eventTypeLeaderboardUIBackgroundColor;
            eventUIMinAnchors = eventType.EventTypeLeaderboards.eventTypeLeaderboardUIMinAnchors;
            eventUIMaxAnchors = eventType.EventTypeLeaderboards.eventTypeLeaderboardUIMaxAnchors;
            eventUITextColor = eventType.EventTypeLeaderboards.eventTypeLeaderboardUITextColor;
            eventUITextOutlineColor = eventType.EventTypeLeaderboards.eventTypeUITextOutlineColor;
            eventUITextAlignment = eventType.EventTypeLeaderboards.eventTypeUITextAlignment;
            eventUIPlayerListTextAlignment = eventType.EventTypeLeaderboards.eventTypeUIPlayerListTextAlignment;
            eventPendingParticipationString = eventType.EventTypeLeaderboards.eventTypePendingParticipationString;
            eventEnableChatNotifications = eventType.EventTypeNotifications.eventTypeEnableChatNotifications;
            if (eventType.EventTypeNotifications.eventTypeChatPrefix != "")
              eventChatPrefix = eventType.EventTypeNotifications.eventTypeChatPrefix;
            else
              eventChatPrefix = _configData.GeneralOptions.chatPrefix;
            if (eventType.EventTypeNotifications.eventTypeChatIconID != 0)
              eventChatIconID = eventType.EventTypeNotifications.eventTypeChatIconID;
            else
              eventChatIconID = _configData.GeneralOptions.chatIconID;
            eventEnableGameTipNotifications = eventType.EventTypeNotifications.eventTypeEnableGameTipNotifications;
            eventGameTipStyle = eventType.EventTypeNotifications.eventTypeGameTipStyle;
            eventGameTipDuration = eventType.EventTypeNotifications.eventTypeGameTipDuration;
            eventEnableUpcomingNotification = eventType.EventTypeNotifications.eventTypeEnableUpcomingNotification;
            eventUpcomingDelay = eventType.EventTypeNotifications.eventTypeUpcomingDelay;
            eventUpcomingString = eventType.EventTypeNotifications.eventTypeUpcomingString;
            eventStartingString = eventType.EventTypeNotifications.eventTypeStartingString;
            eventEndingString = eventType.EventTypeNotifications.eventTypeEndingString;
            eventNoParticipantsString = eventType.EventTypeNotifications.eventTypeNoParticipantsString;
            eventMultipleWinnersDelay = eventType.EventTypeNotifications.eventTypeMultipleWinnersDelay;
            return true;
          }
          else
          {
            debugMessage = $"WARNING: {requestedEvent} is not enabled, please enable the event in your config file to start it.";
            PrintDebug(debugMessage);
            return false;
          }
        }
        else
        {
          ConfigData.AdditionalEventType additionalEventType;
          if (_configData.AdditionalEventTypes.TryGetValue(requestedEvent, out additionalEventType))
          {
            if (additionalEventType.additionalEventTypeEnabled)
            {
              isAdditionalEvent = true;
              eventName = additionalEventType.additionalEventTypeName;
              eventDesc = additionalEventType.additionalEventTypeDesc;
              eventLength = additionalEventType.additionalEventTypeLength;
              eventMultiplier = additionalEventType.additionalEventTypeMultiplier;
              eventEnablePermission = additionalEventType.additionalEventTypePermissionEnabled;
              eventPermission = additionalEventType.additionalEventTypePermission;
              eventShowUIAndNotificationsWithoutPermission = additionalEventType.additionalEventTypeShowUIAndNotificationsWithoutPermission;
              eventEnableUI = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeEnableUI;
              eventUIBackgroundColor = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUIBackgroundColor;
              eventUIMinAnchors = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUIMinAnchors;
              eventUIMaxAnchors = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUIMaxAnchors;
              eventUITextColor = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUITextColor;
              eventUITextOutlineColor = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUITextOutlineColor;
              eventUITextAlignment = additionalEventType.AdditionalEventTypeUIs.additionalEventTypeUITextAlignment;
              eventEnableChatNotifications = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeEnableChatNotifications;
              if (additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeChatPrefix != "")
                eventChatPrefix = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeChatPrefix;
              else
                eventChatPrefix = _configData.GeneralOptions.chatPrefix;
              if (additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeChatIconID != 0)
                eventChatIconID = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeChatIconID;
              else
                eventChatIconID = _configData.GeneralOptions.chatIconID;
              eventEnableGameTipNotifications = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeEnableGameTipNotifications;
              eventGameTipStyle = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeGameTipStyle;
              eventGameTipDuration = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeGameTipDuration;
              eventEnableUpcomingNotification = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeEnableUpcomingNotification;
              eventUpcomingDelay = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeUpcomingDelay;
              eventUpcomingString = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeUpcomingString;
              eventStartingString = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeStartingString;
              eventEndingString = additionalEventType.AdditionalEventTypeNotifications.additionalEventTypeEndingString;
              return true;
            }
            else
            {
              debugMessage = $"WARNING: {requestedEvent} is not enabled, please enable the event in your config file to start it.";
              PrintDebug(debugMessage);
              return false;
            }
          }
          else
          {
            debugMessage = $"CRITICAL WARNING: Cannot find the requested event information: {requestedEvent} - Aborting GetEventInfo()";
            PrintDebug(debugMessage);
            return false;
          }
        }
      }
      else
      {
        debugMessage = $"CRITICAL WARNING: NULL GetEventInfo() request - Aborting GetEventInfo()";
        PrintDebug(debugMessage);
        return false;
      }
    }
    private void CreateEventUIs()
    {
      if (BasePlayer.activePlayerList.Count > 0)
      {
        foreach (var player in BasePlayer.activePlayerList)
        {
          if (eventEnablePermission)
          {
            if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
            {
              if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
                CreateUI(player);
            }
          }
          else
          {
            if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
              CreateUI(player);
          }
        }
      }
    }
    private void CreateUI(BasePlayer player)
    {
      DestroyUI(player);
      var leaderboardEntries = playerList.OrderByDescending(x => x.Value).Take(6);
      int position = 1;
      string LeaderboardList = "";
      string eventDescText = "";
      if (isAdditionalEvent)
      {
        eventPendingParticipationString = eventDesc;
        eventDescText = "";
      }
      else
      {
        eventDescText = eventDesc;
        if (leaderboardEntries.Count() > 0)
        {
          foreach (var entry in leaderboardEntries.Take(5))
          {
            BasePlayer entryPlayer = BasePlayer.FindByID(entry.Key);
            string playerName = entryPlayer != null ? entryPlayer.displayName : "Unknown";
            if (playerName != "Unknown")
            {
              if (playerName.Length > 20)
              {
                playerName = playerName.Substring(0, 20);
              }
              LeaderboardList += $"{position}. {playerName}: {entry.Value}\n";
              position++;
            }
          }
        }
      }
      string backgroundColor, textColor, outlineColor, anchorMin, anchorMax;
      TextAnchor textAlignment, playerListTextAlignment;
      anchorMin = "0.695 0.025";
      anchorMax = "0.83 0.1975";
      anchorMin = eventUIMinAnchors;
      anchorMax = eventUIMaxAnchors;
      backgroundColor = "255 255 255 0.2";
      backgroundColor = eventUIBackgroundColor;
      textColor = "255 255 255 1.0";
      textColor = eventUITextColor;
      outlineColor = "0 0 0 0.25";
      outlineColor = eventUITextOutlineColor;
      if (eventUITextAlignment == "center") { textAlignment = TextAnchor.MiddleCenter; }
      else if (eventUITextAlignment == "left") { textAlignment = TextAnchor.MiddleLeft; }
      else if (eventUITextAlignment == "right") { textAlignment = TextAnchor.MiddleRight; }
      else { textAlignment = TextAnchor.MiddleCenter; }
      if (eventUIPlayerListTextAlignment == "center") { playerListTextAlignment = TextAnchor.MiddleCenter; }
      else if (eventUIPlayerListTextAlignment == "left") { playerListTextAlignment = TextAnchor.MiddleLeft; }
      else if (eventUIPlayerListTextAlignment == "right") { playerListTextAlignment = TextAnchor.MiddleRight; }
      else { playerListTextAlignment = TextAnchor.MiddleCenter; }
      var container = new CuiElementContainer();
      var panel = container.Add( new CuiPanel
      {
        Image = {
          Color = backgroundColor
        },
        RectTransform = {
          AnchorMin = anchorMin,
          AnchorMax = anchorMax
        },
        CursorEnabled = false,
      }, "Under", UI_MAIN);
      if (eventName != "")
      {
        var titleElement = new CuiElement
        {
          Parent = panel,
          Components =
          {
            new CuiTextComponent
            {
              Text = eventName,
              Color = textColor,
              FontSize = 16,
              Align = textAlignment
            },
            new CuiOutlineComponent
            {
              Distance = "1 1",
              Color = outlineColor
            },
            new CuiRectTransformComponent {
              AnchorMin = "0.05 0.75",
              AnchorMax = "0.95 0.99"
            }
          }
        };
        container.Add(titleElement);
      }
      if (eventDescText != "")
      {
        var descElement = new CuiElement
        {
          Parent = panel,
          Components =
          {
            new CuiTextComponent
            {
              Text = eventDescText,
              Color = textColor,
              FontSize = 11,
              Align = textAlignment
            },
            new CuiOutlineComponent
            {
              Distance = "1 1",
              Color = outlineColor
            },
            new CuiRectTransformComponent {
              AnchorMin = "0.05 0.72",
              AnchorMax = "0.95 0.815"
            }
          }
        };
        container.Add(descElement);
      }
      if (LeaderboardList != "")
      {
        var leaderboardElement = new CuiElement
        {
          Parent = panel,
          Components =
          {
            new CuiTextComponent
            {
              Text = LeaderboardList,
              Color = textColor,
              FontSize = 12,
              Align = playerListTextAlignment
            },
            new CuiOutlineComponent
            {
              Distance = "1 1",
              Color = outlineColor
            },
            new CuiRectTransformComponent {
              AnchorMin = "0.05 0.14",
              AnchorMax = "0.95 0.7"
            }
          }
        };
        container.Add(leaderboardElement);
      }
      else
      {
        if (eventPendingParticipationString != "")
        {
           var leaderboardElement = new CuiElement
          {
            Parent = panel,
            Components =
            {
              new CuiTextComponent
              {
                Text = eventPendingParticipationString,
                Color = textColor,
                FontSize = 12,
                Align = textAlignment
              },
              new CuiOutlineComponent
              {
                Distance = "1 1",
                Color = outlineColor
              },
              new CuiRectTransformComponent {
                AnchorMin = "0.05 0.14",
                AnchorMax = "0.95 0.7"
              }
            }
          };
          container.Add(leaderboardElement);
        }
      }
      float timeRemaining = eventLength - (Time.realtimeSinceStartup - eventStartTime);
      TimeSpan timeLeft = TimeSpan.FromSeconds(timeRemaining);
      string timerText = $"{timeLeft.Minutes}m {timeLeft.Seconds}s ";
      var timerElement = new CuiElement
      {
        Parent = panel,
        Components =
        {
          new CuiTextComponent
          {
            Text = timerText,
            Color = textColor,
            FontSize = 11,
            Align = textAlignment
          },
          new CuiOutlineComponent
          {
            Distance = "1 1",
            Color = outlineColor
          },
          new CuiRectTransformComponent {
            AnchorMin = "0.05 0.01",
            AnchorMax = "0.95 0.17"
          }
        }
      };
      container.Add(timerElement);
      CuiHelper.AddUi(player, container);
    }
    private void DestroyUI(BasePlayer player)
    {
      if (player == null) return;
      CuiHelper.DestroyUi(player, UI_MAIN);
    }
    private void CreateEventImages()
    {
      if (BasePlayer.activePlayerList.Count > 0)
      {
        foreach (var player in BasePlayer.activePlayerList)
        {
          if (eventEnablePermission)
          {
            if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
            {
              if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
                CreateImages(player, currentEvent);
            }
          }
          else
          {
            if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
              CreateImages(player, currentEvent);
          }
        }
      }
    }
    private void CreateImages(BasePlayer player, string eventName)
    {
      if (!FindEvent(eventName))
      {
        debugMessage = $"CRITICAL WARNING: Cannot find the requested event: {eventName} - Aborting CreateImages()";
        PrintDebug(debugMessage);
        return;
      }
      if (eventName != "")
      {
        ConfigData.EventType eventType;
        if (_configData.EventTypes.TryGetValue(eventName, out eventType) && eventType.eventTypeEnabled)
        {
          var imagesCount = eventType.EventTypeImages.Count;
          if (imagesCount > 0)
          {
            var imageCount = 1;
            foreach (ConfigData.EventType.EventTypeImage image in eventType.EventTypeImages)
            {
              if (image.eventTypeEnableImage && image.eventTypeImageURL != "")
              {
                var imageName =  eventName + "_" + imageCount;
                if (ImageLibrary && ImageLibrary.Call<bool>("HasImage", JsonConvert.SerializeObject(imageName)))
                {
                  var imageLibraryImage = ImageLibrary.Call<string>("GetImage", JsonConvert.SerializeObject(imageName));
                  CreateImage(player, imageName, imageLibraryImage, image.eventTypeImageTransparency, image.eventTypeImageMinAnchors, image.eventTypeImageMaxAnchors);
                }
                else
                {
                  CreateImage(player, imageName, image.eventTypeImageURL, image.eventTypeImageTransparency, image.eventTypeImageMinAnchors, image.eventTypeImageMaxAnchors);
                }
              }
              imageCount++;
            }
          }
        }
        else
        {
          ConfigData.AdditionalEventType additionalEventType;
          if (_configData.AdditionalEventTypes.TryGetValue(eventName, out additionalEventType) && additionalEventType.additionalEventTypeEnabled)
          {
            var imagesCount = additionalEventType.AdditionalEventTypeImages.Count;
            var imageCount = 1;
            if (imagesCount > 0)
            {
              foreach (ConfigData.AdditionalEventType.AdditionalEventTypeImage image in additionalEventType.AdditionalEventTypeImages)
              {
                if (image.additionalEventTypeEnableImage && image.additionalEventTypeImageURL != "")
                {
                  var imageName =  eventName + "_" + imageCount;
                  if (ImageLibrary && ImageLibrary.Call<bool>("HasImage", JsonConvert.SerializeObject(imageName)))
                  {
                    var imageLibraryImage = ImageLibrary.Call<string>("GetImage", JsonConvert.SerializeObject(imageName));
                    CreateImage(player, imageName, imageLibraryImage, image.additionalEventTypeImageTransparency, image.additionalEventTypeImageMinAnchors, image.additionalEventTypeImageMaxAnchors);
                  }
                  else
                    CreateImage(player, imageName, image.additionalEventTypeImageURL, image.additionalEventTypeImageTransparency, image.additionalEventTypeImageMinAnchors, image.additionalEventTypeImageMaxAnchors);
                }
                imageCount++;
              }
            }
          }
        }
      }
    }
    private void CreateImage(BasePlayer player, string name = "", string URL = "", float transparency = 0.0f, string minAnchors = "", string maxAnchors = "")
    {
      DestroyImage(player, name);
      var container = new CuiElementContainer();
      var panel = container.Add( new CuiPanel
      {
        Image = {
          Color = "0 0 0 0"
        },
        CursorEnabled = false,
      }, "Under", name);
      if (URL.StartsWith("http") || URL.StartsWith("www"))
      {
        var image = new CuiElement
        {
          Parent = name,
          Components =
          {
            new CuiRawImageComponent
            {
              Url=URL,
              Color = string.Format("1 1 1 {0:F1}", (transparency / 100.0f))
            },
            new CuiRectTransformComponent {
              AnchorMin = minAnchors,
              AnchorMax = maxAnchors
            }
          }
        };
        container.Add(image);
      }
      else
      {
        var image = new CuiElement
        {
          Parent = name,
          Components =
          {
            new CuiRawImageComponent
            {
              Png=URL,
              Color = string.Format("1 1 1 {0:F1}", (transparency / 100.0f))
            },
            new CuiRectTransformComponent {
              AnchorMin = minAnchors,
              AnchorMax = maxAnchors
            }
          }
        };
        container.Add(image);
      }
      CuiHelper.AddUi(player, container);
    }
    private void DestroyEventImages(BasePlayer player)
    {
      if (_configData.EventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.EventType> eventType in _configData.EventTypes)
        {
          if (eventType.Value.eventTypeEnabled)
          {
            var imagesCount = eventType.Value.EventTypeImages.Count;
            var imageCount = 1;
            if (imagesCount > 0)
            {
              foreach (ConfigData.EventType.EventTypeImage image in eventType.Value.EventTypeImages)
              {
                if (image.eventTypeEnableImage && image.eventTypeImageURL != "")
                {
                  var imageName = eventType.Key + "_" + imageCount;
                  DestroyImage(player, imageName);
                }
                imageCount++;
              }
            }
          }
        }
      }
      if (_configData.AdditionalEventTypes.Count > 0)
      {
        foreach (KeyValuePair<string, ConfigData.AdditionalEventType> eventType in _configData.AdditionalEventTypes)
        {
          if (eventType.Value.additionalEventTypeEnabled)
          {
            var imagesCount = eventType.Value.AdditionalEventTypeImages.Count;
            var imageCount = 1;
            if (imagesCount > 0)
            {
              foreach (ConfigData.AdditionalEventType.AdditionalEventTypeImage image in eventType.Value.AdditionalEventTypeImages)
              {
                if (image.additionalEventTypeEnableImage && image.additionalEventTypeImageURL != "")
                {
                  var imageName = eventType.Key + "_" + imageCount;
                  DestroyImage(player, imageName);
                }
                imageCount++;
              }
            }
          }
        }
      }
    }
    private void DestroyImage(BasePlayer player, string name)
    {
      if (player == null) return;
        CuiHelper.DestroyUi(player, name);
    }
    private void EndEvent()
    {
      if (!activeEvent)
      {
        debugMessage = "CRITICAL WARNING: No active event running. Aborting EndEvent()";
        PrintDebug(debugMessage);
        return;
      }
      Unsubscribe(nameof(OnDispenserBonus));
      Unsubscribe(nameof(OnDispenserGather));
      Unsubscribe(nameof(OnCollectiblePickup));
      Unsubscribe(nameof(OnEntityDeath));
      Unsubscribe(nameof(OnLootEntityEnd));
      autoEventTimer?.Destroy();
      eventTimer?.Destroy();
      UIUpdateTimer.Destroy();
      rewardsList.Clear();
      finalPlayerList.Clear();
      string eventEndAnnouncement = eventEndingString.Replace("{event_name}", eventName);
      NotifyPlayers(eventEndAnnouncement, eventChatPrefix, eventChatIconID);
      PrintDebug(eventEndAnnouncement);
      if (BasePlayer.activePlayerList.Count > 0)
      {
        foreach (var player in BasePlayer.activePlayerList)
        {
          DestroyUI(player);
          DestroyEventImages(player);
        }
      }
      if (playerList.Count > 0)
      {
        foreach (var player in playerList)
        {
          BasePlayer checkPlayer = BasePlayer.FindByID(player.Key);
          if (checkPlayer)
          {
            if (!finalPlayerList.ContainsKey(player.Key))
              finalPlayerList[player.Key] = player.Value;
          }
        }
      }
      if (!isAdditionalEvent)
      {
        if (finalPlayerList.Count == 0)
        {
          string noParticipantsAnnouncement = eventNoParticipantsString.Replace("{event_name}", eventName);
          NotifyPlayers(noParticipantsAnnouncement, eventChatPrefix, eventChatIconID);
          PrintDebug(noParticipantsAnnouncement);
        }
        else
        {
          if (finalPlayerList.Count > 0)
          {
            int leaderboardCount = 0;
            ConfigData.EventType eventType;
            if (_configData.EventTypes.TryGetValue(currentEvent, out eventType) && eventType.eventTypeEnabled)
            {
              var rewardsCount = eventType.Rewards.Count;
              if (rewardsCount > 0)
              {
                var orderedLeaderboard = finalPlayerList.OrderByDescending(x => x.Value);
                foreach (ConfigData.EventType.WinnerReward reward in eventType.Rewards)
                {
                  if (leaderboardCount < orderedLeaderboard.Count())
                  {
                    var winner = orderedLeaderboard.ElementAt(leaderboardCount);
                    BasePlayer winningPlayer = BasePlayer.FindByID(winner.Key);
                    if (winningPlayer != null)
                    {
                      if (reward.enableWinnerReward)
                      {
                        string rewardDisplayName = "";
                        string rewardDescription = "";
                        string rewardAnnouncementString = "";
                        bool rewardSuccess = false;
                        int currentRewardsCount = 0;
                        var itemsCount = reward.RewardItems.Count;
                        var commandsCount = reward.RewardCommands.Count;
                        var kitsCount = reward.RewardKits.Count;
                        Kits = plugins.Find("Kits");
                        var randomRewardProbability = UnityEngine.Random.Range(0, 100);
                        if (randomRewardProbability <= reward.winnerRewardProbability)
                        {
                          // ITEMS
                          if (itemsCount > 0)
                          {
                            foreach (ConfigData.EventType.WinnerReward.RewardItem item in reward.RewardItems)
                            {
                              if (item.enableWinnerRewardItem)
                              {
                                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(item.winnerRewardItem);
                                string itemEnglishName = string.IsNullOrEmpty(item.winnerRewardItemDisplayName) ? itemDefinition.displayName.english : item.winnerRewardItemDisplayName;
                                rewardDisplayName = $"{item.winnerRewardItemAmount} {itemEnglishName}";
                                var randomItemProbability = UnityEngine.Random.Range(0, 100);
                                if (randomItemProbability <= item.winnerRewardItemProbability)
                                {
                                  if (itemDefinition == null || item.winnerRewardItem == "")
                                  {
                                    debugMessage = "CRITICAL WARNING: No item found, please check config file. Aborting item reward.";
                                    PrintDebug(debugMessage);
                                  }
                                  else
                                  {
                                    GiveItem(winningPlayer, item.winnerRewardItem, item.winnerRewardItemAmount, item.winnerRewardItemCustomSkin);
                                    rewardSuccess = true;
                                    debugMessage = $"Item - {randomItemProbability}% - {rewardDisplayName} ({item.winnerRewardItemProbability}% Probability) given to {winningPlayer.displayName} ({winningPlayer.userID})";
                                    PrintDebug(debugMessage);
                                    if (item.winnerRewardItemDisplayName != "")
                                      rewardsList.Add(rewardDisplayName);
                                    currentRewardsCount += 1;
                                  }
                                }
                                else
                                {
                                  debugMessage = $"Item - {randomItemProbability}% - {rewardDisplayName} ({item.winnerRewardItemProbability}% Probability) SKIPPED giving to {winningPlayer.displayName} ({winningPlayer.userID})";
                                  PrintDebug(debugMessage);
                                }
                              }
                            }
                          }
                          // COMMANDS
                          if (commandsCount > 0)
                          {
                            foreach (ConfigData.EventType.WinnerReward.RewardCommand command in reward.RewardCommands)
                            {
                              if (command.enableWinnerRewardCommand)
                              {
                                rewardDisplayName = command.winnerRewardCommandDisplayName;
                                var randomCommandProbability = UnityEngine.Random.Range(0, 100);
                                if (randomCommandProbability <= command.winnerRewardCommandProbability)
                                {
                                  if (command.winnerRewardCommand == "")
                                  {
                                    debugMessage = "CRITICAL WARNING: Blank command, please check config file. Aborting reward.";
                                    PrintDebug(debugMessage);
                                  }
                                  else
                                  {
                                    ExecuteCommand(command.winnerRewardCommand.Replace("{player.id}", winningPlayer.userID.ToString()).Replace("{player.name}", winningPlayer.displayName));
                                    rewardSuccess = true;
                                    debugMessage = $"Command - {randomCommandProbability}% - {rewardDisplayName} ({command.winnerRewardCommandProbability}% Probability) command executed for {winningPlayer.displayName} ({winningPlayer.userID}): {command.winnerRewardCommand}";
                                    PrintDebug(debugMessage);
                                    if (command.winnerRewardCommandDisplayName != "")
                                      rewardsList.Add(rewardDisplayName);
                                    currentRewardsCount += 1;
                                  }
                                }
                                else
                                {
                                  debugMessage = $"Command - {randomCommandProbability}% - {rewardDisplayName} ({command.winnerRewardCommandProbability}% Probability) SKIPPED giving to {winningPlayer.displayName} ({winningPlayer.userID})";
                                  PrintDebug(debugMessage);
                                }
                              }
                            }
                          }
                          // KITS
                          if (kitsCount > 0)
                          {
                            foreach (ConfigData.EventType.WinnerReward.RewardKit kit in reward.RewardKits)
                            {
                              if (kit.enableWinnerRewardKit)
                              {
                                if (!Kits)
                                {
                                  debugMessage = "CRITICAL WARNING: Kits plugin not installed, please check config file. Aborting reward.";
                                  PrintDebug(debugMessage);
                                }
                                else
                                {
                                  rewardDisplayName = kit.winnerRewardKitDisplayName;
                                  var randomKitProbability = UnityEngine.Random.Range(0, 100);
                                  if (randomKitProbability <= kit.winnerRewardKitProbability)
                                  {
                                    if (kit.winnerRewardKit == "")
                                    {
                                      debugMessage = "CRITICAL WARNING: Blank kit, please check config file. Aborting reward.";
                                      PrintDebug(debugMessage);
                                    }
                                    else
                                    {
                                      object kitSuccess = Kits.CallHook("GiveKit", winningPlayer, kit.winnerRewardKit);
                                      if (kitSuccess is string)
                                      {
                                        debugMessage = "CRITICAL WARNING: Cannot find kit ({rewardDisplayName}) to give to {winningPlayer.displayName} ({winningPlayer.userID}), please check config file. Aborting reward.";
                                        PrintDebug(debugMessage);
                                      }
                                      else
                                      {
                                        rewardSuccess = true;
                                        debugMessage = $"Kit - {randomKitProbability}% - {rewardDisplayName} ({kit.winnerRewardKitProbability}% Probability) kit given to {winningPlayer.displayName} ({winningPlayer.userID})";
                                        PrintDebug(debugMessage);
                                        if (kit.winnerRewardKitDisplayName != "")
                                          rewardsList.Add(rewardDisplayName);
                                        currentRewardsCount += 1;
                                      }
                                    }
                                  }
                                  else
                                  {
                                    debugMessage = $"Kit - {randomKitProbability}% - {rewardDisplayName} ({kit.winnerRewardKitProbability}% Probability) SKIPPED giving to {winningPlayer.displayName} ({winningPlayer.userID})";
                                    PrintDebug(debugMessage);
                                  }
                                }
                              }
                            }
                          }
                        }
                        else
                        {
                          debugMessage = $"Reward ({reward.winnerRewardProbability}% Probability) SKIPPED giving to {winningPlayer.displayName} ({winningPlayer.userID})";
                          PrintDebug(debugMessage);
                        }
                        if (currentRewardsCount > 0)
                        {
                          if (reward.RewardNotification.enableWinnerRewardNotification)
                          {
                            string rewardList;
                            if (reward.RewardNotification.enableWinnerRewardsCSV)
                              rewardList = string.Join(", ", rewardsList);
                            else
                              rewardList = string.Join(" ", rewardsList);
                            var player_points = winner.Value;
                            string formatted_player_points = player_points.ToString("N0");
                            string winnerAnnouncement = reward.RewardNotification.winnerRewardNotification
                                .Replace("{player_name}", winningPlayer.displayName)
                                .Replace("{rewards_list}", rewardList)
                                .Replace("{rewards}", rewardList)
                                .Replace("{event_name}", eventName)
                                .Replace("{points_scored}", formatted_player_points);
                            if (reward.RewardNotification.enableWinnerRewardNotificationToPlayer)
                            {
                              NotifyPlayer(winningPlayer, winnerAnnouncement, eventChatPrefix, eventChatIconID);
                              debugMessage = winnerAnnouncement;
                              PrintDebug(debugMessage);
                            }
                            else
                            {
                              timer.Once(eventMultipleWinnersDelay * leaderboardCount, () =>
                              {
                                NotifyPlayers(winnerAnnouncement, eventChatPrefix, eventChatIconID);
                                debugMessage = winnerAnnouncement;
                                PrintDebug(debugMessage);
                              });
                            }
                          }
                        }
                        else
                        {
                          debugMessage = $"{eventName} - REWARD SKIPPED - Reward Number: {leaderboardCount.ToString()}";
                          PrintDebug(debugMessage);
                        }
                      }
                    }
                    rewardsList.Clear();
                    leaderboardCount += 1;
                  }
                }
              }
              else
              {
                // NO REWARDS
                debugMessage = "CRITICAL WARNING: No reward positions found. Aborting rewards.";
                PrintDebug(debugMessage);
              }
            }
          }
        }
      }
      isAdditionalEvent = false;
      activeEvent = false;
      currentEvent = "";
      playerList.Clear();
      finalPlayerList.Clear();
      rewardsList.Clear();
      StartAutoEventTimer();
    }
    private void CacheImages()
    {
      if (ImageLibrary)
      {
        if (_configData.EventTypes.Count > 0)
        {
          foreach (KeyValuePair<string, ConfigData.EventType> eventType in _configData.EventTypes)
          {
            var imagesCount = eventType.Value.EventTypeImages.Count;
            var imageCount = 1;
            if (imagesCount > 0)
            {
              foreach (ConfigData.EventType.EventTypeImage image in eventType.Value.EventTypeImages)
              {
                if (image.eventTypeEnableImage && image.eventTypeImageURL != "")
                {
                  var imageName = eventType.Key + "_" + imageCount;
                  if (ImageLibrary.Call<bool>("AddImage", image.eventTypeImageURL, JsonConvert.SerializeObject(imageName)))
                  {
                    debugMessage = $"Image ({imageName}_0) cached via ImageLibrary: {image.eventTypeImageURL}";
                    PrintDebug(debugMessage);
                    imageCount++;
                  }
                }
              }
            }
          }
        }
        if (_configData.AdditionalEventTypes.Count > 0)
        {
          foreach (KeyValuePair<string, ConfigData.AdditionalEventType> eventType in _configData.AdditionalEventTypes)
          {
            var imagesCount = eventType.Value.AdditionalEventTypeImages.Count;
            var imageCount = 1;
            if (imagesCount > 0)
            {
              foreach (ConfigData.AdditionalEventType.AdditionalEventTypeImage image in eventType.Value.AdditionalEventTypeImages)
              {
                if (image.additionalEventTypeEnableImage  && image.additionalEventTypeImageURL != "")
                {
                  var imageName = eventType.Key + "_" + imageCount;
                  if (ImageLibrary.Call<bool>("AddImage", image.additionalEventTypeImageURL, JsonConvert.SerializeObject(imageName)))
                  {
                    debugMessage = $"Image ({imageName}_0) cached via ImageLibrary: {image.additionalEventTypeImageURL}";
                    PrintDebug(debugMessage);
                    imageCount++;
                  }
                }
              }
            }
          }
        }
      }
      else
      {
        debugMessage = "ImageLibrary plugin not installed, using Event Image URLs instead of cached images.";
        PrintDebug(debugMessage);
      }
    }
    private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!activeEvent) return;
      if (dispenser == null || entity == null || item == null) return;
      BasePlayer player = entity.ToPlayer();
      if (player == null || player is ScientistNPC || player is NPCPlayer) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      ulong playerID = player.userID;
      if (currentEvent.ToLower() == OreWar) // OreWar
      {
        if (dispenser.gatherType != ResourceDispenser.GatherType.Ore) return;
        var gatherType = item.info.name;
        if (gatherType.Contains("hq_metal_ore")) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == TreeTrimmers) // TreeTrimmers
      {
        if (dispenser.gatherType != ResourceDispenser.GatherType.Tree) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == ResourceRumble) // ResourceRumble
      {
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]+=item.amount;
      }
      else if (currentEvent.ToLower() == ResourceRun) //ResourceRun
      {
        float multiplier = eventMultiplier;
        item.amount = (int)(item.amount * multiplier);
      }
      else
        return;
    }
    private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!activeEvent) return;
      BasePlayer player = entity?.ToPlayer();
      if (player == null || player is ScientistNPC || player is NPCPlayer) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      ulong playerID = player.userID;
      if (currentEvent.ToLower() == ResourceRumble) //ResourceRumble
      {
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]+=item.amount;
      }
      else if (currentEvent.ToLower() == ResourceRun) //ResourceRun
      {
        float multiplier = eventMultiplier;
        item.amount = (int)(item.amount * multiplier);
      }
      else
        return;
    }
    private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
    {
      if (!activeEvent) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      if (collectible == null) return;
      if (player == null || player is ScientistNPC || player is NPCPlayer) return;
      ulong playerID = player.userID;
      if (currentEvent.ToLower() == ResourceRumble) //ResourceRumble
      {
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        foreach (ItemAmount item in collectible.itemList)
          playerList[playerID]+=(int)item.amount;
      }
      else if (currentEvent.ToLower() == ResourceRun) //ResourceRun
      {
        foreach (ItemAmount item in collectible.itemList)
        {
          float multiplier = eventMultiplier;
          item.amount = (int)(item.amount * multiplier);
        }
      }
      else
        return;
    }
    private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
    {
      if (!activeEvent) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      if (entity == null || player == null || player is ScientistNPC || player is NPCPlayer) return;
      if (!containerPrefabShortNames.Contains(entity.ShortPrefabName)) return;
      if (currentEvent.ToLower() == CrateClash) // CrateClash
      {
        LootContainer container = entity as LootContainer;
        int count = 0;
        if (container)
        {
          foreach (var item in container.inventory.itemList)
            count++;
        }
        if (count != 0) return;
        ulong playerID = player.userID;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else
        return;
    }
    private void OnEntityDeath(BaseEntity entity, HitInfo info)
    {
      if (!activeEvent) return;
      if (entity == null || info == null) return;
      BasePlayer player = info.Initiator?.ToPlayer();
      if (player == null || player is ScientistNPC || player is NPCPlayer) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      ulong playerID = player.userID;
      if (currentEvent.ToLower() == BarrelBreakers) //BarrelBreakers
      {
        if (!barrelShortPrefabNames.Contains(entity.ShortPrefabName)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == RoadsignRun) //RoadsignRun
      {
        if (!roadsignShortPrefabNames.Contains(entity.ShortPrefabName)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == BotBash) //BotBash
      {
        if (!(entity is Zombie) && !(entity is ScientistNPC) && !(entity is UnderwaterDweller) && !(entity is TunnelDweller)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == TunnelTussle) //TunnelTussle
      {
        if (!(entity is TunnelDweller)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == UnderwaterWar) //UnderwaterWar
      {
        if (!(entity is UnderwaterDweller)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == PlayerBattle) //PlayerBattle
      {
        if (entity is NPCPlayer || entity is ScientistNPC) return;
				if (!(entity is BasePlayer) || !(player is BasePlayer)) return;
        BasePlayer victim = entity.ToPlayer();
				if (victim.userID == player.userID) return;
				if (player.Team != null && player.Team.members.Contains(victim.userID)) return;
        if (victim.IsSleeping()) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == AnimalAnnihilation) //AnimalAnnihilation
      {
        if (!animalPrefabShortnames.Contains(entity.ShortPrefabName)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else if (currentEvent.ToLower() == FishingFrenzy) //FishingFrenzy
      {
        if (!(entity is SimpleShark)) return;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else
        return;
    }
    private void OnFishCaught(ItemDefinition definition, BaseFishingRod rod, BasePlayer player)
    {
      if (!activeEvent) return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      if (definition == null || player == null || player is ScientistNPC || player is NPCPlayer) return;
      if (currentEvent.ToLower() == FishingFrenzy) //FishingFrenzy
      {
        ulong playerID = player.userID;
        if (!playerList.ContainsKey(playerID))
          playerList[playerID] = 0;
        playerList[playerID]++;
      }
      else
        return;
    }
    private void NotifyPlayers(string message, string prefix, ulong icon)
    {
      if (message == "") return;
      if (eventEnableGameTipNotifications)
      {
        gameTipTimer?.Destroy();
        if (BasePlayer.activePlayerList.Count > 0)
        {
          foreach (var player in BasePlayer.activePlayerList)
          {
            if (eventEnablePermission)
            {
              if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
              {
                ShowGameTip(player, message, eventGameTipStyle);
                gameTipTimer = timer.Once(eventGameTipDuration, () =>
                {
                  HideGameTip(player);
                });
              }
            }
            else
            {
              ShowGameTip(player, message, eventGameTipStyle);
              gameTipTimer = timer.Once(eventGameTipDuration, () =>
              {
                HideGameTip(player);
              });
            }
          }
        }
      }
      if (eventEnableChatNotifications)
      {
        if (BasePlayer.activePlayerList.Count > 0)
        {
          foreach (var player in BasePlayer.activePlayerList)
          {
            if (eventEnablePermission)
            {
              if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
                Player.Message(player, message, prefix, icon);
            }
            else
              Player.Message(player, message, prefix, icon);
          }
        }
      }
    }
    private void NotifyPlayer(BasePlayer player, string message, string prefix, ulong icon)
    {
      if (message == "") return;
      if (eventEnablePermission && !CheckPlayerEventPermission(player,eventPermission)) return;
      if (eventEnableGameTipNotifications)
      {
        ShowGameTip(player, message, eventGameTipStyle);
        gameTipTimer?.Destroy();
        gameTipTimer = timer.Once(eventGameTipDuration, () =>
        {
          HideGameTip(player);
        });
      }
      if (eventEnableChatNotifications)
      {
        Player.Message(player, message, prefix, icon);
      }
    }
    private void ShowGameTip(BasePlayer player, string message, string type)
    {
      if (!player)
        return;
      if (message == "")
        return;
      switch (type)
      {
        case "info":
          player.SendConsoleCommand("gametip.showgametip", message);
          break;
        case "alert":
          player.SendConsoleCommand("gametip.showtoast", 1, message);
          break;
        case "toast":
          player.SendConsoleCommand("gametip.showtoast", 1, message);
          break;
        default:
          player.SendConsoleCommand("gametip.showgametip", message);
          break;
      }
    }
    private void HideGameTip(BasePlayer player)
    {
      player.SendConsoleCommand("gametip.hidegametip");
      player.SendConsoleCommand("gametip.hidetoast");
    }
    private void GiveItem(BasePlayer player, string itemShortName, int amount, ulong skinId = 0)
    {
      ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemShortName);
      if (itemDefinition != null)
      {
        Item item = ItemManager.Create(itemDefinition, amount, skinId);
        if (player.inventory.GiveItem(item) != true)
        {
          debugMessage = $"{player.displayName} inventory full. Dropping {itemShortName} on ground near player.";
          PrintDebug(debugMessage);
          Vector3 position = player.transform.position;
          Vector3 velocity = new Vector3(0, 0, 0);
          item.Drop(position, velocity);
        }
      }
      else
      {
        debugMessage = $"GiveItem() unable to give item: '{itemShortName}' to player: {player.displayName}";
        PrintDebug(debugMessage);
        return;
      }
    }
    private void ExecuteCommand(string command)
    {
      if (!string.IsNullOrEmpty(command))
        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
      else
      {
        debugMessage = "ExecuteCommand() Unable to execute null command.";
        PrintDebug(debugMessage);
        return;
      }
    }
    private void PrintDebug(string debugMessage)
    {
      SaveToLogFile(debugMessage);
      PrintToConsole(debugMessage);
    }
    private void SaveToLogFile(string debugMessage)
    {
      if (!_configData.GeneralOptions.enableLogFile) return;
      string date = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
      string logMessage = $"[{date}] - {debugMessage}";
      LogToFile("log", logMessage, this);
    }
    private void PrintToConsole(string debugMessage)
    {
      if (!_configData.GeneralOptions.enableConsoleMessages) return;
      PrintWarning(debugMessage);
    }
    private void ToggleExtraEventsUI(BasePlayer player)
    {
      if (player == null) return;
      if (_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
      {
        _pluginData.PlayerDisabledExtraEventsUI.Remove(player.userID);
        if (activeEvent)
        {
          if (eventEnablePermission)
          {
            if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
            {
              if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
                CreateUI(player);
              if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
              {
                _pluginData.PlayerDisabledExtraEventsImages.Remove(player.userID);
                CreateImages(player, currentEvent);
              }
            }
          }
          else
          {
            if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
              CreateUI(player);
            if (!_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
            {
              _pluginData.PlayerDisabledExtraEventsImages.Remove(player.userID);
              CreateImages(player, currentEvent);
            }
          }
        }
        message = lang.GetMessage("UI Enabled", this, player.UserIDString);
        if (player != null) Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
      }
      else
      {
        _pluginData.PlayerDisabledExtraEventsUI.Add(player.userID);
        if (activeEvent)
        {
          DestroyUI(player);
          DestroyEventImages(player);
        }
        message = lang.GetMessage("UI Disabled", this, player.UserIDString);
        if (player != null) Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
      }
    }
    private void ToggleExtraEventsImage(BasePlayer player)
    {
      if (player == null) return;
      if (_pluginData.PlayerDisabledExtraEventsImages.Contains(player.userID))
      {
        _pluginData.PlayerDisabledExtraEventsImages.Remove(player.userID);
        if (activeEvent)
        {
          if (eventEnablePermission)
          {
            if (eventShowUIAndNotificationsWithoutPermission || CheckPlayerEventPermission(player,eventPermission))
            {
              if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
                CreateImages(player, currentEvent);
            }
          }
          else
          {
            if (!_pluginData.PlayerDisabledExtraEventsUI.Contains(player.userID))
              CreateImages(player, currentEvent);
          }
        }
        message = lang.GetMessage("Image Enabled", this, player.UserIDString);
        if (player != null) Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
      }
      else
      {
        _pluginData.PlayerDisabledExtraEventsImages.Add(player.userID);
        if (activeEvent)
          DestroyEventImages(player);
        message = lang.GetMessage("Image Disabled", this, player.UserIDString);
        if (player != null) Player.Message(player, message, _configData.GeneralOptions.chatPrefix, _configData.GeneralOptions.chatIconID);
      }
    }
    #region Data
    private void SaveProtoData()
    {
      if (_pluginData != null) ProtoStorage.Save(_pluginData, Name);
    }
    private void LoadProtoData()
    {
      _pluginData = ProtoStorage.Load<PluginData>(Name) ?? new PluginData();
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    private class PluginData
    {
      public HashSet<ulong> PlayerDisabledExtraEventsUI { get; set; } = new HashSet<ulong>();
      public HashSet<ulong> PlayerDisabledExtraEventsImages { get; set; } = new HashSet<ulong>();
    }
    #endregion
    #region Lang
    protected override void LoadDefaultMessages()
    {
      lang.RegisterMessages(new Dictionary<string, string>
      {
        ["No Permission"] = "You do not have permission to use this command.",
        ["Chat Command Reply No Admin Permission"] = "Command Usage:\n/{ChatCommand} ui - Toggle the Event UI\n/{ChatCommand} image - Toggle the Event images",
        ["Chat Command Reply With Admin Permission"] = "Admin Command Usage:\n/{ChatCommand} ui - Toggle the Event UI\n/{ChatCommand} image - Toggle the Event images\n/{ChatCommand} start - Start random event\n/{ChatCommand} start EventName - Start specific event\n/{ChatCommand} end - End current event",
        ["Active Event"] = "An event is already active.",
        ["No Active Event"] = "There is no event currently active.",
        ["UI Enabled"] = "ExtraEvents UI Enabled",
        ["UI Disabled"] = "ExtraEvents UI Disabled",
        ["Image Enabled"] = "ExtraEvents Image Enabled",
        ["Image Disabled"] = "ExtraEvents Image Disabled",
      }, this, "en");
    }
    #endregion
    #region Config
    private class ConfigData
    {
      [JsonProperty(PropertyName = "General Options")]
      public GeneralOptionsSettings GeneralOptions { get; set; } = new GeneralOptionsSettings();
      public class GeneralOptionsSettings
      {
        [JsonProperty(PropertyName = "Chat Prefix")]
        public string chatPrefix { get; set; } = "<color=purple>ExtraEvents:</color>";
        [JsonProperty(PropertyName = "Chat Icon (Steam64 ID)")]
        public ulong chatIconID { get; set; } = 76561199519603325;
        [JsonProperty(PropertyName = "Minimum Players Online to Automatically Start Random Event")]
        public int minPlayers { get; set; } = 3;
        [JsonProperty(PropertyName = "Auto Random Event Start Time Min (seconds)")]
        public int minInitiateTime { get; set; } = 3600;
        [JsonProperty(PropertyName = "Auto Random Event Start Time Max (seconds)")]
        public int maxInitiateTime { get; set; } = 7200;
        [JsonProperty(PropertyName = "Enable Console Messages")]
        public bool enableConsoleMessages { get; set; } = true;
        [JsonProperty(PropertyName = "Enable Log File")]
        public bool enableLogFile { get; set; } = true;
        [JsonProperty(PropertyName = "Chat Command")]
        public string chatCommand { get; set; } = "extraevents";
        [JsonProperty(PropertyName = "Admin Permission")]
        public string adminPermission { get; set; } = "extraevents.admin";
        [JsonProperty(PropertyName = "All Events Permission (optional, overrides individual event permissions if enabled)")]
        public string allEventsPermission { get; set; } = "extraevents.all";
      }
      [JsonProperty(PropertyName = "Event Types")]
      public Dictionary<string, EventType> EventTypes { get; set; } = new Dictionary<string, EventType>
      {
        { "OreWar", new EventType { eventTypeName = "OreWar", eventTypeDesc = "Mine ore nodes to win!", eventTypePermission = "extraevents.orewar" } },
        { "TreeTrimmers", new EventType { eventTypeName = "TreeTrimmers", eventTypeDesc = "Chop trees to win!", eventTypePermission = "extraevents.treetrimmers" } },
        { "BarrelBreakers", new EventType { eventTypeName = "BarrelBreakers", eventTypeDesc = "Break barrels to win!", eventTypePermission = "extraevents.barrelbreakers" } },
        { "FishingFrenzy", new EventType { eventTypeName = "FishingFrenzy", eventTypeDesc = "Catch fish to win!", eventTypePermission = "extraevents.fishingfrenzy" } },
        { "BotBash", new EventType { eventTypeName = "BotBash", eventTypeDesc = "Kill bots to win!", eventTypePermission = "extraevents.botbash" } },
        { "CrateClash", new EventType { eventTypeName = "CrateClash", eventTypeDesc = "Loot crates to win!", eventTypePermission = "extraevents.crateclash" } },
        { "AnimalAnnihilation", new EventType { eventTypeName = "AnimalAnnihilation", eventTypeDesc = "Kill animals to win!", eventTypePermission = "extraevents.animalannihilation" } },
        { "ResourceRumble", new EventType { eventTypeName = "ResourceRumble", eventTypeDesc = "Collect resources to win!", eventTypePermission = "extraevents.resourcerumble" } },
        { "TunnelTussle", new EventType { eventTypeName = "TunnelTussle", eventTypeDesc = "Kill tunnel dwellers to win!", eventTypePermission = "extraevents.tunneltussle" } },
        { "RoadsignRun", new EventType { eventTypeName = "RoadsignRun", eventTypeDesc = "Destroy roadsigns to win!", eventTypePermission = "extraevents.roadsignrun" } },
        { "UnderwaterWar", new EventType { eventTypeName = "UnderwaterWar", eventTypeDesc = "Kill underwater lab scientists to win!", eventTypePermission = "extraevents.underwaterwar" } },
        { "PlayerBattle", new EventType { eventTypeName = "PlayerBattle", eventTypeDesc = "Kill other players to win!", eventTypePermission = "extraevents.playerbattle" } }
      };
      public class EventType {
        [JsonProperty(PropertyName = "Enable Event")]
        public bool eventTypeEnabled { get; set; } = true;
        [JsonProperty(PropertyName = "Event Name")]
        public string eventTypeName { get; set; }
        [JsonProperty(PropertyName = "Event Description")]
        public string eventTypeDesc { get; set; }
        [JsonProperty(PropertyName = "Event Length (seconds)")]
        public int eventTypeLength { get; set; } = 600;
        [JsonProperty(PropertyName = "Enable Event Permission")]
        public bool eventTypePermissionEnabled { get; set; } = false;
        [JsonProperty(PropertyName = "Event Permission")]
        public string eventTypePermission { get; set; }
        [JsonProperty(PropertyName = "Show UI And Notifications To Players Without Event Permission?")]
        public bool eventTypeShowUIAndNotificationsWithoutPermission { get; set; } = false;
        [JsonProperty(PropertyName = "Event Image(s)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<EventTypeImage> EventTypeImages { get; set; } = new List<EventTypeImage>
        {
          new EventTypeImage {  }
        };
        public class EventTypeImage
        {
          [JsonProperty(PropertyName = "Enable Image")]
          public bool eventTypeEnableImage { get; set; } = true;
          [JsonProperty(PropertyName = "Image URL")]
          public string eventTypeImageURL { get; set; } = "https://avatars.cloudflare.steamstatic.com/9df6fd69fc80ebe9387bb7a58ff4ee01d092af11_full.jpg";
          [JsonProperty(PropertyName = "Image Transparency (0.0 - 100.0)")]
          public float eventTypeImageTransparency { get; set; } = 75.0f;
          [JsonProperty(PropertyName = "Image Anchors Min (x y)")]
          public string eventTypeImageMinAnchors { get; set; } = "0.8 0.2";
          [JsonProperty(PropertyName = "Image Anchors Max (x y)")]
          public string eventTypeImageMaxAnchors { get; set; } = "0.83 0.245";
        }
        [JsonProperty(PropertyName = "Event Leaderboard")]
        public EventTypeLeaderboardUISettings EventTypeLeaderboards { get; set; } = new EventTypeLeaderboardUISettings();
        public class EventTypeLeaderboardUISettings
        {
          [JsonProperty(PropertyName = "Enable Leaderboard UI")]
          public bool eventTypeEnableLeaderboardUI { get; set; } = true;
          [JsonProperty(PropertyName = "UI Anchors Min (x y)")]
          public string eventTypeLeaderboardUIMinAnchors { get; set; } = "0.695 0.025";
          [JsonProperty(PropertyName = "UI Anchors Max (x y)")]
          public string eventTypeLeaderboardUIMaxAnchors { get; set; } = "0.83 0.1975";
          [JsonProperty(PropertyName = "UI Background Color (r g b a)")]
          public string eventTypeLeaderboardUIBackgroundColor { get; set; } = "255 255 255 0.2";
          [JsonProperty(PropertyName = "UI Text Color (r g b a)")]
          public string eventTypeLeaderboardUITextColor { get; set; } = "255 255 255 1.0";
          [JsonProperty(PropertyName = "UI Text Outline Color (r g b a)")]
          public string eventTypeUITextOutlineColor { get; set; } = "0 0 0 0.25";
          [JsonProperty(PropertyName = "UI Text Alignment (left, right, center)")]
          public string eventTypeUITextAlignment { get; set; } = "center";
          [JsonProperty(PropertyName = "UI Player List Text Alignment (left, right, center)")]
          public string eventTypeUIPlayerListTextAlignment { get; set; } = "center";
          [JsonProperty(PropertyName = "UI Pending Participation Message")]
          public string eventTypePendingParticipationString { get; set; } = "No one has played... yet.";
        }
        [JsonProperty(PropertyName = "Event Notifications")]
        public EventTypeNotificationsSettings EventTypeNotifications { get; set; } = new EventTypeNotificationsSettings();
        public class EventTypeNotificationsSettings
        {
          [JsonProperty(PropertyName = "Enable Chat Notifications")]
          public bool eventTypeEnableChatNotifications { get; set; } = true;
          [JsonProperty(PropertyName = "Event Chat Prefix")]
          public string eventTypeChatPrefix { get; set; } = "";
          [JsonProperty(PropertyName = "Event Chat Icon (Steam64 ID)")]
          public ulong eventTypeChatIconID { get; set; } = 0;
          [JsonProperty(PropertyName = "Enable GameTip Notifications")]
          public bool eventTypeEnableGameTipNotifications { get; set; } = false;
          [JsonProperty(PropertyName = "GameTip Style (info OR alert)")]
          public string eventTypeGameTipStyle { get; set; } = "info";
          [JsonProperty(PropertyName = "GameTip Duration (seconds)")]
          public float eventTypeGameTipDuration { get; set; } = 3.0f;
          [JsonProperty(PropertyName = "Enable Event Upcoming Notification")]
          public bool eventTypeEnableUpcomingNotification { get; set; } = false;
          [JsonProperty(PropertyName = "Event Upcoming Delay (seconds) (time before event starts after Event Upcoming Notification)")]
          public int eventTypeUpcomingDelay { get; set; } = 30;
          [JsonProperty(PropertyName = "Event Upcoming")]
          public string eventTypeUpcomingString { get; set; } = "The <color=purple>{event_name}</color> event will start in {upcoming_announcement_delay} seconds! <color=purple>{event_description}</color>";
          [JsonProperty(PropertyName = "Event Starting")]
          public string eventTypeStartingString { get; set; } = "The <color=purple>{event_name}</color> event has started! <color=purple>{event_description}</color>";
          [JsonProperty(PropertyName = "Event Ending")]
          public string eventTypeEndingString { get; set; } = "The <color=purple>{event_name}</color> event has ended.";
          [JsonProperty(PropertyName = "No Participants")]
          public string eventTypeNoParticipantsString { get; set; } = "No one participated in the <color=purple>{event_name}</color> event";
          [JsonProperty(PropertyName = "Multiple Winners Notification Delay (seconds)")]
          public float eventTypeMultipleWinnersDelay { get; set; } = 4.0f;
        }
        [JsonProperty(PropertyName = "Event Reward(s)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<WinnerReward> Rewards { get; set; } = new List<WinnerReward>
        {
          new WinnerReward { }
        };
        public class WinnerReward
        {
          [JsonProperty(PropertyName = "Enable Reward")]
          public bool enableWinnerReward { get; set; } = true;
          [JsonProperty(PropertyName = "Reward Probability %")]
          public int winnerRewardProbability { get; set; } = 100;
          [JsonProperty(PropertyName = "Reward Notification")]
          public RewardNotificationSettings RewardNotification { get; set; } = new RewardNotificationSettings();
          public class RewardNotificationSettings
          {
            [JsonProperty(PropertyName = "Enable Reward Notification")]
            public bool enableWinnerRewardNotification { get; set; } = true;
            [JsonProperty(PropertyName = "Only Send Reward Notification To Winning Player?")]
            public bool enableWinnerRewardNotificationToPlayer { get; set; } = false;
            [JsonProperty(PropertyName = "Reward Notification")]
            public string winnerRewardNotification { get; set; } = "<color=purple>{player_name}</color> scored <color=purple>first place</color> in the <color=purple>{event_name}</color> event with <color=purple>{points_scored} points</color> and won <color=purple>{rewards_list}</color>!";
            [JsonProperty(PropertyName = "Separate {rewards_list} With Commas?")]
            public bool enableWinnerRewardsCSV { get; set; } = true;
          }
          [JsonProperty(PropertyName = "Item(s)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
          public List<RewardItem> RewardItems { get; set; } = new List<RewardItem>
          {
            new RewardItem { }
          };
          public class RewardItem {
            [JsonProperty(PropertyName = "Enable Item")]
            public bool enableWinnerRewardItem { get; set; } = true;
            [JsonProperty(PropertyName = "Item Probability %")]
            public int winnerRewardItemProbability { get; set; } = 100;
            [JsonProperty(PropertyName = "Item Display Name")]
            public string winnerRewardItemDisplayName { get; set; } = "Scrap";
            [JsonProperty(PropertyName = "Item Shortname")]
            public string winnerRewardItem { get; set; } = "scrap";
            [JsonProperty(PropertyName = "Item Skin ID")]
            public ulong winnerRewardItemCustomSkin { get; set; } = 0;
            [JsonProperty(PropertyName = "Item Amount")]
            public int winnerRewardItemAmount { get; set; } = 100;
          }
          [JsonProperty(PropertyName = "Command(s)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
          public List<RewardCommand> RewardCommands { get; set; } = new List<RewardCommand>
          {
            new RewardCommand { }
          };
          public class RewardCommand {
            [JsonProperty(PropertyName = "Enable Command")]
            public bool enableWinnerRewardCommand { get; set; } = false;
            [JsonProperty(PropertyName = "Command Probability %")]
            public int winnerRewardCommandProbability { get; set; } = 100;
            [JsonProperty(PropertyName = "Command Display Name")]
            public string winnerRewardCommandDisplayName { get; set; } = "VIP Role";
            [JsonProperty(PropertyName = "Command")]
            public string winnerRewardCommand { get; set; } = "oxide.usergroup add {player.id} vip";
          }
          [JsonProperty(PropertyName = "Kit(s) (plugin required)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
          public List<RewardKit> RewardKits { get; set; } = new List<RewardKit>
          {
            new RewardKit { }
          };
          public class RewardKit
          {
            [JsonProperty(PropertyName = "Enable Kit")]
            public bool enableWinnerRewardKit { get; set; } = false;
            [JsonProperty(PropertyName = "Kit Probability %")]
            public int winnerRewardKitProbability { get; set; } = 100;
            [JsonProperty(PropertyName = "Kit Display Name")]
            public string winnerRewardKitDisplayName { get; set; } = "PVP Kit";
            [JsonProperty(PropertyName = "Kit")]
            public string winnerRewardKit { get; set; } = "pvpkit";
          }
        }
      }
      [JsonProperty(PropertyName = "Additional Event Types")]
      public Dictionary<string, AdditionalEventType> AdditionalEventTypes { get; set; } = new Dictionary<string, AdditionalEventType>
      {
        { "ResourceRun", new AdditionalEventType { additionalEventTypeName = "ResourceRun", additionalEventTypeDesc = "Collect resources at 2x the normal rate!", additionalEventTypePermission = "extraevents.resourcerun" } }
      };
      public class AdditionalEventType {
        [JsonProperty(PropertyName = "Enable Event")]
        public bool additionalEventTypeEnabled { get; set; } = true;
        [JsonProperty(PropertyName = "Event Name")]
        public string additionalEventTypeName { get; set; }
        [JsonProperty(PropertyName = "Event Description")]
        public string additionalEventTypeDesc { get; set; }
        [JsonProperty(PropertyName = "Event Length (seconds)")]
        public int additionalEventTypeLength { get; set; } = 600;
        [JsonProperty(PropertyName = "Event Multiplier")]
        public float additionalEventTypeMultiplier { get; set; } = 2.0f;
        [JsonProperty(PropertyName = "Enable Event Permission")]
        public bool additionalEventTypePermissionEnabled { get; set; } = false;
        [JsonProperty(PropertyName = "Event Permission")]
        public string additionalEventTypePermission { get; set; }
        [JsonProperty(PropertyName = "Show UI And Notifications To Players Without Event Permission?")]
        public bool additionalEventTypeShowUIAndNotificationsWithoutPermission { get; set; } = false;
        [JsonProperty(PropertyName = "Event Image(s)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<AdditionalEventTypeImage> AdditionalEventTypeImages { get; set; } = new List<AdditionalEventTypeImage>
        {
          new AdditionalEventTypeImage {}
        };
        public class AdditionalEventTypeImage
        {
          [JsonProperty(PropertyName = "Enable Image")]
          public bool additionalEventTypeEnableImage { get; set; } = true;
          [JsonProperty(PropertyName = "Image URL")]
          public string additionalEventTypeImageURL { get; set; } = "https://avatars.cloudflare.steamstatic.com/9df6fd69fc80ebe9387bb7a58ff4ee01d092af11_full.jpg";
          [JsonProperty(PropertyName = "Image Transparency (0.0 - 100.0)")]
          public float additionalEventTypeImageTransparency { get; set; } = 75.0f;
          [JsonProperty(PropertyName = "Image Anchors Min (x y)")]
          public string additionalEventTypeImageMinAnchors { get; set; } = "0.8 0.2";
          [JsonProperty(PropertyName = "Image Anchors Max (x y)")]
          public string additionalEventTypeImageMaxAnchors { get; set; } = "0.83 0.245";
        }
        [JsonProperty(PropertyName = "Event UI")]
        public AdditionalEventTypeUISettings AdditionalEventTypeUIs { get; set; } = new AdditionalEventTypeUISettings();
        public class AdditionalEventTypeUISettings
        {
          [JsonProperty(PropertyName = "Enable UI")]
          public bool additionalEventTypeEnableUI { get; set; } = true;
          [JsonProperty(PropertyName = "UI Anchors Min (x y)")]
          public string additionalEventTypeUIMinAnchors { get; set; } = "0.695 0.025";
          [JsonProperty(PropertyName = "UI Anchors Max (x y)")]
          public string additionalEventTypeUIMaxAnchors { get; set; } = "0.83 0.1975";
          [JsonProperty(PropertyName = "UI Background Color (r g b a)")]
          public string additionalEventTypeUIBackgroundColor { get; set; } = "255 255 255 0.2";
          [JsonProperty(PropertyName = "UI Text Color (r g b a)")]
          public string additionalEventTypeUITextColor { get; set; } = "255 255 255 1.0";
          [JsonProperty(PropertyName = "UI Text Outline Color (r g b a)")]
          public string additionalEventTypeUITextOutlineColor { get; set; } = "0 0 0 0.25";
          [JsonProperty(PropertyName = "UI Text Alignment (left, right, center)")]
          public string additionalEventTypeUITextAlignment { get; set; } = "center";
        }
        [JsonProperty(PropertyName = "Event Notifications")]
        public AdditionalEventTypeNotificationsSettings AdditionalEventTypeNotifications { get; set; } = new AdditionalEventTypeNotificationsSettings();
        public class AdditionalEventTypeNotificationsSettings
        {
          [JsonProperty(PropertyName = "Enable Chat Notifications")]
          public bool additionalEventTypeEnableChatNotifications { get; set; } = true;
          [JsonProperty(PropertyName = "Event Chat Prefix")]
          public string additionalEventTypeChatPrefix { get; set; } = "";
          [JsonProperty(PropertyName = "Event Chat Icon (Steam64 ID)")]
          public ulong additionalEventTypeChatIconID { get; set; } = 0;
          [JsonProperty(PropertyName = "Enable GameTip Notifications")]
          public bool additionalEventTypeEnableGameTipNotifications { get; set; } = false;
          [JsonProperty(PropertyName = "GameTip Style (info OR alert)")]
          public string additionalEventTypeGameTipStyle { get; set; } = "info";
          [JsonProperty(PropertyName = "GameTip Duration (seconds)")]
          public float additionalEventTypeGameTipDuration { get; set; } = 3.0f;
          [JsonProperty(PropertyName = "Enable Event Upcoming Notification")]
          public bool additionalEventTypeEnableUpcomingNotification { get; set; } = false;
          [JsonProperty(PropertyName = "Event Upcoming Delay (seconds) (time before event starts after Event Upcoming Notification)")]
          public int additionalEventTypeUpcomingDelay { get; set; } = 30;
          [JsonProperty(PropertyName = "Event Upcoming")]
          public string additionalEventTypeUpcomingString { get; set; } = "The <color=purple>{event_name}</color> event will start in {upcoming_announcement_delay} seconds! <color=purple>{event_description}</color>";
          [JsonProperty(PropertyName = "Event Starting")]
          public string additionalEventTypeStartingString { get; set; } = "The <color=purple>{event_name}</color> event has started! <color=purple>{event_description}</color>";
          [JsonProperty(PropertyName = "Event Ending")]
          public string additionalEventTypeEndingString { get; set; } = "The <color=purple>{event_name}</color> event has ended.";
        }
      }
      public Oxide.Core.VersionNumber Version { get; set; }
    }
    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
          _configData = Config.ReadObject<ConfigData>();
          if (_configData.Version < Version)
          {
            debugMessage = $"Config version {_configData.Version} out of date. Updating...";
            PrintDebug(debugMessage);
            if (_configData.Version < new Core.VersionNumber(1, 8, 0))
            {
              ConfigData.EventType eventType;
              if (_configData.EventTypes.TryGetValue("TunnelTussle", out eventType))
              {
                eventType.eventTypeEnabled = false;
              }
              if (_configData.EventTypes.TryGetValue("RoadsignRun", out eventType))
              {
                eventType.eventTypeEnabled = false;
              }
              if (_configData.EventTypes.TryGetValue("UnderwaterWar", out eventType))
              {
                eventType.eventTypeEnabled = false;
              }
              if (_configData.EventTypes.TryGetValue("PlayerBattle", out eventType))
              {
                eventType.eventTypeEnabled = false;
              }
            }
            if (_configData.Version < new Core.VersionNumber(1, 5, 0))
            {
              if (_configData.EventTypes.Count > 0)
              {
                foreach (KeyValuePair<string, ConfigData.EventType> listItem in _configData.EventTypes)
                {
                  var eventName = listItem.Key.ToLower();
                  listItem.Value.eventTypePermission = "extraevents." + eventName;
                }
              }
              if (_configData.AdditionalEventTypes.Count > 0)
              {
                foreach (KeyValuePair<string, ConfigData.AdditionalEventType> listItem in _configData.AdditionalEventTypes)
                {
                  var eventName = listItem.Key.ToLower();
                  listItem.Value.additionalEventTypePermission = "extraevents." + eventName;
                }
              }
            }
            if (_configData.Version < new Core.VersionNumber(1, 0, 0))
              LoadDefaultConfig();
            _configData.Version = Version;
            debugMessage = $"Config update to {Version} complete.";
            PrintDebug(debugMessage);
          }
        }
        catch
        {
          debugMessage = "Config error, replacing with default.";
          PrintDebug(debugMessage);
          LoadDefaultConfig();
        }
        SaveConfig();
    }
    protected override void LoadDefaultConfig() => _configData = new ConfigData();
    protected override void SaveConfig() => Config.WriteObject(_configData);
    #endregion
  }
}