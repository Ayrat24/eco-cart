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
        // Tiles less than this above the water count as under it: the NavMesh stops just above the water, so
        // nothing placed there could be reached
        private const float DryHeight = 0.2f;

        // The water surface shows when the terrain comes this close to the water anywhere in the chunk
        private const float WaterSurfaceMargin = 0.3f;

        // Metres between the samples taken along the hex outline
        private const float OutlineSampleSpacing = 1f;

        protected Tile[] tiles;

        protected SaveManager SaveManager;
        protected Vector2Int Position;

        private WaterSurface _waterSurface;

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

        protected virtual void Awake()
        {
            _waterSurface = GetComponentInChildren<WaterSurface>(true);
        }

        /// <summary>
        /// Marks tiles under (or right at) the water as water ground and the rest as dry, and shows the water
        /// surface when the baked terrain dips under the water anywhere in the hex. Shorelines don't follow the
        /// hexes, so this decides per chunk instead of per definition.
        /// </summary>
        protected void UpdateWater(WorldData world, bool alwaysShowWater)
        {
            float waterLevel = world.WaterLevel;
            var center = transform.position;
            bool wet = alwaysShowWater;

            foreach (var tile in tiles)
            {
                var local = Layout.LocalPosition(tile.index);
                float height = WorldTerrain.GroundHeight(center + new Vector3(local.x, 0, local.y));
                if (height < waterLevel + DryHeight)
                {
                    tile.groundType = TileGroundType.Water;
                }
                else if (tile.groundType == TileGroundType.Water)
                {
                    tile.groundType = TileGroundType.Ground;
                }

                wet |= height < waterLevel + WaterSurfaceMargin;
            }

            // The tiles stop short of the hex edge, so check the outline as well
            int steps = Mathf.Max(1, Mathf.CeilToInt(Layout.Radius / OutlineSampleSpacing));
            for (int corner = 0; corner < 6 && !wet; corner++)
            {
                var a = Hex.Corner(corner, Layout.Radius);
                var b = Hex.Corner((corner + 1) % 6, Layout.Radius);
                for (int step = 0; step < steps && !wet; step++)
                {
                    var p = Vector2.Lerp(a, b, step / (float)steps);
                    float height = WorldTerrain.GroundHeight(center + new Vector3(p.x, 0, p.y));
                    wet = height < waterLevel + WaterSurfaceMargin;
                }
            }

            if (_waterSurface == null)
            {
                return;
            }

            if (wet)
            {
                _waterSurface.Show(world);
            }
            else
            {
                _waterSurface.Hide();
            }
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
