using System;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class UpgradeButton : VisualElement
{
    private VisualElement iconImage;
    private Label nameText;
    private Label descriptionText;
    private Button button;

    private Upgrade _upgrade;
    private int _previousLevel;
    
    public readonly Subject<Upgrade> OnUpgradeClicked = new();
    

    public UpgradeButton()
    {
    }

    public void Init(Upgrade upgrade)
    {
        _upgrade = upgrade;
        _previousLevel = upgrade.CurrentLevel.Value;
        
        button = this.Q<Button>("Button");
        iconImage = this.Q<VisualElement>("Icon");
        descriptionText = this.Q<Label>("Description");
        nameText = this.Q<Label>("Name");

        button.RegisterCallback<ClickEvent>(Purchase);

        UpdateUI();
    }

    private void UpdateUI()
    {
        iconImage.style.backgroundImage = new StyleBackground(_upgrade.icon);
        nameText.text = _upgrade.upgradeName;
        descriptionText.text = _upgrade.description + "  " + _upgrade.CurrentLevel.Value.ToString();
        button.text = _upgrade.Cost.ToString();
    }

    public void UpdatePurchaseAvailability(int money)
    {
        if (_upgrade.CurrentLevel.Value != _previousLevel)
        {
            descriptionText.text = _upgrade.CurrentLevel.Value.ToString();
        }
        
        button.text = _upgrade.Cost.ToString();
        bool available = money >= _upgrade.Cost;
        button.SetEnabled(available);
    }

    private void Purchase(ClickEvent clickEvent)
    {
        OnUpgradeClicked.OnNext(_upgrade);
    }
}