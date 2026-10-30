using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// A flat hexagon of water at <see cref="WorldData.WaterLevel"/> covering its chunk. Every chunk prefab has one;
    /// the chunk shows it when its terrain dips under the water anywhere, so shorelines can wander freely across
    /// chunks. The terrain hides the parts over dry land. The mesh is subdivided so it bends with the curved world,
    /// and neighbouring hexes meet without cracks. Every surface rises and falls with the same slow tide
    /// (<see cref="WorldData.TideHeight"/>), so the waterline creeps up and down the beaches.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class WaterSurface : MonoBehaviour
    {
        // The water shader only animates waves on vertices at object-space height 0.5 and above (the top of a
        // Bitgem water tile), so the mesh sits at 0.5 and the transform half a metre under the water level.
        private const float WaveHeight = 0.5f;

        [Tooltip("Rings of triangles from the centre to the edge. More rings bend more smoothly in the distance.")]
        [SerializeField, Min(1)] private int subdivisions = 6;

        private MeshFilter _filter;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private float _meshRadius;
        private float _waterLevel;
        private float _tideHeight;
        private float _tidePeriod;

        public void Show(WorldData world)
        {
            gameObject.SetActive(true);
            BuildMesh(world.HexRadius);

            _waterLevel = world.WaterLevel;
            _tideHeight = world.TideHeight;
            _tidePeriod = world.TidePeriod;
            FollowTide();
            UpdateUVs();
        }

        private void Update()
        {
            FollowTide();
        }

        // Every surface reads the same clock, so all the water rises and falls together
        private void FollowTide()
        {
            float tide = _tidePeriod > 0f ? _tideHeight * Mathf.Sin(Time.time * 2f * Mathf.PI / _tidePeriod) : 0f;
            var position = transform.position;
            transform.position = new Vector3(position.x, _waterLevel + tide - WaveHeight, position.z);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuildMesh(float radius)
        {
            if (_mesh != null && Mathf.Approximately(_meshRadius, radius))
            {
                return;
            }

            _meshRadius = radius;
            if (_filter == null)
            {
                _filter = GetComponent<MeshFilter>();
            }

            // A hex is six triangles from the centre; each is split into subdivisions² smaller ones. Neighbouring
            // hexes split their shared edge into the same points, so there are no cracks between them.
            int n = subdivisions;
            int verticesPerSector = (n + 1) * (n + 2) / 2;
            var vertices = new Vector3[6 * verticesPerSector];
            var triangles = new int[6 * n * n * 3];
            int v = 0;
            int t = 0;

            for (int sector = 0; sector < 6; sector++)
            {
                var a = Hex.Corner(sector, radius);
                var b = Hex.Corner((sector + 1) % 6, radius);
                int first = v;

                // Row i is i / n of the way from the centre to the edge a-b and holds i + 1 vertices
                for (int i = 0; i <= n; i++)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        var p = i == 0 ? Vector2.zero : (a * (i - j) + b * j) / n;
                        vertices[v++] = new Vector3(p.x, WaveHeight, p.y);
                    }
                }

                for (int i = 0; i < n; i++)
                {
                    int row = first + i * (i + 1) / 2;
                    int next = first + (i + 1) * (i + 2) / 2;
                    for (int j = 0; j <= i; j++)
                    {
                        triangles[t++] = row + j;
                        triangles[t++] = next + j + 1;
                        triangles[t++] = next + j;

                        if (j < i)
                        {
                            triangles[t++] = row + j;
                            triangles[t++] = row + j + 1;
                            triangles[t++] = next + j + 1;
                        }
                    }
                }
            }

            var normals = new Vector3[vertices.Length];
            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = Vector3.up;
                colors[i] = Color.black; // red marks foam; shore foam comes from scene depth instead
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            _vertices = vertices;
            _uvs = new Vector2[vertices.Length];
            _mesh = new Mesh { name = "Hex Water" };
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
        }

        // The water texture is mapped in world XZ like Bitgem's own volumes, so it lines up across hexes
        private void UpdateUVs()
        {
            var origin = transform.position;
            for (int i = 0; i < _vertices.Length; i++)
            {
                _uvs[i] = new Vector2(origin.x + _vertices[i].x, origin.z + _vertices[i].z);
            }

            _mesh.uv = _uvs;
            _mesh.RecalculateTangents();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
