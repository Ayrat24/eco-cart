using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// One entry of the world palette: how a hex chunk looks (terrain height and texture), what scrap it starts
    /// with, and the prefab spawned for it. Created and edited in <b>Eco → World Editor</b>, which also creates the
    /// prefab (a variant of the land or water base) so unique objects can be added to it.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/World/Chunk Definition", fileName = "ChunkDefinition")]
    public class ChunkDefinition : ScriptableObject
    {
        [Tooltip("Saved with each chunk's tiles. A save made for another definition is discarded on load.")]
        [SerializeField] private string id;
        [SerializeField] private Color color = new(0.55f, 0.75f, 0.35f);
        [SerializeField] private bool isWater;

        [Header("Terrain")]
        [Tooltip("Ground height in metres; 0 is the default ground level. Water chunks use it for the bed.")]
        [SerializeField] private float elevation;
        [Tooltip("Height of random bumps on top of the elevation, in metres.")]
        [SerializeField, Min(0)] private float roughness;
        [SerializeField] private TerrainLayer terrainLayer;

        [Header("Scrap")]
        [SerializeField] private ChunkScrapSettings scrap = new();

        [Header("Prefab")]
        [SerializeField] private Chunk prefab;

        public string Id => id;

        /// <summary>Colour in the World Editor and on the in-game map.</summary>
        public Color Color => color;

        public bool IsWater => isWater;
        public float Elevation => elevation;
        public float Roughness => roughness;
        public TerrainLayer TerrainLayer => terrainLayer;
        public ChunkScrapSettings Scrap => scrap;
        public Chunk Prefab => prefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
