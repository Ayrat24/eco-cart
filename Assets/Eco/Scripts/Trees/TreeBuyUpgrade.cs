using R3;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/TreeBuyUpgrade")]
    public class TreeBuyUpgrade : Upgrade
    {
        [SerializeField] GameObject treePrefab;
        public Subject<GameObject> OnPurchase = new();
        
        protected override void ApplyUpgrade(int level)
        {
            OnPurchase.OnNext(treePrefab);
        }
    }
}
