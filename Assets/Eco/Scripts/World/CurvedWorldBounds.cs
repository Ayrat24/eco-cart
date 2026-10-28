using UnityEngine;
using Eco.Scripts.Profiling;

namespace Eco.Scripts.World
{
    /// <summary>Analytical world bounds for the parabolic bend, including moving and pooled scrap.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class CurvedWorldBounds : MonoBehaviour
    {
        private Renderer[] _renderers;
        private Bounds[] _original;
        private Matrix4x4[] _matrices;
        private MeshFilter[] _meshFilters;
        private Vector4 _lastSettings;
        private bool _valid;

        private void OnEnable()
        {
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
                _original = new Bounds[_renderers.Length];
                _matrices = new Matrix4x4[_renderers.Length];
                _meshFilters = new MeshFilter[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++) _original[i] = _renderers[i].localBounds;
                for (int i = 0; i < _renderers.Length; i++) _meshFilters[i] = _renderers[i].GetComponent<MeshFilter>();
            }
            _valid = false;
            LateUpdate();
        }

        private void LateUpdate()
        {
            using var marker = EcoProfilerMarkers.CurvedBounds.Auto();
            var controller = CurvedWorldController.Active;
            if (controller == null)
            {
                if (_valid) ResetBounds();
                return;
            }
            var origin = controller.FollowTarget != null ? controller.FollowTarget.position : controller.transform.position;
            var settings = new Vector4(origin.x, origin.z, controller.Curvature, controller.FlatRadius);
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))) continue;
                var matrix = renderer.localToWorldMatrix;
                // Water volumes regenerate their mesh after spawning or changing shoreline foam.
                var mesh = _meshFilters[i] != null ? _meshFilters[i].sharedMesh : null;
                var localBounds = mesh != null ? mesh.bounds : _original[i];
                bool meshBoundsChanged = localBounds != _original[i];
                _original[i] = localBounds;
                if (_valid && !meshBoundsChanged && settings == _lastSettings && matrix == _matrices[i]) continue;
                _matrices[i] = matrix;
                Vector3 center = matrix.MultiplyPoint3x4(_original[i].center);
                Vector3 extents = Abs(matrix.MultiplyVector(Vector3.right * _original[i].extents.x)) +
                                  Abs(matrix.MultiplyVector(Vector3.up * _original[i].extents.y)) +
                                  Abs(matrix.MultiplyVector(Vector3.forward * _original[i].extents.z));
                float dx = Mathf.Abs(center.x - origin.x);
                float dz = Mathf.Abs(center.z - origin.z);
                float nearX = Mathf.Max(0, dx - extents.x);
                float nearZ = Mathf.Max(0, dz - extents.z);
                float farX = dx + extents.x;
                float farZ = dz + extents.z;
                float near = Mathf.Max(0, Mathf.Sqrt(nearX * nearX + nearZ * nearZ) - controller.FlatRadius);
                float far = Mathf.Max(0, Mathf.Sqrt(farX * farX + farZ * farZ) - controller.FlatRadius);
                float minDrop = controller.Curvature * near * near;
                float maxDrop = controller.Curvature * far * far;
                center.y -= (minDrop + maxDrop) * 0.5f;
                extents.y += (maxDrop - minDrop) * 0.5f;
                // Allow for the original vegetation wind's vertex displacement.
                extents += Vector3.one;
                renderer.bounds = new Bounds(center, 2 * extents);
            }
            _lastSettings = settings;
            _valid = true;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private void OnDisable() => ResetBounds();

        private void ResetBounds()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].ResetBounds();
            _valid = false;
        }
    }
}
