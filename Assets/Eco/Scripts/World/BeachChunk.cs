namespace Eco.Scripts.World
{
    public class BeachChunk : Chunk
    {
        public void Init()
        {
            // Set all tiles to ground type (beach is just ground)
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    Tile t = tiles[y * ChunkSize + x];
                    t.groundType = TileGroundType.Ground;
                }
            }
            
            // Save tiles for the map
            if (!HasSave)
            {
                SaveTiles();
            }
        }
    }
}

