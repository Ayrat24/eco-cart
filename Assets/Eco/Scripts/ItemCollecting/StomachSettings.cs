using UnityEngine;

namespace Eco.Scripts.ItemCollecting
{
    /// <summary>
    /// Tuning for the jelly's belly (see <see cref="Stomach"/>): how big swallowed items stay, how much the body
    /// grows to hold them and how they float inside. How much the belly holds comes from the
    /// MaxWeight stat (BaseStats plus upgrades); the spring the body grows with is on JellyBody.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Stomach Settings", fileName = "StomachSettings")]
    public class StomachSettings : ScriptableObject
    {
        [Header("Swallowing")]
        [Tooltip("Swallowed items keep this share of their actual size.")]
        [SerializeField, Range(0.1f, 1f)] private float itemScale = 0.85f;
        [Tooltip("How fast items drop in through the mouth, m/s.")]
        [SerializeField, Min(0f)] private float entrySpeed = 1.5f;

        [Header("Growth")]
        [Tooltip("Body volume added per cubic metre of swallowed item (its bounding box at swallowed size). " +
                 "Items only float in the middle of the body, so values above 1 are needed to hold them inside.")]
        [SerializeField, Min(0f)] private float growthPerItemVolume = 2.5f;
        [Tooltip("The body never grows past this many times its normal size.")]
        [SerializeField, Min(1f)] private float maxGrowth = 2f;

        [Header("Floating")]
        [Tooltip("Centre of the space items float in, as a share of the body height.")]
        [SerializeField, Range(0f, 1f)] private float cavityHeight = 0.42f;
        [Tooltip("Size of that space: x/z as a share of the body radius there, y as a share of the body height.")]
        [SerializeField] private Vector3 cavitySize = new(0.72f, 0.3f, 0.72f);
        [Tooltip("Net pull down through the goo, m/s².")]
        [SerializeField] private float sinking = 0.8f;
        [Tooltip("Share of velocity lost per second.")]
        [SerializeField, Min(0f)] private float viscosity = 3f;
        [SerializeField, Min(0f)] private float wallStiffness = 40f;
        [SerializeField, Min(0f)] private float separationStiffness = 25f;
        [Tooltip("Share of the body's acceleration items feel, so they slosh when it starts and stops.")]
        [SerializeField, Min(0f)] private float slosh = 0.5f;
        [SerializeField, Min(0f)] private float drift = 0.4f;
        [Tooltip("Degrees per second items tumble at.")]
        [SerializeField] private float tumble = 40f;

        public float ItemScale => itemScale;
        public float EntrySpeed => entrySpeed;
        public float GrowthPerItemVolume => growthPerItemVolume;
        public float MaxGrowth => maxGrowth;
        public float CavityHeight => cavityHeight;
        public Vector3 CavitySize => cavitySize;
        public float Sinking => sinking;
        public float Viscosity => viscosity;
        public float WallStiffness => wallStiffness;
        public float SeparationStiffness => separationStiffness;
        public float Slosh => slosh;
        public float Drift => drift;
        public float Tumble => tumble;
    }
}
