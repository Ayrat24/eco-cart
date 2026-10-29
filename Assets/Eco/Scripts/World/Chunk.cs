using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// One spawned hex of the world. Pooled per <see cref="ChunkDefinition"/>: <see cref="WorldController"/> places
    /// it on its hex, calls <see cref="Setup"/> then <see cref="Init"/>, and saves its tiles before returning it.
    /// </summary>
    public class Chunk : MonoBehaviour, IPooledObject
    {
        protected Tile[] tiles;

        protected SaveManager SaveManager;
        protected Vector2Int Position;

        public Tile[] Tiles => tiles;
        public ChunkDefinition Definition { get; private set; }
        public HexTileLayout Layout { get; private set; }

        /// <summary>This hex's saved tiles, when they were saved for the same definition and layout.</summary>
        protected TileData[] SavedTiles { get; private set; }
        protected bool HasSave => SavedTiles != null;

        public void Setup(Vector2Int coord, ChunkDefinition definition, SaveManager saveManager, HexTileLayout layout)
        {
            Position = coord;
            Definition = definition;
            SaveManager = saveManager;
            Layout = layout;

            // Fresh tiles every spawn: scrap still in the stomach keeps its old tile and must not touch these
            tiles = new Tile[layout.Count];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new Tile(i) { parentChunk = this };
            }

            SavedTiles = saveManager.Chunks.TryGetValue(coord, out var save) && save.chunk == definition.Id &&
                         save.tiles != null && save.tiles.Length == tiles.Length
                ? save.tiles
                : null;
        }

        public virtual void Init(ChunkContext context)
        {
        }

        public Vector3 GetTileWorldPosition(Tile tile)
        {
            var local = Layout.LocalPosition(tile.index);
            return WorldTerrain.OnGround(transform.position + new Vector3(local.x, 0, local.y));
        }

        public void SaveTiles()
        {
            using var marker = EcoProfilerMarkers.SaveTiles.Auto();

            // Overwrite the chunk's existing save array; readers copy what they need straight away
            if (!SaveManager.Chunks.TryGetValue(Position, out var save) || save.chunk != Definition.Id ||
                save.tiles == null || save.tiles.Length != tiles.Length)
            {
                save = new ChunkSaveData { chunk = Definition.Id, tiles = new TileData[tiles.Length] };
            }

            for (var i = 0; i < tiles.Length; i++)
            {
                save.tiles[i] = tiles[i].GetSaveData();
            }

            SaveManager.SaveChunk(Position, save);
        }

        public void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
            if (tiles == null)
            {
                return;
            }

            foreach (var t in tiles)
            {
                t.Clear();
            }
        }
    }
}
