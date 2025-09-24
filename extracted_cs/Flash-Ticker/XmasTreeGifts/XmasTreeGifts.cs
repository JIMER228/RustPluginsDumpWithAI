using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("XmasTreeGifts", "RustFlash", "1.0.0")]
    [Description("Spawns Christmas presents under Christmas trees when they are placed")]
    class XmasTreeGifts : RustPlugin
    {
        private const string TreePrefabName = "xmas_tree.deployed";
        private const string PresentPrefabPath = "assets/prefabs/misc/xmas/giftbox/giftbox_loot.prefab";
        
        private HashSet<NetworkableId> processedTrees = new HashSet<NetworkableId>();
        
        private const string DataFilename = "XmasTreeGifts_Data";

        void Loaded()
        {
            LoadData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            SaveData();
        }

        private void LoadData()
        {
            var data = Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>(DataFilename);
            processedTrees = new HashSet<NetworkableId>();
            
            if (data != null)
            {
                foreach (var id in data)
                {
                    processedTrees.Add(new NetworkableId(id));
                }
            }
        }

        private void SaveData()
        {
            var saveData = new List<ulong>();
            foreach (var id in processedTrees)
            {
                saveData.Add(id.Value);
            }
            Interface.Oxide.DataFileSystem.WriteObject(DataFilename, saveData);
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity?.ShortPrefabName != TreePrefabName) return;

            var tree = entity as BaseEntity;
            if (tree == null) return;

            if (processedTrees.Contains(tree.net.ID))
            {
                Puts($"Tree {tree.net.ID} already processed, skipping gift spawning");
                return;
            }

            SpawnPresentsUnderTree(tree);
            processedTrees.Add(tree.net.ID);
            SaveData();
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity?.ShortPrefabName == TreePrefabName)
            {
                processedTrees.Remove(entity.net.ID);
                SaveData();
            }
        }

        private void SpawnPresentsUnderTree(BaseEntity tree)
        {
            int presentCount = UnityEngine.Random.Range(2, 5);
            
            for (int i = 0; i < presentCount; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f);
                float radius = UnityEngine.Random.Range(0.5f, 1.5f);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );

                Vector3 spawnPos = tree.transform.position + offset;
                
                BaseEntity present = GameManager.server.CreateEntity(PresentPrefabPath, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
                if (present != null)
                {
                    present.Spawn();
                    Puts($"Spawned present at {spawnPos} for tree {tree.net.ID}");
                }
            }
        }

        [ChatCommand("listtrees")]
        private void ListTreesCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            
            player.ChatMessage($"Processed trees count: {processedTrees.Count}");
            foreach (var treeId in processedTrees)
            {
                player.ChatMessage($"Tree ID: {treeId}");
            }
        }
    }
}