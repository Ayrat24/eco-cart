using Eco.Scripts.Helpers;
using Eco.Scripts.Trees;
using Eco.Scripts.UI;
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
            builder.Register<CurrencyManager>(Lifetime.Singleton);
            builder.Register<SaveManager>(Lifetime.Singleton);
            builder.Register<TreeManager>(Lifetime.Singleton);
        
            builder.RegisterComponentInHierarchy<UpgradeMenu>();
            builder.RegisterComponentInHierarchy<GameController>();
            builder.RegisterComponentInHierarchy<WorldController>();
            builder.RegisterComponentInHierarchy<HelperManager>();
            builder.RegisterComponentInHierarchy<Player>();

            builder.RegisterComponent(upgrades);
            builder.RegisterComponent(settings);
        }
    }
}
