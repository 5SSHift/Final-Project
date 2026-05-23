using System.Security.Cryptography;
using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.Auth
{
    public sealed class AuthService
    {
        private readonly TokenService _tokens;

        // ── Sesiunea curentă ─────────────────────────────────────
        public AuthToken? CurrentToken   { get; private set; }
        public bool       IsLoggedIn     => CurrentToken?.IsValid == true;
        public string     CurrentUser    => CurrentToken?.Username ?? string.Empty;
        public string     CurrentRole    => CurrentToken?.Role     ?? string.Empty;
        public int        CurrentUserId  => CurrentToken?.UserId   ?? 0;
        public bool       IsAdministrator => CurrentRole == "Administrator";
        public bool       IsEmployee      => CurrentRole == "Employee";
        public bool       IsClient        => CurrentRole == "Client";

        // ── Login pending (înainte de verificarea OTP) ────────────
        // Userul a introdus credențiale corecte dar OTP-ul nu e verificat încă
        private User? _pendingUser;
        public string PendingEmail => _pendingUser?.Email ?? string.Empty;

        public AuthService(TokenService tokenService) => _tokens = tokenService;

        // ── Pasul 1: verifică credențialele (fără a seta tokenul) ─
        /// <summary>
        /// Verifică username + parolă. Dacă sunt corecte, stochează userul
        /// temporar în _pendingUser și returnează Ok=true.
        /// Tokenul NU este setat până la FinalizeLoginAsync (după OTP).
        /// </summary>
        public async Task<(bool Ok, string Message, AuthToken? Token)> LoginAsync(
            string username, string password)
        {
            _pendingUser = null;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Username și parola sunt obligatorii.", null);
            try
            {
                using var db = DatabaseConfig.GetConnection();
                var user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1",
                    new { Username = username.Trim() });

                if (user is null || !VerifyPassword(password, user.PasswordHash, user.Salt))
                    return (false, "Username sau parolă incorectă.", null);

                // Credențiale corecte → stochează temporar, așteptăm OTP
                _pendingUser = user;
                return (true, $"Credențiale corecte. OTP trimis pe {user.Email}.", null);
            }
            catch (Exception ex) { return (false, $"Eroare: {ex.Message}", null); }
        }

        // ── Pasul 2: finalizează login după OTP verificat ─────────
        /// <summary>
        /// Apelat DUPĂ ce OTP-ul a fost verificat cu succes.
        /// Setează tokenul RSA și actualizează LastLogin.
        /// </summary>
        public async Task FinalizeLoginAsync()
        {
            if (_pendingUser is null)
                throw new InvalidOperationException("Nu există un login pending.");

            using var db = DatabaseConfig.GetConnection();
            await db.ExecuteAsync("UPDATE Users SET LastLogin=@Now WHERE Id=@Id",
                new { Now = DateTime.UtcNow, _pendingUser.Id });

            CurrentToken = SetToken(_pendingUser);
            _pendingUser = null;
        }

        // ── Anulare login pending (user a anulat OTP) ─────────────
        public void CancelPendingLogin() => _pendingUser = null;

        // ── Login prin device (fără parolă, fără OTP) ────────────
        public async Task<(bool Ok, string Message, AuthToken? Token)> LoginByUserIdAsync(int userId)
        {
            try
            {
                using var db = DatabaseConfig.GetConnection();
                var user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Id = @Id AND IsActive = 1", new { Id = userId });
                if (user is null) return (false, "Utilizator negăsit.", null);

                await db.ExecuteAsync("UPDATE Users SET LastLogin=@Now WHERE Id=@Id",
                    new { Now = DateTime.UtcNow, user.Id });

                return (true, $"Bun venit, {user.Username}!", SetToken(user));
            }
            catch (Exception ex) { return (false, $"Eroare: {ex.Message}", null); }
        }

        // ── Register (creează contul după OTP verificat) ──────────
        public async Task<(bool Ok, string Message)> RegisterAsync(
            string username, string email, string password, string role = "Client")
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return (false, "Username trebuie să aibă minim 3 caractere.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Parola trebuie să aibă minim 6 caractere.");
            try
            {
                using var db = DatabaseConfig.GetConnection();
                var usernameExists = await db.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Username=@Username",
                    new { Username = username.Trim() });
                if (usernameExists > 0) return (false, "Username-ul există deja.");

                var emailExists = await db.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Email=@Email",
                    new { Email = email.Trim() });
                if (emailExists > 0) return (false, "Există deja un cont cu acest email.");

                var (hash, salt) = HashPassword(password);
                await db.ExecuteAsync(
                    @"INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, CreatedAt, IsActive)
                      VALUES (@Username, @Email, @PasswordHash, @Salt, @Role, @CreatedAt, 1)",
                    new { Username = username.Trim(), Email = email.Trim(),
                          PasswordHash = hash, Salt = salt, Role = role,
                          CreatedAt = DateTime.UtcNow });

                return (true, $"Contul '{username}' a fost creat cu succes.");
            }
            catch (Exception ex) { return (false, $"Eroare: {ex.Message}"); }
        }

        public void Logout() => CurrentToken = null;

        // ── Helpers ───────────────────────────────────────────────
        private AuthToken SetToken(User user)
        {
            var raw   = _tokens.CreateToken(user);
            var token = _tokens.ValidateToken(raw)!;
            CurrentToken = token;
            return token;
        }

        public (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(32);
            return (Pbkdf2(password, saltBytes), Convert.ToBase64String(saltBytes));
        }

        private static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            try
            {
                var saltBytes    = Convert.FromBase64String(storedSalt);
                var computedHash = Pbkdf2(password, saltBytes);
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(computedHash),
                    Convert.FromBase64String(storedHash));
            }
            catch { return false; }
        }

        private static string Pbkdf2(string password, byte[] salt)
            => Convert.ToBase64String(
                Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, 310_000,
                    HashAlgorithmName.SHA512, 64));
    }
}
