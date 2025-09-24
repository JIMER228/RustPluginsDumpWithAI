using System;
using System.Collections.Generic;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Skin Zone Items", "imoke", "1.0.0")]
    [Description("Set skins for specific items in zones")]
    class SkinZoneItems : CovalencePlugin
    {
        #region Variables
        
        [PluginReference]
        private Plugin ZoneManager = null;
        
        #endregion
        
        #region Configuration

        private Configuration _config;

        private class Configuration
	{
	    [JsonProperty(PropertyName = "Zone ID Entity Skin")]
	    public Dictionary<string, List<EntitySkin>> ZoneEntitySkins { get; set; }
	}

	private class EntitySkin 
	{	
	    [JsonProperty(PropertyName = "Entity Name")]
	    public string EntityName { get; set; }
			
	    [JsonProperty(PropertyName = "Skin ID")]
	    public ulong SkinId { get; set; }
	}

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion
        
        #region Hooks

        private void OnServerInitialized()
        {
            LoadSkins();
        }
		
	private void OnServerSave()
	{
	    LoadSkins();
	}
		
	private void LoadSkins()
        {
	    if (ZoneManager != null && ZoneManager.IsLoaded)
            {
                var zones = ZoneManager.Call<string[]>("GetZoneIDs");

		foreach(var zoneKey in _config.ZoneEntitySkins.Keys)
		{
		    _config.ZoneEntitySkins.TryGetValue(zoneKey, out var entitySkins);
		    var zoneEntitySkins = entitySkins ?? new List<EntitySkin>();
		
		    var allZoneEntities = ZoneManager.Call<List<BaseEntity>>("GetEntitiesInZone", zoneKey);
	
		    foreach(var skinEntity in zoneEntitySkins)
		    {
		        var entitiesToUpdate = allZoneEntities.Where(x => x.ShortPrefabName == skinEntity.EntityName).ToList();

                        Puts($"Updating {entitiesToUpdate.Count()} skin/s for entity: {skinEntity.EntityName}");
					  
			foreach(var entityToUpdate in entitiesToUpdate)
			{
			    if (entityToUpdate is BaseEntity entity)
			    {
				entity.skinID = skinEntity.SkinId;
				entity.SendNetworkUpdate();
			    }
		        }
		    }
		}
            }
	}

        #endregion
    }
	
	
}
