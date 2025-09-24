using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AirwolfVendorFix", "RFC1920", "1.0.6")]
    [Description("Respawn missing Airwolf, Fishing Village, and Horse vendors")]
    internal class AirwolfVendorFix : RustPlugin
    {
        private ConfigData configData;
        private readonly string vprefab = "assets/prefabs/npc/bandit/shopkeepers/bandit_conversationalist.prefab";
        private readonly string bprefab = "assets/prefabs/npc/bandit/shopkeepers/boat_shopkeeper.prefab";
        private readonly string hprefab = "assets/prefabs/npc/bandit/shopkeepers/stables_shopkeeper.prefab";
        private readonly string bvprefab = "assets/prefabs/npc/bandit/shopkeepers/bandit_shopkeeper.prefab";
        private readonly List<Vector3> boatloc = new List<Vector3>();

        [PluginReference]
        private readonly Plugin GridAPI;

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        private void Init()
        {
            LoadConfigValues();
            if (configData.Options.autoPlaceVendors)
            {
                FindMonuments();
            }
        }

        // For auto-placement
        private void SpawnVendor(string prefab, Vector3 spawnpos, Quaternion spawnrot, bool killold = false, bool stable = false)
        {
            if (killold)
            {
                Collider[] hits = Physics.OverlapSphere(spawnpos, 2f);
                for (int i = 0; i < hits.Length; i++)
                {
                    BaseEntity vendor = hits[i].GetComponentInParent<BaseEntity>();
                    if (vendor == null) continue;
                    if (vendor.ShortPrefabName == "bandit_conversationalist")
                    {
                        vendor.Kill();
                    }
                }
            }
            BaseEntity newvendor = GameManager.server.CreateEntity(prefab, spawnpos, spawnrot, true);

            if (stable)
            {
                InvisibleVendingMachine found = new InvisibleVendingMachine();
                List<BaseEntity> ents = new List<BaseEntity>();
                Vis.Entities(spawnpos, 1, ents);
                foreach (BaseEntity ent in ents)
                {
                    if (ent.ShortPrefabName.Equals("shopkeeper_vm_invis"))
                    {
                        found = ent as InvisibleVendingMachine;
                        break;
                    }
                }

                NPCShopKeeper sk = newvendor as NPCShopKeeper;
                if (found != null) sk.machine = found;
            }
            newvendor.Spawn();
        }

        // For command placement
        private static void SpawnVendor(string prefab, BasePlayer player)
        {
            Vector3 spawnpos = player.transform.position;
            BaseEntity newvendor = GameManager.server.CreateEntity(prefab, player.transform.position, player.transform.rotation, true);
            newvendor.Spawn();
            newvendor.ToPlayer().viewAngles = player.viewAngles + new Vector3(0, 1.5f, 0);
            newvendor.transform.localEulerAngles = spawnpos + new Vector3(0, 1.5f, 0);
        }

        [ChatCommand("bsp")]
        private void CmdSpawnBoatVendor(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "save":
                        boatloc.Add(player.transform.position);
                        break;
                }
                return;
            }
            List<VehicleVendor> ven = new List<VehicleVendor>();
            Vis.Entities(player.transform.position, 3f, ven);
            foreach (VehicleVendor vendor in ven)
            {
                vendor.Kill();
            }
            SpawnVendor(bprefab, player);
        }

        [ChatCommand("vsp")]
        private void CmdSpawnAirwolfVendor(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            List<VehicleVendor> ven = new List<VehicleVendor>();
            Vis.Entities(player.transform.position, 3f, ven);
            foreach (VehicleVendor vendor in ven)
            {
                vendor.Kill();
            }
            SpawnVendor(vprefab, player);
        }

        [ChatCommand("hsp")]
        private void CmdSpawnHorseVendor(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            List<VehicleVendor> ven = new List<VehicleVendor>();
            Vis.Entities(player.transform.position, 3f, ven);
            foreach (VehicleVendor vendor in ven)
            {
                vendor.Kill();
            }
            SpawnVendor(hprefab, player);
        }

        public string PositionToGrid(Vector3 position)
        {
            if (GridAPI != null)
            {
                string[] g = (string[]) GridAPI.CallHook("GetGrid", position);
                return string.Concat(g);
            }
            else
            {
                // From GrTeleport for display only
                Vector2 r = new Vector2((World.Size / 2) + position.x, (World.Size / 2) + position.z);
                float x = Mathf.Floor(r.x / 146.3f) % 26;
                float z = Mathf.Floor(World.Size / 146.3f) - Mathf.Floor(r.y / 146.3f);

                return $"{(char)('A' + x)}{z - 1}";
            }
        }

        private void FindMonuments()
        {
            Vector3 extents = Vector3.zero;
            string name = null;
            bool ishapis =  ConVar.Server.level.Contains("Hapis");

            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (monument.name.Contains("power_sub")) continue;// || monument.name.Contains("cave")) continue;
                name = null;

                if (ishapis)
                {
                    foreach (Match e in Regex.Matches(monument.name, @"\w{4,}|\d{1,}"))
                    {
                        if (e.Value.Equals("MONUMENT")) continue;
                        if (e.Value.Contains("Label")) continue;
                        name += e.Value + " ";
                    }
                    name = name.Trim();
                }
                else
                {
                    name = Regex.Match(monument.name, @"\w{6}\/(.+\/)(.+)\.(.+)").Groups[2].Value.Replace("_", " ").Replace(" 1", "").Titleize();
                }

                if (name.Contains("Fishing Village") && configData.Options.placeBoatVendor)
                {
                    if (configData.Options.debug) Puts($"Working on {name}");
                    List<BaseEntity> ents = new List<BaseEntity>();
                    Vis.Entities(monument.transform.position, 80, ents);
                    bool killvendor = false;
                    bool foundvendor = false;
                    Vector3 spawnpos = Vector3.zero;
                    Quaternion spawnrot = new Quaternion();

                    foreach (BaseEntity ent in ents)
                    {
                        if (ent == null) continue;
                        if (string.IsNullOrEmpty(ent.ShortPrefabName)) continue;
                        if (ent.ShortPrefabName.Equals("boat_shopkeeper"))
                        {
                            if (configData.Options.alwaysPlaceVendors)
                            {
                                if (configData.Options.debug) Puts("FOUND VENDOR, KILLING HIM!");
                                killvendor = true;
                                ent.Kill();
                            }
                        }
                        else if (ent.ShortPrefabName.Equals("shopkeeper_vm_invis"))
                        {
                            // Find invisible shopkeepers but verify no bandit_shopkeeper exists there
                            Collider[] hit = Physics.OverlapSphere(ent.transform.position, 1f);
                            for (int i = 0; i < hit.Length; i++)
                            {
                                BaseEntity subitem = hit[i].GetComponentInParent<BaseEntity>();
                                if (subitem == null) continue;
                                if (subitem.ShortPrefabName.Equals("bandit_shopkeeper") || subitem.ShortPrefabName.Equals("boat_shopkeeper"))
                                {
                                    if (configData.Options.debug) Puts($"Found an invisible shopkeeper at {subitem.transform.position}, but there is already an associated NPC.");
                                }
                                else
                                {
                                    foundvendor = true;
                                    if (configData.Options.debug) Puts($"Found invisible shopkeeper with no associated NPC at {spawnpos.ToString()}");
                                    spawnpos = ent.transform.position;
                                    spawnrot = ent.transform.rotation;
                                }
                            }
                        }
                    }
                    if (killvendor && spawnpos != Vector3.zero)
                    {
                        if (configData.Options.debug) Puts($"Respawning Boat Vendor at {spawnpos}");
                        SpawnVendor(bprefab, spawnpos, spawnrot, true);
                    }
                    else if (foundvendor && spawnpos != Vector3.zero)
                    {
                        if (configData.Options.debug) Puts($"Spawning Boat Vendor at {spawnpos}");
                        SpawnVendor(bprefab, spawnpos, spawnrot);
                    }
                }

                if (name.Contains("Stables") && configData.Options.placeHorseVendor)
                {
                    if (configData.Options.debug) Puts($"Working on {name}");
                    List<BaseEntity> ents = new List<BaseEntity>();
                    Vector3 realpos = monument.transform.position;
                    realpos.y = TerrainMeta.HeightMap.GetHeight(realpos);
                    Vis.Entities(realpos, 80, ents);

                    bool foundvendor = false;
                    bool killvendor = false;
                    Vector3 spawnpos = Vector3.zero;
                    Quaternion spawnrot = new Quaternion();

                    foreach (BaseEntity ent in ents)
                    {
                        if (ent == null) continue;
                        if (string.IsNullOrEmpty(ent.ShortPrefabName)) continue;
                        if (ent.ShortPrefabName.Equals("stables_shopkeeper"))
                        {
                            if (configData.Options.alwaysPlaceVendors)
                            {
                                if (configData.Options.debug) Puts("FOUND VENDOR, KILLING HIM!");
                                killvendor = true;
                                ent.Kill();
                            }
                        }
                        else if (ent.ShortPrefabName.Equals("shopkeeper_vm_invis"))
                        {
                            // Find invisible shopkeepers but verify no bandit_shopkeeper exists there
                            Collider[] hit = Physics.OverlapSphere(ent.transform.position, 1f);
                            for (int i = 0; i < hit.Length; i++)
                            {
                                BaseEntity subitem = hit[i].GetComponentInParent<BaseEntity>();
                                if (subitem == null) continue;
                                if (subitem.ShortPrefabName.Equals("bandit_shopkeeper") || subitem.ShortPrefabName.Equals("stables_shopkeeper"))
                                {
                                    if (configData.Options.debug) Puts($"Found an invisible shopkeeper at {subitem.transform.position}, but there is already an associated NPC.");
                                }
                                else
                                {
                                    foundvendor = true;
                                    if (configData.Options.debug) Puts($"Found invisible shopkeeper with no associated NPC at {spawnpos.ToString()}");
                                    spawnpos = ent.transform.position;
                                    spawnpos.y += 0.2f;
                                    spawnrot = ent.transform.rotation;
                                    continue;
                                }
                            }
                        }
                    }
                    if (killvendor && spawnpos != Vector3.zero)
                    {
                        if (configData.Options.debug) Puts($"Respawning Horse Vendor at {spawnpos}");
                        SpawnVendor(hprefab, spawnpos, spawnrot, true, true);
                    }
                    else if (foundvendor && spawnpos != Vector3.zero)
                    {
                        if (configData.Options.debug) Puts($"Spawning Horse Vendor at {spawnpos}");
                        SpawnVendor(hprefab, spawnpos, spawnrot, false, true);
                    }
                }
                if (name.Contains("Bandit") && configData.Options.placeMiniVendor)
                {
                    if (configData.Options.debug) Puts($"Working on {name}");
                    List<BaseEntity> ents = new List<BaseEntity>();
                    Vis.Entities(monument.transform.position, 80, ents);
                    bool foundrfb = false;
                    bool foundoor = false;
                    bool foundvendor = false;
                    Vector3 spawnpos = Vector3.zero;
                    Vector3 rfbloc = Vector3.zero;
                    Quaternion spawnrot = new Quaternion();

                    foreach (BaseEntity entity in ents)
                    {
                        if (entity.ShortPrefabName.Equals("rfbroadcaster.static") && !foundrfb)
                        {
                            foundrfb = true;
                            rfbloc = entity.transform.position;
                            spawnpos = Vector3.Lerp(monument.transform.position, rfbloc, 0.975f);

                            Collider[] hits = Physics.OverlapSphere(entity.transform.position, 2f);
                            for (int i = 0; i < hits.Length; i++)
                            {
                                BaseEntity door = hits[i].GetComponentInParent<BaseEntity>();
                                if (door == null) continue;
                                if (door.ShortPrefabName.Equals("bandit_conversationalist"))
                                {
                                    foundvendor = true;
                                    break;
                                }
                                else if (door.ShortPrefabName.Equals("door.hinged.wood.static") && !foundoor)
                                {
                                    foundoor = true;
                                    spawnpos.y -= 1.2f;
                                    Quaternion rotation = Quaternion.LookRotation(spawnpos);
                                    spawnrot = rotation *= Quaternion.Euler(0, 180, 0);
                                }
                            }
                        }
                        else if (entity.ShortPrefabName.Equals("shopkeeper_vm_invis") && configData.Options.fixInvisibleBanditVendors)
                        {
                            bool foundnpc = false;
                            Collider[] hit = Physics.OverlapSphere(entity.transform.position, 1f);
                            for (int i = 0; i < hit.Length; i++)
                            {
                                BaseEntity subitem = hit[i].GetComponentInParent<BaseEntity>();
                                if (subitem == null) continue;
                                if (subitem.ShortPrefabName.Equals("bandit_shopkeeper"))
                                {
                                    foundnpc = true;
                                    if (configData.Options.debug) Puts($"Found an invisible shopkeeper at {subitem.transform.position}, and there is already an associated NPC.");
                                }
                            }
                            if (!foundnpc)
                            {
                                if (configData.Options.debug) Puts($"Spawning missing shop vendor at {entity.transform.position}");
                                SpawnVendor(bvprefab, entity.transform.position, entity.transform.rotation);
                            }
                        }
                    }
                    if (foundvendor)
                    {
                        if (configData.Options.alwaysPlaceVendors)
                        {
                            SpawnVendor(vprefab, spawnpos, spawnrot, true);
                        }
                    }
                    else if (foundrfb && foundoor)
                    {
                        SpawnVendor(vprefab, spawnpos, spawnrot);
                    }
                }
            }
        }

        #region config
        private class ConfigData
        {
            public Options Options = new Options();
            public VersionNumber Version;
        }

        private class Options
        {
            public bool autoPlaceVendors;
            public bool alwaysPlaceVendors;
            public bool placeBoatVendor;
            public bool placeMiniVendor;
            public bool placeHorseVendor;
            public bool fixInvisibleBanditVendors;
            //public bool fixInvisibleBoatVendors;
            public bool debug;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file");
            configData = new ConfigData
            {
                Options = new Options()
                {
                    autoPlaceVendors = false,
                    alwaysPlaceVendors = false,
                    placeBoatVendor = true,
                    placeMiniVendor = true,
                    placeHorseVendor = true,
                    fixInvisibleBanditVendors = true,
                    //fixInvisibleBoatVendors = true,
                    debug = false
                },
                Version = Version,
            };
            SaveConfig(configData);
        }

        private void LoadConfigValues()
        {
            configData = Config.ReadObject<ConfigData>();
            configData.Version = Version;

            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion
    }
}
