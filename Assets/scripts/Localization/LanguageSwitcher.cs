using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageSwitcher : MonoBehaviour
{
    private const string SavedLanguageKey = "SelectedLanguage";

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        string savedLanguage =
            PlayerPrefs.GetString(SavedLanguageKey, "en");

        ChangeLanguage(savedLanguage);
    }

    public void SelectEnglish()
    {
        ChangeLanguage("en");
    }

    public void SelectArabic()
    {
        ChangeLanguage("ar");
    }

    public void ChangeLanguage(string languageCode)
    {
        Locale locale =
            LocalizationSettings.AvailableLocales.GetLocale(
                new LocaleIdentifier(languageCode)
            );

        if (locale == null)
        {
            Debug.LogError(
                $"Locale '{languageCode}' wasn't found."
            );

            return;
        }

        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetString(
            SavedLanguageKey,
            languageCode
        );

        PlayerPrefs.Save();
    }
}