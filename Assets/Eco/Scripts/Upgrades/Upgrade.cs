using R3;
using UnityEngine;

public abstract class Upgrade : ScriptableObject
{
    public Sprite icon;
    public string description;
    public int maxLevel;
    public int cost;
    
    public ReactiveProperty<int> CurrentLevel = new(1);
    
    public void Init()
    {
        Load();
    }
    
    public void BuyUpgrade()
    {
        cost = CalculateCost();
        CurrentLevel.Value += 1;
        
        ApplyUpgrade(CurrentLevel.Value);
    }

    protected virtual int CalculateCost()
    {
        return cost + (CurrentLevel.Value + 1) * 5;
    }

    protected abstract void ApplyUpgrade(int level);
    
    protected virtual void Load()
    {
        CurrentLevel.Value = 1;
        cost = 10;
    }
}