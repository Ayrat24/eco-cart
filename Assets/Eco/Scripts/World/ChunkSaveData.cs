namespace Eco.Scripts.World
{
    /// <summary>Saved state of one hex chunk: the definition it was made for and its tiles.</summary>
    [System.Serializable]
    public class ChunkSaveData
    {
        /// <summary><see cref="ChunkDefinition.Id"/>; the save is discarded when the hex was repainted since.</summary>
        public string chunk;

        public TileData[] tiles;
    }
}
