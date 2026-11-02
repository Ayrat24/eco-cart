using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Eco/Tree Growth Settings")]
    public sealed class TreeGrowthSettings : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float duration = 1.2f;
        [SerializeField] private AnimationCurve height = new(
            new Keyframe(0f, 0.02f), new Keyframe(0.55f, 0.92f),
            new Keyframe(0.75f, 1.045f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve width = new(
            new Keyframe(0f, 0.08f), new Keyframe(0.3f, 0.22f),
            new Keyframe(0.75f, 1.025f), new Keyframe(1f, 1f));
        [SerializeField, Range(0f, 10f)] private float swayDegrees = 2.5f;

        public float Duration => Mathf.Max(0.1f, duration);
        public Vector3 ScaleAt(float progress) => new(
            Mathf.Max(0.001f, width.Evaluate(progress)),
            Mathf.Max(0.001f, height.Evaluate(progress)),
            Mathf.Max(0.001f, width.Evaluate(progress)));
        public float SwayAt(float progress) => Mathf.Sin(progress * Mathf.PI * 3f) *
            Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 2f) * swayDegrees;
    }
}
