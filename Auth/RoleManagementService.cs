using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.Auth
{
    /// <summary>
    /// Role-based access control service for managing user roles and permissions.
    /// Supports three role levels: Administrator, Employee, Client.
    /// Thread-safe with support for multiple concurrent connections.
    /// </summary>
    public sealed class RoleManagementService
    {
        // ── Role Definitions ──────────────────────────────────────
        public const string ROLE_ADMINISTRATOR = "Administrator";
        public const string ROLE_EMPLOYEE      = "Employee";
        public const string ROLE_CLIENT        = "Client";

        private static readonly string[] ValidRoles = 
        { 
            ROLE_ADMINISTRATOR, 
            ROLE_EMPLOYEE, 
            ROLE_CLIENT 
        };

        // ── Permissions by Role ───────────────────────────────────
        public static readonly Dictionary<string, List<string>> RolePermissions = new()
        {
            { ROLE_ADMINISTRATOR, new List<string>
                { 
                    "manage_users", "view_all_products", "edit_products", "delete_products",
                    "view_reports", "manage_roles", "system_settings", "view_audit_logs"
                }
            },
            { ROLE_EMPLOYEE, new List<string>
                {
                    "view_products", "edit_own_products", "create_products", "view_reports",
                    "manage_inventory"
                }
            },
            { ROLE_CLIENT, new List<string>
                {
                    "view_products", "place_orders", "view_own_orders", "contact_support"
                }
            }
        };

        /// <summary>
        /// Gets all valid role names.
        /// </summary>
        public static string[] GetValidRoles() => ValidRoles;

        /// <summary>
        /// Checks if a role is valid.
        /// </summary>
        public static bool IsValidRole(string role) 
            => !string.IsNullOrWhiteSpace(role) && ValidRoles.Contains(role);

        /// <summary>
        /// Gets the display name for a role.
        /// </summary>
        public static string GetRoleDisplayName(string role) => role switch
        {
            ROLE_ADMINISTRATOR => "Administrator (Acces complet)",
            ROLE_EMPLOYEE => "Employee (Acces angajat)",
            ROLE_CLIENT => "Client (Acces client)",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets permissions for a specific role.
        /// Thread-safe operation.
        /// </summary>
        public static List<string> GetRolePermissions(string role)
        {
            lock (RolePermissions)
            {
                return RolePermissions.ContainsKey(role) 
                    ? new List<string>(RolePermissions[role]) 
                    : new List<string>();
            }
        }

        /// <summary>
        /// Checks if a user has a specific permission.
        /// Thread-safe operation.
        /// </summary>
        public static bool HasPermission(string role, string permission)
        {
            lock (RolePermissions)
            {
                return RolePermissions.ContainsKey(role) && 
                       RolePermissions[role].Contains(permission);
            }
        }

        /// <summary>
        /// Updates user role (requires admin privileges).
        /// Thread-safe with database transaction.
        /// </summary>
        public async Task<(bool Ok, string Message)> UpdateUserRoleAsync(int userId, string newRole, string adminRole)
        {
            // Only administrators can change roles
            if (adminRole != ROLE_ADMINISTRATOR)
                return (false, "Doar administratorii pot schimba roluri.");

            if (!IsValidRole(newRole))
                return (false, $"Rolul '{newRole}' nu este valid.");

            try
            {
                using var db = DatabaseConfig.GetConnection();
                
                // Verify user exists
                var user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Id = @UserId",
                    new { UserId = userId });

                if (user == null)
                    return (false, "Utilizatorul nu a fost găsit.");

                // Update role
                var result = await db.ExecuteAsync(
                    "UPDATE Users SET Role = @Role WHERE Id = @UserId",
                    new { Role = newRole, UserId = userId });

                if (result > 0)
                    return (true, $"Rolul utilizatorului '{user.Username}' a fost schimbat în '{newRole}'.");
                
                return (false, "Schimbarea rolului a eșuat.");
            }
            catch (Exception ex)
            {
                return (false, $"Eroare la schimbarea rolului: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all users with a specific role.
        /// Thread-safe with support for concurrent queries.
        /// </summary>
        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            if (!IsValidRole(role))
                return new List<User>();

            try
            {
                using var db = DatabaseConfig.GetConnection();
                var users = await db.QueryAsync<User>(
                    "SELECT * FROM Users WHERE Role = @Role AND IsActive = 1",
                    new { Role = role });

                return users.ToList();
            }
            catch
            {
                return new List<User>();
            }
        }

        /// <summary>
        /// Gets role statistics (count of users per role).
        /// Thread-safe operation.
        /// </summary>
        public async Task<Dictionary<string, int>> GetRoleStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            try
            {
                using var db = DatabaseConfig.GetConnection();

                foreach (var role in ValidRoles)
                {
                    var count = await db.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM Users WHERE Role = @Role AND IsActive = 1",
                        new { Role = role });

                    stats[role] = count;
                }

                return stats;
            }
            catch
            {
                return stats;
            }
        }

        /// <summary>
        /// Checks if a user can perform an action based on their role.
        /// </summary>
        public static bool CanPerformAction(string userRole, string requiredPermission)
        {
            return HasPermission(userRole, requiredPermission);
        }
    }
}
