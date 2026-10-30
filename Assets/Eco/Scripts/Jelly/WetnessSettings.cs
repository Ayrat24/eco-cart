using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Tuning for how the jelly soaks up water and spends it on its slime trail (see <see cref="Wetness"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Wetness Settings", fileName = "WetnessSettings")]
    public class WetnessSettings : ScriptableObject
    {
        [Tooltip("Seconds in the water that fill the bar from empty.")]
        [SerializeField, Min(0.1f)] private float fillSeconds = 3f;
        [Tooltip("Metres of trail a full bar lays.")]
        [SerializeField, Min(1f)] private float trailLength = 100f;

        public float FillSeconds => fillSeconds;
        public float TrailLength => trailLength;
    }
}
