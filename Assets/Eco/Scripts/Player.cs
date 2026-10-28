using System;
using System.Collections.Generic;
using Eco.Scripts.Hats;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
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
        [SerializeField] private ToolController toolController;
        [SerializeField] private Transform hatParent;
        [SerializeField] private List<HatDefinition> hats = new();

        private HatController _hatController;
        private CurrencyManager _currencyManager;
        private PlayerStats _stats;
        private IDisposable _subscription;

        public Stomach Stomach => stomach;
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

        public void Spawn(SaveManager saveManager)
        {
            transform.position = saveManager.Progress.playerPosition.ToVector3();
            agent.enabled = true;

            itemCollector.Init(_currencyManager, _stats, stomach);

            var builder = new DisposableBuilder();
            _stats.MaxWeight.Subscribe(SetCapacity).AddTo(ref builder);
            _stats.MoveSpeed.Subscribe(speed => agent.speed = speed).AddTo(ref builder);
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

        private void SetCapacity(int capacity)
        {
            stomach.SetCapacity(capacity);
            OnStomachChanged.OnNext(stomach);
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.playerPosition = new SaveManager.Vector3Serializable(transform.position);
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
