// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MinicopterNitro", "by RIPJAWBONES", "1.0.1")]
    [Description("Add a speed boost to helicopters")]

    class MinicopterNitro : RustPlugin
    {
        private Configuration config;
        private Dictionary<ulong, MyHelicopterWrapper> activeNitro = new Dictionary<ulong, MyHelicopterWrapper>();
        private Dictionary<ulong, float> cooldownTimers = new Dictionary<ulong, float>();

        class Configuration
        {
            public Dictionary<string, HelicopterSettings> Helicopters { get; set; }
        }

        class HelicopterSettings
        {
            public bool Enabled { get; set; } = true;
            public float ModifiedHelicopterSpeed { get; set; } = 2.0f;
            public float VelocityModifier { get; set; } = 5.0f;
            public int NitroDurationSeconds { get; set; } = 2;
            public int CooldownSeconds { get; set; } = 30;
            public BUTTON NitroButton { get; set; } = BUTTON.SPRINT;
            public string EffectPrefab { get; set; } = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
            public float MinimumGroundDistance { get; set; } = 15f; // Default value is 5 meters
        }

        private void Init()
        {
            LoadConfig();
            permission.RegisterPermission("minicopternitro.use", this);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration
            {
                Helicopters = new Dictionary<string, HelicopterSettings>
                {
                    {
                        "minicopter.entity", new HelicopterSettings()
                    },
                    {
                        "scraptransporthelicopter", new HelicopterSettings()
                        {
                            ModifiedHelicopterSpeed = 2.0f,
                            EffectPrefab = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab"
                        }
                    }
                }
            };
            SaveConfig();
        }

        private void LoadConfig()
        {
            config = Config.ReadObject<Configuration>();
            if (config == null)
            {
                PrintError("Config is null, using default values.");
                LoadDefaultConfig();
            }
        }

        private void SaveConfig() => Config.WriteObject(config, true);

        class MyHelicopterWrapper
        {
            public float startSpeed { get; private set; }
            public BaseEntity helicopter { get; private set; }

            public MyHelicopterWrapper(BaseEntity heli)
            {
                helicopter = heli;
                startSpeed = heli.GetComponent<Rigidbody>().velocity.magnitude;
            }

            public void ModifySpeed(float modifier)
            {
                helicopter.GetComponent<Rigidbody>().velocity *= modifier;
            }

            public void RestoreOriginalValues()
            {
                helicopter.GetComponent<Rigidbody>().velocity = helicopter.transform.forward * startSpeed;
            }
        }

        void ModifyHelicopter(BaseEntity heli, BasePlayer player)
        {
            string prefabName = heli.ShortPrefabName;
            HelicopterSettings settings = config.Helicopters.GetValueOrDefault(prefabName);

            if (settings != null && settings.Enabled)
            {
                MyHelicopterWrapper wrapper = new MyHelicopterWrapper(heli);
                wrapper.ModifySpeed(settings.ModifiedHelicopterSpeed);

                Effect.server.Run(settings.EffectPrefab, player.transform.position);

                activeNitro[player.userID] = wrapper;
            }
        }

        void EndModifyHelicopter(BasePlayer player)
        {
            if (activeNitro.TryGetValue(player.userID, out MyHelicopterWrapper wrapper))
            {
                activeNitro.Remove(player.userID);
            }
        }

        [ChatCommand("nitro")]
        void CmdNitro(BasePlayer player, string command, string[] args)
        {
            UseNitro(player);
        }

        [ConsoleCommand("nitro")]
        void ConsoleCmdNitro(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                UseNitro(arg.Player());
        }

        private void UseNitro(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "minicopternitro.use"))
                return;

            if (player.GetMountedVehicle() is Minicopter copter && copter.IsDriver(player))
            {
                string prefabName = copter.ShortPrefabName;
                HelicopterSettings settings = config.Helicopters.GetValueOrDefault(prefabName);

                if (settings != null && settings.Enabled)
                {
                    if (!cooldownTimers.ContainsKey(player.userID) || cooldownTimers[player.userID] < Time.realtimeSinceStartup)
                    {
                        if (IsPlayerNearGround(player, settings.MinimumGroundDistance))
                        {
                            SendReply(player, "You are too close to the ground to use Nitro.");
                            return;
                        }

                        SendReply(player, "Nitro Boost Activated!");
                        ModifyHelicopter(copter, player);
                        cooldownTimers[player.userID] = Time.realtimeSinceStartup + settings.CooldownSeconds;

                        timer.Once(settings.NitroDurationSeconds, () =>
                        {
                            if (activeNitro.ContainsKey(player.userID))
                            {
                                EndModifyHelicopter(player);
                            }
                        });
                    }
                    else
                    {
                        int remainingCooldown = Mathf.CeilToInt(cooldownTimers[player.userID] - Time.realtimeSinceStartup);
                        SendReply(player, $"You are on a cooldown for {remainingCooldown} seconds.");
                    }
                }
            }
            else if (player.GetMountedVehicle() is ScrapTransportHelicopter scrapCopter && scrapCopter.IsDriver(player))
            {
                string prefabName = scrapCopter.ShortPrefabName;
                HelicopterSettings settings = config.Helicopters.GetValueOrDefault(prefabName);

                if (settings != null && settings.Enabled)
                {
                    if (!cooldownTimers.ContainsKey(player.userID) || cooldownTimers[player.userID] < Time.realtimeSinceStartup)
                    {
                        if (IsPlayerNearGround(player, settings.MinimumGroundDistance))
                        {
                            SendReply(player, "You are too close to the ground to use Nitro.");
                            return;
                        }

                        SendReply(player, "Nitro Boost Activated!");
                        ModifyHelicopter(scrapCopter, player);
                        cooldownTimers[player.userID] = Time.realtimeSinceStartup + settings.CooldownSeconds;

                        timer.Once(settings.NitroDurationSeconds, () =>
                        {
                            if (activeNitro.ContainsKey(player.userID))
                            {
                                EndModifyHelicopter(player);
                            }
                        });
                    }
                    else
                    {
                        int remainingCooldown = Mathf.CeilToInt(cooldownTimers[player.userID] - Time.realtimeSinceStartup);
                        SendReply(player, $"You are on a cooldown for {remainingCooldown} seconds.");
                    }
                }
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!permission.UserHasPermission(player.UserIDString, "minicopternitro.use"))
                return;

            if (player.GetMountedVehicle() is Minicopter copter && copter.IsDriver(player))
            {
                string prefabName = copter.ShortPrefabName;
                HelicopterSettings settings = config.Helicopters.GetValueOrDefault(prefabName);

                if (settings != null && settings.Enabled)
                {
                    if (input.WasJustPressed(settings.NitroButton))
                    {
                        if (!cooldownTimers.ContainsKey(player.userID) || cooldownTimers[player.userID] < Time.realtimeSinceStartup)
                        {
                            if (IsPlayerNearGround(player, settings.MinimumGroundDistance))
                            {
                                SendReply(player, "You are too close to the ground to use Nitro.");
                                return;
                            }

                            SendReply(player, "Nitro Boost Activated!");
                            ModifyHelicopter(copter, player);
                            cooldownTimers[player.userID] = Time.realtimeSinceStartup + settings.CooldownSeconds;

                            timer.Once(settings.NitroDurationSeconds, () =>
                            {
                                if (activeNitro.ContainsKey(player.userID))
                                {
                                    EndModifyHelicopter(player);
                                }
                            });
                        }
                        else
                        {
                            int remainingCooldown = Mathf.CeilToInt(cooldownTimers[player.userID] - Time.realtimeSinceStartup);
                            SendReply(player, $"You are on a cooldown for {remainingCooldown} seconds.");
                        }
                    }
                }
            }
            else if (player.GetMountedVehicle() is ScrapTransportHelicopter scrapCopter && scrapCopter.IsDriver(player))
            {
                string prefabName = scrapCopter.ShortPrefabName;
                HelicopterSettings settings = config.Helicopters.GetValueOrDefault(prefabName);

                if (settings != null && settings.Enabled)
                {
                    if (input.WasJustPressed(settings.NitroButton))
                    {
                        if (!cooldownTimers.ContainsKey(player.userID) || cooldownTimers[player.userID] < Time.realtimeSinceStartup)
                        {
                            if (IsPlayerNearGround(player, settings.MinimumGroundDistance))
                            {
                                SendReply(player, "You are too close to the ground to use Nitro.");
                                return;
                            }

                            SendReply(player, "Nitro Boost Activated!");
                            ModifyHelicopter(scrapCopter, player);
                            cooldownTimers[player.userID] = Time.realtimeSinceStartup + settings.CooldownSeconds;

                            timer.Once(settings.NitroDurationSeconds, () =>
                            {
                                if (activeNitro.ContainsKey(player.userID))
                                {
                                    EndModifyHelicopter(player);
                                }
                            });
                        }
                        else
                        {
                            int remainingCooldown = Mathf.CeilToInt(cooldownTimers[player.userID] - Time.realtimeSinceStartup);
                            SendReply(player, $"You are on a cooldown for {remainingCooldown} seconds.");
                        }
                    }
                }
            }
        }

        private bool IsPlayerNearGround(BasePlayer player, float minimumGroundDistance)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(player.transform.position, Vector3.down, out hitInfo, 10f, LayerMask.GetMask("Terrain", "Construction")))
            {
                float distanceToGround = hitInfo.distance;
                return distanceToGround < minimumGroundDistance;
            }
            return false;
        }

        private void Unload()
        {
            foreach (var pair in activeNitro)
            {
                pair.Value.RestoreOriginalValues();
            }
        }
    }
}
