using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Pooling
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly Queue<T> poolQueue = new();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly bool autoExpand;

        public ObjectPool(T prefab, int initialSize, Transform parent = null, bool autoExpand = true)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.autoExpand = autoExpand;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                poolQueue.Enqueue(obj);
            }
        }

        public T Get()
        {
            if (poolQueue.Count == 0)
            {
                if (autoExpand)
                {
                    AddToPool(1);
                }
                else
                {
                    Debug.LogWarning($"Pool of {typeof(T).Name} is empty!");
                    return null;
                }
            }

            T obj = poolQueue.Dequeue();
            obj.gameObject.SetActive(true);

            if (obj is IPooledObject pooled)
            {
                pooled.OnSpawn();
            }

            return obj;
        }

        public void ReturnToPool(T obj)
        {
            obj.transform.SetParent(parent);
            obj.gameObject.SetActive(false);
            poolQueue.Enqueue(obj);
            
            if (obj is IPooledObject pooled)
            {
                pooled.OnDespawn();
            }
        }

        private void AddToPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T obj = Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                poolQueue.Enqueue(obj);
            }
        }
    }
}