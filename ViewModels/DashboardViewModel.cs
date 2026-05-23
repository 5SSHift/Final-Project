using Dapper;
using Wpf.Auth;
using Wpf.Config;

namespace Wpf.ViewModels
{
    public sealed class DashboardViewModel : BaseViewModel
    {
        private readonly AuthService _auth;

        public string WelcomeMessage      => $"Bun venit, {_auth.CurrentUser}!";
        public string UserRoleDescription => _auth.CurrentRole switch
        {
            "Administrator" => "Ai acces complet — gestionare utilizatori, produse și comenzi.",
            "Employee"      => "Ai acces angajat — gestionare produse și toate comenzile.",
            _               => "Ai acces client — vizualizare produse și comenzile tale."
        };

        public bool CanViewUsers  => _auth.CurrentRole is "Administrator" or "Employee";
        public bool CanViewOrders => true;

        public string TokenUsername => _auth.CurrentToken?.Username ?? "—";
        public string TokenRole     => _auth.CurrentToken?.Role     ?? "—";
        public string TokenExpiry   => _auth.CurrentToken is null ? "—"
            : _auth.CurrentToken.ExpiresAt.ToLocalTime().ToString("HH:mm  dd/MM/yyyy");

        private int _productCount;
        public int ProductCount { get => _productCount; set => SetProperty(ref _productCount, value); }

        private int _userCount;
        public int UserCount { get => _userCount; set => SetProperty(ref _userCount, value); }

        private int _orderCount;
        public int OrderCount { get => _orderCount; set => SetProperty(ref _orderCount, value); }

        private decimal _totalValue;
        public decimal TotalValue { get => _totalValue; set => SetProperty(ref _totalValue, value); }

        private string _dbVersion = "Se încarcă...";
        public string DbVersion { get => _dbVersion; set => SetProperty(ref _dbVersion, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public DashboardViewModel(AuthService auth) => _auth = auth;

        public async Task LoadStatsAsync()
        {
            IsLoading = true;
            try
            {
                using var db = DatabaseConfig.GetConnection();

                ProductCount = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Products");
                OrderCount   = _auth.IsClient
                    ? await db.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM Orders WHERE Client_Id = @Id",
                        new { Id = _auth.CurrentUserId })
                    : await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Orders");

                if (CanViewUsers)
                    UserCount = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Users");

                TotalValue = await db.QueryFirstOrDefaultAsync<decimal>(
                    "SELECT ISNULL(SUM(CAST(Price AS DECIMAL(18,2)) * Stock), 0) FROM Products");

                var ver = await db.QueryFirstOrDefaultAsync<string>("SELECT @@VERSION");
                DbVersion = ver?.Split('\n')[0].Trim() ?? "—";
            }
            catch (Exception ex) { DbVersion = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        public void Reset()
        {
            ProductCount = 0;
            UserCount = 0;
            OrderCount = 0;
            TotalValue = 0;
            DbVersion = "Se încarcă...";
            IsLoading = false;
        }
    }
}
