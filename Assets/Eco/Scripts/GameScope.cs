using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{
    [SerializeField] TrashStats trashStats;
    [SerializeField] UpgradesCollection upgradesCollection;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<MoneyController>(Lifetime.Singleton);
        
        builder.RegisterComponentInHierarchy<Field>();
        builder.RegisterComponentInHierarchy<Cart>();
        builder.RegisterComponentInHierarchy<UpgradeMenu>();
        builder.RegisterComponentInHierarchy<MoneyDisplay>();


        builder.RegisterComponent(trashStats);
        builder.RegisterComponent(upgradesCollection);
    }
}
