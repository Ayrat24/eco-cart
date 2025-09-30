using System.Collections.Generic;
using Eco.Scripts.Upgrades;

namespace Eco.Scripts.Trees
{
    public class TreeCurrencyEarner
    {
        private List<TreeTypeEarner> _treeTypeEarners;
        private readonly UpgradesCollection _upgrades;
        private readonly CurrencyManager _currencyManager;

        public TreeCurrencyEarner(UpgradesCollection upgrades, CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
        }

        public void Init()
        {
            var treeTypes = _upgrades.GetUpgradeTypes<TreeBuyUpgrade>();

            _treeTypeEarners = new List<TreeTypeEarner>();
            foreach (var treeType in treeTypes)
            {
                var earner = new TreeTypeEarner(treeType, _currencyManager);
                earner.Init();
                
                _treeTypeEarners.Add(earner);
            }
        }

        public void Clear()
        {
            foreach (var earner in _treeTypeEarners)
            {
                earner.Clear();
            }
        }
    }
}
