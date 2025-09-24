namespace Oxide.Plugins
{
    [Info("MLRS Respawner", "WhiteThunder", "1.0.0")]
    [Description("Respawns MLRS at Desert Military Bases if missing when the plugin loads.")]
    internal class MLRSRespawner : CovalencePlugin
    {
        private void OnServerInitialized()
        {
            foreach (var monument in TerrainMeta.Path.Monuments)
            {
                if (!monument.name.Contains("desert_military_base"))
                    continue;

                foreach (var spawner in monument.GetComponentsInChildren<IndividualSpawner>())
                {
                    if (!spawner.entityPrefab.resourcePath.Contains("mlrs.entity"))
                        continue;

                    // The customBoundsCheckMask is 0 which won't find the MLRS, so use the standard mask.
                    spawner.useCustomBoundsCheckMask = false;

                    if (spawner.HasSpaceToSpawn())
                    {
                        spawner.SpawnInitial();
                        LogWarning("Spawned missing MLRS at Desert Military Base.");
                    }
                }
            }
        }
    }
}
