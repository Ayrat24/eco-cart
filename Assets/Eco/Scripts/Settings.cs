using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Eco.Scripts
{
    [CreateAssetMenu(fileName = "Eco", menuName = "Game Settings")]
    public class Settings : ScriptableObject
    {
        [SerializeField] DetailQualities detailQuality;
        [SerializeField] string defaultLanguageCode = "en";
        [SerializeField] bool defaultFreeUpgrades = false;
        
        public static DetailQualities DetailQuality { get; private set; }
        public static string LanguageCode { get; private set; }
        public static bool FreeUpgrades { get; private set; }

        private const string DetailQualityKey = "DetailQuality";
        private const string LanguageCodeKey = "LanguageCode";
        private const string FreeUpgradesKey = "FreeUpgrades";

        public void Load()
        {
            // Load detail quality
            int savedDetailQuality = PlayerPrefs.GetInt(DetailQualityKey, (int)detailQuality);
            DetailQuality = (DetailQualities)savedDetailQuality;
            
            // Load language
            LanguageCode = PlayerPrefs.GetString(LanguageCodeKey, defaultLanguageCode);
            ApplyLanguage();
            
            // Load free upgrades
            FreeUpgrades = PlayerPrefs.GetInt(FreeUpgradesKey, defaultFreeUpgrades ? 1 : 0) == 1;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(DetailQualityKey, (int)DetailQuality);
            PlayerPrefs.SetString(LanguageCodeKey, LanguageCode);
            PlayerPrefs.SetInt(FreeUpgradesKey, FreeUpgrades ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetDetailQuality(DetailQualities quality)
        {
            DetailQuality = quality;
        }

        public static void SetLanguage(string languageCode)
        {
            LanguageCode = languageCode;
            ApplyLanguage();
        }

        public static void SetFreeUpgrades(bool enabled)
        {
            FreeUpgrades = enabled;
        }

        private static void ApplyLanguage()
        {
            if (LocalizationSettings.AvailableLocales == null) return;

            var locale = LocalizationSettings.AvailableLocales.GetLocale(LanguageCode);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        public enum DetailQualities
        {
            Low,
            Medium,
            High
        }
    }
}
