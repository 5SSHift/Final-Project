using Microsoft.Data.SqlClient;
using System.Configuration;

namespace Wpf.Config
{
    public static class DatabaseConfig
    {
        private static readonly Lazy<string> _lazy = new(() =>
        {
            var b = new SqlConnectionStringBuilder(
                ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' lipsește din App.config."))
            {
                Pooling = true, MinPoolSize = 5, MaxPoolSize = 100, ConnectTimeout = 15,
                MultipleActiveResultSets = true
            };
            return b.ConnectionString;
        });

        public static string ConnectionString => _lazy.Value;
        public static string? ServerName   => new SqlConnectionStringBuilder(ConnectionString).DataSource;
        public static string? DatabaseName => new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;

        public static SqlConnection GetConnection() => new(ConnectionString);

        public static async Task<(bool Success, string Message, string? ServerVersion)> TestConnectionAsync()
        {
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                return (true, $"Conectat la {conn.DataSource} / {conn.Database}", conn.ServerVersion);
            }
            catch (Exception ex) { return (false, $"✗ {ex.Message}", null); }
        }

        public static async Task InitializeDatabaseAsync()
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await Exec(conn, @"
                IF OBJECT_ID('dbo.Users','U') IS NULL
                CREATE TABLE Users (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    Username NVARCHAR(100) NOT NULL UNIQUE,
                    Email NVARCHAR(255) NOT NULL DEFAULT '' UNIQUE,
                    PasswordHash NVARCHAR(512) NOT NULL,
                    Salt NVARCHAR(256) NOT NULL,
                    Role NVARCHAR(50) NOT NULL DEFAULT 'Client',
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                    LastLogin DATETIME NULL);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Users_Username')
                    CREATE INDEX IX_Users_Username ON Users(Username);");

            await Exec(conn, @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Users') AND name='Email')
                    ALTER TABLE Users ADD Email NVARCHAR(255) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Users') AND name='UQ_Users_Email')
                    ALTER TABLE Users ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);");

            await Exec(conn, @"
                IF OBJECT_ID('dbo.UserDevices','U') IS NULL
                CREATE TABLE UserDevices (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    UserId INT NOT NULL,
                    Fingerprint NVARCHAR(256) NOT NULL,
                    DeviceName NVARCHAR(256) NOT NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    RegisteredAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                    LastAuthAt DATETIME NULL,
                    CONSTRAINT FK_UserDevices_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE);");

            await Exec(conn, @"
                IF OBJECT_ID('dbo.UserDevices','U') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.columns
                       WHERE object_id = OBJECT_ID('dbo.UserDevices')
                         AND name = 'Fingerprint'
                         AND (max_length = -1 OR max_length > 512)
                   )
                    ALTER TABLE UserDevices ALTER COLUMN Fingerprint NVARCHAR(256) NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserDevices_Fingerprint')
                    CREATE INDEX IX_UserDevices_Fingerprint ON UserDevices(Fingerprint);");

            await Exec(conn, @"
                IF OBJECT_ID('dbo.Products','U') IS NULL
                CREATE TABLE Products (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    Name NVARCHAR(255) NOT NULL,
                    Description NVARCHAR(2000) NOT NULL DEFAULT '',
                    Price DECIMAL(10,2) NOT NULL,
                    Stock INT NOT NULL DEFAULT 0,
                    Category NVARCHAR(100) NOT NULL DEFAULT '',
                    ImagePath NVARCHAR(500) NOT NULL DEFAULT '',
                    ImageData VARBINARY(MAX) NULL,
                    DiscountPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
                    IsOnOffer BIT NOT NULL DEFAULT 0,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE());");

            // Adaugă coloanele noi la Products dacă tabela există deja
            foreach (var col in new[]
            {
                ("Category",           "NVARCHAR(100) NOT NULL DEFAULT ''"),
                ("ImagePath",          "NVARCHAR(500) NOT NULL DEFAULT ''"),
                ("ImageData",          "VARBINARY(MAX) NULL"),
                ("DiscountPercentage", "DECIMAL(5,2) NOT NULL DEFAULT 0"),
                ("IsOnOffer",          "BIT NOT NULL DEFAULT 0")
            })
            {
                await Exec(conn, $@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Products') AND name='{col.Item1}')
                        ALTER TABLE Products ADD {col.Item1} {col.Item2};");
            }

            await Exec(conn, @"
                IF OBJECT_ID('dbo.Orders','U') IS NULL
                CREATE TABLE Orders (
                    OrderID INT PRIMARY KEY IDENTITY(1,1),
                    Client_Id INT NOT NULL,
                    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
                    ShippingAddress NVARCHAR(500) NOT NULL DEFAULT '',
                    PaymentMethod NVARCHAR(100) NOT NULL DEFAULT '',
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                    CONSTRAINT FK_Orders_Users FOREIGN KEY (Client_Id) REFERENCES Users(Id));
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Orders_Client_Id')
                    CREATE INDEX IX_Orders_Client_Id ON Orders(Client_Id);");

            await Exec(conn, @"
                IF OBJECT_ID('dbo.OrderDetails','U') IS NULL
                CREATE TABLE OrderDetails (
                    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
                    OrderID INT NOT NULL,
                    Product_Id INT NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    CONSTRAINT FK_OD_Orders FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
                    CONSTRAINT FK_OD_Products FOREIGN KEY (Product_Id) REFERENCES Products(Id));");

            await Exec(conn, @"
                IF OBJECT_ID('dbo.OtpCodes','U') IS NULL
                CREATE TABLE OtpCodes (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    Email NVARCHAR(255) NOT NULL,
                    CodeHash NVARCHAR(512) NOT NULL,
                    Purpose NVARCHAR(50) NOT NULL DEFAULT 'login',
                    ExpiresAt DATETIME NOT NULL,
                    IsUsed BIT NOT NULL DEFAULT 0,
                    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE());
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_OtpCodes_Email')
                    CREATE INDEX IX_OtpCodes_Email ON OtpCodes(Email, Purpose);");
        }

        private static async Task Exec(SqlConnection conn, string sql)
        {
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<DatabaseStats?> GetDatabaseStatsAsync()
        {
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string? serverVersion = null;
                int productCount = 0;

                // Server version
                await using (var cmd = new SqlCommand("SELECT @@VERSION", conn))
                {
                    var sv = await cmd.ExecuteScalarAsync();
                    serverVersion = sv?.ToString();
                }

                // Product count
                await using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
                {
                    var cnt = await cmd.ExecuteScalarAsync();
                    if (cnt != null && int.TryParse(cnt.ToString(), out var n))
                    {
                        productCount = n;
                    }
                }

                return new DatabaseStats
                {
                    ServerVersion = serverVersion,
                    ProductCount = productCount
                };
            }
            catch
            {
                return null;
            }
        }
    }

    public class DatabaseStats
    {
        public string? ServerVersion { get; set; }
        public int ProductCount { get; set; }
    }
}
