using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    [CreateAssetMenu(fileName = "Helper Buy", menuName = "Helper/Buy")]
    public class HelperBuyUpgrade : Upgrade
    {
        [SerializeField] HelperManager.HelperClass helperClass;
        public Subject<HelperManager.HelperClass> OnPurchase = new();
        
        protected override void ApplyUpgrade(int level)
        {
            OnPurchase.OnNext(helperClass);
        }
    }
}
