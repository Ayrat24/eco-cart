namespace Eco.Scripts.World
{
    /// <summary>
    /// A hex of lake or sea: no scrap, and always a water surface over the baked bed. Its shore can be dry land
    /// (the terrain doesn't follow the hexes), and the land chunks around it show water too where their terrain
    /// dips under it.
    /// </summary>
    public class WaterChunk : Chunk
    {
        public override void Init(ChunkContext context)
        {
            UpdateWater(context.World, true);

            // To show water on the map
            if (!HasSave)
            {
                SaveTiles();
            }
        }
    }
}
