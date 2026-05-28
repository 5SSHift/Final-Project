using System.Collections.ObjectModel;
using Wpf.Auth;
using Wpf.Config;
using Wpf.Data.Repositories;
using Wpf.Models;
using Wpf.Views.Pages;

namespace Wpf.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IProductRepository      _productRepository;
        private readonly AuthService             _auth;
        private readonly NavigationService       _nav;
        private readonly DashboardViewModel      _dashboardVm;
        private readonly SettingsViewModel       _settingsVm;
        private readonly UsersViewModel          _usersVm;
        private readonly OrdersViewModel         _ordersVm;
        private readonly ProductsViewModel       _productsVm;
        private readonly ClientProductsViewModel _clientProductsVm;
        private readonly AdminDashboardViewModel _adminDashboardVm;
        private readonly CartViewModel            _cartVm;

        private ObservableCollection<Product> _products = [];
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private bool _isDatabaseConnected;
        public bool IsDatabaseConnected
        {
            get => _isDatabaseConnected;
            set { SetProperty(ref _isDatabaseConnected, value); OnPropertyChanged(nameof(DbShortStatus)); }
        }

        private string _statusMessage = "Pregătit.";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _databaseStatusMessage = string.Empty;
        public string DatabaseStatusMessage { get => _databaseStatusMessage; set => SetProperty(ref _databaseStatusMessage, value); }

        private string _currentPage = string.Empty;
        public string CurrentPage   { get => _currentPage;   set => SetProperty(ref _currentPage, value); }

        public string UserInfo      => _auth.CurrentUser;
        public string CurrentRole   => _auth.CurrentRole;
        public string DbShortStatus => IsDatabaseConnected ? "Conectat" : "Neconectat";

        public bool IsAdministrator => _auth.IsAdministrator;
        public CartViewModel Cart => _cartVm;
        public bool IsEmployee      => _auth.IsEmployee;
        public bool IsClient        => _auth.IsClient;

        // ── Role-based nav visibility ─────────────────────────────
        // Client: Products (shop), Settings
        // Employee: Products (shop), Products (raw/datagrid), Settings
        // Admin: everything
        public bool ShowDashboard       => IsAdministrator;
        public bool ShowRawProducts     => IsAdministrator || IsEmployee;    // datagrid raw
        public bool ShowClientProducts  => true;                              // shop cards
        public bool ShowOrders          => IsAdministrator || IsEmployee;
        public bool ShowAdminDashboard  => IsAdministrator;
        public bool ShowUsers           => IsAdministrator;

        // ── Commands ──────────────────────────────────────────────
        public RelayCommand LinkDatabaseCommand      { get; }
        public RelayCommand NavDashboardCommand      { get; }
        public RelayCommand NavProductsCommand       { get; }
        public RelayCommand NavClientProductsCommand { get; }
        public RelayCommand NavOrdersCommand         { get; }
        public RelayCommand NavUsersCommand          { get; }
        public RelayCommand NavAdminDashboardCommand { get; }
        public RelayCommand NavSettingsCommand       { get; }
        public RelayCommand NavCartCommand          { get; }
        public RelayCommand LogoutCommand            { get; }

        public Action? OnLogout { get; set; }

        public MainViewModel(
            IProductRepository productRepository, AuthService auth,
            NavigationService nav, DashboardViewModel dashboardVm,
            SettingsViewModel settingsVm, UsersViewModel usersVm,
            OrdersViewModel ordersVm, ProductsViewModel productsVm,
            ClientProductsViewModel clientProductsVm,
            AdminDashboardViewModel adminDashboardVm,
            CartViewModel            cartVm)
        {
            _productRepository = productRepository; _auth = auth; _nav = nav;
            _dashboardVm       = dashboardVm;        _settingsVm = settingsVm;
            _usersVm           = usersVm;            _ordersVm   = ordersVm;
            _productsVm        = productsVm;
            _clientProductsVm  = clientProductsVm;
            _adminDashboardVm  = adminDashboardVm;
            _cartVm            = cartVm;

            LinkDatabaseCommand      = new RelayCommand(_ => LinkDatabaseAsync(), _ => !IsLoading);
            NavDashboardCommand      = new RelayCommand(_ => NavigateTo("Dashboard"),      _ => _auth.IsAdministrator);
            NavProductsCommand       = new RelayCommand(_ => NavigateTo("Products"),       _ => _auth.IsAdministrator || _auth.IsEmployee);
            NavClientProductsCommand = new RelayCommand(_ => NavigateTo("ClientProducts"));
            NavOrdersCommand         = new RelayCommand(_ => NavigateTo("Orders"),         _ => !_auth.IsClient);
            NavUsersCommand          = new RelayCommand(_ => NavigateTo("Users"),          _ => _auth.IsAdministrator);
            NavAdminDashboardCommand = new RelayCommand(_ => NavigateTo("AdminDashboard"), _ => _auth.IsAdministrator);
            NavSettingsCommand       = new RelayCommand(_ => NavigateTo("Settings"));
            NavCartCommand          = new RelayCommand(_ => NavigateTo("Cart"));

            LogoutCommand       = new RelayCommand(_ => OnLogout?.Invoke());
        }

        private void NavigateTo(string page)
        {
            CurrentPage = page;
            System.Windows.Controls.Page target = page switch
            {
                "Dashboard"      => new DashboardPage(_dashboardVm),
                "Products"       => new ProductsPage(_productsVm),
                "ClientProducts" => CreateClientProductsPage(),
                "AdminDashboard" => new AdminDashboardPage(_adminDashboardVm),
                "Orders"         => new OrdersPage(_ordersVm),
                "Users"          => new UsersPage(_usersVm),
                "Settings"       => new SettingsPage(_settingsVm),
                "Cart"           => new CartPage(_cartVm),
                _                => new ClientProductsPage(_clientProductsVm)
            };
            _nav.NavigateTo(target);
        }

        // Navigate to correct home page based on role
        private ClientProductsPage CreateClientProductsPage()
        {
            var page = new ClientProductsPage(_clientProductsVm);
            page.OnViewCart = () => NavigateTo("Cart");
            return page;
        }

        public void NavigateToHome()
        {
            if (_auth.IsAdministrator)      NavigateTo("AdminDashboard");
            else if (_auth.IsEmployee)      NavigateTo("ClientProducts");
            else                            NavigateTo("ClientProducts");
        }

        public void ResetSession()
        {
            Products = []; IsDatabaseConnected = false;
            StatusMessage = "Pregătit."; DatabaseStatusMessage = string.Empty;
            CurrentPage = string.Empty;
            _dashboardVm.Reset(); _productsVm.Reset();
            _ordersVm.Reset(); _usersVm.Reset(); _adminDashboardVm.Reset();
            _clientProductsVm.Reset();
            _cartVm.Reset();
        }

        public Task AutoConnectAsync() => ConnectAndLoadAsync();

        private async Task ConnectAndLoadAsync()
        {
            IsLoading = true;
            DatabaseStatusMessage = "Se conectează...";
            try
            {
                var (ok, msg, _) = await DatabaseConfig.TestConnectionAsync();
                if (ok)
                {
                    await DatabaseConfig.InitializeDatabaseAsync();
                    IsDatabaseConnected   = true;
                    DatabaseStatusMessage = $"✓ {msg}";
                    StatusMessage         = "Conectat.";
                }
                else
                {
                    IsDatabaseConnected   = false;
                    DatabaseStatusMessage = msg;
                    StatusMessage         = "Conexiune eșuată.";
                }
            }
            catch (Exception ex) { DatabaseStatusMessage = $"✗ {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async void LinkDatabaseAsync()
        {
            IsLoading = true;
            DatabaseStatusMessage = "Se testează...";
            try
            {
                var (ok, msg, _) = await DatabaseConfig.TestConnectionAsync();
                if (ok)
                {
                    await DatabaseConfig.InitializeDatabaseAsync();
                    IsDatabaseConnected   = true;
                    DatabaseStatusMessage = $"✓ {msg}";
                    StatusMessage         = "Conectat!";
                }
                else { IsDatabaseConnected = false; DatabaseStatusMessage = msg; }
            }
            catch (Exception ex) { DatabaseStatusMessage = $"✗ {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
