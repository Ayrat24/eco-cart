using Eco.Scripts.Helpers;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Eco.Scripts
{
    public class GameScope : LifetimeScope
    {
        [SerializeField] UpgradeTree upgradeTree;
        [SerializeField] BaseStats baseStats;
        [SerializeField] Settings settings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveManager>(Lifetime.Scoped);
            builder.Register<CurrencyManager>(Lifetime.Scoped);
            builder.Register<ProgressTracker>(Lifetime.Scoped);
            builder.Register<PlayerStats>(Lifetime.Scoped);
            builder.Register<UpgradeManager>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<GameController>();
            builder.RegisterComponentInHierarchy<WorldController>();
            builder.RegisterComponentInHierarchy<HelperManager>();
            builder.RegisterComponentInHierarchy<Player>();

            builder.RegisterInstance(upgradeTree);
            builder.RegisterInstance(baseStats);
            builder.RegisterComponent(settings);
        }
    }
}
