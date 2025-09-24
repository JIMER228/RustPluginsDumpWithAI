using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Plugins
{
    [Info("VIP Playtime", "CodeFlow", "1.4.1")]
    [Description("Otorga el grupo VIP a los jugadores después de cierto tiempo de juego.")]
    public class VIPPlaytime : CovalencePlugin
    {
        private PluginConfig config;
        private Dictionary<string, PlayerData> playerData;

        private class PluginConfig
        {
            [JsonProperty("VIPHours")]
            public double VIPHours { get; set; } = 20;

            [JsonProperty("VIPGroup")]
            public string VIPGroup { get; set; } = "vip";

            [JsonProperty("SaveInterval")]
            public int SaveInterval { get; set; } = 900;

            [JsonProperty("ViptimeCommand")]
            public string ViptimeCommand { get; set; } = "viptime";
        }

        private class PlayerData
        {
            public double Playtime { get; set; }
            public DateTime LastLogin { get; set; }
        }

        private void Init()
        {
            LoadConfig();
            LoadData();
            timer.Every(config.SaveInterval, SaveData);
            AddCovalenceCommand(config.ViptimeCommand, "CmdPlaytime");

            foreach (var player in players.Connected)
            {
                OnUserConnected(player);
            }
        }

        private void OnServerSave() => SaveData();

        private void OnUserConnected(IPlayer player)
        {
            var userId = player.Id;

            if (!playerData.ContainsKey(userId))
            {
                playerData[userId] = new PlayerData { Playtime = 0, LastLogin = DateTime.UtcNow };
                Puts($"Nuevo jugador agregado: {userId}");
            }
            else
            {
                playerData[userId].LastLogin = DateTime.UtcNow;
                Puts($"Jugador {userId} ha iniciado sesión. Último inicio de sesión actualizado.");
            }

            SaveData();
        }

        private void OnUserDisconnected(IPlayer player)
        {
            var userId = player.Id;

            if (playerData.ContainsKey(userId))
            {
                var sessionTime = DateTime.UtcNow - playerData[userId].LastLogin;
                playerData[userId].Playtime += sessionTime.TotalSeconds;
                Puts($"Jugador {userId} se ha desconectado. Tiempo de juego actualizado: {playerData[userId].Playtime} segundos.");
                CheckPlaytime(userId, player);
            }
            else
            {
                Puts($"No se encontraron datos de juego para el jugador {userId} al desconectar.");
            }

            SaveData();
        }

        private void CheckPlaytime(string userId, IPlayer player)
        {
            if (playerData[userId].Playtime / 3600 >= config.VIPHours && !permission.UserHasGroup(userId, config.VIPGroup))
            {
                permission.AddUserGroup(userId, config.VIPGroup);
                player.Message("¡Felicidades! Ahora eres VIP.");
                Puts($"Jugador {userId} ha sido añadido al grupo VIP.");
            }
        }

        private void CmdPlaytime(IPlayer player, string command, string[] args)
        {
            var userId = player.Id;

            if (playerData.ContainsKey(userId))
            {
                var playtimeSeconds = playerData[userId].Playtime;
                var hoursPlayed = Math.Floor(playtimeSeconds / 3600);
                var minutesPlayed = Math.Floor((playtimeSeconds % 3600) / 60);
                var secondsPlayed = Math.Floor(playtimeSeconds % 60);

                var remainingSeconds = (config.VIPHours * 3600) - playtimeSeconds;
                var remainingHours = Math.Floor(remainingSeconds / 3600);
                var remainingMinutes = Math.Floor((remainingSeconds % 3600) / 60);
                var remainingSecs = Math.Floor(remainingSeconds % 60);

                if (playtimeSeconds >= config.VIPHours * 3600)
                {
                    player.Message("¡Ya eres VIP!");
                }
                else
                {
                    player.Message($"Has jugado {hoursPlayed:F0} horas, {minutesPlayed:F0} minutos y {secondsPlayed:F0} segundos.");
                    player.Message($"Te faltan {remainingHours:F0} horas, {remainingMinutes:F0} minutos y {remainingSecs:F0} segundos para ser VIP.");
                }

                Puts($"Jugador {userId} ha consultado su tiempo de juego: {hoursPlayed:F0} horas, {minutesPlayed:F0} minutos y {secondsPlayed:F0} segundos.");
            }
            else
            {
                player.Message("No se encontraron datos de juego. Intenta nuevamente más tarde.");
                Puts($"No se encontraron datos de juego para el jugador {userId}.");
            }
        }

        private void LoadConfig()
        {
            var configPath = Path.Combine(Interface.Oxide.ConfigDirectory, "VIPPlaytime.json");
            config = File.Exists(configPath)
                ? JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText(configPath))
                : new PluginConfig();

            if (!File.Exists(configPath))
            {
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            var configPath = Path.Combine(Interface.Oxide.ConfigDirectory, "VIPPlaytime.json");
            File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void LoadData()
        {
            try
            {
                playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>("VIPPlaytimeData")
                            ?? new Dictionary<string, PlayerData>();
            }
            catch (Exception ex)
            {
                Puts($"Error al cargar datos de juego: {ex.Message}");
                playerData = new Dictionary<string, PlayerData>();
            }
        }

        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject("VIPPlaytimeData", playerData);
                Puts("Datos de juego guardados correctamente.");
            }
            catch (Exception ex)
            {
                Puts($"Error al guardar datos de juego: {ex.Message}");
            }
        }
    }
}
