using System;
using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trees;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Eco.Scripts.UI
{
    /// <summary>Render-only tree meshes and a terrain-following clearance disc; never enters gameplay pools.</summary>
    public sealed class TreePlacementPreview : IDisposable
    {
        private const int Segments = 64;
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        private readonly Material _template;
        private GameObject _root;
        private Material _treeMaterial;
        private Material _groundMaterial;
        private Mesh _groundMesh;
        private Vector3[] _vertices;
        private TreeDefinition _definition;
        private Vector3 _position;
        private bool _hasPosition;

        public TreePlacementPreview(Material template)
        {
            _template = template;
        }

        public void Show(TreeDefinition definition, Vector3 position, bool valid)
        {
            if (_definition != definition || _root == null) Create(definition);
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.position = position;
            var color = valid ? new Color(0.2f, 0.85f, 0.45f, 0.3f) : new Color(1f, 0.15f, 0.1f, 0.3f);
            _treeMaterial.SetColor(Tint, color);
            color.a = 1f;
            _groundMaterial.SetColor(Tint, color);
            if (!_hasPosition || position != _position)
            {
                _position = position;
                _hasPosition = true;
                UpdateGround(position, definition.PlacementRadius);
            }
        }

        private void Create(TreeDefinition definition)
        {
            Dispose();
            var prefab = PoolManager.Instance.GetTreePrefab(definition.PrefabId);
            if (prefab == null || _template == null) return;
            _definition = definition;
            _root = new GameObject("Tree placement preview") { layer = 2 };
            _treeMaterial = new Material(_template);
            _groundMaterial = new Material(_template);
            _groundMaterial.SetFloat("_VertexColors", 1f);
            _groundMaterial.SetFloat("_GroundOverlay", 1f);
            _groundMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            _groundMaterial.SetInt("_Cull", (int)CullMode.Off);
            _groundMaterial.renderQueue = 3100;
            var lowerLods = new HashSet<Renderer>();
            foreach (var group in prefab.Model.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers) lowerLods.Add(renderer);
                }
                if (lods.Length > 0)
                {
                    foreach (var renderer in lods[0].renderers) lowerLods.Remove(renderer);
                }
            }
            // Copy mesh data only: no colliders, scripts, particles, NavMesh obstacles or pool reservations.
            foreach (var filter in prefab.Model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent<MeshRenderer>(out var source)) continue;
                if (lowerLods.Contains(source)) continue;
                var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
                part.transform.SetParent(_root.transform, false);
                var matrix = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                part.transform.localPosition = matrix.GetColumn(3);
                part.transform.localRotation = matrix.rotation;
                part.transform.localScale = matrix.lossyScale;
                part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = part.GetComponent<MeshRenderer>();
                Configure(renderer);
                var materials = new Material[filter.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = _treeMaterial;
                renderer.sharedMaterials = materials;
                var originals = source.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (i >= originals.Length || originals[i] == null) continue;
                    var original = originals[i];
                    string textureName = original.HasProperty("_BaseTexture") ? "_BaseTexture"
                        : original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                    if (!original.HasProperty(textureName)) continue;
                    var block = new MaterialPropertyBlock();
                    var texture = original.GetTexture(textureName);
                    if (texture != null) block.SetTexture("_BaseMap", texture);
                    var scale = original.GetTextureScale(textureName);
                    var offset = original.GetTextureOffset(textureName);
                    block.SetVector("_BaseMap_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
                    renderer.SetPropertyBlock(block, i);
                }
                // Curved shaders can displace geometry well outside its original local bounds.
                renderer.localBounds = new Bounds(filter.sharedMesh.bounds.center, Vector3.one * 1000f);
            }
            var ground = new GameObject("Placement clearance", typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
            ground.transform.SetParent(_root.transform, false);
            _groundMesh = new Mesh { name = "Placement clearance disc" };
            _groundMesh.MarkDynamic();
            _vertices = new Vector3[1 + Segments * 2];
            var colors = new Color[_vertices.Length];
            colors[0] = new Color(1, 1, 1, 0.13f);
            var indices = new int[Segments * 9];
            for (int i = 0; i < Segments; i++)
            {
                int inner = 1 + i * 2;
                int outer = inner + 1;
                int next = 1 + ((i + 1) % Segments) * 2;
                colors[inner] = new Color(1, 1, 1, 0.25f);
                colors[outer] = new Color(1, 1, 1, 0.95f);
                int t = i * 9;
                indices[t] = 0; indices[t + 1] = inner; indices[t + 2] = next;
                indices[t + 3] = inner; indices[t + 4] = outer; indices[t + 5] = next;
                indices[t + 6] = outer; indices[t + 7] = next + 1; indices[t + 8] = next;
            }
            _groundMesh.vertices = _vertices;
            _groundMesh.colors = colors;
            _groundMesh.triangles = indices;
            ground.GetComponent<MeshFilter>().sharedMesh = _groundMesh;
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            Configure(groundRenderer);
            groundRenderer.sharedMaterial = _groundMaterial;
        }

        private void UpdateGround(Vector3 center, float radius)
        {
            _vertices[0] = Vector3.up * 0.06f;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (int edge = 0; edge < 2; edge++)
                {
                    var point = center + direction * (edge == 0 ? radius * 0.94f : radius);
                    point.y = WorldTerrain.GroundHeight(point) + 0.06f;
                    _vertices[1 + i * 2 + edge] = point - center;
                }
            }
            _groundMesh.vertices = _vertices;
            _groundMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        }

        private static void Configure(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Dispose()
        {
            if (_root != null) { _root.SetActive(false); Object.Destroy(_root); }
            if (_groundMesh != null) Object.Destroy(_groundMesh);
            if (_treeMaterial != null) Object.Destroy(_treeMaterial);
            if (_groundMaterial != null) Object.Destroy(_groundMaterial);
            _root = null;
            _definition = null;
            _hasPosition = false;
        }
    }
}
