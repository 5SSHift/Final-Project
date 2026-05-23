using System.Configuration;

namespace Wpf.Config
{
    /// <summary>
    /// Database initialization helper class.
    /// Handles database setup and initialization tasks on application startup.
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initializes the database on application startup asynchronously
        /// </summary>
        public static async Task InitializeAsync()
        {
            try
            {
                // Check if auto-initialization is enabled
                var autoInitialize = ConfigurationManager.AppSettings["Database:AutoInitialize"];
                if (autoInitialize == "true")
                {
                    // Test connection first
                    var (success, message, version) = await DatabaseConfig.TestConnectionAsync();

                    if (success)
                    {
                        // Initialize database tables
                        await DatabaseConfig.InitializeDatabaseAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException(message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the connection string from configuration
        /// </summary>
        public static string GetConnectionString(string? name = null)
        {
            var connectionStringName = name ?? "DefaultConnection";
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionString == null)
            {
                throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' not found in App.config");
            }

            return connectionString.ConnectionString;
        }

        /// <summary>
        /// Validates the database connection and schema
        /// </summary>
        public static async Task<bool> ValidateDatabaseAsync()
        {
            try
            {
                var (success, _, _) = await DatabaseConfig.TestConnectionAsync();
                return success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets detailed database information
        /// </summary>
        public static async Task<DatabaseInfo?> GetDatabaseInfoAsync()
        {
            try
            {
                var stats = await DatabaseConfig.GetDatabaseStatsAsync();

                if (stats != null)
                {
                    return new DatabaseInfo
                    {
                        ServerName = DatabaseConfig.ServerName,
                        DatabaseName = DatabaseConfig.DatabaseName,
                        ServerVersion = stats.ServerVersion,
                        ProductCount = stats.ProductCount,
                        ConnectionString = MaskConnectionString(DatabaseConfig.ConnectionString)
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Masks sensitive information in connection string for display
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);

                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "***";
                }

                return builder.ConnectionString;
            }
            catch
            {
                return "***";
            }
        }
    }

    /// <summary>
    /// Database information model
    /// </summary>
    public class DatabaseInfo
    {
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public string? ServerVersion { get; set; }
        public int ProductCount { get; set; }
        public string? ConnectionString { get; set; }
    }
}
