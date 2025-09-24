/*
 * Setup Instructions:
 *      https://oxidemod.org/threads/setting-up-a-c-workspace-in-visual-studio-2015.10010/
 * Oxide API Docs:
 *      https://umod.org/documentation/api/overview
 * Rust API Docs:
 *      https://umod.org/documentation/games/rust
*/

using System;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("System Notifications", "thenetimp", "0.0.1")]
    [Description("Notification messages to chat console to let players know when things arrive.")]
    internal class SystemNotifications : RustPlugin
    {
      private const string PERM = "systemnotifications.use";

    #region Initialization
    // Initialize the plugin, registering permissions
    void Init()
      {
          permission.RegisterPermission(PERM, this);
          InitConfig();
      }

      // When the server is initialized do nothing
      private void OnServerInitialized()
      {
      }
      #endregion

      #region Configuration
      SystemNotificationsConfig config;

      public class PlayerNotifications
      {
          public bool enabled { get; set; }
          public string message_online { get; set; }
          public string message_offline { get; set; }
          public string message_color { get; set; }
      }

      public class CargoPlaneDeployed
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class SupplyDropDeployed
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class TimedCrateDeployed
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class CargoShipEntered
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class Ch47Entered
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class PatrolCopterEntered
      {
          public bool enabled { get; set; }
          public string message { get; set; }
          public string message_color { get; set; }
      }

      public class ServerInfo
      {
          public bool wipe_info_enabled { get; set; }
          public string wipe_info_message { get; set; }
          public string wipe_info_message_color { get; set; }
      }

      public class SystemNotificationsConfig
      {
          public PlayerNotifications player_notifications = new PlayerNotifications();
          public CargoPlaneDeployed cargo_plane_deployed = new CargoPlaneDeployed();
          public SupplyDropDeployed supply_drop_deployed = new SupplyDropDeployed();
          public TimedCrateDeployed timed_crate_deployed = new TimedCrateDeployed();
          public CargoShipEntered cargo_ship_entered = new CargoShipEntered();
          public Ch47Entered ch47_entered = new Ch47Entered();
          public PatrolCopterEntered patrol_copter_entered = new PatrolCopterEntered();
          public ServerInfo server_info = new ServerInfo();
      }

      protected override void LoadDefaultConfig()
      {
          SystemNotificationsConfig _config = new SystemNotificationsConfig();

          _config.player_notifications.enabled = true;
          _config.player_notifications.message_online = "An opponent entered the server.";
          _config.player_notifications.message_offline = "An opponent has retreated from the server.";
          _config.player_notifications.message_color = "#FFF";
          _config.cargo_plane_deployed.enabled = true;
          _config.cargo_plane_deployed.message = "Supply plane incoming...";
          _config.cargo_plane_deployed.message_color = "#0C0";
          _config.supply_drop_deployed.enabled = true;
          _config.supply_drop_deployed.message = "Supply cargo dropped! Look up!";
          _config.supply_drop_deployed.message_color = "#0C0";
          _config.timed_crate_deployed.enabled = false;
          _config.timed_crate_deployed.message = "A hackable crate has be put on the map.";
          _config.timed_crate_deployed.message_color = "#0C0";
          _config.cargo_ship_entered.enabled = true;
          _config.cargo_ship_entered.message = "Cargoship incoming...";
          _config.cargo_ship_entered.message_color = "#0C0";
          _config.ch47_entered.enabled = false;
          _config.ch47_entered.message = "Chinook 47 incoming...";
          _config.ch47_entered.message_color = "#0C0";
          _config.patrol_copter_entered.enabled = true;
          _config.patrol_copter_entered.message = "Patrol Helecopter! Run for cover, or shoot it down!";
          _config.patrol_copter_entered.message_color = "#0C0";
          _config.server_info.wipe_info_enabled = false;
          _config.server_info.wipe_info_message = "Jan 1, 2019 UTC";
          _config.server_info.wipe_info_message_color = "#FFF";

          SaveConfig(_config);
      }

      void SaveConfig(SystemNotificationsConfig config)
      {
          Config.WriteObject(config, true);
      }

      void InitConfig()
      {
          config = Config.ReadObject<SystemNotificationsConfig>();
      }
      #endregion

      #region Spawn Messages
      void OnEntitySpawned(BaseEntity entity)
      {
          bool isCargoPlane = entity is CargoPlane;
          bool isSupplyDrop = entity is SupplyDrop;
          bool isHackableCrate = entity is HackableLockedCrate;
          bool isCh47Helicopter = entity is CH47Helicopter;
          bool isHelicopter = entity is BaseHelicopter;
          bool isCargoShip = entity is CargoShip;

          if (!isCargoPlane && !isSupplyDrop && !isHackableCrate && !isCh47Helicopter && !isHelicopter && !isCargoShip)
              return;

          if (isCargoPlane)       AnnounceCargoPlaneDeployed(); //
          if (isCargoShip)        AnnounceCargoShipEntered(); //
          if (isSupplyDrop)       AnnounceSupplyDropDeployedDrop(); //
          if (isHackableCrate)    AnnounceCrateDeployedDrop(); //
          if (isCh47Helicopter)   AnnounceCh47Helicopter();
          if (isHelicopter)       AnnounceHelicopter(); //

      }
      
      void AnnounceCargoShipEntered()
      {
        if(config.cargo_ship_entered.enabled)
        {
          Announce(config.cargo_ship_entered.message, config.cargo_ship_entered.message_color, 14);
        }
      }

      void AnnounceCargoPlaneDeployed()
      {
        if(config.cargo_plane_deployed.enabled)
        {
          Announce(config.cargo_plane_deployed.message, config.cargo_plane_deployed.message_color, 14);
        }
      }

      void AnnounceSupplyDropDeployedDrop()
      {
        if(config.supply_drop_deployed.enabled)
        {
          Announce(config.supply_drop_deployed.message, config.supply_drop_deployed.message_color, 14);
        }
      }

      void AnnounceCrateDeployedDrop()
      {
        if(config.timed_crate_deployed.enabled)
        {
          Announce(config.timed_crate_deployed.message, config.timed_crate_deployed.message_color, 14);
        }
      }

      void AnnounceCh47Helicopter()
      {
        if(config.ch47_entered.enabled)
        {
          Announce(config.ch47_entered.message, config.ch47_entered.message_color, 14);
        }
      }

      void AnnounceHelicopter()
      {
        if(config.patrol_copter_entered.enabled)
        {
          Announce(config.patrol_copter_entered.message, config.patrol_copter_entered.message_color, 14);
        }
      }

      #endregion

      #region Player Messages
      void OnPlayerConnected(Network.Message packet)
      {
        if(config.player_notifications.enabled) {
          Announce(config.player_notifications.message_online, config.player_notifications.message_color, 14);
        }

        // Post wipe info when a player connects
        if(config.server_info.wipe_info_enabled) {
          Announce(config.server_info.wipe_info_message, config.server_info.wipe_info_message_color, 14);
        }
      }
      void OnPlayerDisconnected(BasePlayer player, string reason)
      {
        if(config.player_notifications.enabled) {
          Announce(config.player_notifications.message_offline, config.player_notifications.message_color, 14);
        }
      }
      #endregion

      #region Announcement
      void Announce(string message, string color, int size)
      {
          ulong icon = 0;
          string dropMsg = $"<size={size}><color={color}>{message}</color></size>";
          Server.Broadcast(dropMsg, null, icon);
      }
      #endregion
    }
}