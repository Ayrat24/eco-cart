using System.Collections.Generic;
using Eco.Scripts.Trash;
using Eco.Scripts.World;
using UnityEngine;

namespace Eco.Scripts.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        [SerializeField] private List<TrashItem> trashPrefabs;
        private readonly Dictionary<int, ObjectPool<TrashItem>> _trashPools = new();
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

        private void Start()
        {
            foreach (var prefab in trashPrefabs)
            {
                var objectPool = new ObjectPool<TrashItem>(prefab, 1, transform);
                var o = objectPool.Get();

                var id = o.GetPrefabId();

                //TODO: optimize getting id without spawning it first!!!
                _trashPools.Add(id, objectPool);
                _ids.Add(id);


                _trashPools[id].ReturnToPool(o);
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

        public TrashItem GetTrash(int id)
        {
            return _trashPools[id].Get();
        }

        public Tree GetTree(int id)
        {
            return _treePools[id].Get();
        }

        public void ReturnItem(ITileItem item)
        {
            switch (item)
            {
                case TrashItem trashItem:
                    _trashPools[trashItem.GetPrefabId()].ReturnToPool(trashItem);
                    return;
                case Tree tree:
                    _treePools[tree.GetPrefabId()].ReturnToPool(tree);
                    break;
            }
        }

        public TrashItem GetRandomTrash()
        {
            var randomIndex = Random.Range(0, _ids.Count);
            return _trashPools[_ids[randomIndex]].Get();
        }
    }
}