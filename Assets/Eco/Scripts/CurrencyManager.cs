using LargeNumbers;
using R3;

namespace Eco.Scripts
{
    public class CurrencyManager
    {
        public readonly ReactiveProperty<AlphabeticNotation> CurrentMoney = new();

        public void AddMoney(AlphabeticNotation money)
        {
            CurrentMoney.Value += money;
        }

        public void RemoveMoney(AlphabeticNotation money)
        {
            CurrentMoney.Value -= money;
        }

        public void Init(SaveManager saveManager)
        {
            CurrentMoney.Value = saveManager.Progress.currency;
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.currency = CurrentMoney.Value;
        }
    }
}