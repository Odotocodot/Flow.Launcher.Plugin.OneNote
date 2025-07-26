using System;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool>? canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke() != false;
        }
        
        public void Execute(object? parameter)
        {
            execute();
        }
    }

    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> execute;
        private readonly Predicate<T?>? canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        
        public bool CanExecute(T? parameter) => canExecute?.Invoke(parameter) != false;
        public void Execute(T? parameter) => execute(parameter);

        public bool CanExecute(object? parameter) => CanExecute((T?)parameter);
        public void Execute(object? parameter) => Execute((T?)parameter);
    }
}