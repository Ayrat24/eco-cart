using System.Runtime.InteropServices;
using Eco.Scripts.Profiling;
using Eco.Scripts.World;
using R3;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// The slime the jelly leaves on bare ground as it moves, and the grass that grows where it went. Every
    /// <see cref="SlimeTrailSettings.PointSpacing"/> metres it checks the ground under the jelly: over grass, near
    /// water (or with <see cref="CanPaint"/> off) it lays nothing and the strip ends; over bare ground it adds a point,
    /// reports the metres in <see cref="OnPainted"/>, and gives the point to <see cref="TerrainPainter.GrowGrass"/>,
    /// which claims the ground around it at once and paints the grass in a batch before it starts growing. Ground
    /// claimed by the last few points still counts as bare, since the claimed ring reaches ahead of the jelly.
    /// The trail is one strip mesh in world space; each vertex carries when its point was laid and when it dries
    /// up, so the <c>Eco/SlimeTrail</c> shader fades it and the mesh only changes when a point is added or
    /// dropped. A jump longer than <see cref="SlimeTrailSettings.MaxGap"/> (a spawn or teleport) starts a new
    /// strip. Lives on its own object at the world origin, not under the player.
    /// </summary>
    public class SlimeTrail : MonoBehaviour
    {
        [SerializeField] private SlimeTrailSettings settings;
        [SerializeField] private WorldData world;
        [SerializeField] private JellyBody body;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        /// <summary>Off, the jelly lays no slime and grows no grass, e.g. while it has no paint left.</summary>
        public bool CanPaint { get; set; } = true;

        /// <summary>Multiplies <see cref="SlimeTrailSettings.Width"/>; the trail width upgrade raises it.</summary>
        public float WidthScale { get; set; } = 1f;

        /// <summary>Whether the jelly's last step laid slime. Stays as it was while it stands still.</summary>
        public bool IsPainting { get; private set; }

        /// <summary>Metres of slime laid, once per step that laid some.</summary>
        public readonly Subject<float> OnPainted = new();

        private struct TrailPoint
        {
            public Vector3 Center;
            public Vector3 Direction;
            public float HalfWidth;
            public float Along;
            public float Birth;
            public bool StartsStrip;
            public Vector3 Left;
            public Vector3 Right;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Position;
            // x = across (-1 left edge, 1 right edge), y = metres along the trail, z = laid at, w = dries up at
            public Vector4 Data;
        }

        private TrailPoint[] _points;
        private int _first;
        private int _count;
        private Vector3 _head;
        private bool _hasHead;
        private bool _stripPending;
        private float _along;
        private bool _dirty;
        // Claim of the last point given to TerrainPainter.GrowGrass; every point gets the next one
        private int _claim;

        private Mesh _mesh;
        private Material _material;
        private NativeArray<Vertex> _vertices;
        private NativeArray<ushort> _indices;

        private void Awake()
        {
            _material = meshRenderer.material;
            settings.ApplyLook(_material);

            int capacity = Mathf.CeilToInt(settings.Lifetime * settings.MaxTrailSpeed / settings.PointSpacing) + 2;
            capacity = Mathf.Min(capacity, ushort.MaxValue / 2);
            _points = new TrailPoint[capacity];
            _vertices = new NativeArray<Vertex>(capacity * 2, Allocator.Persistent);
            _indices = new NativeArray<ushort>((capacity - 1) * 6, Allocator.Persistent);

            _mesh = new Mesh { name = "Slime Trail" };
            _mesh.MarkDynamic();
            _mesh.SetVertexBufferParams(_vertices.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4));
            _mesh.SetIndexBufferParams(_indices.Length, IndexFormat.UInt16);
            _mesh.subMeshCount = 1;
            meshFilter.sharedMesh = _mesh;
            meshRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            using var marker = EcoProfilerMarkers.SlimeTrail.Auto();

#if UNITY_EDITOR
            // Settings tweaked in play mode show at once
            settings.ApplyLook(_material);
#endif

            float now = Time.time;
            DropDriedPoints(now);
            Follow(body.transform.position, now);

            if (_dirty)
            {
                _dirty = false;
                RebuildMesh();
            }
        }

        private void Follow(Vector3 position, float now)
        {
            float dx = position.x - _head.x;
            float dz = position.z - _head.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            // A new strip starts where the jump landed, once the jelly moves on from there
            if (!_hasHead || distance > settings.MaxGap)
            {
                _head = position;
                _hasHead = true;
                _stripPending = true;
                IsPainting = false;
                return;
            }

            if (distance < settings.PointSpacing)
            {
                return;
            }

            var direction = new Vector3(dx / distance, 0f, dz / distance);
            _along += distance;
            IsPainting = CanPaint && !TerrainPainter.IsGrass(position, _claim - RecentClaims() + 1) &&
                         !IsNearWater(position);
            if (IsPainting)
            {
                if (_stripPending)
                {
                    _stripPending = false;
                    AddPoint(_head, direction, now, true);
                }

                AddPoint(position, direction, now, false);
                OnPainted.OnNext(distance);
            }
            else
            {
                // The strip ends here and starts again where the bare ground does
                _stripPending = true;
            }

            _head = position;
        }

        // How many of the latest points can have claimed the ground under the jelly on its way: those within the
        // widest claimed radius (plus a step) behind it
        private int RecentClaims()
        {
            return Mathf.CeilToInt(WidestClaim() / settings.PointSpacing) + 2;
        }

        // Half the trail's width right now, before it wobbles
        private float BaseHalfWidth()
        {
            return 0.5f * settings.Width * WidthScale * body.WorldScale;
        }

        // Metres from a point's centre that its slime and grass can reach
        private float WidestClaim()
        {
            return BaseHalfWidth() * (1f + settings.WidthWobble) *
                   Mathf.Max(1f, settings.GrassTextureWidth, settings.GrassDetailWidth);
        }

        // Measured from the highest tide, so the water never comes up over the slime or the grass it grows
        private bool IsNearWater(Vector3 position)
        {
            return WorldTerrain.DipsBelow(position, world.WaterLevel + world.TideHeight,
                WidestClaim() + settings.WaterClearance);
        }

        private void AddPoint(Vector3 center, Vector3 direction, float now, bool startsStrip)
        {
            float wobble = (Mathf.PerlinNoise1D(_along * 0.6f) - 0.5f) * 2f * settings.WidthWobble;
            var point = new TrailPoint
            {
                Center = center,
                Direction = direction,
                HalfWidth = BaseHalfWidth() * (1f + wobble),
                Along = _along,
                Birth = now,
                StartsStrip = startsStrip
            };
            PlaceEdges(ref point, direction);

            // Mitre the previous point between its way in and this way out, so corners don't pinch or overlap
            if (!startsStrip && _count > 0)
            {
                ref var previous = ref _points[(_first + _count - 1) % _points.Length];
                var mitre = previous.Direction + direction;
                PlaceEdges(ref previous, mitre.sqrMagnitude > 0.01f ? mitre.normalized : direction);
            }

            if (_count == _points.Length)
            {
                _first = (_first + 1) % _points.Length;
                _count--;
            }

            _points[(_first + _count) % _points.Length] = point;
            _count++;
            _dirty = true;

            _claim++;
            TerrainPainter.GrowGrass(center, point.HalfWidth * settings.GrassDetailWidth,
                point.HalfWidth * settings.GrassTextureWidth, now + settings.GrowDelay, settings.GrowDuration, _claim);
        }

        private void PlaceEdges(ref TrailPoint point, Vector3 direction)
        {
            var right = new Vector3(direction.z, 0f, -direction.x) * point.HalfWidth;
            point.Left = OnGround(point.Center - right);
            point.Right = OnGround(point.Center + right);
        }

        private Vector3 OnGround(Vector3 position)
        {
            position.y = WorldTerrain.GroundHeight(position) + settings.GroundOffset;
            return position;
        }

        private void DropDriedPoints(float now)
        {
            while (_count > 0 && now - _points[_first].Birth >= settings.Lifetime)
            {
                _first = (_first + 1) % _points.Length;
                _count--;
                _dirty = true;
            }
        }

        private void RebuildMesh()
        {
            int vertexCount = _count * 2;
            int indexCount = 0;
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < _count; i++)
            {
                var point = _points[(_first + i) % _points.Length];
                float driesAt = point.Birth + settings.Lifetime;
                int v = i * 2;
                _vertices[v] = new Vertex
                {
                    Position = point.Left, Data = new Vector4(-1f, point.Along, point.Birth, driesAt)
                };
                _vertices[v + 1] = new Vertex
                {
                    Position = point.Right, Data = new Vector4(1f, point.Along, point.Birth, driesAt)
                };
                min = Vector3.Min(min, Vector3.Min(point.Left, point.Right));
                max = Vector3.Max(max, Vector3.Max(point.Left, point.Right));

                if (i == 0 || point.StartsStrip)
                {
                    continue;
                }

                // Clockwise seen from above: left/right of the previous point, then of this one
                _indices[indexCount++] = (ushort)(v - 2);
                _indices[indexCount++] = (ushort)v;
                _indices[indexCount++] = (ushort)(v - 1);
                _indices[indexCount++] = (ushort)(v - 1);
                _indices[indexCount++] = (ushort)v;
                _indices[indexCount++] = (ushort)(v + 1);
            }

            meshRenderer.enabled = indexCount > 0;
            if (indexCount == 0)
            {
                return;
            }

            const MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;
            _mesh.SetVertexBufferData(_vertices, 0, 0, vertexCount, 0, flags);
            _mesh.SetIndexBufferData(_indices, 0, 0, indexCount, flags);
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), flags);
            // CurvedWorldBounds bends these for culling
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            _mesh.bounds = bounds;
        }

        private void OnDestroy()
        {
            OnPainted.Dispose();
            if (_vertices.IsCreated) _vertices.Dispose();
            if (_indices.IsCreated) _indices.Dispose();
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }
    }
}
