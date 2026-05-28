using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Wpf.Auth;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class OrdersViewModel : BaseViewModel
    {
        private readonly IOrderRepository _repo;
        private readonly AuthService      _auth;

        // ── Colecții ──────────────────────────────────────────────
        private ObservableCollection<Order> _orders = [];
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set
            {
                SetProperty(ref _orders, value);
                var view = CollectionViewSource.GetDefaultView(value);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(nameof(Order.StatusOrder), ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(nameof(Order.OrderID),     ListSortDirection.Descending));
                view.Filter = FilterOrder;
                OrdersView = view;
            }
        }

        private ICollectionView? _ordersView;
        public ICollectionView? OrdersView
        {
            get => _ordersView;
            private set => SetProperty(ref _ordersView, value);
        }

        // ── Căutare ───────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                OrdersView?.Refresh();
            }
        }

        private bool FilterOrder(object item)
        {
            if (item is not Order o) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var q = SearchText.Trim().ToLowerInvariant();
            return o.OrderID.ToString().Contains(q)
                || o.ClientUsername.ToLowerInvariant().Contains(q)
                || o.Status.ToLowerInvariant().Contains(q)
                || o.ShippingAddress.ToLowerInvariant().Contains(q)
                || o.PaymentMethod.ToLowerInvariant().Contains(q)
                || o.TotalAmount.ToString("C2").Contains(q);
        }

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                SetProperty(ref _selectedOrder, value);
                OnPropertyChanged(nameof(CanApprove));
                OnPropertyChanged(nameof(CanReject));
                OnPropertyChanged(nameof(CanDeliver));
                OnPropertyChanged(nameof(HasSelectedOrder));
                _ = LoadDetailsAsync();
            }
        }

        private ObservableCollection<OrderDetail> _details = [];
        public ObservableCollection<OrderDetail> Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        // ── State ─────────────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(CanApprove)); OnPropertyChanged(nameof(CanReject)); OnPropertyChanged(nameof(CanDeliver)); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // ── Vizibilitate / permisiuni ─────────────────────────────
        public bool CanViewAll    => _auth.CurrentRole is "Administrator" or "Employee";
        public bool IsAdmin       => _auth.CurrentRole == "Administrator";
        public bool HasSelectedOrder => SelectedOrder is not null;

        // Acceptă: doar admin, comanda e Pending și nu e în curs de procesare
        public bool CanApprove => IsAdmin && !IsLoading && SelectedOrder?.Status == "Pending";

        // Respinge: doar admin, comanda nu e deja Cancelled sau Delivered
        public bool CanReject  => IsAdmin && !IsLoading
                               && SelectedOrder?.Status is "Pending" or "Processing";

        // Livrează: doar admin, comanda e în Processing
        public bool CanDeliver => IsAdmin && !IsLoading && SelectedOrder?.Status == "Processing";

        // ── Badge culori status ───────────────────────────────────
        public static string StatusColor(string? status) => status switch
        {
            "Pending"    => "#F59E0B",
            "Processing" => "#2563EB",
            "Shipped"    => "#7C3AED",
            "Delivered"  => "#16A34A",
            "Cancelled"  => "#DC2626",
            _            => "#6B7280"
        };

        // ── Commands ──────────────────────────────────────────────
        public RelayCommand RefreshCommand { get; }
        public RelayCommand ApproveCommand { get; }
        public RelayCommand RejectCommand  { get; }
        public RelayCommand DeliverCommand { get; }

        public OrdersViewModel(IOrderRepository repo, AuthService auth)
        {
            _repo  = repo;
            _auth  = auth;


            RefreshCommand = new RelayCommand(_ => _ = LoadOrdersAsync(),        _ => !IsLoading);
            ApproveCommand = new RelayCommand(_ => _ = ApproveOrderAsync(),      _ => CanApprove);
            RejectCommand  = new RelayCommand(_ => _ = RejectOrderAsync(),       _ => CanReject);
            DeliverCommand = new RelayCommand(_ => _ = DeliverOrderAsync(),      _ => CanDeliver);
        }

        // ── Load ──────────────────────────────────────────────────
        public async Task LoadOrdersAsync()
        {
            IsLoading = true;
            StatusMessage = "Se încarcă comenzile...";
            try
            {
                List<Order> orders = CanViewAll
                    ? await _repo.GetAllOrdersAsync()
                    : await _repo.GetOrdersByClientAsync(_auth.CurrentUserId);

                Orders        = new ObservableCollection<Order>(orders);
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

        // ── Aprobare ──────────────────────────────────────────────
        private async Task ApproveOrderAsync()
        {
            if (SelectedOrder is null) return;
            IsLoading = true;
            try
            {
                var error = await _repo.ApproveOrderAsync(SelectedOrder.OrderID);
                if (error is not null)
                {
                    StatusMessage = $"⚠ {error}";
                    return;
                }
                StatusMessage = $"✓ Comanda #{SelectedOrder.OrderID} acceptată. Stocul a fost actualizat.";
                await LoadOrdersAsync();
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // ── Respingere ────────────────────────────────────────────
        private async Task RejectOrderAsync()
        {
            if (SelectedOrder is null) return;
            IsLoading = true;
            try
            {
                var error = await _repo.RejectOrderAsync(SelectedOrder.OrderID);
                if (error is not null)
                {
                    StatusMessage = $"⚠ {error}";
                    return;
                }

                var wasProcessing = SelectedOrder.Status == "Processing";
                StatusMessage = wasProcessing
                    ? $"✓ Comanda #{SelectedOrder.OrderID} respinsă. Stocul a fost restabilit."
                    : $"✓ Comanda #{SelectedOrder.OrderID} respinsă.";
                await LoadOrdersAsync();
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // ── Marcare Livrat ────────────────────────────────────────
        private async Task DeliverOrderAsync()
        {
            if (SelectedOrder is null) return;
            IsLoading = true;
            try
            {
                var ok = await _repo.UpdateStatusAsync(SelectedOrder.OrderID, "Delivered");
                StatusMessage = ok
                    ? $"✓ Comanda #{SelectedOrder.OrderID} marcată ca Livrată."
                    : "⚠ Nu s-a putut actualiza statusul.";
                await LoadOrdersAsync();
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // ── Reset ─────────────────────────────────────────────────
        public void Reset()
        {
            Orders        = [];
            OrdersView    = null;
            SelectedOrder = null;
            Details       = [];
            StatusMessage = string.Empty;
            IsLoading     = false;
        }
    }
}
