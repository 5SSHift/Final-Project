using global::Wpf.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public class ClientProductsViewModel : BaseViewModel
    {
        private readonly IProductRepository _productRepository;

        // Eveniment prin care anunțăm View-ul că s-a terminat încărcarea listei brute de categorii
        public event EventHandler<List<string>> CategoriesLoaded;

        // Coșul injectat — același singleton cu CartViewModel din DI
        public CartViewModel Cart { get; }

        private ObservableCollection<Product> _products = new();
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private ObservableCollection<Product> _filteredProducts = new();
        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        // Listă utilizată pentru consistență internă
        private ObservableCollection<string> _categories = new();
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // 🛠️ MODIFICAT: Set de categorii selectate simultan
        private HashSet<string> _selectedCategories = new() { "Toate" };
        public HashSet<string> SelectedCategories
        {
            get => _selectedCategories;
            set => SetProperty(ref _selectedCategories, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterProducts();
                    OnPropertyChanged(nameof(IsSearchEmpty));
                }
            }
        }

        public bool IsSearchEmpty => string.IsNullOrWhiteSpace(SearchText);

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // ── Comenzi ───────────────────────────────────────────────
        public RelayCommand LoadProductsCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddToCartCommand { get; }

        public ClientProductsViewModel(IProductRepository productRepository, CartViewModel cart)
        {
            _productRepository = productRepository;
            Cart = cart;

            LoadProductsCommand = new RelayCommand(async _ => await LoadProductsAsync(), _ => !IsLoading);
            RefreshCommand = new RelayCommand(async _ => await LoadProductsAsync(), _ => !IsLoading);

            AddToCartCommand = new RelayCommand(p =>
            {
                if (p is Product product && product.Stock > 0)
                {
                    Cart.AddProduct(product);
                    StatusMessage = $"✓ \"{product.Name}\" adăugat în coș ({Cart.ItemCount} produse)";
                }
            }, p => p is Product prod && prod.Stock > 0 && !IsLoading);
        }

        public async Task LoadProductsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            StatusMessage = "Se încarcă produsele...";
            try
            {
                var data = await _productRepository.GetAllProductsAsync();
                var list = data.OrderBy(p => p.Category).ToList();

                Products = new ObservableCollection<Product>(list);

                var cats = new List<string> { "Toate" };
                var distinctCategories = list.Select(p => p.Category)
                                             .Where(c => !string.IsNullOrEmpty(c))
                                             .Distinct()
                                             .OrderBy(c => c);
                cats.AddRange(distinctCategories);

                Categories = new ObservableCollection<string>(cats);

                // Forțăm resetarea selecției la starea inițială când se încarcă baze de date noi
                _selectedCategories = new HashSet<string> { "Toate" };

                // Anunțăm View-ul să își regenereze controalele vizuale de tip CheckBox
                CategoriesLoaded?.Invoke(this, cats);

                FilterProducts();
                StatusMessage = $"{list.Count} produse disponibile";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // 🛠️ MODIFICAT: Metodă apelată de View pentru a trimite noile selecții
        public void UpdateSelectedCategories(List<string> selected)
        {
            SelectedCategories = new HashSet<string>(selected);
            FilterProducts();
        }

        public void LoadProducts() => _ = LoadProductsAsync();

        // 🛠️ MODIFICAT: Logica de filtrare compusă
        private void FilterProducts()
        {
            if (Products == null) return;

            var f = Products.AsEnumerable();

            // Filtrare pe categorii multiple:
            // Dacă setul nu conține "Toate", produsul trebuie să aibă categoria inclusă în listă
            if (!SelectedCategories.Contains("Toate"))
            {
                f = f.Where(p => !string.IsNullOrEmpty(p.Category) && SelectedCategories.Contains(p.Category));
            }

            // Filtrare text (Search)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                f = f.Where(p =>
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            FilteredProducts = new ObservableCollection<Product>(f);
        }

        public void Reset()
        {
            SearchText = string.Empty;
            _selectedCategories = new HashSet<string> { "Toate" };
            // Cerem UI-ului să redeseneze elementele conform stării resetate
            CategoriesLoaded?.Invoke(this, Categories.ToList());
            FilterProducts();
            StatusMessage = string.Empty;
        }
    }
}
