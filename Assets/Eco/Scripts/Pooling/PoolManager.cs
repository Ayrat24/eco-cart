using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Profiling;
using Eco.Scripts.Scrap;
using Eco.Scripts.World;
using UnityEngine;
using Tree = Eco.Scripts.Trees.Tree;

namespace Eco.Scripts.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        private List<ScrapItem> _scrapPrefabs;
        private readonly Dictionary<int, ObjectPool<ScrapItem>> _scrapPools = new();
        private readonly List<int> _ids = new();

        [SerializeField] private List<Tree> treePrefabs;
        private readonly Dictionary<int, ObjectPool<Tree>> _treePools = new();

        public static PoolManager Instance { get; private set; }


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void InitializePools(List<ScrapItem> scrapPrefabs)
        {
            using var marker = EcoProfilerMarkers.InitPools.Auto();

            _scrapPrefabs = scrapPrefabs;

            foreach (var prefab in _scrapPrefabs)
            {
                var objectPool = new ObjectPool<ScrapItem>(prefab, 1, transform);
                var o = objectPool.Get();

                var id = o.GetPrefabId();

                //TODO: optimize getting id without spawning it first!!!
                _scrapPools.Add(id, objectPool);
                _ids.Add(id);

                _scrapPools[id].ReturnToPool(o);
            }

            foreach (var prefab in treePrefabs)
            {
                var objectPool = new ObjectPool<Tree>(prefab, 1, transform);
                var o = objectPool.Get();

                var id = o.GetPrefabId();

                //TODO: optimize getting id without spawning it first!!!
                _treePools.Add(id, objectPool);

                _treePools[id].ReturnToPool(o);
            }
        }

        public ScrapItem GetScrap(int id)
        {
            if (!_scrapPools.ContainsKey(id))
            {
                return _scrapPools.Values.ToList()[0].Get();
            }

            return _scrapPools[id].Get();
        }

        public Tree GetTree(int id)
        {
            return _treePools[id].Get();
        }

        public void ReturnItem(ITileItem item)
        {
            switch (item)
            {
                case ScrapItem scrapItem:
                    var id = scrapItem.GetPrefabId();
                    _scrapPools[id].ReturnToPool(scrapItem);
                    break;

                case Tree tree:
                    _treePools[tree.GetPrefabId()].ReturnToPool(tree);
                    break;
            }
        }

        public ScrapItem GetScrap(ScrapItem scrapItem)
        {
            var id = scrapItem.GetPrefabId();
            if (_scrapPools.TryGetValue(id, out var scrapPool))
            {
                return scrapPool.Get();
            }

            var pool = new ObjectPool<ScrapItem>(scrapItem, 0, transform);
            _scrapPools.Add(id, pool);
            _ids.Add(id);
            return pool.Get();
        }

        public ScrapItem GetRandomScrap()
        {
            var randomIndex = Random.Range(0, _ids.Count);
            return _scrapPools[_ids[randomIndex]].Get();
        }
    }
}