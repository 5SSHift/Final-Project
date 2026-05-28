using System.Windows;

namespace Wpf.Services
{
    /// <summary>
    /// Manages the application language at runtime by swapping ResourceDictionary entries.
    /// Supported languages: "ro-RO" (Romanian, default) and "en-US" (English).
    /// </summary>
    public static class LanguageService
    {
        private const string SettingKey  = "AppLanguage";
        private const string DefaultLang = "ro-RO";

        public static string CurrentLanguage { get; private set; } = DefaultLang;

        // ── Initialization ──────────────────────────────────────────────────────
        /// <summary>
        /// Called once at application startup. Loads the persisted language or falls
        /// back to the default (Romanian).
        /// </summary>
        public static void Initialize()
        {
            var saved = Properties.Settings.Default[SettingKey] as string;
            ApplyLanguage(string.IsNullOrWhiteSpace(saved) ? DefaultLang : saved, persist: false);
        }

        // ── Public API ──────────────────────────────────────────────────────────
        /// <summary>Switches the UI language and persists the choice.</summary>
        public static void SwitchLanguage(string languageCode)
        {
            if (CurrentLanguage == languageCode) return;
            ApplyLanguage(languageCode, persist: true);
        }

        // ── Internals ───────────────────────────────────────────────────────────
        private static void ApplyLanguage(string languageCode, bool persist)
        {
            var dict = LoadDictionary(languageCode);
            if (dict == null) return;

            // Replace any previously-loaded language dictionary
            var merged = Application.Current.Resources.MergedDictionaries;
            var old = merged.FirstOrDefault(IsLanguageDictionary);
            if (old != null) merged.Remove(old);
            merged.Add(dict);

            CurrentLanguage = languageCode;

            if (persist)
            {
                Properties.Settings.Default[SettingKey] = languageCode;
                Properties.Settings.Default.Save();
            }
        }

        private static ResourceDictionary? LoadDictionary(string languageCode)
        {
            try
            {
                var uri = new Uri($"/Resources/Languages/{languageCode}.xaml", UriKind.Relative);
                var dict = new ResourceDictionary { Source = uri };
                // Tag it so we can find it later
                dict["__LangCode__"] = languageCode;
                return dict;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsLanguageDictionary(ResourceDictionary d) =>
            d.Contains("__LangCode__");
    }
}
