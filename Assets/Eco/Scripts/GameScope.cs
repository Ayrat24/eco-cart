using Eco.Scripts;
using Eco.Scripts.Cart;
using Eco.Scripts.Helpers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{
    [SerializeField] UpgradesCollection upgrades;
    [SerializeField] Settings settings;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<MoneyController>(Lifetime.Singleton);
        builder.Register<SaveManager>(Lifetime.Singleton);
        
        builder.RegisterComponentInHierarchy<Cart>();
        builder.RegisterComponentInHierarchy<UpgradeMenu>();
        builder.RegisterComponentInHierarchy<MoneyDisplay>();
        builder.RegisterComponentInHierarchy<GameController>();
        builder.RegisterComponentInHierarchy<WorldController>();
        builder.RegisterComponentInHierarchy<HelperManager>();
        builder.RegisterComponentInHierarchy<Player>();

        builder.RegisterComponent(upgrades);
        builder.RegisterComponent(settings);
    }
}
