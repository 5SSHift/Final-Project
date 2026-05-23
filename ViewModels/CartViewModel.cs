using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Wpf.Auth;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    /// <summary>
    /// Coșul de cumpărături — singleton per sesiune.
    /// Prețurile se recalculează automat la orice modificare de cantitate.
    /// </summary>
    public sealed class CartViewModel : BaseViewModel
    {
        private readonly IOrderRepository _orderRepo;
        private readonly AuthService      _auth;

        // ── Colecție coș ─────────────────────────────────────────
        private ObservableCollection<CartItem> _items = [];
        public ObservableCollection<CartItem> Items
        {
            get => _items;
            private set { SetProperty(ref _items, value); SubscribeItems(); RecalcTotals(); }
        }

        // ── Totale calculate ─────────────────────────────────────
        private decimal _subtotal;
        public decimal Subtotal  { get => _subtotal; private set => SetProperty(ref _subtotal, value); }

        public decimal _savings;
        public decimal Savings   { get => _savings;  private set => SetProperty(ref _savings, value); }

        public decimal Total     => Subtotal;
        public int     ItemCount => Items.Sum(i => i.Quantity);
        public bool    IsEmpty   => Items.Count == 0;
        public bool    HasSavings => Savings > 0;

        // ── Checkout form ─────────────────────────────────────────
        private string _shippingAddress = string.Empty;
        public string ShippingAddress
        {
            get => _shippingAddress;
            set => SetProperty(ref _shippingAddress, value);
        }

        private string _selectedPayment = "Card bancar";
        public string SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        public string[] PaymentOptions { get; } =
            { "Card bancar", "Numerar la livrare", "Transfer bancar" };

        // ── Status ────────────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _orderPlaced;
        public bool OrderPlaced { get => _orderPlaced; set => SetProperty(ref _orderPlaced, value); }

        // ── Comenzi ───────────────────────────────────────────────
        public RelayCommand IncreaseCommand  { get; }
        public RelayCommand DecreaseCommand  { get; }
        public RelayCommand RemoveCommand    { get; }
        public RelayCommand ClearCommand     { get; }
        public RelayCommand PlaceOrderCommand { get; }

        // Callback apelat după plasarea comenzii (navigare)
        public Action? OnOrderPlaced { get; set; }

        public CartViewModel(IOrderRepository orderRepo, AuthService auth)
        {
            _orderRepo = orderRepo;
            _auth      = auth;

            IncreaseCommand   = new RelayCommand(p => { if (p is CartItem i) i.Quantity++;                         RecalcTotals(); }, _ => !IsLoading);
            DecreaseCommand   = new RelayCommand(p => { if (p is CartItem i) i.Quantity--;                         RecalcTotals(); }, _ => !IsLoading);
            RemoveCommand     = new RelayCommand(p => { if (p is CartItem i) RemoveItem(i); },                     _ => !IsLoading);
            ClearCommand      = new RelayCommand(_ => Clear(),                                                     _ => !IsLoading && !IsEmpty);
            PlaceOrderCommand = new RelayCommand(_ => _ = PlaceOrderAsync(),
                _ => !IsLoading && !IsEmpty && !string.IsNullOrWhiteSpace(ShippingAddress));

            SubscribeItems();
        }

        // ── Public API ────────────────────────────────────────────

        /// <summary>Adaugă un produs în coș sau crește cantitatea dacă există deja.</summary>
        public void AddProduct(Product product)
        {
            var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                var item = new CartItem(product);
                item.PropertyChanged += Item_PropertyChanged;
                Items.Add(item);
            }

            RecalcTotals();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ItemCount));
        }

        public void RemoveItem(CartItem item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            Items.Remove(item);
            RecalcTotals();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ItemCount));
        }

        public void Clear()
        {
            foreach (var i in Items) i.PropertyChanged -= Item_PropertyChanged;
            Items.Clear();
            RecalcTotals();
            OrderPlaced    = false;
            StatusMessage  = string.Empty;
            ShippingAddress = string.Empty;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ItemCount));
        }

        public void Reset()
        {
            Clear();
        }

        // ── Order placement ───────────────────────────────────────
        private async Task PlaceOrderAsync()
        {
            if (IsEmpty || string.IsNullOrWhiteSpace(ShippingAddress)) return;

            IsLoading     = true;
            StatusMessage = "Se plasează comanda...";
            try
            {
                var order = new Order
                {
                    Client_Id       = _auth.CurrentUserId,
                    Status          = "Pending",
                    TotalAmount     = Total,
                    ShippingAddress = ShippingAddress.Trim(),
                    PaymentMethod   = SelectedPayment
                };

                var details = Items.Select(i => new OrderDetail
                {
                    Product_Id = i.ProductId,
                    Quantity   = i.Quantity,
                    UnitPrice  = i.UnitPrice
                }).ToList();

                var orderId = await _orderRepo.CreateOrderAsync(order, details);
                if (orderId > 0)
                {
                    StatusMessage = $"✓ Comanda #{orderId} plasată cu succes!";
                    OrderPlaced   = true;
                    Clear();
                    OnOrderPlaced?.Invoke();
                }
                else
                {
                    StatusMessage = "✗ Comanda nu a putut fi plasată. Încearcă din nou.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Eroare: {ex.Message}";
            }
            finally { IsLoading = false; }
        }

        // ── Recalcul totale ───────────────────────────────────────
        private void RecalcTotals()
        {
            Subtotal = Items.Sum(i => i.LineTotal);
            Savings  = Items.Sum(i => i.LineSavings);
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(HasSavings));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void SubscribeItems()
        {
            _items.CollectionChanged += (_, e) =>
            {
                RecalcTotals();
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(ItemCount));
            };
        }

        private void Item_PropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.LineTotal) ||
                e.PropertyName == nameof(CartItem.LineSavings))
                RecalcTotals();
        }
    }
}
