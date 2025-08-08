using Eco.Scripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{
    [SerializeField] UpgradesCollection upgrades;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<MoneyController>(Lifetime.Singleton);
        builder.Register<SaveManager>(Lifetime.Singleton);
        
        builder.RegisterComponentInHierarchy<Cart>();
        builder.RegisterComponentInHierarchy<UpgradeMenu>();
        builder.RegisterComponentInHierarchy<MoneyDisplay>();
        builder.RegisterComponentInHierarchy<GameController>();
        builder.RegisterComponentInHierarchy<WorldController>();


        builder.RegisterComponent(upgrades);
    }
}
