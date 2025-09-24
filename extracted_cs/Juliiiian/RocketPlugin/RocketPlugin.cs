using System;
using UnityEngine;
using Oxide.Ext.Rocket;
using Oxide.Ext.Rocket.WebSockets;
using ConVar;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Rocket", "Juliiian", "0.0.1")]
    [Description("Testing out my WebSocket.")]
    internal class RocketPlugin : RustPlugin
    {
        private Socket _socket;

        //BasePlayer.activePlayerList.AsEnumerable();

        #region Config
        private class PluginConfig
        {
            public string ApiToken;
            public string Url;
        }

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ApiToken = "null",
                Url = "ws://127.0.0.1:3151"
            };
        }

        #endregion

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            try
            {
                SocketSettings conf = new SocketSettings(config.ApiToken, config.Url);
                _socket = new Socket(conf, RocketExtension.GlobalLogger);
                _socket.Connect();
               


                BaseAnimalNPC[] AnimalEnt = UnityEngine.Object.FindObjectsOfType<BaseAnimalNPC>();

                for (int i = 0; i < AnimalEnt.Length; i++)
                {
                    Puts(AnimalEnt[i].ShortPrefabName);
                    Puts(AnimalEnt[i].ServerPosition.ToString());
                }

                
                Timer GeneralUpdater = timer.Every(5f, () =>
                {

                    BasePlayer[] Players = UnityEngine.Object.FindObjectsOfType<BasePlayer>();

                    for (int i = 0; i < Players.Length; i++)
                    {
                        EventPayload<SocketToServerEvents> payload = new EventPayload<SocketToServerEvents>()
                        {
                            EventName = SocketToServerEvents.Ping,
                            Data = $"{Players[i].name}: {Players[i].ServerPosition.ToString()}"
                        };
                        _socket.Send(payload);
                    }
                });
                
            }
            catch (Exception ex)
            {
                Puts(ex.Message);
            }
        }

        private string FindGridPosition(Vector3 position) => PhoneController.PositionToGridCoord(position);
        
        private void Reload()
        {
            rust.RunServerCommand("oxide.reload RocketPlugin");
        }
       
    }
}