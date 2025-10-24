using Eco.Scripts.Helpers;
using Eco.Scripts.Trees;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Eco.Scripts
{
    public class GameScope : LifetimeScope
    {
        [SerializeField] UpgradesCollection upgrades;
        [SerializeField] Settings settings;
    
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveManager>(Lifetime.Scoped);
            builder.Register<CurrencyManager>(Lifetime.Scoped);
            builder.Register<TreeCurrencyEarner>(Lifetime.Scoped);
            builder.Register<ProgressTracker>(Lifetime.Scoped);
        
            builder.RegisterComponentInHierarchy<GameController>();
            builder.RegisterComponentInHierarchy<WorldController>();
            builder.RegisterComponentInHierarchy<HelperManager>();
            builder.RegisterComponentInHierarchy<Player>();

            builder.RegisterComponent(upgrades);
            builder.RegisterComponent(settings);
        }
    }
}
