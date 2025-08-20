using Eco.Scripts;
using R3;
using UnityEngine;

public class CurrencyManager
{
    public readonly ReactiveProperty<int> CurrentMoney = new();
    
    public void AddMoney(int money)
    {
        CurrentMoney.Value += money;
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