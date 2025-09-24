// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;

// Possible Future Features:
// - Pilot eject when the heli gets destroyed (monitoring OnEntityTakeDamage maybe?)
// - Add option for minimum parachute height (or alternativley, if the fall distance is bellow x amount, spawn the player higher up in the air)
// - Option to spawn heli with no crates if the pilot has an inventory


namespace Oxide.Plugins
{
    [Info("PilotEject", "S1m0n", "1.0.8")]
    public class PilotEject : RustPlugin
    {
        private DynamicConfigFile InventoryData;
        StoredData storedData;

        class StoredData
        {
            public List<ItemInfo> mainContiner = new List<ItemInfo>();
            public List<ItemInfo> beltContainer = new List<ItemInfo>();
            public List<ItemInfo> clothesContainer = new List<ItemInfo>();
        }

        List<ItemInfo> mainContiner = new List<ItemInfo>();
        List<ItemInfo> beltContainer = new List<ItemInfo>();
        List<ItemInfo> clothesContainer = new List<ItemInfo>();

        class ItemInfo
        {
            public string shortname;
            public int amount;
            public float chance;
            public ulong skinID;
        }

        bool Changed = false;

        float minTimeToEvent = 300.0f;
        float maxTimeToEvent = 600.0f;
        float chanceOfOccuring = 0.20f;
        float pilotLifeLength = 600f;
        float minimumSpawnHeight = 20f;
        bool minimumSpawnHeightEnabled = false;
        float time;
        bool killMSG = true;

        Timer repeat;
        Timer rp;
        const string heliprefab = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        const string permissionName = "piloteject.admin";

        public List<BasePlayer> pilots = new List<BasePlayer>();

