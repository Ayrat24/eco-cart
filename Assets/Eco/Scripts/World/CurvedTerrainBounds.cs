using UnityEngine;
using Eco.Scripts.Profiling;

namespace Eco.Scripts.World
{
    /// <summary>
    /// Terrain counterpart of <see cref="CurvedWorldBounds"/>. Unity culls each terrain patch by its flat heightmap
    /// bounds, so patches the bend pulls into view would vanish in squares at the screen edges. This lowers every
    /// patch's culling bounds by the bend's drop across it, and culls patches beyond the camera's horizon, which the
    /// bent bounds would otherwise bring back into the frustum.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class CurvedTerrainBounds : MonoBehaviour
    {
        // Overriding the bounds makes Unity redo its patch data (about a millisecond), so they hold for the bend
        // origin and camera anywhere within Margin of where they were built, and are rebuilt after RefreshDistance.
        // The difference covers the camera moving after this LateUpdate.
        private const float Margin = 5;
        private const float RefreshDistance = 4;
        // Patches behind the horizon get empty bounds this far below the terrain: beyond every camera's far plane
        private const float HiddenDepth = 100000;

        private Terrain _terrain;
        private TerrainData _data;
        private PatchExtents[] _original;
        private PatchExtents[] _bent;
        private Vector3 _builtOrigin;
        private Vector3 _builtCamera;
        private Vector2 _builtSettings;
        private bool _valid;

        private void OnEnable()
        {
            _terrain = GetComponent<Terrain>();
            LateUpdate();
        }

        private void LateUpdate()
        {
            using var marker = EcoProfilerMarkers.CurvedTerrainBounds.Auto();
            var controller = CurvedWorldController.Active;
            var data = _terrain.terrainData;
            // Only the play-mode copy is changed: the overrides would otherwise stay on the TerrainData asset
            if (controller == null || !TerrainPainter.IsRuntimeCopy(data))
            {
                Restore();
                return;
            }
            if (data != _data)
            {
                Restore();
                _data = data;
                _original = data.GetPatchMinMaxHeights();
                _bent = new PatchExtents[_original.Length];
            }

            var origin = controller.FollowTarget != null ? controller.FollowTarget.position : controller.transform.position;
            origin.y = 0;
            var mainCamera = Camera.main;
            var cameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
            var settings = new Vector2(controller.Curvature, controller.FlatRadius);
            if (_valid && settings == _builtSettings && Vector3.Distance(origin, _builtOrigin) < RefreshDistance &&
                Vector3.Distance(cameraPosition, _builtCamera) < RefreshDistance) return;

            Vector3 position = _terrain.GetPosition();
            Vector3 size = data.size;
            float toNormalized = 1f / size.y;
            // The root patch, stored last, spans the whole terrain
            var root = _original[_original.Length - 1];
            float lowest = position.y + root.min * size.y;
            float highest = position.y + root.max * size.y;
            // The largest horizon while the camera and the origin each stay within Margin
            float cameraOffset = Vector2.Distance(new Vector2(cameraPosition.x, cameraPosition.z),
                new Vector2(origin.x, origin.z)) + 2 * Margin;
            float horizon = mainCamera != null
                ? HorizonDistance(cameraOffset, cameraPosition.y + Margin - lowest, highest - lowest,
                    controller.Curvature, controller.FlatRadius)
                : float.PositiveInfinity;
            float hidden = (lowest - position.y - HiddenDepth) * toNormalized;

            // The quadtree is stored finest level first, each level row-major (x + z * n), down to the single root
            int finest = 1;
            for (int count = 1; count < _original.Length; count += finest * finest) finest *= 2;
            int index = 0;
            for (int n = finest; n >= 1; n /= 2)
            {
                float cellX = size.x / n;
                float cellZ = size.z / n;
                for (int z = 0; z < n; z++)
                {
                    float z0 = position.z + z * cellZ - origin.z;
                    float z1 = z0 + cellZ;
                    float nearZ = Mathf.Max(0, Mathf.Max(z0, -z1));
                    float farZ = Mathf.Max(Mathf.Abs(z0), Mathf.Abs(z1));
                    for (int x = 0; x < n; x++, index++)
                    {
                        float x0 = position.x + x * cellX - origin.x;
                        float x1 = x0 + cellX;
                        float nearX = Mathf.Max(0, Mathf.Max(x0, -x1));
                        float farX = Mathf.Max(Mathf.Abs(x0), Mathf.Abs(x1));
                        float near = Mathf.Sqrt(nearX * nearX + nearZ * nearZ) - Margin;
                        if (near >= horizon)
                        {
                            _bent[index].min = hidden;
                            _bent[index].max = hidden;
                            continue;
                        }
                        // Only the part inside the horizon can be seen, so the drop past it doesn't matter. A parent
                        // patch's bounds still contain its visible children's.
                        float far = Mathf.Min(Mathf.Sqrt(farX * farX + farZ * farZ) + Margin, horizon);
                        near = Mathf.Max(0, near - controller.FlatRadius);
                        far = Mathf.Max(0, far - controller.FlatRadius);
                        _bent[index].min = _original[index].min - controller.Curvature * far * far * toNormalized;
                        _bent[index].max = _original[index].max - controller.Curvature * near * near * toNormalized;
                    }
                }
            }
            data.OverrideMinMaxPatchHeights(_bent);
            _builtOrigin = origin;
            _builtCamera = cameraPosition;
            _builtSettings = settings;
            _valid = true;
        }

        /// <summary>
        /// Distance from the bend origin past which terrain can't be seen from a camera
        /// <paramref name="cameraOffset"/> away from it and <paramref name="cameraHeight"/> above the lowest terrain:
        /// the sight line would pass under the bent terrain, which is never lower than its lowest point bent by the
        /// same parabola. <paramref name="terrainHeight"/> is the terrain's full height range.
        /// </summary>
        private static float HorizonDistance(float cameraOffset, float cameraHeight, float terrainHeight,
            float curvature, float flatRadius)
        {
            if (curvature <= 0) return float.PositiveInfinity;
            float beyondFlat = cameraOffset - flatRadius;
            return cameraOffset + Mathf.Sqrt(Mathf.Max(0, cameraHeight) / curvature + beyondFlat * beyondFlat) +
                   Mathf.Sqrt(terrainHeight / curvature);
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (_valid && _data != null) _data.OverrideMinMaxPatchHeights(_original);
            _valid = false;
        }
    }
}
