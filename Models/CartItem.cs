using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wpf.Models
{
    /// <summary>
    /// Un element din coșul de cumpărături.
    /// Cantitatea și totalul se recalculează automat în UI prin binding.
    /// </summary>
    public class CartItem : INotifyPropertyChanged
    {
        private readonly Product _product;

        public int     ProductId   => _product.Id;
        public string  ProductName => _product.Name;
        public string  Category    => _product.Category;
        public byte[]? ImageData   => _product.ImageData;
        public decimal UnitPrice   => _product.FinalPrice;   // prețul DUPĂ reducere
        public decimal OriginalPrice => _product.Price;       // prețul înainte de reducere
        public bool    HasDiscount => _product.IsOnOffer && _product.DiscountPercentage > 0;
        public decimal DiscountPct => _product.DiscountPercentage;
        public int     MaxStock    => _product.Stock;

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 1) value = 1;
                if (value > MaxStock) value = MaxStock;
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(LineSavings));
            }
        }

        /// <summary>Total linie = cantitate × preț final</summary>
        public decimal LineTotal    => Quantity * UnitPrice;

        /// <summary>Economie linie față de prețul original</summary>
        public decimal LineSavings  => HasDiscount ? Quantity * (OriginalPrice - UnitPrice) : 0;

        public CartItem(Product product, int quantity = 1)
        {
            _product = product;
            _quantity = Math.Max(1, Math.Min(quantity, product.Stock));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
