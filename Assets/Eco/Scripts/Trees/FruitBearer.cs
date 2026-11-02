using System;
using Eco.Scripts.Pooling;
using Eco.Scripts.Scrap;
using Eco.Scripts.World;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.Trees
{
    /// <summary>
    /// Hangs a <see cref="TreeDefinition"/>'s fruit on a tree's spots, one per fruit on the type's
    /// <see cref="FruitGrowth"/> clock, up to <see cref="TreeDefinition.MaxFruits"/>. Each fruit is a pooled
    /// <see cref="ScrapItem"/> that tentacles find and grab like any other scrap; a fruit that left its spot is no
    /// longer this tree's. A full tree holds its next fruit back until one is taken.
    /// </summary>
    [RequireComponent(typeof(Tree))]
    public class FruitBearer : MonoBehaviour
    {
        [Tooltip("Where fruit hangs: just outside the leaves as the game camera sees them, and no higher than " +
                 "JellyHandSettings.ReachUp lets tentacles find it.")]
        [SerializeField] private Transform[] spots;

        private TreeDefinition _definition;
        private FruitGrowth _growth;
        private ScrapItem[] _fruits;
        private int _count;

        // Clock value the fruit now growing started from
        private double _cycle;

        public TreeDefinition Definition => _definition;
        private int Capacity => Mathf.Min(_definition.MaxFruits, spots.Length);

        public void Begin(TreeDefinition definition, FruitGrowth growth, int fruits, double cycle)
        {
            _definition = definition;
            _growth = growth;
            _cycle = cycle;
            _fruits ??= new ScrapItem[spots.Length];

            for (int i = Mathf.Min(fruits, Capacity); i > 0; i--)
            {
                Hang();
            }

            _growth.Add(this);
        }

        /// <summary>Catches up with the clock, so a tree back from an unloaded chunk fills up at once.</summary>
        public void Grow(double clock)
        {
            ForgetPickedFruit();

            int capacity = Capacity;
            if (_count < capacity)
            {
                int cycles = (int)Math.Floor(clock - _cycle);
                if (cycles <= 0)
                {
                    return;
                }

                _cycle += cycles;
                for (int i = Mathf.Min(cycles, capacity - _count); i > 0; i--)
                {
                    Ripen(Hang());
                }
            }

            if (_count >= capacity)
            {
                _cycle = clock;
            }
        }

        public void Save(ref TileData data)
        {
            if (_definition == null)
            {
                return;
            }

            ForgetPickedFruit();
            data.fruits = _count;
            data.fruitCycle = _cycle;
        }

        /// <summary>Fruit on the branches goes back to its pool with the tree; its save has the count.</summary>
        public void Clear()
        {
            if (_definition == null)
            {
                return;
            }

            ForgetPickedFruit();
            for (int i = 0; i < _fruits.Length; i++)
            {
                if (_fruits[i] != null)
                {
                    Tween.StopAll(_fruits[i].transform);
                    PoolManager.Instance.ReturnItem(_fruits[i]);
                    _fruits[i] = null;
                }
            }

            _count = 0;
            _growth.Remove(this);
            _definition = null;
        }

        private ScrapItem Hang()
        {
            // A random free spot, so trees don't all fill up in the same order
            int skip = Random.Range(0, spots.Length - _count);
            int spot = 0;
            while (_fruits[spot] != null || skip-- > 0)
            {
                spot++;
            }

            var fruit = PoolManager.Instance.GetFruit(_definition.Fruit);
            fruit.Hang(spots[spot]);
            _fruits[spot] = fruit;
            _count++;
            return fruit;
        }

        // Counts as taken until grown: a tentacle carrying it animates the same scale
        private void Ripen(ScrapItem fruit)
        {
            fruit.SetPickedUpStatus(true);
            Tween.Scale(fruit.transform, 0f, 1f, _definition.RipenDuration, Ease.OutBack)
                .OnComplete(fruit, ripe => ripe.SetPickedUpStatus(false));
        }

        private void ForgetPickedFruit()
        {
            for (int i = 0; i < _fruits.Length; i++)
            {
                if (_fruits[i] != null && _fruits[i].transform.parent != spots[i])
                {
                    _fruits[i] = null;
                    _count--;
                }
            }
        }
    }
}
