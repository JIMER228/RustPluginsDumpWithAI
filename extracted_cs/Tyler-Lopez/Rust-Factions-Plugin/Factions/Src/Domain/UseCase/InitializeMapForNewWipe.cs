namespace Oxide.Plugins
{
    partial class Factions
    {
        public void InitializeMapForNewWipe()
        {
            // Scan all possible previous grids and delete ZoneManager zones which may have existed
            byte currentRow = 0;
            byte currentCol = 0;
            var currentGrid = new FactionsGrid(currentRow, currentCol);
            // While the current grid has been erased successfully
            while (_zoneManagerRepository.EraseZone(currentGrid.ToString()))
            {
                // Iterate through all columns of this row and continue so long as they erase successfully
                do { currentGrid = new FactionsGrid(currentRow, ++currentCol); } while (_zoneManagerRepository.EraseZone(currentGrid.ToString()));
                // Increment current grid to next row, reset at column 0
                currentCol = 0;
                currentRow++;
                currentGrid = new FactionsGrid(currentRow, currentCol);
            }

            // For each factionsGrid on the new map create a ZoneManager zone for them
            var map = new FactionsGridManager(ConVar.Server.worldsize);
            foreach (var grid in map)
            {
                _zoneManagerRepository.CreateZoneForGrid(
                    factionsGrid: grid,
                    center: map.GetGridCenter(grid),
                    gridSize: FactionsGridManager.GetGridSize()
                );
            }
        }
    }
}