using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wpf.Models;

namespace Wpf.Auth
{
    // ─────────────────────────────────────────────────────────────
    //  TokenService – generare și verificare token RSA-semnat
    //
    //  La pornirea aplicației se generează o pereche RSA 2048-bit.
    //  Cheia privată semnează tokenul la login.
    //  Cheia publică verifică tokenul la fiecare request.
    //
    //  Format token: base64url(payload_json) + "." + base64url(semnatura_RSA_SHA256)
    //  Similar cu JWT RS256, dar fără header (implementare proprie).
    // ─────────────────────────────────────────────────────────────
    public sealed class TokenService : IDisposable
    {
        private readonly RSA          _rsa;
        private readonly TimeSpan     _tokenLifetime;
        private readonly JsonSerializerOptions _jsonOpts = new()
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Cheie publică exportată (poate fi distribuită)
        public string PublicKeyPem { get; }

        public TokenService(TimeSpan? lifetime = null)
        {
            _tokenLifetime = lifetime ?? TimeSpan.FromHours(8);

            // Generează pereche RSA 2048-bit la instanțiere
            _rsa = RSA.Create(2048);
            PublicKeyPem = _rsa.ExportRSAPublicKeyPem();
        }

        // ── Creare token ─────────────────────────────────────────
        /// <summary>
        /// Creează un token RSA-semnat pentru utilizatorul dat.
        /// Semnătura folosește SHA-256 cu padding PKCS#1 v1.5.
        /// </summary>
        public string CreateToken(User user)
        {
            var payload = new TokenPayload
            {
                Sub  = user.Username,
                UserId = user.Id,
                Role = user.Role,
                Iat  = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Exp  = DateTimeOffset.UtcNow.Add(_tokenLifetime).ToUnixTimeSeconds()
            };

            var payloadJson  = JsonSerializer.Serialize(payload, _jsonOpts);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var payloadB64   = Base64UrlEncode(payloadBytes);

            // Semnătura RSA-SHA256
            var dataToSign  = Encoding.UTF8.GetBytes(payloadB64);
            var signature   = _rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var signatureB64 = Base64UrlEncode(signature);

            return $"{payloadB64}.{signatureB64}";
        }

        // ── Validare token ───────────────────────────────────────
        /// <summary>
        /// Verifică semnătura RSA și expirarea tokenului.
        /// Returnează AuthToken dacă e valid, null dacă e invalid/expirat.
        /// </summary>
        public AuthToken? ValidateToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var parts = raw.Split('.');
            if (parts.Length != 2) return null;

            var payloadB64   = parts[0];
            var signatureB64 = parts[1];

            try
            {
                // 1. Verifică semnătura
                var dataToVerify = Encoding.UTF8.GetBytes(payloadB64);
                var signature    = Base64UrlDecode(signatureB64);

                var isSignatureValid = _rsa.VerifyData(
                    dataToVerify, signature,
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                if (!isSignatureValid) return null;

                // 2. Decodifică payload
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadB64));
                var payload     = JsonSerializer.Deserialize<TokenPayload>(payloadJson, _jsonOpts);
                if (payload is null) return null;

                var issuedAt  = DateTimeOffset.FromUnixTimeSeconds(payload.Iat).UtcDateTime;
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Exp).UtcDateTime;

                // 3. Verifică expirarea
                if (DateTime.UtcNow > expiresAt) return null;

                return new AuthToken
                {
                    Raw       = raw,
                    UserId    = payload.UserId,
                    Username  = payload.Sub,
                    Role      = payload.Role,
                    IssuedAt  = issuedAt,
                    ExpiresAt = expiresAt
                };
            }
            catch
            {
                return null; // token malformat
            }
        }

        // ── Base64Url helpers (RFC 4648 §5) ──────────────────────
        public static string Base64UrlEncode(byte[] data)
            => Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        public static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "=";  break;
            }
            return Convert.FromBase64String(s);
        }

        public void Dispose() => _rsa.Dispose();

        // ── Payload JSON intern ───────────────────────────────────
        private sealed class TokenPayload
        {
            public string Sub    { get; set; } = string.Empty;
            public int    UserId { get; set; }
            public string Role   { get; set; } = string.Empty;
            public long   Iat    { get; set; }
            public long   Exp    { get; set; }
        }
    }
}
