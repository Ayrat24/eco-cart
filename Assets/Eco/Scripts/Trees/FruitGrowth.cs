using System;
using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    /// <summary>
    /// Keeps one growth clock per fruit tree type, counted in fruits: it runs while the upgrade tree has that type's
    /// fruit unlocked, at the speed of its upgrades. Trees only store where they stand on their type's clock, so
    /// unloaded chunks keep growing and a refund or an upgrade acts on every tree at once. The clocks are saved in
    /// <see cref="SaveManager.PlayerProgress.FruitClocks"/> by tree id and stop while the game is closed.
    /// </summary>
    public class FruitGrowth : IDisposable
    {
        private const int TickFrames = 15;

        private readonly PlayerStats _stats;
        private readonly List<TreeDefinition> _fruitTrees = new();
        private readonly Dictionary<string, double> _clocks = new();
        private readonly HashSet<FruitBearer> _bearers = new();

        private IDisposable _subscription;
        private float _lastTick;

        public FruitGrowth(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Load(IReadOnlyList<TreeDefinition> trees, SaveManager saveManager)
        {
            _fruitTrees.Clear();
            foreach (var tree in trees)
            {
                if (tree != null && tree.Fruit != null)
                {
                    _fruitTrees.Add(tree);
                }
            }

            _clocks.Clear();
            foreach (var pair in saveManager.Progress.FruitClocks)
            {
                _clocks[pair.Key] = pair.Value;
            }

            _lastTick = Time.time;
            _subscription?.Dispose();
            _subscription = Observable.IntervalFrame(TickFrames).Subscribe(_ => Tick());
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.FruitClocks = new Dictionary<string, double>(_clocks);
        }

        public double GetClock(TreeDefinition tree)
        {
            return _clocks.GetValueOrDefault(tree.Id);
        }

        public void Add(FruitBearer bearer)
        {
            _bearers.Add(bearer);
        }

        public void Remove(FruitBearer bearer)
        {
            _bearers.Remove(bearer);
        }

        private void Tick()
        {
            using var marker = EcoProfilerMarkers.GrowFruit.Auto();

            float elapsed = Time.time - _lastTick;
            _lastTick = Time.time;

            foreach (var tree in _fruitTrees)
            {
                if (_stats.TryGetFruitInterval(tree, out float interval))
                {
                    _clocks[tree.Id] = GetClock(tree) + elapsed / interval;
                }
            }

            foreach (var bearer in _bearers)
            {
                bearer.Grow(GetClock(bearer.Definition));
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
