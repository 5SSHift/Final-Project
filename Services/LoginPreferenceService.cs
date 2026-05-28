using System.IO;
using System.Text.Json;

namespace Wpf.Services
{
    /// <summary>
    /// Persistă preferința fiecărui utilizator privind modul de autentificare:
    ///   - AutoLogin  (implicit) — aplicația se deschide direct la repornire
    ///   - ManualLogin           — se cere parola la fiecare pornire
    ///
    /// Preferința este stocată per UserId în AppData/Local/ProductManager/login_prefs.json.
    /// Administratorii sunt excluși din această logică (mereu manual).
    /// </summary>
    public static class LoginPreferenceService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProductManager",
            "login_prefs.json");

        // Cache în memorie pentru a evita I/O la fiecare verificare
        private static Dictionary<int, bool>? _cache;

        // ── API public ─────────────────────────────────────────────────────

        /// <summary>
        /// Returnează true dacă utilizatorul a ales autentificarea manuală (cu parolă).
        /// Implicit (fără preferință salvată) → auto-login activat.
        /// </summary>
        public static bool RequiresManualLogin(int userId)
        {
            var prefs = Load();
            return prefs.TryGetValue(userId, out var manual) && manual;
        }

        /// <summary>
        /// Salvează preferința utilizatorului.
        /// </summary>
        public static void SetRequireManualLogin(int userId, bool requireManual)
        {
            var prefs = Load();
            prefs[userId] = requireManual;
            Save(prefs);
        }

        // ── Persistență ───────────────────────────────────────────────────

        private static Dictionary<int, bool> Load()
        {
            if (_cache != null) return _cache;
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _cache = JsonSerializer.Deserialize<Dictionary<int, bool>>(json)
                             ?? new Dictionary<int, bool>();
                    return _cache;
                }
            }
            catch { /* fișier corupt — resetăm */ }

            _cache = new Dictionary<int, bool>();
            return _cache;
        }

        private static void Save(Dictionary<int, bool> prefs)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(prefs));
            }
            catch { /* nu blocăm UI pentru erori de I/O */ }
        }
    }
}
