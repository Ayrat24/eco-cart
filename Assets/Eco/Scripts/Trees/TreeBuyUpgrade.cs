using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/TreeBuyUpgrade")]
    public class TreeBuyUpgrade : Upgrade
    {
        [SerializeField] private int prefabId;
        [SerializeField] GameObject treePrefab;
        public Subject<int> OnPurchase = new();

        protected override int CalculateCost()
        {
            return 10;
        }

        protected override void ApplyUpgrade(int level)
        {
            OnPurchase.OnNext(prefabId);
        }
    }
}
