using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.Auth
{
    /// <summary>
    /// Device-based one-time authentication service.
    /// Stores device fingerprints to enable one-time login without password.
    /// </summary>
    public sealed class DeviceAuthService
    {
        /// <summary>
        /// Generează sau citește un GUID persistent salvat local pe disc.
        /// Combinat cu MachineName și UserName, garantează unicitate per instalare.
        /// </summary>
        private static string GetOrCreateLocalDeviceId()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProductManager", "device.id");

            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim();

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var id = Guid.NewGuid().ToString();
                File.WriteAllText(path, id);
                return id;
            }
            catch
            {
                // Fallback dacă discul nu e accesibil — nu e persistent, dar nu crașează
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Generează un fingerprint unic bazat pe mașină, utilizator Windows și GUID local persistent.
        /// </summary>
        public static string GenerateDeviceFingerprint()
        {
            var machineId  = Environment.MachineName;
            var userName   = Environment.UserName;
            var localId    = GetOrCreateLocalDeviceId();

            var fingerprint = $"{machineId}|{userName}|{localId}";

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Registers a device for one-time authentication.
        /// Deactivates all other users previously registered on this device so that
        /// only the most recently logged-in user is auto-authenticated on restart.
        /// </summary>
        public async Task<(bool Ok, string Message)> RegisterDeviceAsync(int userId, string deviceName)
        {
            try
            {
                var fingerprint = GenerateDeviceFingerprint();

                using var db = DatabaseConfig.GetConnection();

                // Deactivate ALL existing registrations for this device fingerprint,
                // regardless of which user they belong to. This guarantees that only
                // the current user will be auto-logged in on next restart.
                await db.ExecuteAsync(
                    "UPDATE UserDevices SET IsActive = 0 WHERE Fingerprint = @Fingerprint",
                    new { Fingerprint = fingerprint });

                // Check if this exact user already has a (now inactive) row — reactivate it.
                var existing = await db.QueryFirstOrDefaultAsync<UserDevice>(
                    "SELECT * FROM UserDevices WHERE UserId = @UserId AND Fingerprint = @Fingerprint",
                    new { UserId = userId, Fingerprint = fingerprint });

                if (existing != null)
                {
                    // Reactivate and refresh the timestamp
                    await db.ExecuteAsync(
                        "UPDATE UserDevices SET IsActive = 1, RegisteredAt = @Now, DeviceName = @DeviceName WHERE Id = @Id",
                        new { Now = DateTime.UtcNow, DeviceName = deviceName, Id = existing.Id });
                }
                else
                {
                    await db.ExecuteAsync(
                        @"INSERT INTO UserDevices (UserId, Fingerprint, DeviceName, IsActive, RegisteredAt)
                          VALUES (@UserId, @Fingerprint, @DeviceName, 1, @RegisteredAt)",
                        new
                        {
                            UserId       = userId,
                            Fingerprint  = fingerprint,
                            DeviceName   = deviceName,
                            RegisteredAt = DateTime.UtcNow
                        });
                }

                return (true, "Device registered successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Device registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts one-time authentication using device fingerprint.
        /// </summary>
        public async Task<(bool Ok, int UserId, string Message)> AuthenticateDeviceAsync()
        {
            try
            {
                var fingerprint = GenerateDeviceFingerprint();
                
                using var db = DatabaseConfig.GetConnection();
                
                // Find user by device fingerprint — ORDER BY RegisteredAt DESC ensures
                // the most recently authenticated user is returned when multiple rows exist.
                var device = await db.QueryFirstOrDefaultAsync<UserDevice>(
                    @"SELECT TOP 1 * FROM UserDevices
                      WHERE Fingerprint = @Fingerprint AND IsActive = 1
                      ORDER BY RegisteredAt DESC",
                    new { Fingerprint = fingerprint });
                
                if (device == null)
                    return (false, 0, "Device not recognized.");
                
                // Get user information
                var user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Id = @UserId AND IsActive = 1",
                    new { UserId = device.UserId });
                
                if (user == null)
                    return (false, 0, "User not found or inactive.");
                
                // Update last authentication time
                await db.ExecuteAsync(
                    "UPDATE UserDevices SET LastAuthAt = @LastAuthAt WHERE Id = @Id",
                    new { LastAuthAt = DateTime.UtcNow, Id = device.Id });
                
                return (true, user.Id, "Device authenticated successfully.");
            }
            catch (Exception ex)
            {
                return (false, 0, $"Device authentication failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregisters a device from one-time authentication.
        /// </summary>
        public async Task<(bool Ok, string Message)> UnregisterDeviceAsync(int userId)
        {
            try
            {
                var fingerprint = GenerateDeviceFingerprint();
                
                using var db = DatabaseConfig.GetConnection();
                
                await db.ExecuteAsync(
                    "UPDATE UserDevices SET IsActive = 0 WHERE UserId = @UserId AND Fingerprint = @Fingerprint",
                    new { UserId = userId, Fingerprint = fingerprint });
                
                return (true, "Device unregistered successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Device unregistration failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// User device model for one-time authentication.
    /// </summary>
    public class UserDevice
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastAuthAt { get; set; }
    }
}
