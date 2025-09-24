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

        Puts($"Spawned attacker at {spawnPosition} equipped with {item.name}");
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
}