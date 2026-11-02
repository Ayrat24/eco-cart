using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// A map with one texel of <typeparamref name="T"/> per terrain detail cell, published as a global shader texture
    /// and living only on the GPU: stamps go up through a small staging row and a GPU copy per cell, so stamping a few
    /// cells never uploads the whole map. Maps of a <see cref="Time.time"/> let the shader compare with the clock, so
    /// nothing runs per frame.
    /// </summary>
    public sealed class CellMap<T> where T : unmanaged
    {
        private const int StagingSize = 256;

        private readonly int _mapId;
        private readonly string _name;
        private readonly TextureFormat _format;
        private readonly FilterMode _filter;
        private readonly List<(int x, int z, T value)> _pending = new();
        private Texture2D _map;
        private Texture2D _staging;

        public CellMap(string property, string name, TextureFormat format, FilterMode filter = FilterMode.Point)
        {
            _mapId = Shader.PropertyToID(property);
            _name = name;
            _format = format;
            _filter = filter;
        }

        public void Reset(int width, int height)
        {
            _pending.Clear();
            if (_map != null)
            {
                Object.Destroy(_map);
            }

            // All zero (a new texture's memory isn't). The map is never read back, so the CPU copy goes once
            // it is uploaded.
            _map = new Texture2D(width, height, _format, false, true)
            {
                name = _name,
                filterMode = _filter,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = _map.GetPixelData<T>(0);
            // A cleared native buffer copies in about a fifth of the time of writing each texel
            using (var zeros = new NativeArray<T>(pixels.Length, Allocator.Temp))
            {
                pixels.CopyFrom(zeros);
            }
            _map.Apply(false, true);

            if (_staging == null)
            {
                _staging = new Texture2D(StagingSize, 1, _format, false, true)
                {
                    name = _name + " Staging",
                    filterMode = FilterMode.Point
                };
            }

            Shader.SetGlobalTexture(_mapId, _map);
        }

        public void Stamp(int x, int z, T value) => _pending.Add((x, z, value));

        public bool Upload()
        {
            if (_pending.Count == 0)
            {
                return false;
            }

            var staging = _staging.GetPixelData<T>(0);
            for (int start = 0; start < _pending.Count; start += StagingSize)
            {
                int count = Mathf.Min(StagingSize, _pending.Count - start);
                for (int i = 0; i < count; i++)
                {
                    staging[i] = _pending[start + i].value;
                }
                _staging.Apply(false);

                for (int i = 0; i < count; i++)
                {
                    var stamp = _pending[start + i];
                    Graphics.CopyTexture(_staging, 0, 0, i, 0, 1, 1, _map, 0, 0, stamp.x, stamp.z);
                }
            }

            _pending.Clear();
            return true;
        }
    }
}
