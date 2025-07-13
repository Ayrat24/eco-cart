using PrimeTween;
using UnityEngine;

public class MoneyController : MonoBehaviour
{
    private static uint _currentMoney;
    private uint _previousAmountOfMoney;
    
    public delegate void OnMoneyChanged(uint money);
    public static event OnMoneyChanged OnMoneyChangedEvent;
    
    public static void AddMoney(uint money)
    {
        _currentMoney += money;
        OnMoneyChangedEvent?.Invoke(_currentMoney);
    }
}