        void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //chat
                ["Heli Malfunctioned"] = "Вертолет вышел из строя! Пилоту пришлось спрыгнуть с вертолёта с парашютом. Иди, найди его и укради его добычу",
                ["No Permission"] = "У вас нет прав, чтобы использовать эту команду!",
                ["Pilot Killed"] = "<color=#ffd479>{0}</color> убил пилота",
                ["Pilot Inventory Set"] = "Инвентарь пилотов успешно сохранен!",
                ["Malfunction Timer Warning (Console)"] = "Вертолет выйдет из строя через <color=#ffd479>{0} сек.</color>",

            }, this);

            InventoryData = Interface.Oxide.DataFileSystem.GetFile("PilotEject");
        }

        void OnServerInitialized()
        {
            LoadData();
            permission.RegisterPermission(permissionName, this);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            SaveData();
        }

        void SaveData()
        {
            storedData.mainContiner = mainContiner;
            storedData.beltContainer = beltContainer;
            storedData.clothesContainer = clothesContainer;
            InventoryData.WriteObject(storedData);
        }

        void LoadData()
        {
            try
            {
                storedData = InventoryData.ReadObject<StoredData>();
                mainContiner = storedData.mainContiner;
                beltContainer = storedData.beltContainer;
                clothesContainer = storedData.clothesContainer;
            }
            catch
            {
                Puts("Failed to load data, creating new file");
                storedData = new StoredData();
            }
        }

        void LoadVariables()
        {
            minTimeToEvent = Convert.ToSingle(GetConfig("Settings", "Min Time Until Event", 300.0f));
            maxTimeToEvent = Convert.ToSingle(GetConfig("Settings", "Max Time Until Event", 600.0f));
            chanceOfOccuring = Convert.ToSingle(GetConfig("Settings", "Chance Of Occuring", 0.20f));
            killMSG = Convert.ToBoolean(GetConfig("Settings", "Pilot Death Message Enabled", true));
            pilotLifeLength = Convert.ToSingle(GetConfig("Settings", "Pilot Life Length", 600f));
            minimumSpawnHeight = Convert.ToSingle(GetConfig("Settings", "Pilot Minimum Spawn Height", 20f));
            minimumSpawnHeightEnabled = Convert.ToBoolean(GetConfig("Settings", "Min Spawn Height Enabled", false));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        void Init()
        {
            LoadVariables();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!killMSG) return;
            if (!(entity is BasePlayer)) return;
            BasePlayer pilot = entity as BasePlayer;

            if (!pilots.Contains(pilot)) return;
            if (info.Initiator is BasePlayer)
                if (info.InitiatorPlayer != null)
                    rust.BroadcastChat(string.Format(msg("<color=#ffd479>{0}</color> убил пилота", info.InitiatorPlayer.UserIDString), info.InitiatorPlayer?.displayName));

            if (pilots.Contains(pilot))
                pilots.Remove(pilot);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.name != heliprefab) return;
            if (UnityEngine.Random.Range(0f, 1f) > chanceOfOccuring) return;
            DoEvent(entity);
        }

        [ChatCommand("callbrokenheli")]
        void CallBrokenHeliCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                player.ChatMessage(msg("No Permission", player.UserIDString));
                return;
            }
            CallBrokenHeli();
        }

        [ConsoleCommand("callbrokenheli")]
        void CallBrokeHeliCONSOLECMD(ConsoleSystem.Arg args)
        {
            if (args.Connection != null) return;
            CallBrokenHeli();
        }

        [ChatCommand("setpilotinventory")]
        void SetpilotinventoryCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                player.ChatMessage(msg("No Permission", player.UserIDString));
                return;
            }

            mainContiner.Clear();
            beltContainer.Clear();
            clothesContainer.Clear();

            foreach (Item item in player.inventory.containerMain.itemList)
                mainContiner.Add(new ItemInfo() { shortname = item.info.shortname, amount = item.amount, chance = 1f, skinID = item.skin });
            foreach (Item item in player.inventory.containerBelt.itemList)
                beltContainer.Add(new ItemInfo() { shortname = item.info.shortname, amount = item.amount, chance = 1f, skinID = item.skin });
            foreach (Item item in player.inventory.containerWear.itemList)
                clothesContainer.Add(new ItemInfo() { shortname = item.info.shortname, amount = item.amount, chance = 1f, skinID = item.skin });

            SaveData();
            LoadData();

            player.ChatMessage(msg("Pilot Inventory Set", player.UserIDString));
        }

        void DoEvent(BaseNetworkable entity)
        {
            float x = UnityEngine.Random.Range(minTimeToEvent, maxTimeToEvent);
            Puts(string.Format(msg("Malfunction Timer Warning (Console)"), x.ToString()));

            timer.Once(x, () =>
            {
                if (!entity) return;
                rust.BroadcastChat(msg("Heli Malfunctioned"));
                BaseHelicopter heli = entity as BaseHelicopter;
                Vector3 helipos = heli.GetEstimatedWorldPosition();
                timer.Once(1f, () =>
                {
                    if (!entity) return;
                    heli.SetFlag(BaseEntity.Flags.OnFire, true);
                    heli.SendNetworkUpdateImmediate();
                    heli.Hurt(heli.health - 10.0f);
                    if (heli == null) PrintError("There was an error with the helicopter, if you see this message please notifiy redBDGR with the error code [01]");
                    BasePlayer pilot = HandlePlayer(helipos);
                    BaseEntity chute = CreateParachute();
                    heli.Hurt(heli.health);
                    if (pilot == null) PrintError("Pilot error");
                    AddItems(pilot);
                    pilot.displayName = "Helicopter Pilot";
                    pilots.Add(pilot);
                    pilot.SendNetworkUpdateImmediate();
                    chute.SetParent(pilot);
                    chute.Spawn();
                    MovePlayerDown(pilot, chute);
                    timer.Once(time, () =>
                    {
                        timer.Once(pilotLifeLength, () => { if (pilot.IsValid()) pilot.Kill(); });
                        if (pilot.IsDead() || pilot.IsDestroyed)
                            return;
                        pilot.Heal(100.0f);
                        pilot.StartWounded();

                        repeat = timer.Repeat(5.0f, 0, () =>
                        {
                            if (!pilot.IsDead() || !pilot.IsDestroyed)
                            {
                                pilot.StopWounded();
                                pilot.Heal(100.0f);
                                pilot.StartWounded();
                            }
                            else
                            {
                                if (!chute.IsDestroyed)
                                    chute.Kill();
                                repeat.Destroy();
                            }
                        });
                    });
                });
            });
        }

        // Credit to Lilnitedemon for providing insight to the calculations for this
        void MovePlayerDown(BasePlayer player, BaseEntity chute)
        {
            Vector3 ground = GetGroundPosition(player.transform.position);
            float dist = player.transform.position.y - ground.y;
            float repeattimes = (dist / 0.4f);
            time = repeattimes * 0.1f;

            rp = timer.Repeat(0.1f, Convert.ToInt32(repeattimes), () =>
            {
                Vector3 downpos = new Vector3(player.transform.position.x, player.transform.position.y - 0.4f, player.transform.position.z);
                player.MovePosition(downpos);

                foreach (var people in BasePlayer.activePlayerList)
                    people.SendEntityUpdate();
            });
            return;
        }

        static Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            var groundlayer = 295764225;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, groundlayer)) sourcePos = hitInfo.point;
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        BaseEntity CreateParachute()
        {
            BaseEntity ent = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(0, 0, 0), new Quaternion(), true);
            return ent;
        }
        
        BasePlayer HandlePlayer(Vector3 pos)
        {
            if (minimumSpawnHeightEnabled)
                if (pos.y < minimumSpawnHeight)
                    pos.y = minimumSpawnHeight;
            BaseEntity ent = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", pos, new Quaternion(), true);
            ent.Spawn();
            return ent as BasePlayer;
        }

        void CallBrokenHeli()
        {
            var ent = GameManager.server.CreateEntity(heliprefab, new Vector3(), new Quaternion(), true);
            if (!ent) return;
            ent.Spawn();
            DoEvent(ent as BaseNetworkable);
        }

        void AddItems(BasePlayer player)
        {
            foreach (var item in mainContiner)
            {
                if (UnityEngine.Random.Range(0f, 1f) < item.chance)
                {
                    Item newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinID);
                    if (newitem == null) continue;
                    newitem.MoveToContainer(player.inventory.containerMain);
                }
            }
            foreach (var item in beltContainer)
            {
                if (UnityEngine.Random.Range(0f, 1f) < item.chance)
                {
                    Item newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinID);
                    if (newitem == null) continue;
                    newitem.MoveToContainer(player.inventory.containerBelt);
                }
            }
            foreach (var item in clothesContainer)
            {
                if (UnityEngine.Random.Range(0f, 1f) < item.chance)
                {
                    Item newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinID);
                    if (newitem == null) continue;
                    newitem.MoveToContainer(player.inventory.containerWear);
                }
            }
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
    }
}