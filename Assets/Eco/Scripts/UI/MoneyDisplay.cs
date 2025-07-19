using System;
using R3;
using TMPro;
using UnityEngine;
using VContainer;

public class MoneyDisplay : MonoBehaviour
{
    private const float AnimationDuration = .9f;

    [SerializeField] protected TextMeshProUGUI countLabel;
    [SerializeField] private RectTransform selfRect;

    private IDisposable _subscription;

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }

    public void UpdateValue(int value)
    {
        countLabel.text = value.ToString();

    }

    [Inject]
    public void Init(MoneyController moneyController)
    {
        _subscription = moneyController.CurrentMoney.Subscribe(UpdateValue);
    }
}