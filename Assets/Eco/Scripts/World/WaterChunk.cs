using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// A hex of lake or sea: a flat water surface at <see cref="WorldData.WaterLevel"/> over the baked lake bed.
    /// The surface is a subdivided hexagon, so it bends with the curved world and meets its neighbours seamlessly.
    /// </summary>
    public class WaterChunk : Chunk
    {
        // The water shader only animates waves on vertices at object-space height 0.5 and above (the top of a
        // Bitgem water tile), so the surface mesh sits at 0.5 and its transform half a metre lower.
        private const float WaveHeight = 0.5f;

        [SerializeField] private MeshFilter surface;
        [Tooltip("Rings of triangles from the centre to the edge. More rings bend more smoothly in the distance.")]
        [SerializeField, Min(1)] private int subdivisions = 6;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private float _meshRadius;

        public override void Init(ChunkContext context)
        {
            foreach (var tile in tiles)
            {
                tile.groundType = TileGroundType.Water;
            }

            BuildSurface(context.World.HexRadius);
            UpdateSurfaceUVs();

            // To show water on the map
            if (!HasSave)
            {
                SaveTiles();
            }
        }

        private void BuildSurface(float radius)
        {
            if (surface == null || (_mesh != null && Mathf.Approximately(_meshRadius, radius)))
            {
                return;
            }

            _meshRadius = radius;
            surface.transform.localPosition = new Vector3(0, -WaveHeight, 0);

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

            _vertices = vertices;
            _uvs = new Vector2[vertices.Length];
            _mesh = new Mesh { name = "Hex Water" };
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            surface.sharedMesh = _mesh;
        }

        // The water texture is mapped in world XZ like Bitgem's own volumes, so it lines up across hexes
        private void UpdateSurfaceUVs()
        {
            if (_mesh == null)
            {
                return;
            }

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
