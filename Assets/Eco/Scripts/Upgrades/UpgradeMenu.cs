using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

public class UpgradeMenu : MonoBehaviour
{
    [SerializeField] UpgradeButton upgradeButtonPrefab;
    [SerializeField] Transform upgradeButtonsParent;
    [SerializeField] MoneyDisplay moneyDisplay;

    private UpgradesCollection _upgradesCollection;
    private bool _spawnedButtons;
    private readonly List<UpgradeButton> _buttons = new();
    private CurrencyManager _currencyManager;

    [Inject]
    public void Initialize(UpgradesCollection upgradesCollection, CurrencyManager currencyManager)
    {
        _upgradesCollection = upgradesCollection;
        _currencyManager = currencyManager;
    }

    public void Open()
    {
        if (!_spawnedButtons)
        {
            SpawnButtons();
        }

        gameObject.SetActive(true);
    }

    private void SpawnButtons()
    {
        foreach (var upgrade in _upgradesCollection.trashScoreUpgrades)
        {
            var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            button.Init(upgrade, _currencyManager);
            _buttons.Add(button);
        }

        foreach (var upgrade in _upgradesCollection.treeBuyUpgrades)
        {
            var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            button.Init(upgrade, _currencyManager);
            _buttons.Add(button);
        }

        foreach (var upgrade in _upgradesCollection.helperBuyUpgrades)
        {
            var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            button.Init(upgrade, _currencyManager);
            _buttons.Add(button);
        }

        foreach (var upgrade in _upgradesCollection.cartBuyUpgrades)
        {
            var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            button.Init(upgrade, _currencyManager);
            _buttons.Add(button);
        }

        _spawnedButtons = true;
    }
}