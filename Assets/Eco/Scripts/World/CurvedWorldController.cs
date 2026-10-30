using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.World
{
    /// <summary>Shared visual deformation only; physics and navigation stay in the original world.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CurvedWorldController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private WorldVisualSettings settings;
        // Retain values in older scenes until they are assigned the shared settings asset.
        [SerializeField, HideInInspector] private float curvature = 0.012f;
        [SerializeField, HideInInspector] private float flatRadius = 6;
        [SerializeField] private bool previewInEditMode = true;

        private static readonly int BendId = Shader.PropertyToID("_EcoWorldBend");
        private static readonly int PreviousBendId = Shader.PropertyToID("_EcoWorldBendPrevious");
        private static readonly int TerrainVariationId = Shader.PropertyToID("_EcoTerrainVariation");
        private static readonly int SoilDetailId = Shader.PropertyToID("_EcoSoilDetail");
        private static readonly int SoilDetailMapId = Shader.PropertyToID("_EcoSoilDetailMap");
        private static readonly int SoilLayerMaskId = Shader.PropertyToID("_EcoSoilLayerMask");
        private Vector4 _current;
        private Vector4 _previous;
        private int _lastFrame = -1;
        private WorldVisualSettings _subscribedSettings;

        public static CurvedWorldController Active { get; private set; }
        public Transform FollowTarget { get => followTarget; set => followTarget = value; }
        public float Curvature => settings != null ? settings.Curvature : curvature;
        public float FlatRadius => settings != null ? settings.FlatRadius : flatRadius;
        public Vector4 BendParameters => _current;
        public WorldVisualSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                if (isActiveAndEnabled)
                {
                    SubscribeSettings();
                    Publish();
                }
            }
        }

        public Vector3 BendPosition(Vector3 worldPosition)
        {
            var origin = followTarget != null ? followTarget.position : transform.position;
            float distance = Mathf.Max(0, Vector2.Distance(new Vector2(worldPosition.x, worldPosition.z),
                new Vector2(origin.x, origin.z)) - FlatRadius);
            worldPosition.y -= Curvature * distance * distance;
            return worldPosition;
        }

        private Vector3 UnbendPosition(Vector3 renderedPosition)
        {
            // The bend leaves XZ unchanged, so its inverse is exact.
            float drop = renderedPosition.y - BendPosition(renderedPosition).y;
            renderedPosition.y += drop;
            return renderedPosition;
        }

        /// <summary>Map a screen ray back to the flat physics world for click-to-move.</summary>
        public bool RaycastGround(Ray renderedRay, out RaycastHit hit, float maxDistance, int layerMask)
        {
            if (Curvature <= 0)
                return Physics.Raycast(renderedRay, out hit, maxDistance, layerMask);

            // The inverse ray is curved. Short line segments retain the first visible intersection.
            // At the maximum curvature, 1-unit segments deviate by at most about 1 cm.
            int steps = Mathf.Max(1, Mathf.CeilToInt(maxDistance));
            Vector3 previous = UnbendPosition(renderedRay.origin);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = UnbendPosition(renderedRay.GetPoint(maxDistance * i / steps));
                if (Physics.Linecast(previous, next, out hit, layerMask)) return true;
                previous = next;
            }
            hit = default;
            return false;
        }

        private void OnEnable()
        {
            Active = this;
            SubscribeSettings();
            _current = ReadSettings();
            _previous = _current;
            RenderPipelineManager.beginFrameRendering += BeforeRendering;
            Publish();
        }

        private Vector4 ReadSettings()
        {
            var origin = followTarget != null ? followTarget.position : transform.position;
            return new Vector4(origin.x, origin.z,
                Application.isPlaying || previewInEditMode ? Curvature : 0, FlatRadius);
        }

        private void LateUpdate() => Publish();
        private void BeforeRendering(ScriptableRenderContext context, Camera[] cameras) => Publish();

        private void Publish()
        {
            if (_lastFrame != Time.frameCount)
            {
                _previous = _current;
                _lastFrame = Time.frameCount;
            }
            _current = ReadSettings();
            Shader.SetGlobalVector(BendId, _current);
            Shader.SetGlobalVector(PreviousBendId, Application.isPlaying ? _previous : _current);
            PublishTerrainVariation(settings != null ? settings.TerrainVariation : Vector4.zero);
            Shader.SetGlobalVector(SoilDetailId, settings != null ? settings.SoilDetail : new Vector4(0, 0, 1, 1));
            Shader.SetGlobalTexture(SoilDetailMapId, settings != null ? settings.SoilDetailTexture : null);
            Shader.SetGlobalVector(SoilLayerMaskId, settings != null ? settings.SoilLayerMask : Vector4.zero);
        }

        private static void PublishTerrainVariation(Vector4 variation)
        {
            var previous = Shader.GetGlobalVector(TerrainVariationId);
            if (previous == variation) return;
            Shader.SetGlobalVector(TerrainVariationId, variation);
            // Distant terrain caches the layer samples. Rebuild only when anti-tiling changes;
            // macro colour is evaluated in world space in both near and distant passes.
            if (previous.x == variation.x) return;
            foreach (var terrain in Terrain.activeTerrains)
                if (terrain.terrainData != null) terrain.terrainData.SetBaseMapDirty();
        }

        private void OnValidate()
        {
            curvature = Mathf.Clamp(curvature, 0, 0.04f);
            flatRadius = Mathf.Max(0, flatRadius);
            if (isActiveAndEnabled)
            {
                SubscribeSettings();
                Publish();
            }
        }

        private void SubscribeSettings()
        {
            if (_subscribedSettings != null) _subscribedSettings.Changed -= Publish;
            _subscribedSettings = settings;
            if (_subscribedSettings != null) _subscribedSettings.Changed += Publish;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= BeforeRendering;
            if (_subscribedSettings != null) _subscribedSettings.Changed -= Publish;
            _subscribedSettings = null;
            if (Active != this) return;
            Active = null;
            Shader.SetGlobalVector(BendId, Vector4.zero);
            Shader.SetGlobalVector(PreviousBendId, Vector4.zero);
            PublishTerrainVariation(Vector4.zero);
            Shader.SetGlobalVector(SoilDetailId, new Vector4(0, 0, 1, 1));
            Shader.SetGlobalTexture(SoilDetailMapId, null);
            Shader.SetGlobalVector(SoilLayerMaskId, Vector4.zero);
        }
    }
}
