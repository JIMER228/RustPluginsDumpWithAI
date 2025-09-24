// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ObjectLimiter", "pirojok", "0.0.1")]
    public class ObjectLimiter : RustPlugin
    {
        void OnEntitySpawned(BaseEntity entity)
        {
            if(entity.ShortPrefabName != "foundation") return;
            var player = BasePlayer.FindByID(entity.OwnerID);
            var cupboard = player.GetBuildingPrivilege();
            if(cupboard == null) return;
            var a =0;
            var entities = player.GetBuildingPrivilege()?.GetBuilding()?.decayEntities;
            if (entities != null)
            {
                foreach (var entitys in entities)
                {

                    if(entitys.ShortPrefabName == "foundation")
                        a++;
                }
            }
            if(a > config.CountLimit)
            {
                player.ChatMessage($"Вы превысили допустимое кол-во фундаметов (<color=green>{config.CountLimit}</color>)");
                entity.Kill();
                return;
            }
            if(a%config.CountLimitMess==0)
            {
                player.ChatMessage($"В данном шкафу можно ещё поставить <color=green>{config.CountLimit-a}</color> объектов!");
            }
        }
        #region Configuration
        private Configur config;

        private class Configur
        {      
            [JsonProperty("Кол-во допустимого строительва фундаментов")]
            public int CountLimit = 100;
            [JsonProperty("Через какое кол-во фундаметов сообщать о лимите?")]
            public int CountLimitMess = 10;
            public static Configur GetNewConfiguration()
            {
                return new Configur
                {
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = Configur.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<Configur>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion 
    }
}