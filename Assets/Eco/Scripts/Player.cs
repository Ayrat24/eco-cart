using System;
using System.Collections.Generic;
using Eco.Scripts.Hats;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Jelly;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using VContainer;
using R3;
using UnityEngine.AI;

namespace Eco.Scripts
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ItemCollector itemCollector;
        [SerializeField] private Stomach stomach;
        [SerializeField] private Wetness wetness;
        [SerializeField] private ToolController toolController;
        [SerializeField] private Transform hatParent;
        [SerializeField] private List<HatDefinition> hats = new();

        private HatController _hatController;
        private CurrencyManager _currencyManager;
        private PlayerStats _stats;
        private IDisposable _subscription;

        public Stomach Stomach => stomach;
        public Wetness Wetness => wetness;
        public HatController Hats => _hatController;
        public IReadOnlyList<HatDefinition> AvailableHats => hats;

        // Fires with the stomach whenever its capacity changes
        public readonly Subject<Stomach> OnStomachChanged = new();

        [Inject]
        private void Init(CurrencyManager currencyManager, PlayerStats stats)
        {
            _currencyManager = currencyManager;
            _stats = stats;
        }

        public void Spawn(SaveManager saveManager, Vector3 position)
        {
            transform.position = position;
            agent.enabled = true;

            itemCollector.Init(_currencyManager, _stats, stomach);
            wetness.Init(_stats, saveManager.Progress.wetness);

            var builder = new DisposableBuilder();
            _stats.MaxWeight.Subscribe(SetCapacity).AddTo(ref builder);
            _stats.MoveSpeed.Subscribe(speed => agent.speed = speed).AddTo(ref builder);
            wetness.Unlocked.Subscribe(SetWading).AddTo(ref builder);
            _subscription = builder.Build();

            toolController.Init();
            _hatController = new HatController(hats, hatParent);
            _hatController.Initialize(saveManager);

            PlayerClickMovement.OnLeftClicked += Recycle;
        }

        private void Recycle()
        {
            stomach.Recycle();
        }

        // Wading comes with the trail. An agent can't walk off an area it may not use, so losing it means leaving
        // the water at once
        private void SetWading(bool allowed)
        {
            agent.areaMask = allowed ? NavMesh.AllAreas : WorldController.LandAreas;
            if (!allowed)
            {
                agent.Warp(WorldController.ClosestLand(transform.position));
            }
        }

        private void SetCapacity(int capacity)
        {
            stomach.SetCapacity(capacity);
            OnStomachChanged.OnNext(stomach);
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.playerPosition = new SaveManager.Vector3Serializable(transform.position);
            saveManager.Progress.wetness = wetness.Metres;
            _hatController.SaveHat(saveManager);
        }

        private void OnDestroy()
        {
            PlayerClickMovement.OnLeftClicked -= Recycle;

            _subscription?.Dispose();
            toolController?.Clear();
        }
    }
}
