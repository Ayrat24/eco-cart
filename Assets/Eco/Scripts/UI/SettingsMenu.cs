using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class SettingsMenu
    {
        private readonly UIDocument _uiDocument;
        private readonly Settings _settings;

        private VisualElement _container;
        private Button _openButton;
        private Button _closeButton;
        private DropdownField _languageDropdown;
        private DropdownField _qualityDropdown;
        private Toggle _freeUpgradesToggle;
        private Button _saveButton;
        private Button _resetProgressButton;
        private VisualElement _confirmationPopup;
        private Button _cancelResetButton;
        private Button _confirmResetButton;

        public SettingsMenu(UIDocument uiDocument, Settings settings)
        {
            _uiDocument = uiDocument;
            _settings = settings;
        }

        public void Init()
        {
            var root = _uiDocument.rootVisualElement;

            _container = root.Q<VisualElement>("SettingsMenu");
            if (_container == null)
            {
                Debug.LogError("SettingsMenu container not found in UI! Make sure SettingsMenu exists in GameUI.uxml");
                return;
            }

            _openButton = root.Q<Button>("SettingsButton");
            if (_openButton == null)
            {
                Debug.LogError("SettingsButton not found in UI!");
                return;
            }

            _closeButton = _container.Q<Button>("CloseSettingsButton");
            _languageDropdown = _container.Q<DropdownField>("LanguageDropdown");
            _qualityDropdown = _container.Q<DropdownField>("QualityDropdown");
            _freeUpgradesToggle = _container.Q<Toggle>("FreeUpgradesToggle");
            _saveButton = _container.Q<Button>("SaveSettingsButton");
            _resetProgressButton = _container.Q<Button>("ResetProgressButton");
            _confirmationPopup = _container.Q<VisualElement>("ConfirmationPopup");

            if (_confirmationPopup != null)
            {
                _cancelResetButton = _confirmationPopup.Q<Button>("CancelResetButton");
                _confirmResetButton = _confirmationPopup.Q<Button>("ConfirmResetButton");
            }

            // Setup language dropdown
            if (_languageDropdown != null)
            {
                _languageDropdown.choices = new System.Collections.Generic.List<string> { "English", "Русский" };
                _languageDropdown.index = Settings.LanguageCode == "ru" ? 1 : 0;
            }

            // Setup quality dropdown
            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = new System.Collections.Generic.List<string> { "Low", "Medium", "High" };
                _qualityDropdown.index = (int)Settings.DetailQuality;
            }

            // Setup free upgrades toggle
            if (_freeUpgradesToggle != null)
            {
                _freeUpgradesToggle.value = Settings.FreeUpgrades;
            }

            // Register callbacks
            if (_openButton != null)
                _openButton.clicked += Open;
            if (_closeButton != null)
                _closeButton.clicked += Close;
            if (_saveButton != null)
                _saveButton.clicked += SaveSettings;
            if (_resetProgressButton != null)
                _resetProgressButton.clicked += ShowResetConfirmation;
            if (_cancelResetButton != null)
                _cancelResetButton.clicked += HideResetConfirmation;
            if (_confirmResetButton != null)
                _confirmResetButton.clicked += ConfirmReset;

            // Start closed
            Close();
        }

        private void Open()
        {
            _container.style.display = DisplayStyle.Flex;
        }

        private void Close()
        {
            _container.style.display = DisplayStyle.None;
        }

        private void SaveSettings()
        {
            // Update language
            string languageCode = _languageDropdown.index == 1 ? "ru" : "en";
            Settings.SetLanguage(languageCode);

            // Update quality
            Settings.SetDetailQuality((Settings.DetailQualities)_qualityDropdown.index);

            // Update free upgrades
            if (_freeUpgradesToggle != null)
            {
                Settings.SetFreeUpgrades(_freeUpgradesToggle.value);
            }

            // Save to PlayerPrefs
            _settings.Save();

            Debug.Log($"Settings saved: Language={languageCode}, Quality={Settings.DetailQuality}, FreeUpgrades={Settings.FreeUpgrades}");
        }

        private void ShowResetConfirmation()
        {
            _confirmationPopup.style.display = DisplayStyle.Flex;
        }

        private void HideResetConfirmation()
        {
            _confirmationPopup.style.display = DisplayStyle.None;
        }

        private void ConfirmReset()
        {
            Debug.Log("Resetting all progress...");

            SaveManager.DeleteProgress();
            WorldSelector.Instance.LoadWorld(0);
        }

        public void Clear()
        {
            if (_openButton != null)
                _openButton.clicked -= Open;
            if (_closeButton != null)
                _closeButton.clicked -= Close;
            if (_saveButton != null)
                _saveButton.clicked -= SaveSettings;
            if (_resetProgressButton != null)
                _resetProgressButton.clicked -= ShowResetConfirmation;
            if (_cancelResetButton != null)
                _cancelResetButton.clicked -= HideResetConfirmation;
            if (_confirmResetButton != null)
                _confirmResetButton.clicked -= ConfirmReset;
        }
    }
}