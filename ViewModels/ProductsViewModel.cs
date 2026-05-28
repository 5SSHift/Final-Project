using System.Collections.ObjectModel;
using System.IO;
using Wpf.Auth;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class ProductsViewModel : BaseViewModel
    {
        private readonly IProductRepository _repo;
        private readonly AuthService        _auth;

        // ── Permisiuni bazate pe rol ──────────────────────────────
        public bool CanEdit   => _auth.IsAdministrator || _auth.IsEmployee;
        public bool CanDelete => _auth.IsAdministrator;

        // ── Colecție principală ───────────────────────────────────
        private ObservableCollection<Product> _products = [];
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { SetProperty(ref _products, value); ApplyFilter(); }
        }

        private ObservableCollection<Product> _filteredProducts = [];
        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        // ── Produs selectat ───────────────────────────────────────
        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value is not null) PopulateForm(value);
                OnPropertyChanged(nameof(HasSelection));
            }
        }
        public bool HasSelection => _selectedProduct is not null;

        // ── Formular (Add / Edit) ─────────────────────────────────
        private string _formName        = string.Empty;
        public string FormName
        {
            get => _formName;
            set => SetProperty(ref _formName, value);
        }

        private string _formDescription = string.Empty;
        public string FormDescription
        {
            get => _formDescription;
            set => SetProperty(ref _formDescription, value);
        }

        private string _formPrice = string.Empty;
        public string FormPrice
        {
            get => _formPrice;
            set => SetProperty(ref _formPrice, value);
        }

        private string _formStock = string.Empty;
        public string FormStock
        {
            get => _formStock;
            set => SetProperty(ref _formStock, value);
        }

        private string _formCategory = string.Empty;
        public string FormCategory
        {
            get => _formCategory;
            set => SetProperty(ref _formCategory, value);
        }

        private byte[]? _formImageData;
        public byte[]? FormImageData
        {
            get => _formImageData;
            set => SetProperty(ref _formImageData, value);
        }

        // Numele fișierului selectat — afișat în UI ca feedback vizual
        private string _formImageFileName = string.Empty;
        public string FormImageFileName
        {
            get => _formImageFileName;
            set => SetProperty(ref _formImageFileName, value);
        }

        private string _formDiscount = string.Empty;
        public string FormDiscount
        {
            get => _formDiscount;
            set => SetProperty(ref _formDiscount, value);
        }

        private bool _formIsOnOffer;
        public bool FormIsOnOffer
        {
            get => _formIsOnOffer;
            set => SetProperty(ref _formIsOnOffer, value);
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set { SetProperty(ref _isEditMode, value); OnPropertyChanged(nameof(FormTitle)); }
        }
        public string FormTitle => _isEditMode ? "Editează produs" : "Produs nou";

        // ── Căutare ───────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); ApplyFilter(); }
        }

        // ── Stare ─────────────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // ── Comenzi ───────────────────────────────────────────────
        public RelayCommand RefreshCommand   { get; }
        public RelayCommand AddCommand       { get; }
        public RelayCommand EditCommand      { get; }
        public RelayCommand DeleteCommand    { get; }
        public RelayCommand SaveCommand      { get; }
        public RelayCommand CancelFormCommand { get; }
        public RelayCommand BrowseImageCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public ProductsViewModel(IProductRepository repo, AuthService auth)
        {
            _repo  = repo;
            _auth  = auth;

            RefreshCommand    = new RelayCommand(_ => _ = LoadAsync(),         _ => !IsLoading);
            AddCommand        = new RelayCommand(_ => OpenAddForm(),           _ => CanEdit && !IsLoading);
            EditCommand       = new RelayCommand(_ => OpenEditForm(),          _ => CanEdit && HasSelection);
            DeleteCommand     = new RelayCommand(_ => _ = DeleteAsync(),       _ => CanDelete && HasSelection);
            SaveCommand       = new RelayCommand(_ => _ = SaveAsync(),         _ => CanEdit && !IsLoading);
            CancelFormCommand  = new RelayCommand(_ => CloseForm());
            BrowseImageCommand = new RelayCommand(_ => BrowseImage());
        }

        // ── Încărcare ──────────────────────────────────────────────
        public async Task LoadAsync()
        {
            IsLoading     = true;
            ErrorMessage  = string.Empty;
            StatusMessage = "Se încarcă produsele...";
            try
            {
                var list = await _repo.GetAllProductsAsync();
                Products      = new ObservableCollection<Product>(list);
                StatusMessage = $"{list.Count} produse găsite.";
            }
            catch (Exception ex) { ErrorMessage = $"Eroare la încărcare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // ── Filtrare locală ───────────────────────────────────────
        private void BrowseImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Selectează imaginea produsului",
                Filter = "Imagini|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Toate|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                FormImageData     = File.ReadAllBytes(dlg.FileName);
                FormImageFileName = Path.GetFileName(dlg.FileName);
            }
            catch
            {
                ErrorMessage = "Nu s-a putut citi imaginea selectată.";
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                FilteredProducts = new ObservableCollection<Product>(_products);
            }
            else
            {
                var q = _searchText.Trim().ToLower();
                FilteredProducts = new ObservableCollection<Product>(
                    _products.Where(p =>
                        p.Name.ToLower().Contains(q) ||
                        p.Description.ToLower().Contains(q) ||
                        p.Price.ToString().Contains(q)));
            }

            OnPropertyChanged(nameof(FilteredProducts));
        }

        // ── Formular Add ──────────────────────────────────────────
        private void OpenAddForm()
        {
            _isEditMode      = false;
            FormName         = string.Empty;
            FormDescription  = string.Empty;
            FormPrice        = string.Empty;
            FormStock        = string.Empty;
            ErrorMessage     = string.Empty;
            IsFormVisible    = true;
            OnPropertyChanged(nameof(FormTitle));
        }

        // ── Formular Edit ─────────────────────────────────────────
        private void OpenEditForm()
        {
            if (_selectedProduct is null) return;
            _isEditMode   = true;
            ErrorMessage  = string.Empty;
            IsFormVisible = true;
            OnPropertyChanged(nameof(FormTitle));
        }

        private void PopulateForm(Product p)
        {
            FormName        = p.Name;
            FormDescription = p.Description;
            FormPrice       = p.Price.ToString("F2");
            FormStock       = p.Stock.ToString();
            FormCategory    = p.Category;
            FormImageData     = p.ImageData;
            FormImageFileName = p.ImageData != null ? "imagine salvată" : string.Empty;
            FormDiscount    = p.DiscountPercentage.ToString("F0");
            FormIsOnOffer   = p.IsOnOffer;
        }

        private void CloseForm()
        {
            IsFormVisible = false;
            ErrorMessage  = string.Empty;
        }

        // ── Save (Add sau Edit) ───────────────────────────────────
        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            { ErrorMessage = "Numele produsului este obligatoriu."; return; }

            if (!decimal.TryParse(FormPrice.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) || price < 0)
            { ErrorMessage = "Prețul trebuie să fie un număr pozitiv."; return; }

            if (!int.TryParse(FormStock, out var stock) || stock < 0)
            { ErrorMessage = "Stocul trebuie să fie un număr întreg pozitiv."; return; }

            IsLoading = true;
            try
            {
                if (_isEditMode && _selectedProduct is not null)
                {
                    // ── Update ────────────────────────────────────
                    _selectedProduct.Name               = FormName.Trim();
                    _selectedProduct.Description        = FormDescription.Trim();
                    _selectedProduct.Price              = price;
                    _selectedProduct.Stock              = stock;
                    _selectedProduct.Category           = FormCategory.Trim();
                    _selectedProduct.ImageData          = FormImageData;
                    _selectedProduct.DiscountPercentage = decimal.TryParse(FormDiscount, out var disc2) ? disc2 : 0;
                    _selectedProduct.IsOnOffer          = FormIsOnOffer;

                    var ok = await _repo.UpdateAsync(_selectedProduct);
                    StatusMessage = ok
                        ? $"✓ Produsul '{_selectedProduct.Name}' actualizat."
                        : "✗ Actualizarea a eșuat.";
                }
                else
                {
                    // ── Insert ────────────────────────────────────
                    var newProduct = new Product
                    {
                        Name               = FormName.Trim(),
                        Description        = FormDescription.Trim(),
                        Price              = price,
                        Stock              = stock,
                        Category           = FormCategory.Trim(),
                        ImageData          = FormImageData,
                        DiscountPercentage = decimal.TryParse(FormDiscount, out var disc) ? disc : 0,
                        IsOnOffer          = FormIsOnOffer
                    };

                    var newId = await _repo.CreateAsync(newProduct);
                    StatusMessage = newId > 0
                        ? $"✓ Produs '{newProduct.Name}' adăugat (ID: {newId})."
                        : "✗ Adăugarea a eșuat.";
                }

                CloseForm();
                await LoadAsync();
            }
            catch (Exception ex) { ErrorMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // ── Delete ────────────────────────────────────────────────
        private async Task DeleteAsync()
        {
            if (_selectedProduct is null) return;

            IsLoading = true;
            try
            {
                var ok = await _repo.DeleteAsync(_selectedProduct.Id);
                StatusMessage = ok
                    ? $"✓ Produsul '{_selectedProduct.Name}' șters."
                    : "✗ Ștergerea a eșuat.";

                SelectedProduct = null;
                await LoadAsync();
            }
            catch (Exception ex) { ErrorMessage = $"Eroare la ștergere: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        public void Reset()
        {
            Products = [];
            FilteredProducts = [];
            SelectedProduct = null;
            SearchText = string.Empty;
            IsFormVisible = false;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
            IsLoading = false;
            FormName = string.Empty;
            FormDescription = string.Empty;
            FormPrice = string.Empty;
            FormStock = string.Empty;
            FormCategory = string.Empty;
            FormImageData     = null;
            FormImageFileName = string.Empty;
            FormDiscount = string.Empty;
            FormIsOnOffer = false;
        }
    }
}
