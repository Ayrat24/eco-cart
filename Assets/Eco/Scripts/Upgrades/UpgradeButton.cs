using Eco.Scripts.Upgrades;
using LargeNumbers;
using R3;
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

        SetUpUI();
    }

    private void SetUpUI()
    {
        iconImage.style.backgroundImage = new StyleBackground(_upgrade.icon);
        _upgrade.upgradeLocalizedName.StringChanged += OnUpgradeNameStringChanged;
        _upgrade.upgradeLocalizedDescription.StringChanged += OnUpgradeDescriptionStringChanged;
        button.text = _upgrade.GetButtonText();
    }

    public void UpdatePurchaseAvailability(AlphabeticNotation money)
    {
        if (_upgrade.CurrentLevel.Value != _previousLevel)
        {
            _upgrade.upgradeLocalizedDescription.RefreshString();
        }

        button.text = _upgrade.GetButtonText();
        bool available =_upgrade.Available &&  money >= _upgrade.Cost;
        button.SetEnabled(available);
    }

    private void Purchase(ClickEvent clickEvent)
    {
        OnUpgradeClicked.OnNext(_upgrade);
    }

    private void OnUpgradeNameStringChanged(string value)
    {
        nameText.text = value;
    }

    private void OnUpgradeDescriptionStringChanged(string value)
    {
        descriptionText.text = _upgrade.GetDescription(value);
    }

    public void Clean()
    {
        _upgrade.upgradeLocalizedName.StringChanged -= OnUpgradeNameStringChanged;
        _upgrade.upgradeLocalizedDescription.StringChanged -= OnUpgradeDescriptionStringChanged;
    }
}