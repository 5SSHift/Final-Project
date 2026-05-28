using System.Windows.Input;

namespace Wpf.ViewModels
{
    /// <summary>
    /// RelayCommand implementation for MVVM pattern.
    /// Allows binding commands in XAML to methods in ViewModel.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);
    }

    /// <summary>
    /// Generic RelayCommand implementation for MVVM pattern with typed parameters.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is null)
                return _canExecute?.Invoke(default) ?? true;
            
            return parameter is T typedParameter && (_canExecute?.Invoke(typedParameter) ?? true);
        }

        public void Execute(object? parameter)
        {
            if (parameter is null)
                _execute(default);
            else if (parameter is T typedParameter)
                _execute(typedParameter);
        }
    }
}
