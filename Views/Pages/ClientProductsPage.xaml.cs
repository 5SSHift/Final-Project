using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class ClientProductsPage : Page
    {
        private readonly ClientProductsViewModel _vm;
        private bool _isUpdatingCategories = false;

        // Aceasta este delegarea pe care o va apela MainViewModel
        public Action? OnViewCart { get; set; }

        public ClientProductsPage(ClientProductsViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;

            // Abonare la evenimentul din ViewModel
            _vm.CategoriesLoaded += OnCategoriesLoaded;

            Loaded += async (_, _) => await _vm.LoadProductsAsync();
        }

        private void OnCategoriesLoaded(object sender, List<string> categories)
        {
            _isUpdatingCategories = true;
            CategoriesPanel.Items.Clear();

            foreach (var categoryName in categories)
            {
                var chk = new CheckBox
                {
                    Content = categoryName,
                    Tag = categoryName,
                    Style = (Style)FindResource("CategoryTagFilter"),
                    IsChecked = _vm.SelectedCategories.Contains(categoryName)
                };

                chk.Checked += CategoryCheckBox_Changed;
                chk.Unchecked += CategoryCheckBox_Changed;

                CategoriesPanel.Items.Add(chk);
            }
            _isUpdatingCategories = false;
        }

        private void CategoryCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingCategories || _vm == null) return;

            if (sender is CheckBox clickedCheckBox && clickedCheckBox.Tag is string currentCategory)
            {
                _isUpdatingCategories = true;

                if (currentCategory == "Toate")
                {
                    if (clickedCheckBox.IsChecked == true)
                    {
                        foreach (CheckBox cb in CategoriesPanel.Items)
                            if (cb.Tag.ToString() != "Toate") cb.IsChecked = false;
                    }
                    else if (!_vm.SelectedCategories.Any(c => c != "Toate"))
                        clickedCheckBox.IsChecked = true;
                }
                else
                {
                    if (clickedCheckBox.IsChecked == true)
                    {
                        foreach (CheckBox cb in CategoriesPanel.Items)
                            if (cb.Tag.ToString() == "Toate") cb.IsChecked = false;
                    }
                }

                var currentSelection = CategoriesPanel.Items
                    .OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Tag.ToString())
                    .ToList();

                if (!currentSelection.Any())
                {
                    var allBtn = CategoriesPanel.Items.OfType<CheckBox>().FirstOrDefault(cb => cb.Tag.ToString() == "Toate");
                    if (allBtn != null)
                    {
                        allBtn.IsChecked = true;
                        currentSelection.Add("Toate");
                    }
                }

                _isUpdatingCategories = false;
                _vm.UpdateSelectedCategories(currentSelection);
            }
        }

        // Metoda corectată care declanșează acțiunea de navigare definită în MainViewModel
        private void ViewCart_Click(object sender, RoutedEventArgs e)
        {
            OnViewCart?.Invoke();
        }
    }
}