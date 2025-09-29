using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    [CreateAssetMenu(fileName = "Helper Buy", menuName = "Helper/Buy")]
    public class HelperBuyUpgrade : Upgrade
    {
        [SerializeField] HelperManager.HelperClass helperClass;
        public readonly Subject<HelperManager.HelperClass> OnPurchase = new();
        
        public HelperManager.HelperClass GetHelperClass() => helperClass;

        protected override void ApplyUpgrade(int level)
        {
            OnPurchase.OnNext(helperClass);
        }
    }
}