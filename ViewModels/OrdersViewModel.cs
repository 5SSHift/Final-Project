using System.Collections.ObjectModel;
using Wpf.Auth;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class OrdersViewModel : BaseViewModel
    {
        private readonly IOrderRepository _repo;
        private readonly AuthService      _auth;

        private ObservableCollection<Order> _orders = [];
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set { SetProperty(ref _selectedOrder, value); _ = LoadDetailsAsync(); }
        }

        private ObservableCollection<OrderDetail> _details = [];
        public ObservableCollection<OrderDetail> Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Poate vedea toate comenzile? (Admin/Employee)
        public bool CanViewAll => _auth.CurrentRole is "Administrator" or "Employee";

        // Status options pentru ComboBox
        public string[] StatusOptions { get; } =
            { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        public RelayCommand RefreshCommand { get; }

        public OrdersViewModel(IOrderRepository repo, AuthService auth)
        {
            _repo  = repo;
            _auth  = auth;
            RefreshCommand = new RelayCommand(_ => _ = LoadOrdersAsync(), _ => !IsLoading);
        }

        public async Task LoadOrdersAsync()
        {
            IsLoading = true;
            StatusMessage = "Se încarcă comenzile...";
            try
            {
                List<Order> orders;
                if (CanViewAll)
                    orders = await _repo.GetAllOrdersAsync();
                else
                    orders = await _repo.GetOrdersByClientAsync(_auth.CurrentUserId);

                Orders = new ObservableCollection<Order>(orders);
                StatusMessage = $"{orders.Count} comenzi găsite";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task LoadDetailsAsync()
        {
            if (SelectedOrder is null) { Details = []; return; }
            try
            {
                var details = await _repo.GetOrderDetailsAsync(SelectedOrder.OrderID);
                Details = new ObservableCollection<OrderDetail>(details);
            }
            catch { Details = []; }
        }

        public void Reset()
        {
            Orders = [];
            SelectedOrder = null;
            Details = [];
            StatusMessage = string.Empty;
            IsLoading = false;
        }
    }
}
