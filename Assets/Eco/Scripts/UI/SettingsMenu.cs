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
        private Button _saveButton;

        public SettingsMenu(UIDocument uiDocument, Settings settings)
        {
            _uiDocument = uiDocument;
            _settings = settings;
        }

        public void Init()
        {
            var root = _uiDocument.rootVisualElement;

            _container = root.Q<VisualElement>("SettingsMenu");
            _openButton = root.Q<Button>("SettingsButton");
            _closeButton = _container.Q<Button>("CloseSettingsButton");
            _languageDropdown = _container.Q<DropdownField>("LanguageDropdown");
            _qualityDropdown = _container.Q<DropdownField>("QualityDropdown");
            _saveButton = _container.Q<Button>("SaveSettingsButton");

            // Setup language dropdown
            _languageDropdown.choices = new System.Collections.Generic.List<string> { "English", "Русский" };
            _languageDropdown.index = Settings.LanguageCode == "ru" ? 1 : 0;

            // Setup quality dropdown
            _qualityDropdown.choices = new System.Collections.Generic.List<string> { "Low", "Medium", "High" };
            _qualityDropdown.index = (int)Settings.DetailQuality;

            // Register callbacks
            _openButton.clicked += Open;
            _closeButton.clicked += Close;
            _saveButton.clicked += SaveSettings;

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

            // Save to PlayerPrefs
            _settings.Save();

            Debug.Log($"Settings saved: Language={languageCode}, Quality={Settings.DetailQuality}");
        }

        public void Clear()
        {
            if (_openButton != null)
                _openButton.clicked -= Open;
            if (_closeButton != null)
                _closeButton.clicked -= Close;
            if (_saveButton != null)
                _saveButton.clicked -= SaveSettings;
        }
    }
}

