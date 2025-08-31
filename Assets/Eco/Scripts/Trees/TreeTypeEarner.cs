using System;
using R3;

namespace Eco.Scripts.Trees
{
    public class TreeTypeEarner
    {
        private readonly TreeBuyUpgrade _upgrade;
        private readonly CurrencyManager _currencyManager;
        private IDisposable _earnSubscription;
        private IDisposable _intervalChangedSubscription;

        public TreeTypeEarner(TreeBuyUpgrade upgrade, CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
            _upgrade = upgrade;
        }

        public void Init()
        {
            _intervalChangedSubscription = _upgrade.IntervalUpgrade.CurrentLevel.Subscribe(_ => StartEarning());
            StartEarning();
        }

        private void StartEarning()
        {
            _earnSubscription?.Dispose();
            _earnSubscription = Observable.Interval(TimeSpan.FromSeconds(_upgrade.IntervalUpgrade.Interval))
                .Subscribe(_ => { GainCurrency(); });
        }

        private void GainCurrency()
        {
            _currencyManager.AddMoney(_upgrade.CurrentLevel.Value * _upgrade.ScoreUpgrade.Score);
        }

        public void Clear()
        {
            _earnSubscription?.Dispose();
            _intervalChangedSubscription?.Dispose();
        }
    }
}