namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.IO;
  using Facepunch;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using UnityEngine;
  using UnityEngine.AI;
  using UnityEngine.SceneManagement;

  [Info("ZombieAttack", "chucklenugget", "0.1.0")]
  public partial class ZombieAttack : RustPlugin
  {
    const string ZOMBIE_PREFAB = "assets/prefabs/npc/murderer/murderer.prefab";
    const string PERM_ADMIN = "zombieattack.admin";

    const int MAX_ATTACKERS_ALLOWED = 20;
    const float MAX_SPAWN_RADIUS = 200f;

    static string[] WeaponItems = new string[] {
      "mace",
      "salvaged.sword",
      "longsword",
      "hatchet",
      "machete",
      "bone.club",
      "knife.bone",
      "pistol.eoka",
      "pistol.nailgun"
    };

    DynamicConfigFile SettingsFile;
    ZombieAttackSettings Settings;
    Timer AttackTimer;

    void Loaded()
    {
      SettingsFile = Interface.Oxide.DataFileSystem.GetFile(Name + Path.DirectorySeparatorChar + "settings");
      permission.RegisterPermission(PERM_ADMIN, this);

      try
      {
        Settings = SettingsFile.ReadObject<ZombieAttackSettings>();
      }
      catch (Exception ex)
      {
        PrintError($"Error while loading configuration: {ex.ToString()}");
        Settings = new ZombieAttackSettings();
      }

      StartTimer();
    }

    void Unload()
    {
      if (AttackTimer != null)
        AttackTimer.Destroy();
    }

    void OnServerSave()
    {
      SettingsFile.WriteObject(Settings);
    }

    void StartTimer()
    {
      if (AttackTimer != null)
        AttackTimer.Destroy();

      AttackTimer = timer.Every(Settings.IntervalMinutes * 60, BeginRandomAttack);
    }

    void BeginRandomAttack()
    {
      if (Settings.Locations.Count == 0)
      {
        Puts($"Can't send periodic attack, because no attack locations have been defined!");
        return;
      }

      BeginAttack(GetRandomAttackLocation());
    }

    void BeginAttack(ZombieAttackLocation location)
    {
      int attackers = UnityEngine.Random.Range(Settings.MinAttackers, Settings.MaxAttackers);

      Puts($"Sending {attackers} attackers to {location}");

      for (int idx = 0; idx < attackers; idx++)
      {
        Vector3 spawnPosition = GetRandomPositionNear(location.Position, 50f);

        BaseCombatEntity entity = SpawnAttacker(ZOMBIE_PREFAB, spawnPosition);
        entity.enableSaving = false;
        entity.Spawn();
        entity.InitializeHealth(entity.StartHealth(), entity.StartMaxHealth());

        ItemDefinition item = ItemManager.FindItemDefinition(WeaponItems.GetRandom());

        var npc = entity.gameObject.GetComponent<NPCPlayer>();
        npc.inventory.containerBelt.Clear();
        npc.inventory.containerBelt.AddItem(item, 1);
        npc.EquipTest();
        npc.SetDestination(location.Position);

        Puts($"Spawned attacker at {spawnPosition} equipped with {item.displayName}");
      }

      PrintToChat("<color=#ff0000>THE ATTACK HAS BEGUN!</color>");
    }

    BaseCombatEntity SpawnAttacker(string prefab, Vector3 position)
    {
      GameObject gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(prefab), position, new Quaternion());
      gameObject.name = prefab;

      SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
      UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

      if (!gameObject.activeSelf)
        gameObject.SetActive(true);

      return gameObject.GetComponent<BaseCombatEntity>();
    }

    ZombieAttackLocation GetRandomAttackLocation()
    {
      if (Settings.Locations.Count == 0)
        return null;

      return Settings.Locations[Settings.Locations.Keys.ToArray().GetRandom()];
    }

    bool TryFindPointOnNavMesh(Vector3 nearPosition, out Vector3 position)
    {
      NavMeshHit hit;
      if (NavMesh.SamplePosition(nearPosition, out hit, 50f, 1))
      {
        position = hit.position;
        return true;
      }

      position = Vector3.zero;
      return false;
    }

    Vector3 GetRandomPositionNear(Vector3 nearPosition, float radius)
    {
      float x = UnityEngine.Random.insideUnitCircle.x * radius;
      float z = UnityEngine.Random.insideUnitCircle.x * radius;
      return nearPosition + new Vector3(x, 0, z);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using UnityEngine;

  public partial class ZombieAttack : RustPlugin
  {
    class ZombieAttackLocation
    {
      [JsonProperty("name")]
      public string Name;

      [JsonProperty("position"), JsonConverter(typeof(UnityVector3Converter))]
      public Vector3 Position;

      [JsonProperty("radius")]
      public float Radius;

      public ZombieAttackLocation()
      {
      }

      public ZombieAttackLocation(string name, Vector3 position, float radius)
      {
        Name = name;
        Position = position;
        Radius = radius;
      }

      public override string ToString()
      {
        return $"{Name} {Position.ToString()}";
      }
    }

    class UnityVector3Converter : JsonConverter
    {
      public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
      {
        var vector = (Vector3) value;
        writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
      }

      public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
      {
        string[] tokens = reader.Value.ToString().Trim().Split(' ');
        float x = Convert.ToSingle(tokens[0]);
        float y = Convert.ToSingle(tokens[1]);
        float z = Convert.ToSingle(tokens[2]);
        return new Vector3(x, y, z);
      }

      public override bool CanConvert(Type objectType)
      {
        return objectType == typeof(Vector3);
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public partial class ZombieAttack : RustPlugin
  {
    class ZombieAttackSettings
    {
      [JsonProperty("intervalMinutes")]
      public float IntervalMinutes = 30f;

      [JsonProperty("minAttackers")]
      public int MinAttackers = 5;

      [JsonProperty("maxAttackers")]
      public int MaxAttackers = 10;

      [JsonProperty("locations")]
      public Dictionary<string, ZombieAttackLocation> Locations = new Dictionary<string, ZombieAttackLocation>();
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;
  using UnityEngine;

  public partial class ZombieAttack : RustPlugin
  {
    void OnAddCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 2)
      {
        SendReply(player, "Usage: <color=#ffd479>/za add NAME RADIUS</color>");
        return;
      }

      Vector3 position;
      if (!TryFindPointOnNavMesh(player.transform.position, out position))
      {
        SendReply(player, $"Couldn't find a position on the navmesh near your location. Please try again.");
        return;
      }

      string name = args[0].ToLowerInvariant();
      float radius;

      try
      {
        radius = Convert.ToSingle(args[1]);
      }
      catch
      {
        SendReply(player, $"Radius must be a valid number between 0 and {MAX_SPAWN_RADIUS}");
        return;
      }

      var location = new ZombieAttackLocation(name, position, radius);
      Settings.Locations[name] = location;

      SendReply(player, $"Added <color=#ffd479>{location}</color> as an attack location with a spawn radius of <color=#ffd479>{location.Radius}</color>.");
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnCallCommand(BasePlayer player, string[] args)
    {
      if (args.Length > 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za call [NAME]</color>");
        return;
      }

      if (Settings.Locations.Count == 0)
      {
        SendReply(player, "Can't send an attack, because no locations have been defined!");
        return;
      }

      ZombieAttackLocation location;

      if (args.Length == 0)
      {
        location = GetRandomAttackLocation();
      }
      else
      {
        string name = args[0].ToLowerInvariant();
        if (!Settings.Locations.TryGetValue(name, out location))
        {
          SendReply(player, $"No attack location named <color=#ffd479>{name}</color> has been defined.");
          return;
        }
      }

      SendReply(player, $"Sending attack to <color=#ffd479>{location}</color>!");
      BeginAttack(location);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnClearCommand(BasePlayer player)
    {
      Settings.Locations.Clear();
      SendReply(player, "All attack locations have been removed.");
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class ZombieAttack : RustPlugin
  {
    [ChatCommand("za")]
    void OnCommand(BasePlayer player, string command, string[] args)
    {
      if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
      {
        SendReply(player, "You don't have permission to do that.");
        return;
      }

      if (args.Length == 0)
      {
        OnHelpCommand(player);
        return;
      }

      var restArgs = args.Skip(1).ToArray();

      switch (args[0].ToLowerInvariant())
      {
        case "add":
          OnAddCommand(player, restArgs);
          return;
        case "remove":
          OnRemoveCommand(player, restArgs);
          return;
        case "list":
          OnListCommand(player);
          return;
        case "clear":
          OnClearCommand(player);
          return;
        case "interval":
          OnIntervalCommand(player, restArgs);
          return;
        case "number":
          OnNumberCommand(player, restArgs);
          return;
        case "call":
          OnCallCommand(player, restArgs);
          return;
        default:
          OnHelpCommand(player);
          return;
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class ZombieAttack : RustPlugin
  {
    void OnHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("<color=#ffd479>/za interval MINUTES</color> Set the interval for periodic attacks");
      sb.AppendLine("<color=#ffd479>/za list</color> List the current attack locations");
      sb.AppendLine("<color=#ffd479>/za add NAME</color> Add your current position as an attack location");
      sb.AppendLine("<color=#ffd479>/za remove NAME</color> Remove an attack location from the list");
      sb.AppendLine("<color=#ffd479>/za clear</color> Remove all attack locations");
      sb.AppendLine("<color=#ffd479>/za call [NAME]</color> Call an attack immediately");
      sb.AppendLine("<color=#ffd479>/za help</color> Show this message");

      SendReply(player, sb.ToString());
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;

  public partial class ZombieAttack : RustPlugin
  {
    void OnIntervalCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za interval MINUTES</color>");
        return;
      }

      int intervalMinutes;

      try
      {
        intervalMinutes = Convert.ToInt32(args[0]);
      }
      catch
      {
        SendReply(player, "You must specify a valid number of minutes.");
        return;
      }

      if (intervalMinutes < 1)
      {
        SendReply(player, "The interval must be greater than 0.");
        return;
      }

      SendReply(player, $"Attacks will now occur every <color=#ffd479>{intervalMinutes}</color> minutes.");

      Settings.IntervalMinutes = intervalMinutes;
      StartTimer();
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class ZombieAttack : RustPlugin
  {
    void OnListCommand(BasePlayer player)
    {
      if (Settings.Locations.Count == 0)
      {
        SendReply(player, "No attack locations have been defined.");
        return;
      }

      var sb = new StringBuilder();

      sb.AppendLine("The following attack locations have been defined:");
      foreach (ZombieAttackLocation location in Settings.Locations.Values)
        sb.AppendLine($"<color=#ffd479>{location.Name}:</color> {location.Position}");

      SendReply(player, sb.ToString());
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;

  public partial class ZombieAttack : RustPlugin
  {
    void OnNumberCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 2)
      {
        SendReply(player, "Usage: <color=#ffd479>/za number MIN MAX</color>");
        return;
      }

      int min;
      int max;

      try
      {
        min = Convert.ToInt32(args[0]);
        max = Convert.ToInt32(args[1]);
      }
      catch
      {
        SendReply(player, "You must specify valid numbers for both minimum and maximum.");
        return;
      }

      if (min < 1 || max < 1 || min > MAX_ATTACKERS_ALLOWED || max > MAX_ATTACKERS_ALLOWED)
      {
        SendReply(player, $"Both minimum and maximum must be between 1 and {MAX_ATTACKERS_ALLOWED}.");
        return;
      }

      Settings.MinAttackers = min;
      Settings.MaxAttackers = max;

      SendReply(player, $"Attacks will now involve between <color=#ffd479>{min}</color> and <color=#ffd479>{max}</color> attackers.");
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnRemoveCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za remove NAME</color>");
        return;
      }

      string name = args[0].ToLowerInvariant();

      ZombieAttackLocation location;
      if (!Settings.Locations.TryGetValue(name, out location))
      {
        SendReply(player, $"No location named <color=#ffd479>{name}</color> has been defined.");
        return;
      }

      Settings.Locations.Remove(name);
      SendReply(player, $"Removed {location} as an attack location.");
    }
  }
}