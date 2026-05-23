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

        private string _currentFilter = "All";

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public RelayCommand FilterAllCommand      { get; }
        public RelayCommand FilterAdminCommand    { get; }
        public RelayCommand FilterEmployeeCommand { get; }
        public RelayCommand FilterClientCommand   { get; }

        public UsersViewModel()
        {
            FilterAllCommand      = new RelayCommand(_ => { _currentFilter = "All";           ApplyFilter(); });
            FilterAdminCommand    = new RelayCommand(_ => { _currentFilter = "Administrator"; ApplyFilter(); });
            FilterEmployeeCommand = new RelayCommand(_ => { _currentFilter = "Employee";      ApplyFilter(); });
            FilterClientCommand   = new RelayCommand(_ => { _currentFilter = "Client";        ApplyFilter(); });
        }

        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            StatusMessage = "Se încarcă...";
            try
            {
                using var db = DatabaseConfig.GetConnection();
                var users = await db.QueryAsync<User>(
                    "SELECT Id,Username,Role,CreatedAt,LastLogin,IsActive FROM Users ORDER BY CreatedAt DESC");
                Users = new ObservableCollection<User>(users);
                StatusMessage = $"{Users.Count} utilizatori găsiți";
            }
            catch (Exception ex) { StatusMessage = $"Eroare: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private void ApplyFilter()
        {
            var filtered = _currentFilter == "All"
                ? _users
                : new ObservableCollection<User>(_users.Where(u => u.Role == _currentFilter));
            FilteredUsers = filtered;
        }

        public void Reset()
        {
            Users = [];
            FilteredUsers = [];
            StatusMessage = string.Empty;
            IsLoading = false;
            _currentFilter = "All";
        }
    }
}
