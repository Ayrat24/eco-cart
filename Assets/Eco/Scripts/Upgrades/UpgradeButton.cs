using System;
using Eco.Scripts.Upgrades;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button button;
    
    private Upgrade _upgrade;
    private IDisposable _subscription;
    private CurrencyManager currencyManager;
    
    public void Init(Upgrade upgrade, CurrencyManager currencyManager)
    {
        _upgrade = upgrade;
        this.currencyManager = currencyManager;

        var d = Disposable.CreateBuilder();
        upgrade.CurrentLevel.Subscribe(x => UpdateUI()).AddTo(ref d);
        currencyManager.CurrentMoney.Subscribe(x => UpdatePurchaseAvailability()).AddTo(ref d);
        _subscription = d.Build();
        
        UpdateUI();
        UpdatePurchaseAvailability();
    }

    private void UpdateUI()
    {
        iconImage.sprite = _upgrade.icon;
        descriptionText.text = _upgrade.CurrentLevel.Value.ToString();
        costText.text = _upgrade.Cost.ToString();
    }

    private void UpdatePurchaseAvailability()
    {
        bool available = currencyManager.CurrentMoney.Value >= _upgrade.Cost;
        button.interactable = available;
    }

    public void Purchase()
    {
        currencyManager.AddMoney(-_upgrade.Cost);
        _upgrade.BuyUpgrade();
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}