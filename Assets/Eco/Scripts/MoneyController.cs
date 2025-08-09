using R3;

public class MoneyController
{
    public ReactiveProperty<int> CurrentMoney = new(1000);
    
    public void AddMoney(int money)
    {
        CurrentMoney.Value += money;
    }
}