using System.Collections.ObjectModel;
using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class UsersViewModel : BaseViewModel
    {
        private ObservableCollection<User> _users = [];
        public ObservableCollection<User> Users
        {
            get => _users;
            set { SetProperty(ref _users, value); ApplyFilter(); }
        }

        private ObservableCollection<User> _filteredUsers = [];
        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        // Setul de roluri active — dacă e gol = toți
        private readonly HashSet<string> _activeFilters = [];

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Stare toggle per rol — OneWay, actualizat doar prin Command
        public bool FilterAdminActive    => _activeFilters.Contains("Administrator");
        public bool FilterEmployeeActive => _activeFilters.Contains("Employee");
        public bool FilterClientActive   => _activeFilters.Contains("Client");

        public RelayCommand FilterAllCommand      { get; }
        public RelayCommand FilterAdminCommand    { get; }
        public RelayCommand FilterEmployeeCommand { get; }
        public RelayCommand FilterClientCommand   { get; }

        public UsersViewModel()
        {
            FilterAllCommand = new RelayCommand(_ =>
            {
                _activeFilters.Clear();
                RefreshFilterStates();
                ApplyFilter();
            });

            FilterAdminCommand    = new RelayCommand(_ => ToggleFilter("Administrator"));
            FilterEmployeeCommand = new RelayCommand(_ => ToggleFilter("Employee"));
            FilterClientCommand   = new RelayCommand(_ => ToggleFilter("Client"));
        }

        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            StatusMessage = "Se încarcă...";
            try
            {
                using var db = DatabaseConfig.GetConnection();
                var users = await db.QueryAsync<User>(
                    "SELECT Id,Username,Email,Role,CreatedAt,LastLogin,IsActive FROM Users ORDER BY CreatedAt DESC");
                Users = new ObservableCollection<User>(users);
                StatusMessage = $"{FilteredUsers.Count} utilizatori găsiți";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private void ToggleFilter(string role)
        {
            if (_activeFilters.Contains(role))
                _activeFilters.Remove(role);
            else
                _activeFilters.Add(role);

            RefreshFilterStates();
            ApplyFilter();
        }


        private void ApplyFilter()
        {
            var filtered = _activeFilters.Count == 0
                ? _users
                : new ObservableCollection<User>(_users.Where(u => _activeFilters.Contains(u.Role)));

            FilteredUsers = filtered;
            StatusMessage = $"{FilteredUsers.Count} utilizatori găsiți";
        }

        private void RefreshFilterStates()
        {
            OnPropertyChanged(nameof(FilterAdminActive));
            OnPropertyChanged(nameof(FilterEmployeeActive));
            OnPropertyChanged(nameof(FilterClientActive));
        }

        public void Reset()
        {
            Users = [];
            FilteredUsers = [];
            StatusMessage = string.Empty;
            IsLoading = false;
            _activeFilters.Clear();
            RefreshFilterStates();
        }
    }
}
