using System.Collections.ObjectModel;
using System.IO;
using Dapper;
using Wpf.Auth;
using Wpf.Config;
using Wpf.Data.Repositories;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private readonly IProductRepository _repo;
        private readonly AuthService _auth;

        // ── Products ─────────────────────────────────────────────
        private ObservableCollection<Product> _products = [];
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (!SetProperty(ref _selectedProduct, value)) return;
                if (value != null)
                    LoadProductFormFromSelection();
            }
        }

        private string _productName        = string.Empty;
        public string ProductName        { get => _productName;        set => SetProperty(ref _productName, value); }
        private string _productDescription = string.Empty;
        public string ProductDescription { get => _productDescription; set => SetProperty(ref _productDescription, value); }
        private decimal _productPrice;
        public decimal ProductPrice      { get => _productPrice;       set => SetProperty(ref _productPrice, value); }
        private int _productStock;
        public int ProductStock          { get => _productStock;       set => SetProperty(ref _productStock, value); }
        private string _productCategory  = string.Empty;
        public string ProductCategory    { get => _productCategory;    set => SetProperty(ref _productCategory, value); }
        private byte[]? _productImageData;
        public byte[]? ProductImageData  { get => _productImageData;  set => SetProperty(ref _productImageData, value); }
        private string _productImageFileName = string.Empty;
        public string ProductImageFileName { get => _productImageFileName; set => SetProperty(ref _productImageFileName, value); }
        private decimal _productDiscount;
        public decimal ProductDiscount   { get => _productDiscount;    set => SetProperty(ref _productDiscount, value); }
        private bool _productIsOnOffer;
        public bool ProductIsOnOffer     { get => _productIsOnOffer;   set => SetProperty(ref _productIsOnOffer, value); }

        // ── Users ─────────────────────────────────────────────────
        private ObservableCollection<User> _users = [];
        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (!SetProperty(ref _selectedUser, value)) return;
                OnPropertyChanged(nameof(UserFormTitle));
                OnPropertyChanged(nameof(IsCreatingUser));
                if (value != null)
                    LoadUserFormFromSelection();
            }
        }

        private string _username     = string.Empty;
        public string Username       { get => _username;      set => SetProperty(ref _username, value); }
        private string _email        = string.Empty;
        public string Email          { get => _email;         set => SetProperty(ref _email, value); }
        private string _selectedRole = "Client";
        public string SelectedRole   { get => _selectedRole;  set => SetProperty(ref _selectedRole, value); }
        public string[] Roles { get; } = { "Administrator", "Employee", "Client" };

        // Parola (doar la creare; ascunsă la editare)
        private string _userPassword = string.Empty;
        public string UserPassword
        {
            get => _userPassword;
            set { SetProperty(ref _userPassword, value); OnPropertyChanged(nameof(PasswordMismatch)); }
        }

        private string _userPasswordConfirm = string.Empty;
        public string UserPasswordConfirm
        {
            get => _userPasswordConfirm;
            set { SetProperty(ref _userPasswordConfirm, value); OnPropertyChanged(nameof(PasswordMismatch)); }
        }

        // Arată avertisment când parolele nu coincid (și ambele sunt completate)
        public bool PasswordMismatch =>
            !string.IsNullOrEmpty(UserPassword) &&
            !string.IsNullOrEmpty(UserPasswordConfirm) &&
            UserPassword != UserPasswordConfirm;

        // True când formularul e pentru un utilizator NOU (nu editare)
        public bool IsCreatingUser => SelectedUser == null && IsUserFormVisible;

        private bool _isUserFormVisible;
        public bool IsUserFormVisible
        {
            get => _isUserFormVisible;
            set { SetProperty(ref _isUserFormVisible, value); OnPropertyChanged(nameof(UserFormTitle)); OnPropertyChanged(nameof(IsCreatingUser)); }
        }

        public string UserFormTitle => SelectedUser == null
            ? "Adauga utilizator"
            : "Actualizeaza utilizator";

        // Eveniment ridicat când formularul utilizator e închis/golit
        // (folosit de code-behind pentru a goli PasswordBox-urile)
        public event Action? UserFormClosed;

        // ── Status / Loading / Tab ────────────────────────────────
        private string _statusMessage = string.Empty;
        public string StatusMessage   { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        private bool _isLoading;
        public bool IsLoading         { get => _isLoading;     set => SetProperty(ref _isLoading, value); }
        private string _activeTab = "Products";
        public string ActiveTab       { get => _activeTab;     set => SetProperty(ref _activeTab, value); }

        // ── Commands ──────────────────────────────────────────────
        public RelayCommand AddProductCommand    { get; }
        public RelayCommand UpdateProductCommand { get; }
        public RelayCommand DeleteProductCommand { get; }
        public RelayCommand ClearProductFormCommand { get; }
        public RelayCommand LoadProductsCommand  { get; }
        public RelayCommand BrowseImageCommand   { get; }

        public RelayCommand AddUserCommand          { get; }
        public RelayCommand OpenAddUserFormCommand   { get; }   // butonul de sus — deschide mereu formularul de creare
        public RelayCommand UpdateUserCommand { get; }
        public RelayCommand DeleteUserCommand { get; }
        public RelayCommand ClearUserFormCommand { get; }
        public RelayCommand LoadUsersCommand  { get; }

        public RelayCommand SelectProductCommand { get; }
        public RelayCommand SelectUserCommand    { get; }
        public RelayCommand SwitchTabCommand     { get; }

        public AdminDashboardViewModel(IProductRepository productRepository, AuthService auth)
        {
            _repo  = productRepository;
            _auth  = auth;

            AddProductCommand       = new RelayCommand(_ => _ = AddProductAsync(),    _ => !IsLoading && IsValidProductForm());
            UpdateProductCommand    = new RelayCommand(_ => _ = UpdateProductAsync(), _ => !IsLoading && SelectedProduct != null);
            DeleteProductCommand    = new RelayCommand(_ => _ = DeleteProductAsync(), _ => !IsLoading && SelectedProduct != null);
            ClearProductFormCommand = new RelayCommand(_ => ClearProductForm());
            LoadProductsCommand     = new RelayCommand(_ => _ = LoadProductsAsync());
            BrowseImageCommand      = new RelayCommand(_ => BrowseImage());

            AddUserCommand          = new RelayCommand(_ => _ = AddUserAsync(),    _ => !IsLoading && IsCreatingUser && IsValidUserForm());
            OpenAddUserFormCommand  = new RelayCommand(_ => OpenAddUserForm());
            UpdateUserCommand    = new RelayCommand(_ => _ = UpdateUserAsync(), _ => !IsLoading && IsUserFormVisible && SelectedUser != null);
            DeleteUserCommand    = new RelayCommand(_ => _ = DeleteUserAsync(), _ => !IsLoading && SelectedUser != null);
            ClearUserFormCommand = new RelayCommand(_ => ClearUserForm());
            LoadUsersCommand     = new RelayCommand(_ => _ = LoadUsersAsync());

            SelectProductCommand = new RelayCommand(p => { if (p is Product prod) { SelectedProduct = prod; LoadProductFormFromSelection(); } });
            SelectUserCommand    = new RelayCommand(p => { if (p is User u) { SelectedUser = u; LoadUserFormFromSelection(); } });
            SwitchTabCommand     = new RelayCommand(p => ActiveTab = p?.ToString() ?? "Products");
        }

        // ── Called by page Loaded event (NOT constructor — avoids async void) ─
        public async Task InitializeAsync()
        {
            await DatabaseConfig.InitializeDatabaseAsync();
            await LoadProductsAsync();
            await LoadUsersAsync();
        }

        public void Reset()
        {
            Products = [];
            SelectedProduct = null;
            ProductName = string.Empty;
            ProductDescription = string.Empty;
            ProductPrice = 0;
            ProductStock = 0;
            ProductCategory = string.Empty;
            ProductImageData = null;
            ProductImageFileName = string.Empty;
            ProductDiscount = 0;
            ProductIsOnOffer = false;

            Users = [];
            SelectedUser = null;
            Username = string.Empty;
            Email = string.Empty;
            SelectedRole = "Client";
            IsUserFormVisible = false;

            StatusMessage = string.Empty;
            IsLoading = false;
            ActiveTab = "Products";
        }

        // ── IMAGE PICKER ──────────────────────────────────────────
        private void BrowseImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Selectează imaginea produsului",
                Filter = "Imagini|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Toate fișierele|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;
            try
            {
                ProductImageData     = File.ReadAllBytes(dialog.FileName);
                ProductImageFileName = Path.GetFileName(dialog.FileName);
            }
            catch { StatusMessage = "Nu s-a putut citi imaginea."; }
        }

        // ── PRODUCTS ──────────────────────────────────────────────
        public async Task LoadProductsAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _repo.GetAllProductsAsync();
                Products      = new ObservableCollection<Product>(list);
                StatusMessage = $"{list.Count} produse în baza de date";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private bool IsValidProductForm()
            => !string.IsNullOrWhiteSpace(ProductName) &&
               !string.IsNullOrWhiteSpace(ProductCategory) &&
               ProductPrice > 0 && ProductStock >= 0;

        private async Task AddProductAsync()
        {
            if (!IsValidProductForm())
            {
                StatusMessage = "Completeaza numele, categoria, pretul si stocul produsului.";
                return;
            }
            IsLoading = true;
            try
            {
                var p = new Product
                {
                    Name = ProductName, Description = ProductDescription,
                    Price = ProductPrice, Stock = ProductStock,
                    Category = ProductCategory, ImageData = ProductImageData,
                    IsOnOffer = ProductIsOnOffer, DiscountPercentage = ProductDiscount
                };
                await _repo.CreateAsync(p);
                await LoadProductsAsync();
                ClearProductForm();
                StatusMessage = "✓ Produs adăugat!";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task UpdateProductAsync()
        {
            if (SelectedProduct == null) return;
            IsLoading = true;
            try
            {
                SelectedProduct.Name = ProductName; SelectedProduct.Description = ProductDescription;
                SelectedProduct.Price = ProductPrice; SelectedProduct.Stock = ProductStock;
                SelectedProduct.Category = ProductCategory; SelectedProduct.ImageData = ProductImageData;
                SelectedProduct.IsOnOffer = ProductIsOnOffer; SelectedProduct.DiscountPercentage = ProductDiscount;
                await _repo.UpdateAsync(SelectedProduct);
                await LoadProductsAsync();
                ClearProductForm();
                StatusMessage = "✓ Produs actualizat!";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null) return;
            IsLoading = true;
            try
            {
                await _repo.DeleteAsync(SelectedProduct.Id);
                await LoadProductsAsync();
                ClearProductForm();
                StatusMessage = "✓ Produs șters!";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        public void LoadProductFormFromSelection()
        {
            if (SelectedProduct == null) return;
            ProductName        = SelectedProduct.Name;
            ProductDescription = SelectedProduct.Description;
            ProductPrice       = SelectedProduct.Price;
            ProductStock       = SelectedProduct.Stock;
            ProductCategory    = SelectedProduct.Category;
            ProductImageData     = SelectedProduct.ImageData;
            ProductImageFileName = SelectedProduct.ImageData != null ? "imagine salvată" : string.Empty;
            ProductDiscount    = SelectedProduct.DiscountPercentage;
            ProductIsOnOffer   = SelectedProduct.IsOnOffer;
        }

        private void ClearProductForm()
        {
            ProductName = string.Empty; ProductDescription = string.Empty;
            ProductPrice = 0; ProductStock = 0; ProductCategory = string.Empty;
            ProductImageData = null; ProductImageFileName = string.Empty; ProductDiscount = 0; ProductIsOnOffer = false;
            SelectedProduct = null;
        }

        // ── USERS ─────────────────────────────────────────────────
        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            try
            {
                await using var db = DatabaseConfig.GetConnection();
                var users = (await db.QueryAsync<User>(
                    "SELECT * FROM Users WHERE IsActive = 1 ORDER BY CreatedAt DESC")).ToList();
                Users         = new ObservableCollection<User>(users);
                StatusMessage = $"{users.Count} utilizatori în baza de date";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private bool IsValidUserForm()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email)    ||
                string.IsNullOrWhiteSpace(SelectedRole))
                return false;

            // Parola e obligatorie doar la creare
            if (IsCreatingUser)
            {
                if (string.IsNullOrWhiteSpace(UserPassword))        return false;
                if (UserPassword != UserPasswordConfirm)             return false;
                if (UserPassword.Length < 6)                         return false;
            }
            return true;
        }

        private async Task AddUserAsync()
        {
            if (!IsValidUserForm())
            {
                StatusMessage = "Completeaza username, email si rolul utilizatorului.";
                return;
            }

            IsLoading = true;
            try
            {
                var result = await _auth.RegisterAsync(
                    Username.Trim(),
                    Email.Trim(),
                    UserPassword,
                    SelectedRole);

                if (!result.Ok)
                {
                    StatusMessage = result.Message;
                    return;
                }

                await LoadUsersAsync(); CloseUserForm();
                StatusMessage = $"✓ Utilizator '{Username.Trim()}' creat cu succes!";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task UpdateUserAsync()
        {
            if (SelectedUser == null) return;
            IsLoading = true;
            try
            {
                await using var db = DatabaseConfig.GetConnection();
                await db.ExecuteAsync(
                    "UPDATE Users SET Username=@Username,Email=@Email,Role=@Role WHERE Id=@Id",
                    new { Username=Username.Trim(), Email=Email.Trim(), Role=SelectedRole, Id=SelectedUser.Id });
                await LoadUsersAsync(); CloseUserForm();
                StatusMessage = "✓ Utilizator actualizat!";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;
            if (SelectedUser.Id == _auth.CurrentUserId)
            {
                StatusMessage = "Nu poti sterge utilizatorul cu care esti autentificat.";
                return;
            }

            IsLoading = true;
            try
            {
                await using var db = DatabaseConfig.GetConnection();
                await db.OpenAsync();
                await using var tx = await db.BeginTransactionAsync();

                var userId = SelectedUser.Id;
                var userEmail = SelectedUser.Email;
                var orderCount = await db.QuerySingleAsync<int>(
                    @"IF OBJECT_ID('dbo.Orders','U') IS NULL
                          SELECT 0;
                      ELSE
                          SELECT COUNT(1) FROM Orders WHERE Client_Id=@Id;",
                    new { Id = userId },
                    tx);

                if (orderCount > 0)
                {
                    await db.ExecuteAsync(@"
                        IF OBJECT_ID('dbo.OrderDetails','U') IS NOT NULL
                        BEGIN
                            DELETE od
                            FROM OrderDetails od
                            INNER JOIN Orders o ON o.OrderID = od.OrderID
                            WHERE o.Client_Id = @Id;
                        END",
                        new { Id = userId },
                        tx);

                    await db.ExecuteAsync(
                        @"IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
                              DELETE FROM Orders WHERE Client_Id=@Id;",
                        new { Id = userId },
                        tx);
                }

                await db.ExecuteAsync(
                    @"IF OBJECT_ID('dbo.UserDevices','U') IS NOT NULL
                          DELETE FROM UserDevices WHERE UserId=@Id;",
                    new { Id = userId },
                    tx);

                await db.ExecuteAsync(
                    @"IF OBJECT_ID('dbo.OtpCodes','U') IS NOT NULL
                          DELETE FROM OtpCodes WHERE Email=@Email;",
                    new { Email = userEmail },
                    tx);

                await db.ExecuteAsync(
                    "DELETE FROM Users WHERE Id=@Id",
                    new { Id = userId },
                    tx);

                await tx.CommitAsync();

                await LoadUsersAsync(); CloseUserForm();
                StatusMessage = orderCount > 0
                    ? $"Utilizator sters impreuna cu {orderCount} comenzi."
                    : "Utilizator sters.";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        public void LoadUserFormFromSelection()
        {
            if (SelectedUser == null) return;
            // Curăță PasswordBox-urile înainte de a popula formularul cu noul user
            UserFormClosed?.Invoke();
            Username = SelectedUser.Username; Email = SelectedUser.Email; SelectedRole = SelectedUser.Role;
            IsUserFormVisible = true;
            OnPropertyChanged(nameof(UserFormTitle));
        }

        private void OpenAddUserForm()
        {
            SelectedUser = null;
            Username = string.Empty;
            Email = string.Empty;
            SelectedRole = "Client";
            UserPassword = string.Empty;
            UserPasswordConfirm = string.Empty;
            IsUserFormVisible = true;
            OnPropertyChanged(nameof(UserFormTitle));
            // Curăță PasswordBox-urile din code-behind
            UserFormClosed?.Invoke();
        }

        private void CloseUserForm()
        {
            Username = string.Empty;
            Email = string.Empty;
            SelectedRole = "Client";
            UserPassword = string.Empty;
            UserPasswordConfirm = string.Empty;
            SelectedUser = null;
            IsUserFormVisible = false;
            OnPropertyChanged(nameof(UserFormTitle));
            UserFormClosed?.Invoke(); // Semnalăm code-behind-ul să golească PasswordBox-urile
        }

        private void ClearUserForm()
        {
            CloseUserForm();
        }
    }
}
