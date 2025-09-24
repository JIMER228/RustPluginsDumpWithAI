using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SimpleSpawn", "YourName", "1.6.0")]
    [Description("Базовая система кастомного спавна")]
    public class SimpleSpawn : RustPlugin
    {
        private Vector3? _spawn;
        private const string PERM = "simplespawn.admin";

        void Init()
        {
            permission.RegisterPermission(PERM, this);
            try { _spawn = Interface.Oxide.DataFileSystem.ReadObject<SpawnData>(Name)?.Position; }
            catch { }
        }

        void OnPlayerInit(BasePlayer p) => Spawn(p);
        void OnPlayerRespawned(BasePlayer p) => Spawn(p);

        [ChatCommand("spawn")]
        void CmdSpawn(BasePlayer p, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(p.UserIDString, PERM))
            {
                p.ChatMessage("Нет прав!");
                return;
            }

            if (args.Length == 0)
            {
                p.ChatMessage(_spawn.HasValue 
                    ? $"Точка спавна: {_spawn.Value}" 
                    : "Точка спавна не установлена");
                return;
            }

            switch (args[0].ToLower())
            {
                case "set":
                    _spawn = p.transform.position;
                    SaveData();
                    p.ChatMessage("Точка спавна установлена!");
                    break;
                case "none":
                    _spawn = null;
                    SaveData();
                    p.ChatMessage("Точка спавна сброшена");
                    break;
            }
        }

        void Spawn(BasePlayer p)
        {
            if (_spawn.HasValue && p.IsConnected)
                p.transform.position = _spawn.Value;
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, new SpawnData { Position = _spawn ?? Vector3.zero });

        class SpawnData { public Vector3 Position { get; set; } }
    }
}