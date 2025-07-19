using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

public class UpgradeMenu : MonoBehaviour
{
    [SerializeField] UpgradeButton upgradeButtonPrefab;
    [SerializeField] Transform upgradeButtonsParent;
    [FormerlySerializedAs("moneyCounter")] [SerializeField] MoneyDisplay moneyDisplay;

    private UpgradesCollection _upgradesCollection;
    private bool _spawnedButtons;
    private readonly List<UpgradeButton> _buttons = new();
    private MoneyController _moneyController;

    [Inject]
    public void Initialize(UpgradesCollection upgradesCollection, MoneyController moneyController)
    {
        _upgradesCollection = upgradesCollection;
        _moneyController = moneyController;
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
        foreach (var upgrade in _upgradesCollection.upgrades)
        {
            var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            button.Init(upgrade, _moneyController);
            _buttons.Add(button);
        }
        
        _spawnedButtons = true;
    }
}
