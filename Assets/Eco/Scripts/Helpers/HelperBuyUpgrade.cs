using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    [CreateAssetMenu(fileName = "Helper Buy", menuName = "Helper/Buy")]
    public class HelperBuyUpgrade : SelectableUpgrade
    {
        [SerializeField] private Helper prefab;
        public readonly Subject<HelperBuyUpgrade> OnPurchase = new();
        
        public Helper GetPrefab() => prefab;

        protected override string SelectableGroupId => "helpers";

        protected override void ApplyUpgrade(int level)
        {
            OnPurchase.OnNext(this);
        }
    }
}