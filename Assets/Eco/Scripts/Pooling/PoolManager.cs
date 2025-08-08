using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        [SerializeField] private List<Trash.TrashItem> trashPrefabs;
        private readonly Dictionary<Trash.TrashItem, ObjectPool<Trash.TrashItem>> _pools = new();

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
                var objectPool = new ObjectPool<Trash.TrashItem>(prefab, 1);
                _pools.Add(prefab, objectPool);
            }
        }

        public Trash.TrashItem GetTrash()
        {
            return _pools[trashPrefabs[0]].Get();
        }

        public void ReturnTrash(Trash.TrashItem trashItem)
        {
            _pools[trashPrefabs[0]].ReturnToPool(trashItem);
        }
    }
}