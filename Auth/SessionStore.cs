using System.IO;

namespace Wpf.Auth
{
    /// <summary>
    /// Stochează local dacă utilizatorul s-a deconectat explicit.
    /// Fișierul .logout este creat la logout și șters la login reușit.
    /// Garantează că la repornirea aplicației după logout se afișează Login,
    /// indiferent dacă dispozitivul e încă înregistrat în baza de date.
    /// </summary>
    public static class SessionStore
    {
        private static readonly string FlagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProductManager",
            ".logout");

        /// <summary>Marchează că userul s-a deconectat explicit.</summary>
        public static void MarkLoggedOut()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FlagPath)!);
                File.WriteAllText(FlagPath, DateTime.UtcNow.ToString("O"));
            }
            catch { /* nu blocăm UI pentru o eroare de fișier */ }
        }

        /// <summary>Șterge marcajul de logout după un login reușit.</summary>
        public static void ClearLoggedOut()
        {
            try { if (File.Exists(FlagPath)) File.Delete(FlagPath); }
            catch { }
        }

        /// <summary>True dacă userul s-a deconectat explicit în sesiunea anterioară.</summary>
        public static bool WasExplicitlyLoggedOut() => File.Exists(FlagPath);
    }
}
